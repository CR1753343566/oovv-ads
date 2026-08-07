using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Serilog;
using TwinCAT;
using TwinCAT.Ads;
using oovv_ads_control.Ads;
using oovv_ads_control.Services;
using oovv_ads_control.ViewModels.Pages;

namespace oovv_ads_control.ViewModels
{
    /// <summary>
    /// 主窗口（壳）的 ViewModel：持有全局唯一的 AdsConnectionManager、常驻的连接/状态栏数据，
    /// 以及页面导航（Pages + CurrentPage）。业务页面通过构造函数拿到同一个 AdsConnectionManager，
    /// 不会各自创建连接。真正的弹窗功能不走这里的 Pages 导航，直接在需要的地方 new Window().ShowDialog()。
    /// </summary>
    public sealed class ShellViewModel : ViewModelBase, IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<ShellViewModel>();

        private readonly AdsConnectionManager _connectionManager = new();
        private readonly PlcNotificationService _notificationService;
        private readonly PlcVariableService _variableService;
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        //private string _netId = AmsNetId.Local.ToString();
        private string _netId = "10.241.133.46.1.1";
        private string _port = "851";
        private string _connectionStateText = ConnectionState.None.ToString();
        private string _adsStateText = AdsState.Invalid.ToString();
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private bool _isStartingUp = true;
        private IPageViewModel? _currentPage;
        private readonly ConnectingViewModel _connectingPage = new();

        private static readonly TimeSpan RampDuration = TimeSpan.FromSeconds(5);
        private const double RampHoldProgress = 95;
        private static readonly TimeSpan FinishDuration = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

        public ShellViewModel()
        {
            _notificationService = new PlcNotificationService(_connectionManager);
            _variableService = new PlcVariableService(_connectionManager);

            _connectionManager.SessionStateChanged += (_, e) =>
                _dispatcher.Invoke(() => HandleStateChanged($"Session: {e.OldState} -> {e.NewState}（原因：{e.Reason}）"));

            _connectionManager.ConnectionStateChanged += (_, e) =>
                _dispatcher.Invoke(() => HandleStateChanged($"Connection: {e.OldState} -> {e.NewState}（原因：{e.Reason}）"));

            _connectionManager.RouterStateChanged += (_, e) =>
                _dispatcher.Invoke(() => AddMessage($"Router: 状态变为 {e.State}"));

            _connectionManager.AdsStateChanged += (_, e) =>
                _dispatcher.Invoke(() =>
                {
                    AdsStateText = e.State.AdsState.ToString();
                    AddMessage($"PLC 状态变为 {e.State.AdsState}");
                });

            // 断线的唯一状态同步入口——不管是手动点"断开"、心跳检测失败主动断开，还是 ConnectAsync
            // 失败时的内部清理，都会经过 AdsConnectionManager.Disconnect()，从而统一触发这里，
            // 保证连接状态栏不会因为某条路径没同步而显示成过期的"已连接"。
            _connectionManager.Disconnected += (_, _) =>
                _dispatcher.Invoke(() =>
                {
                    ConnectionStateText = ConnectionState.None.ToString();
                    AdsStateText = AdsState.Invalid.ToString();
                    RaiseConnectionChanged();
                });

            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected && !IsBusy);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);

            // 新增页面：在这里 Pages.Add(new XxxViewModel(_connectionManager))，需要订阅变量变化的话
            // 再传一个 _notificationService，需要按需读写变量的话再传一个 _variableService；
            // 并在 App.xaml 里给 XxxViewModel 加一条 DataTemplate 指向 XxxView。
            Pages.Add(new DashboardViewModel(_connectionManager, _notificationService, _variableService));

            CurrentPage = _connectingPage;

            // 软件启动后自动连接 PLC：全屏显示 5 秒一轮的加载页，连上就进首页，连不上就一直重试
            _ = StartupConnectAsync();
        }

        public string NetId
        {
            get => _netId;
            set => SetField(ref _netId, value);
        }

        public string Port
        {
            get => _port;
            set => SetField(ref _port, value);
        }

        public string ConnectionStateText
        {
            get => _connectionStateText;
            private set => SetField(ref _connectionStateText, value);
        }

        public string AdsStateText
        {
            get => _adsStateText;
            private set => SetField(ref _adsStateText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetField(ref _statusMessage, value);
        }

        public bool IsConnected => _connectionManager.IsConnected;

        /// <summary>系统级 PLC 变量订阅服务，页面需要监听变量变化时通过构造函数拿这同一个实例。</summary>
        public PlcNotificationService NotificationService => _notificationService;

        /// <summary>系统级 PLC 变量按需读写服务，页面需要读写变量时通过构造函数拿这同一个实例。</summary>
        public PlcVariableService VariableService => _variableService;

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetField(ref _isBusy, value);
        }

        /// <summary>
        /// 仅代表"启动阶段是否已经走完一次成功连接"，一旦第一次连接成功就永久变 false，
        /// 之后即使中途断线也不会再回到全屏加载页——那种情况走正常的连接栏手动重连。
        /// </summary>
        public bool IsStartingUp
        {
            get => _isStartingUp;
            private set => SetField(ref _isStartingUp, value);
        }

        public ObservableCollection<string> Messages { get; } = new();

        public ObservableCollection<IPageViewModel> Pages { get; } = new();

        public IPageViewModel? CurrentPage
        {
            get => _currentPage;
            set => SetField(ref _currentPage, value);
        }

        public ICommand ConnectCommand { get; }

        public ICommand DisconnectCommand { get; }

        /// <summary>
        /// 启动时的自动连接流程：进度条只爬升一次——RampDuration 内从 0 爬到 RampHoldProgress（90%），
        /// 如果这期间还没连上，就停在 90% 原地等待，后台继续重试；不管是爬升途中还是停留等待时连上，
        /// 都会立刻从当前进度快速冲到 100%（FinishDuration），再进首页——不会出现"瞬间跳转无感知"。
        /// </summary>
        private async System.Threading.Tasks.Task StartupConnectAsync()
        {
            CurrentPage = _connectingPage;
            _connectingPage.StatusText = "正在连接 PLC...";

#if DEBUG
            // 开发阶段不卡在这里等连接成功，直接进首页；后台继续尝试连接，真接上 PLC 之后
            // PlcNotificationService/PlcVariableService 会自动补上（它们本来就是"没连接也能先注册，
            // 连上了自动补"的设计），页面数据会自己从默认值刷新成真实值。
            _ = ConnectAsync();
            CurrentPage = Pages.Count > 0 ? Pages[0] : null;
            IsStartingUp = false;
#else
            // Release 构建：必须真正连接成功才能进首页，行为不变
            using var rampCts = new CancellationTokenSource();
            var rampTask = _connectingPage.RunRampAsync(RampDuration, RampHoldProgress, rampCts.Token);

            while (!IsConnected)
            {
                await ConnectAsync();

                if (IsConnected)
                    break;

                _connectingPage.StatusText = $"连接失败，正在重试...（{StatusMessage}）";
                await Task.Delay(RetryDelay);
            }

            rampCts.Cancel();
            await rampTask; // 等爬升动画彻底停下来，避免和下面的冲刺动画同时改 Progress

            _connectingPage.StatusText = "连接成功，正在进入首页...";
            await _connectingPage.RunFinishAsync(FinishDuration);

            CurrentPage = Pages.Count > 0 ? Pages[0] : null;
            IsStartingUp = false;
#endif
        }

        /// <summary>
        /// 实际发起 ADS 连接。被启动流程和顶部连接栏的手动 ConnectCommand 共用，
        /// 手动重连时不会跳走当前页面，只更新连接栏/状态栏。
        /// </summary>
        private async System.Threading.Tasks.Task ConnectAsync()
        {
            IsBusy = true;
            try
            {
                var netId = AmsNetId.Parse(NetId);
                var port = int.Parse(Port);

                await _connectionManager.ConnectAsync(netId, port, SessionSettings.Default);

                ConnectionStateText = _connectionManager.Connection?.State.ToString() ?? ConnectionState.None.ToString();
                AddMessage($"已连接到 {netId}:{port}");
            }
            catch (Exception ex)
            {
                AddMessage($"连接失败：{ex.Message}");
                _connectionManager.Disconnect();
            }
            finally
            {
                IsBusy = false;
                RaiseConnectionChanged();
            }
        }

        private void Disconnect()
        {
            // 状态栏刷新统一交给上面的 Disconnected 事件处理，这里只需要发起断开 + 记一条用户可读的消息
            _connectionManager.Disconnect();
            AddMessage("已断开连接");
        }

        private void HandleStateChanged(string message)
        {
            ConnectionStateText = _connectionManager.Connection?.State.ToString() ?? ConnectionState.None.ToString();
            AddMessage(message);
            RaiseConnectionChanged();
        }

        private void RaiseConnectionChanged()
        {
            OnPropertyChanged(nameof(IsConnected));
            CommandManager.InvalidateRequerySuggested();
        }

        private void AddMessage(string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss} {message}";
            Messages.Add(line);
            StatusMessage = message;
            Logger.Information("{Message}", message);
        }

        public void Dispose()
        {
            foreach (var page in Pages)
            {
                if (page is IDisposable disposablePage)
                    disposablePage.Dispose();
            }

            _notificationService.Dispose();
            _variableService.Dispose();
            _connectionManager.Dispose();
        }
    }
}
