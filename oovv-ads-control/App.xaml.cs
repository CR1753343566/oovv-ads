using System;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using oovv_ads_control.Logging;

namespace oovv_ads_control
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLogging.Initialize();

            // 未处理异常统一记到日志文件里再弹窗提示，而不是让程序直接静默崩掉——
            // 这是一个长期连着 PLC 的控制软件，崩溃比多一次弹窗提示更糟。
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DispatcherUnhandledException -= OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;

            AppLogging.Shutdown();
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "UI 线程未处理异常");
            MessageBox.Show($"发生未处理的异常，已记录到日志：\n{AppLogging.LogDirectory}\n\n{e.Exception.Message}",
                "未处理异常", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject as Exception, "非 UI 线程未处理异常（IsTerminating={IsTerminating}）", e.IsTerminating);

            if (e.IsTerminating)
                Log.CloseAndFlush();
        }
    }
}
