using oovv_ads_control.Ads;

namespace oovv_ads_control.ViewModels.Pages
{
    /// <summary>
    /// 示例页面：演示"页面 ViewModel 通过构造函数拿共享的 AdsConnectionManager"这一模式。
    /// 后续新增页面照这个样子加：新建 XxxViewModel + XxxView，在 ShellViewModel 里 Pages.Add(...)。
    /// </summary>
    public sealed class DashboardViewModel : ViewModelBase, IPageViewModel
    {
        private readonly AdsConnectionManager _connectionManager;

        public DashboardViewModel(AdsConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public string Title => "仪表盘";

        public bool IsConnected => _connectionManager.IsConnected;
    }
}
