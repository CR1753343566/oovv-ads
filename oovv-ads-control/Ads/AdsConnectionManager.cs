using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TwinCAT;
using TwinCAT.Ads;

namespace oovv_ads_control.Ads
{
    /// <summary>
    /// 封装 AdsSession/AdsConnection 的连接生命周期与三层事件监听，
    /// 让 UI 层（ViewModel）只需要订阅事件、不用直接操作 TwinCAT.Ads 的类型。
    /// </summary>
    public sealed class AdsConnectionManager : IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<AdsConnectionManager>();

        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(2);
        private const int HeartbeatFailureThreshold = 3;

        private CancellationTokenSource? _heartbeatCts;
        private int _consecutiveHeartbeatFailures;

        public AdsSession? Session { get; private set; }

        public AdsConnection? Connection { get; private set; }

        public bool IsConnected => Connection != null;

        /// <summary>会话级连接状态变化（长期连接、自动重连体现在这一层）。</summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? SessionStateChanged;

        /// <summary>连接级状态变化。</summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        /// <summary>ADS 路由器状态变化。</summary>
        public event EventHandler<AmsRouterNotificationEventArgs>? RouterStateChanged;

        /// <summary>PLC 运行状态（Run/Stop/Config...）变化。</summary>
        public event EventHandler<AdsStateChangedEventArgs>? AdsStateChanged;

        /// <summary>
        /// 连接被确认可用时触发（本地端口打开成功 + ReadState 验证目标真的在线之后），仅此一次。
        /// 依赖 ConnectionStateChanged 的话会漏掉"第一次连接成功"这个事件——因为 Connection.ConnectionStateChanged
        /// 是在 Session.Connect() 返回之后才订阅的，而 Connect() 内部很可能已经把状态从 None 变成 Connected 了，
        /// 订阅代码执行时这次状态变化已经发生过、错过了。需要"连上就要做点什么"的逻辑请订阅这个事件，不要用 ConnectionStateChanged。
        /// </summary>
        public event EventHandler? Connected;

        /// <summary>主动 Disconnect 时触发（不覆盖断线自动重连的 Lost 状态，那个走 ConnectionStateChanged）。</summary>
        public event EventHandler? Disconnected;

        public async Task ConnectAsync(AmsNetId netId, int port, SessionSettings settings, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
                throw new InvalidOperationException("已经处于连接状态，请先 Disconnect。");

            Logger.Information("开始连接 {NetId}:{Port}", netId, port);

            Session = new AdsSession(netId, port, settings);
            Session.ConnectionStateChanged += OnSessionStateChanged;

            Connection = (AdsConnection)Session.Connect();
            Connection.ConnectionStateChanged += OnConnectionStateChanged;
            Connection.RouterStateChanged += OnRouterStateChanged;

            // 注册需要一次 ADS 往返，连接建立后异步注册
            await Connection.RegisterAdsStateChangedAsync(OnAdsStateChanged, cancellationToken);

            // Connection.IsConnected 只代表"本地 ADS 端口打开成功"，不代表目标真的存在——
            // 官方文档原话："It does not indicate if the target port is available. Use the method
            // ReadState to determine if the target port is available."
            // 所以必须真正读一次目标状态，读不到就说明这个 AmsNetId/Port 背后根本没有东西在监听。
            var stateResult = await Connection.ReadStateAsync(cancellationToken);
            if (!stateResult.Succeeded)
            {
                Logger.Warning("连接 {NetId}:{Port} 失败：目标设备无响应（{ErrorCode}）", netId, port, stateResult.ErrorCode);
                Disconnect();
                throw new InvalidOperationException($"目标设备无响应（{stateResult.ErrorCode}）");
            }

            Logger.Information("连接 {NetId}:{Port} 成功", netId, port);
            Connected?.Invoke(this, EventArgs.Empty);
            StartHeartbeat();
        }

        /// <summary>
        /// 断开连接。清理顺序：先解绑事件，再释放 connection，最后释放 session，
        /// 避免"资源已释放但回调还在进来"的竞态。
        /// </summary>
        public void Disconnect()
        {
            StopHeartbeat();

            bool wasConnected = Connection != null;

            if (wasConnected)
                Logger.Information("断开连接 {Address}", Connection?.Address);

            if (Connection != null)
            {
                Connection.ConnectionStateChanged -= OnConnectionStateChanged;
                Connection.RouterStateChanged -= OnRouterStateChanged;

                if (!Connection.Disposed)
                    Connection.Dispose();

                Connection = null;
            }

            if (Session != null)
            {
                Session.ConnectionStateChanged -= OnSessionStateChanged;

                if (!Session.Disposed)
                    Session.Dispose();

                Session = null;
            }

            if (wasConnected)
                Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private void OnSessionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            Logger.Debug("Session 状态变化 {OldState} -> {NewState}（原因：{Reason}）", e.OldState, e.NewState, e.Reason);

            if (Connection == null && e.NewState == ConnectionState.Connected)
                Connection = (AdsConnection?)Session?.Connection;

            SessionStateChanged?.Invoke(this, e);
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            Logger.Information("Connection 状态变化 {OldState} -> {NewState}（原因：{Reason}）", e.OldState, e.NewState, e.Reason);
            ConnectionStateChanged?.Invoke(this, e);
        }

        private void OnRouterStateChanged(object? sender, AmsRouterNotificationEventArgs e)
        {
            Logger.Information("路由器状态变化 {State}", e.State);
            RouterStateChanged?.Invoke(this, e);
        }

        private void OnAdsStateChanged(object? sender, AdsStateChangedEventArgs e)
        {
            Logger.Information("PLC AdsState 变化 {AdsState}", e.State.AdsState);
            AdsStateChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 主动心跳：ConnectionStateChanged 这类事件都是被动的，依赖底层库自己判断出通信异常才会触发，
        /// 网线拔了但 TCP 还没超时、PLC 卡死但端口还开着这类"假死"场景可能很久都不会触发事件。
        /// 这里按固定节奏主动 ReadStateAsync 一次，连续失败到阈值就主动 Disconnect，
        /// 剩下的交给 ShellViewModel 的自动重连循环去处理，不在这里做重试。
        /// </summary>
        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatCts = new CancellationTokenSource();
            _ = RunHeartbeatAsync(_heartbeatCts.Token);
        }

        private void StopHeartbeat()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
            _consecutiveHeartbeatFailures = 0;
        }

        private async Task RunHeartbeatAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var timer = new PeriodicTimer(HeartbeatInterval);
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    var connection = Connection;
                    if (connection == null)
                        return;

                    try
                    {
                        var result = await connection.ReadStateAsync(cancellationToken);
                        if (result.Succeeded)
                        {
                            _consecutiveHeartbeatFailures = 0;
                        }
                        else
                        {
                            HandleHeartbeatFailure(result.ErrorCode.ToString());
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        HandleHeartbeatFailure(ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // StopHeartbeat 正常停止，不是异常情况
            }
        }

        private void HandleHeartbeatFailure(string reason)
        {
            _consecutiveHeartbeatFailures++;
            Logger.Warning("心跳失败（第 {Count} 次）：{Reason}", _consecutiveHeartbeatFailures, reason);

            if (_consecutiveHeartbeatFailures >= HeartbeatFailureThreshold)
            {
                Logger.Warning("心跳连续失败 {Count} 次，判定连接已丢失，主动断开", _consecutiveHeartbeatFailures);
                Disconnect();
            }
        }

        public void Dispose() => Disconnect();
    }
}
