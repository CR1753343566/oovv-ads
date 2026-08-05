using System;
using System.Threading;
using System.Threading.Tasks;

namespace oovv_ads_control.ViewModels.Pages
{
    /// <summary>
    /// 启动时的连接加载页：进度条先在 RampDuration 内爬升到 HoldProgress 并停住等待，
    /// 后台连接成功的那一刻由 ShellViewModel 调用 RunFinishAsync 快速冲到 100%，再进首页。
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
        /// 在 duration 时间内把 Progress 从 0 匀速爬升到 targetProgress，到达后就停在那里不动。
        /// 如果连接提前成功、cancellationToken 被取消，爬升会在当前进度停下（安静结束，不抛异常）。
        /// </summary>
        public async Task RunRampAsync(TimeSpan duration, double targetProgress, CancellationToken cancellationToken)
        {
            Progress = 0;
            const int stepMs = 50;
            int steps = Math.Max(1, (int)(duration.TotalMilliseconds / stepMs));

            try
            {
                for (int i = 1; i <= steps; i++)
                {
                    await Task.Delay(stepMs, cancellationToken);
                    Progress = i * targetProgress / steps;
                }
            }
            catch (OperationCanceledException)
            {
                // 连接已经成功，爬升被主动叫停，属于正常情况
            }
        }

        /// <summary>
        /// 连接成功后调用：把 Progress 从当前值快速冲到 100（不管当时停在爬升途中还是停留在 90%）。
        /// </summary>
        public async Task RunFinishAsync(TimeSpan duration)
        {
            double start = Progress;
            const int stepMs = 20;
            int steps = Math.Max(1, (int)(duration.TotalMilliseconds / stepMs));

            for (int i = 1; i <= steps; i++)
            {
                await Task.Delay(stepMs);
                Progress = start + (100 - start) * i / steps;
            }

            Progress = 100;
        }
    }
}
