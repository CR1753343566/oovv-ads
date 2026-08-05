using System;
using System.Globalization;
using System.Windows.Input;

namespace oovv_ads_control.ViewModels.Dialogs
{
    /// <summary>
    /// 数值输入弹窗的校验逻辑。不碰 ADS——只负责"展示实际值快照 + 收集一个通过校验的新设定值"，
    /// 调用方从 Result 里拿到值之后自己决定要不要、怎么写回 PLC（比如 PlcVariableService.WriteAsync）。
    /// </summary>
    public sealed class NumericInputDialogViewModel : ViewModelBase
    {
        private readonly double _minimum;
        private readonly double _maximum;
        private readonly string _rangeValueText;

        private string _inputText;
        private bool _isValid;
        private string _validationMessage = string.Empty;

        public NumericInputDialogViewModel(string parameterName, string unit, double actualValue,
            double currentSetpoint, double minimum, double maximum)
        {
            ParameterName = parameterName;
            Unit = unit;
            _minimum = minimum;
            _maximum = maximum;

            // 实际值只是弹窗打开这一刻的快照，弹窗打开期间不会跟着 PLC 刷新
            ActualValueText = actualValue.ToString("0.###", CultureInfo.InvariantCulture);
            _rangeValueText = $"{minimum.ToString("0.###", CultureInfo.InvariantCulture)} ~ {maximum.ToString("0.###", CultureInfo.InvariantCulture)}";
            RangeText = $"允许范围：{_rangeValueText}";

            _inputText = currentSetpoint.ToString("0.###", CultureInfo.InvariantCulture);

            AppendDigitCommand = new RelayCommand(p => AppendChar((string)p!));
            ToggleSignCommand = new RelayCommand(_ => ToggleSign());
            BackspaceCommand = new RelayCommand(_ => Backspace());
            ClearCommand = new RelayCommand(_ => Clear());
            SaveCommand = new RelayCommand(_ => Save(), _ => IsValid);
            CancelCommand = new RelayCommand(_ => Cancel());

            Validate();
        }

        public string ParameterName { get; }

        public string Unit { get; }

        public string ActualValueText { get; }

        public string RangeText { get; }

        public string InputText
        {
            get => _inputText;
            private set => SetField(ref _inputText, value);
        }

        public bool IsValid
        {
            get => _isValid;
            private set => SetField(ref _isValid, value);
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            private set => SetField(ref _validationMessage, value);
        }

        /// <summary>Save 时写入；Cancel 或直接关闭窗口时保持 null。</summary>
        public double? Result { get; private set; }

        /// <summary>Save/Cancel 都通过这个事件通知外层 Window 关闭自己。</summary>
        public event EventHandler? RequestClose;

        public ICommand AppendDigitCommand { get; }
        public ICommand ToggleSignCommand { get; }
        public ICommand BackspaceCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private void AppendChar(string ch)
        {
            if (ch == "." && InputText.Contains('.'))
                return; // 只允许一个小数点

            InputText += ch;
            Validate();
        }

        private void ToggleSign()
        {
            InputText = InputText.StartsWith('-') ? InputText[1..] : "-" + InputText;
            Validate();
        }

        private void Backspace()
        {
            if (InputText.Length > 0)
                InputText = InputText[..^1];

            Validate();
        }

        private void Clear()
        {
            InputText = string.Empty;
            Validate();
        }

        private void Validate()
        {
            if (!double.TryParse(InputText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                IsValid = false;
                ValidationMessage = "请输入有效数值";
            }
            else if (value < _minimum || value > _maximum)
            {
                IsValid = false;
                ValidationMessage = $"超出允许范围（{_rangeValueText}）";
            }
            else
            {
                IsValid = true;
                ValidationMessage = string.Empty;
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void Save()
        {
            if (!IsValid)
                return;

            Result = double.Parse(InputText, NumberStyles.Float, CultureInfo.InvariantCulture);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
