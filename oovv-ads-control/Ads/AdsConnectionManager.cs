using System;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task ConnectAsync(AmsNetId netId, int port, SessionSettings settings, CancellationToken cancellationToken = default)
        {
            if (IsConnected)
                throw new InvalidOperationException("已经处于连接状态，请先 Disconnect。");

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
                Disconnect();
                throw new InvalidOperationException($"目标设备无响应（{stateResult.ErrorCode}）");
            }
        }

        /// <summary>
        /// 断开连接。清理顺序：先解绑事件，再释放 connection，最后释放 session，
        /// 避免"资源已释放但回调还在进来"的竞态。
        /// </summary>
        public void Disconnect()
        {
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
        }

        private void OnSessionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
        {
            if (Connection == null && e.NewState == ConnectionState.Connected)
                Connection = (AdsConnection?)Session?.Connection;

            SessionStateChanged?.Invoke(this, e);
        }

        private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
            => ConnectionStateChanged?.Invoke(this, e);

        private void OnRouterStateChanged(object? sender, AmsRouterNotificationEventArgs e)
            => RouterStateChanged?.Invoke(this, e);

        private void OnAdsStateChanged(object? sender, AdsStateChangedEventArgs e)
            => AdsStateChanged?.Invoke(this, e);

        public void Dispose() => Disconnect();
    }
}
