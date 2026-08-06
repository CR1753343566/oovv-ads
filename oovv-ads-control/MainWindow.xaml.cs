using System;
using System.Windows;
using oovv_ads_control.ViewModels;

namespace oovv_ads_control
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ShellViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;

#if DEBUG
            // XAML 里定义的是最终部署要的 Kiosk 全屏（15寸触摸屏、无边框、不可缩放）。
            // 开发阶段用这个更方便：正常窗口、能拖动/缩放/切到 VS，只在 Debug 构建生效，
            // Release 构建（实际部署用的）不会走这段，还是走 XAML 里的 Kiosk 设置。
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            ResizeMode = ResizeMode.CanResize;
            Width = 1024;
            Height = 768;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
#endif
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
