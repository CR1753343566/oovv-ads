using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TwinCAT.Ads;
using oovv_ads_control.Ads;
using TwinCAT;

namespace oovv_ads_control.Services
{
    /// <summary>
    /// 系统级 PLC 变量订阅服务：在 AdsConnectionManager 之上维护一份"订阅登记表"。
    /// 单靠 AddDeviceNotification 一次性注册是不够的——断线重连后旧的通知句柄会失效，
    /// PLC 端也不会再推送，如果不重新注册，订阅者会在完全无感知的情况下"哑火"。
    /// 这里的做法：Subscribe 时把订阅信息登记下来（不仅仅是拿到一个句柄），
    /// 每次连接变为 Connected 时，把登记表里所有订阅重新 AddDeviceNotification 一遍；
    /// 连接一旦不是 Connected，就把缓存的句柄清空，避免拿着已经失效的句柄去 Delete。
    /// </summary>
    public sealed class PlcNotificationService : IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<PlcNotificationService>();

        private sealed class Subscription
        {
            public required string VariableName { get; init; }
            public required int Size { get; init; }
            public required NotificationSettings Settings { get; init; }
            public required Action<ReadOnlyMemory<byte>> OnValueChanged { get; init; }
            public uint? Handle { get; set; }
        }

        private readonly AdsConnectionManager _connectionManager;
        private readonly object _gate = new();
        private readonly Dictionary<Guid, Subscription> _subscriptions = new();
        private readonly Dictionary<uint, Guid> _handleToSubscriptionId = new();
        private AdsConnection? _attachedConnection;

        public PlcNotificationService(AdsConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;

            // Connected/Disconnected 是 AdsConnectionManager 主动、可靠触发的信号，用来做"连上了就重新注册一遍"；
            // ConnectionStateChanged 只用来处理断线自动重连（同一个 Connection 实例的 Lost -> Connected），
            // 它订阅的时间点在第一次连接成功之后，天然会错过"第一次连接成功"这个事件，不能只靠它。
            _connectionManager.Connected += OnConnected;
            _connectionManager.Disconnected += OnDisconnected;
            _connectionManager.ConnectionStateChanged += OnConnectionStateChanged;

            if (_connectionManager.IsConnected)
                AttachToCurrentConnection();
        }

        /// <summary>
        /// 订阅一个 PLC 变量的变化通知（原始字节，marshal 方式和 BaseSamples 的 EventReading 一致）。
        /// 回调在 ADS 内部线程触发，不在 UI 线程——需要更新界面的话调用方自己 Dispatcher.Invoke。
        /// 当前没连接也可以先订阅，登记表会在连接建立后自动补注册。
        /// 返回的 IDisposable 用于取消订阅，Dispose 时会调用 DeleteDeviceNotification 释放 PLC 端资源。
        /// </summary>
        public IDisposable Subscribe(string variableName, int size, NotificationSettings settings, Action<ReadOnlyMemory<byte>> onValueChanged)
        {
            var id = Guid.NewGuid();
            var subscription = new Subscription
            {
                VariableName = variableName,
                Size = size,
                Settings = settings,
                OnValueChanged = onValueChanged,
            };

            lock (_gate)
            {
                _subscriptions[id] = subscription;
            }

            Logger.Debug("Subscribe 登记 {VariableName}（size={Size}），当前 IsConnected={IsConnected}",
                variableName, size, _connectionManager.IsConnected);

            if (_connectionManager.IsConnected)
                _ = RegisterAsync(id, subscription);

            return new Unsubscriber(this, id);
        }

        private async Task RegisterAsync(Guid id, Subscription subscription)
        {
            var connection = _connectionManager.Connection;
            if (connection == null)
            {
                Logger.Debug("RegisterAsync 跳过 {VariableName}：Connection 为 null", subscription.VariableName);
                return;
            }

            try
            {
                var result = await connection.AddDeviceNotificationAsync(
                    subscription.VariableName, subscription.Size, subscription.Settings, null, CancellationToken.None);

                if (!result.Succeeded)
                {
                    Logger.Warning("AddDeviceNotificationAsync 失败：{VariableName} -> {ErrorCode}（符号路径大概率不对，或者类型/size 对不上）",
                        subscription.VariableName, result.ErrorCode);
                    return;
                }

                lock (_gate)
                {
                    // 等注册结果的这段时间里订阅可能已经被取消了，这里要再确认一次还在不在登记表里
                    if (!_subscriptions.ContainsKey(id))
                    {
                        _ = connection.DeleteDeviceNotificationAsync(result.Handle, CancellationToken.None);
                        return;
                    }

                    subscription.Handle = result.Handle;
                    _handleToSubscriptionId[result.Handle] = id;
                }

                Logger.Information("AddDeviceNotificationAsync 成功：{VariableName} -> handle={Handle}",
                    subscription.VariableName, result.Handle);
            }
            catch (Exception ex)
            {
                // 注册失败（变量名不存在、连接又断了……）：不重试，等下一次连接变为 Connected 再补注册一次
                Logger.Warning(ex, "RegisterAsync 抛异常：{VariableName}", subscription.VariableName);
            }
        }

        private void Unsubscribe(Guid id)
        {
            Subscription? subscription;

            lock (_gate)
            {
                if (!_subscriptions.Remove(id, out subscription))
                    return;

                if (subscription.Handle.HasValue)
                    _handleToSubscriptionId.Remove(subscription.Handle.Value);
            }

            if (subscription.Handle.HasValue && _connectionManager.Connection is { } connection)
                _ = connection.DeleteDeviceNotificationAsync(subscription.Handle.Value, CancellationToken.None);
        }

        private void OnConnected(object? sender, EventArgs e) => HandleConnected();

        private void OnDisconnected(object? sender, EventArgs e) => HandleDisconnected();

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            // 断线自动重连（同一个 Connection 实例）走这里；第一次连接成功走上面的 Connected 事件。
            if (e.NewState == ConnectionState.Connected)
                HandleConnected();
            else
                HandleDisconnected();
        }

        private void HandleConnected()
        {
            Logger.Information("连接已确认可用，重新注册所有订阅");
            AttachToCurrentConnection();
            _ = ReregisterAllAsync();
        }

        private void HandleDisconnected()
        {
            // 连接不再可用：旧句柄大概率已经失效，清空缓存，避免之后拿过期句柄去 Delete
            Logger.Information("连接已断开，清空句柄缓存");
            lock (_gate)
            {
                _handleToSubscriptionId.Clear();
                foreach (var subscription in _subscriptions.Values)
                    subscription.Handle = null;
            }
        }

        /// <summary>
        /// AdsConnection 在断线自动重连（Resurrect）时是同一个实例，但手动断开重连会创建新实例——
        /// 用引用比较确保 AdsNotification 事件订阅始终挂在"当前真正在用"的那个 Connection 上。
        /// </summary>
        private void AttachToCurrentConnection()
        {
            var connection = _connectionManager.Connection;
            if (ReferenceEquals(connection, _attachedConnection))
                return;

            if (_attachedConnection != null)
                _attachedConnection.AdsNotification -= OnAdsNotification;

            if (connection != null)
                connection.AdsNotification += OnAdsNotification;

            _attachedConnection = connection;
            Logger.Debug("AdsNotification 事件已挂到{Target}", connection == null ? "（无连接）" : "当前 Connection 上");
        }

        private async Task ReregisterAllAsync()
        {
            List<(Guid Id, Subscription Subscription)> snapshot;
            lock (_gate)
            {
                snapshot = new List<(Guid, Subscription)>(_subscriptions.Count);
                foreach (var kv in _subscriptions)
                    snapshot.Add((kv.Key, kv.Value));
            }

            foreach (var (id, subscription) in snapshot)
                await RegisterAsync(id, subscription);
        }

        private void OnAdsNotification(object? sender, AdsNotificationEventArgs e)
        {
            Action<ReadOnlyMemory<byte>>? callback = null;

            lock (_gate)
            {
                if (_handleToSubscriptionId.TryGetValue(e.Handle, out var id) &&
                    _subscriptions.TryGetValue(id, out var subscription))
                {
                    callback = subscription.OnValueChanged;
                }
            }

            Logger.Debug("收到 AdsNotification：handle={Handle}，{Bytes} 字节，{Matched}",
                e.Handle, e.Data.Length, callback == null ? "找不到对应订阅（handle 没对上）" : "匹配到订阅，回调中");

            callback?.Invoke(e.Data);
        }

        public void Dispose()
        {
            _connectionManager.Connected -= OnConnected;
            _connectionManager.Disconnected -= OnDisconnected;
            _connectionManager.ConnectionStateChanged -= OnConnectionStateChanged;

            if (_attachedConnection != null)
                _attachedConnection.AdsNotification -= OnAdsNotification;

            List<Guid> ids;
            lock (_gate)
            {
                ids = new List<Guid>(_subscriptions.Keys);
            }

            foreach (var id in ids)
                Unsubscribe(id);
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly PlcNotificationService _service;
            private readonly Guid _id;
            private bool _disposed;

            public Unsubscriber(PlcNotificationService service, Guid id)
            {
                _service = service;
                _id = id;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _service.Unsubscribe(_id);
            }
        }
    }
}
