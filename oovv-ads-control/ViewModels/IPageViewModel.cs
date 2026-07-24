namespace oovv_ads_control.ViewModels
{
    /// <summary>
    /// 所有"页面"（在 Shell 中间内容区切换的 ViewModel）都实现这个接口，
    /// 供导航列表显示标题、供 App.xaml 的 DataTemplate 按类型匹配对应 View。
    /// </summary>
    public interface IPageViewModel
    {
        string Title { get; }
    }
}
