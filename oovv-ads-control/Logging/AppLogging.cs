using System;
using System.IO;
using Serilog;

namespace oovv_ads_control.Logging
{
    /// <summary>
    /// 全局日志初始化入口。日志落在 %LocalAppData%\oovv-ads-control\logs\ 下，按天滚动、保留最近 14 天，
    /// 同时也写到 Debug 输出（VS 的"输出"窗口）方便开发时看。
    /// 用 Serilog 的静态 Log.Logger 作为整个项目的日志入口——项目目前没有 DI 容器，
    /// 各个类里用 `Log.ForContext&lt;T&gt;()` 拿一个带 SourceContext 的 logger 就行，不用到处传参数。
    /// </summary>
    public static class AppLogging
    {
        public static string LogDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "oovv-ads-control", "logs");

        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        public static void Initialize()
        {
            Directory.CreateDirectory(LogDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Debug(outputTemplate: OutputTemplate)
                .WriteTo.File(
                    Path.Combine(LogDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: OutputTemplate)
                .CreateLogger();

            Log.Information("===== 应用启动，日志目录：{LogDirectory} =====", LogDirectory);
        }

        /// <summary>
        /// File sink 是缓冲写入的，退出前必须 flush，否则最后一批日志可能丢失。
        /// </summary>
        public static void Shutdown()
        {
            Log.Information("===== 应用退出 =====");
            Log.CloseAndFlush();
        }
    }
}
