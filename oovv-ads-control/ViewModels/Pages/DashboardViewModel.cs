using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TwinCAT.Ads;
using oovv_ads_control.Ads;
using oovv_ads_control.Models;
using oovv_ads_control.Models.ReadStatus;
using oovv_ads_control.Services;
using oovv_ads_control.Views.Dialogs;

namespace oovv_ads_control.ViewModels.Pages
{
    /// <summary>
    /// 示例页面：演示"页面 ViewModel 通过构造函数拿共享的 AdsConnectionManager/PlcNotificationService/PlcVariableService"
    /// 这一模式，订阅 PlcVariables.ReadStatus.BLVac3 的变化通知绑定展示三个字段，
    /// 并演示"显示 + 编辑 + 写回"的完整流程（Com_StripperEleMach.fEM_C_AbPos_Axis1）。
    /// 后续新增页面照这个样子加：新建 XxxViewModel + XxxView，在 ShellViewModel 里 Pages.Add(...)。
    /// </summary>
    public sealed class DashboardViewModel : ViewModelBase, IPageViewModel, IDisposable
    {
        // TODO: 这两个数字只是占位，还没有真实的物理限位范围——轴1绝对位置的允许范围需要你确认后改过来，
        // 现在这两个值不能拿去连真实机构测试写入。
        private const double AbPosAxis1Minimum = 0;
        private const double AbPosAxis1Maximum = 100;

        private readonly AdsConnectionManager _connectionManager;
        private readonly PlcVariableService _variableService;
        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private IDisposable? _blVac3Subscription;
        private IDisposable? _abPosAxis1Subscription;

        private bool _param026;
        private float _param027;
        private double _param028;
        private float _abPosAxis1;
        private string _editableText = string.Empty;

        public DashboardViewModel(AdsConnectionManager connectionManager, PlcNotificationService notificationService, PlcVariableService variableService)
        {
            _connectionManager = connectionManager;
            _variableService = variableService;

            // 符号路径只在 PlcVariables 里写一次，这里不再出现裸字符串
            _blVac3Subscription = notificationService.Subscribe(
                PlcVariables.ReadStatus.BLVac3.Path,
                Marshal.SizeOf<ReadBlVac3ValuesToPcStruct>(),
                new NotificationSettings(AdsTransMode.OnChange, 200, 0),
                OnBlVac3ValueChanged);

            _abPosAxis1Subscription = notificationService.Subscribe(
                PlcVariables.StripperEleMach.AbPosAxis1.Path,
                Marshal.SizeOf<float>(),
                new NotificationSettings(AdsTransMode.OnChange, 200, 0),
                OnAbPosAxis1ValueChanged);

            EditAbPosAxis1Command = new RelayCommand(async _ => await EditAbPosAxis1Async());
            EditTextCommand = new RelayCommand(_ => EditText());
        }

        public string Title => "仪表盘";

        public bool IsConnected => _connectionManager.IsConnected;

        public bool Param026
        {
            get => _param026;
            private set => SetField(ref _param026, value);
        }

        public float Param027
        {
            get => _param027;
            private set => SetField(ref _param027, value);
        }

        public double Param028
        {
            get => _param028;
            private set => SetField(ref _param028, value);
        }

        /// <summary>Com_StripperEleMach.fEM_C_AbPos_Axis1 的当前值，由通知实时推送更新。</summary>
        public float AbPosAxis1
        {
            get => _abPosAxis1;
            private set => SetField(ref _abPosAxis1, value);
        }

        public ICommand EditAbPosAxis1Command { get; }

        /// <summary>纯本地演示用，不连 PLC——用来验证 TextInputDialog/AlphanumericKeypad 这条链路。</summary>
        public string EditableText
        {
            get => _editableText;
            private set => SetField(ref _editableText, value);
        }

        public ICommand EditTextCommand { get; }

        /// <summary>
        /// 通知回调在 ADS 内部线程触发，不在 UI 线程——所以这里要 Dispatcher.Invoke 一下才能安全更新绑定属性。
        /// MemoryMarshal.Read 用的是 CLR 原生顺序布局（Pack/字段顺序生效，但 [MarshalAs] 在这里不起作用，
        /// 那个 attribute 只对 Marshal.SizeOf/ReadAny 这类走 interop 编组的路径有意义）。
        /// </summary>
        private void OnBlVac3ValueChanged(ReadOnlyMemory<byte> data)
        {
            var value = MemoryMarshal.Read<ReadBlVac3ValuesToPcStruct>(data.Span);

            _dispatcher.Invoke(() =>
            {
                Param026 = value.Param026;
                Param027 = value.Param027;
                Param028 = value.Param028;
            });
        }

        private void OnAbPosAxis1ValueChanged(ReadOnlyMemory<byte> data)
        {
            var value = MemoryMarshal.Read<float>(data.Span);
            _dispatcher.Invoke(() => AbPosAxis1 = value);
        }

        private async System.Threading.Tasks.Task EditAbPosAxis1Async()
        {
            // 用当前已经收到的最新推送值作为弹窗里的"实际值"和预填的"设定值"快照
            var current = AbPosAxis1;

            var newValue = NumericInputDialog.ShowDialog(
                Application.Current.MainWindow,
                parameterName: "StripperEleMach 轴1绝对位置",
                unit: "mm",
                actualValue: current,
                currentSetpoint: current,
                minimum: AbPosAxis1Minimum,
                maximum: AbPosAxis1Maximum);

            if (!newValue.HasValue)
                return;

            try
            {
                await _variableService.WriteAsync(PlcVariables.StripperEleMach.AbPosAxis1, (float)newValue.Value);
                AbPosAxis1 = (float)newValue.Value; // 乐观更新，真正的确认以后续推送的通知为准
            }
            catch (Exception ex)
            {
                MessageBox.Show($"写入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditText()
        {
            var newValue = TextInputDialog.ShowDialog(
                Application.Current.MainWindow,
                title: "编辑文本",
                currentValue: EditableText);

            if (newValue != null)
                EditableText = newValue;
        }

        public void Dispose()
        {
            _blVac3Subscription?.Dispose();
            _blVac3Subscription = null;

            _abPosAxis1Subscription?.Dispose();
            _abPosAxis1Subscription = null;
        }
    }
}
