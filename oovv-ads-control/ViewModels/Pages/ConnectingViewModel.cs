using System;
using System.Threading;
using System.Threading.Tasks;

namespace oovv_ads_control.ViewModels.Pages
{
    /// <summary>
    /// 启动时的连接加载页：一个 5 秒一轮的进度条，配合 ShellViewModel 的自动重连循环——
    /// 5 秒内连上就切首页，5 秒内没连上就重新走一轮，直到连接成功。
    /// </summary>
    public sealed class ConnectingViewModel : ViewModelBase, IPageViewModel
    {
        private double _progress;
        private string _statusText = "正在连接 PLC...";

        public string Title => "连接中";

        public double Progress
        {
            get => _progress;
            private set => SetField(ref _progress, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        /// <summary>
        /// 在 duration 时间内把 Progress 从 0 匀速推到 100；
        /// 如果外部在期间连接成功并取消 cancellationToken，动画会提前安静结束。
        /// </summary>
        public async Task AnimateProgressAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            Progress = 0;
            const int stepMs = 50;
            int steps = Math.Max(1, (int)(duration.TotalMilliseconds / stepMs));

            try
            {
                for (int i = 1; i <= steps; i++)
                {
                    await Task.Delay(stepMs, cancellationToken);
                    Progress = i * 100.0 / steps;
                }
            }
            catch (OperationCanceledException)
            {
                // 连接已提前成功，动画被主动取消，属于正常情况
            }
        }
    }
}
