using System;
using System.Windows.Input;

namespace oovv_ads_control.ViewModels.Dialogs
{
    /// <summary>
    /// 字母数字键盘弹窗的编辑逻辑：维护 Text + 光标位置(CursorPosition) + Shift/Caps/插入覆盖 三个状态。
    /// 完全不关心具体键盘长什么样（那是 AlphanumericKeypad 的事），只暴露"插入一个字符/退格/删除/
    /// 左右移动/Home/End"这些跟具体按键布局无关的通用文本编辑操作，方便单独测试。
    /// </summary>
    public sealed class TextInputDialogViewModel : ViewModelBase
    {
        private readonly int? _maxLength;

        private string _text;
        private int _cursorPosition;
        private bool _isShiftActive;
        private bool _isCapsLockActive;
        private bool _isOverwriteMode;

        public TextInputDialogViewModel(string title, string currentValue, int? maxLength = null)
        {
            Title = title;
            _maxLength = maxLength;
            _text = currentValue ?? string.Empty;
            _cursorPosition = _text.Length;

            InsertCharCommand = new RelayCommand(p => InsertChar((string)p!));
            BackspaceCommand = new RelayCommand(_ => Backspace());
            DeleteCommand = new RelayCommand(_ => Delete());
            MoveLeftCommand = new RelayCommand(_ => MoveLeft());
            MoveRightCommand = new RelayCommand(_ => MoveRight());
            HomeCommand = new RelayCommand(_ => MoveHome());
            EndCommand = new RelayCommand(_ => MoveEnd());
            ToggleShiftCommand = new RelayCommand(_ => IsShiftActive = !IsShiftActive);
            ToggleCapsCommand = new RelayCommand(_ => IsCapsLockActive = !IsCapsLockActive);
            ToggleInsertCommand = new RelayCommand(_ => IsOverwriteMode = !IsOverwriteMode);
            ClearCommand = new RelayCommand(_ => Clear());
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public string Title { get; }

        public string Text
        {
            get => _text;
            private set => SetField(ref _text, value);
        }

        public int CursorPosition
        {
            get => _cursorPosition;
            private set => SetField(ref _cursorPosition, value);
        }

        /// <summary>触屏惯例：敲一个字符就自动回落（不用像物理键盘那样一直按住），跟真正锁定的 Caps 不一样。</summary>
        public bool IsShiftActive
        {
            get => _isShiftActive;
            private set => SetField(ref _isShiftActive, value);
        }

        public bool IsCapsLockActive
        {
            get => _isCapsLockActive;
            private set => SetField(ref _isCapsLockActive, value);
        }

        public bool IsOverwriteMode
        {
            get => _isOverwriteMode;
            private set => SetField(ref _isOverwriteMode, value);
        }

        public string? Result { get; private set; }

        public event EventHandler? RequestClose;

        public ICommand InsertCharCommand { get; }
        public ICommand BackspaceCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand MoveLeftCommand { get; }
        public ICommand MoveRightCommand { get; }
        public ICommand HomeCommand { get; }
        public ICommand EndCommand { get; }
        public ICommand ToggleShiftCommand { get; }
        public ICommand ToggleCapsCommand { get; }
        public ICommand ToggleInsertCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>
        /// AlphanumericKeypad 用这个算某个键当前该显示/插入什么字符：
        /// 字母的大小写看 Caps XOR Shift；符号键只看 Shift（真实键盘上 Caps 不影响符号键）。
        /// </summary>
        public string ResolveLabel(string normal, string? shifted, bool isLetter)
        {
            if (isLetter)
                return (IsCapsLockActive ^ IsShiftActive) ? normal.ToUpperInvariant() : normal.ToLowerInvariant();

            return IsShiftActive && shifted != null ? shifted : normal;
        }

        private void InsertChar(string ch)
        {
            bool willOverwrite = IsOverwriteMode && CursorPosition < Text.Length;

            if (_maxLength.HasValue && Text.Length >= _maxLength.Value && !willOverwrite)
                return;

            Text = willOverwrite
                ? Text[..CursorPosition] + ch + Text[(CursorPosition + 1)..]
                : Text[..CursorPosition] + ch + Text[CursorPosition..];

            CursorPosition++;

            if (IsShiftActive)
                IsShiftActive = false;
        }

        private void Backspace()
        {
            if (CursorPosition == 0)
                return;

            Text = Text[..(CursorPosition - 1)] + Text[CursorPosition..];
            CursorPosition--;
        }

        private void Delete()
        {
            if (CursorPosition >= Text.Length)
                return;

            Text = Text[..CursorPosition] + Text[(CursorPosition + 1)..];
        }

        private void MoveLeft() => CursorPosition = Math.Max(0, CursorPosition - 1);

        private void MoveRight() => CursorPosition = Math.Min(Text.Length, CursorPosition + 1);

        private void MoveHome() => CursorPosition = 0;

        private void MoveEnd() => CursorPosition = Text.Length;

        private void Clear()
        {
            Text = string.Empty;
            CursorPosition = 0;
        }

        private void Save()
        {
            Result = Text;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            Result = null;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
