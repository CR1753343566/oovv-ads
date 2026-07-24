using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using TwinCAT;
using TwinCAT.Ads;
using oovv_ads_control.Ads;
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
        private readonly AdsConnectionManager _connectionManager = new();
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private string _netId = AmsNetId.Local.ToString();
        private string _port = "851";
        private string _connectionStateText = ConnectionState.None.ToString();
        private string _adsStateText = AdsState.Invalid.ToString();
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private bool _isStartingUp = true;
        private IPageViewModel? _currentPage;
        private readonly ConnectingViewModel _connectingPage = new();

        private static readonly TimeSpan ConnectCycleDuration = TimeSpan.FromSeconds(5);

        public ShellViewModel()
        {
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

            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected && !IsBusy);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);

            // 新增页面：在这里 Pages.Add(new XxxViewModel(_connectionManager))，
            // 并在 App.xaml 里给 XxxViewModel 加一条 DataTemplate 指向 XxxView。
            Pages.Add(new DashboardViewModel(_connectionManager));

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
        /// 启动时的自动连接流程：每一轮用 5 秒进度条动画。
        /// 5 秒内连上 -> 立刻取消动画、进首页；5 秒内没连上 -> 补齐这一轮动画后重新再来一轮，直到连接成功。
        /// </summary>
        private async System.Threading.Tasks.Task StartupConnectAsync()
        {
            CurrentPage = _connectingPage;

            while (!IsConnected)
            {
                _connectingPage.StatusText = "正在连接 PLC...";

                using var cts = new CancellationTokenSource();
                var progressTask = _connectingPage.AnimateProgressAsync(ConnectCycleDuration, cts.Token);

                await ConnectAsync();

                if (IsConnected)
                {
                    cts.Cancel();
                    break;
                }

                _connectingPage.StatusText = $"连接失败，正在重试...（{StatusMessage}）";
                await progressTask; // 补齐这一轮的 5 秒节奏，避免连接瞬间失败时疯狂空转重试
            }

            CurrentPage = Pages.Count > 0 ? Pages[0] : null;
            IsStartingUp = false;
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
            _connectionManager.Disconnect();
            ConnectionStateText = ConnectionState.None.ToString();
            AdsStateText = AdsState.Invalid.ToString();
            AddMessage("已断开连接");
            RaiseConnectionChanged();
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
            System.Diagnostics.Debug.WriteLine(line);
        }

        public void Dispose() => _connectionManager.Dispose();
    }
}
