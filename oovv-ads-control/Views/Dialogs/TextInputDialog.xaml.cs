using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using oovv_ads_control.ViewModels.Dialogs;
using oovv_ads_control.Views.Controls;

namespace oovv_ads_control.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for TextInputDialog.xaml
    /// </summary>
    public partial class TextInputDialog : Window
    {
        private readonly TextInputDialogViewModel _viewModel;

        private TextInputDialog(TextInputDialogViewModel viewModel, Window? owner)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // 占宿主窗口宽 96% / 高 50%——不能用 SystemParameters.PrimaryScreenWidth/Height（物理屏幕分辨率），
            // 因为 Debug 构建下 MainWindow 是普通窗口、没有铺满屏幕（见 MainWindow.xaml.cs 的 #if DEBUG），
            // 用屏幕分辨率算出来的尺寸会跟 MainWindow 实际大小脱节，Debug 下弹窗可能比主窗口还大。
            // Release 下 MainWindow 本来就铺满屏幕，ActualWidth/Height 跟屏幕分辨率是同一个数，效果不变。
            // owner 为 null 时（理论上不会发生）才退回屏幕分辨率兜底。
            var baseWidth = owner?.ActualWidth ?? SystemParameters.PrimaryScreenWidth;
            var baseHeight = owner?.ActualHeight ?? SystemParameters.PrimaryScreenHeight;
            Width = baseWidth * 0.96;
            Height = baseHeight * 0.5;

            // 贴底居中：WindowStartupLocation 只有 Manual/CenterScreen/CenterOwner 三种，没有贴底选项，
            // 只能手动算 Left/Top——水平相对宿主窗口居中，垂直贴在宿主窗口底边。
            // owner 为 null 时交给 ShowDialog 里的 CenterScreen 兜底，这里不用管。
            if (owner != null)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = owner.Left + (owner.ActualWidth - Width) / 2;
                Top = owner.Top + owner.ActualHeight - 1.06*Height;
            }

            // HeaderGrid 跟键盘共用同一套 AlphanumericKeyboardLayout.ColumnCount 列坐标系，
            // 列定义在这里生成，XAML 里的 Grid.Column/ColumnSpan 才有意义
            for (int i = 0; i < AlphanumericKeyboardLayout.ColumnCount; i++)
                HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

            viewModel.RequestClose += (_, _) => DialogResult = viewModel.Result != null;

            // 光标是自己画的（CaretIndicator），位置在这里手动同步
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 窗口这时候还没排过版，GetRectFromCharacterIndex 量出来的位置不可靠——
            // 等 Loaded（真正渲染过一次）之后再算一次，光标才能从弹窗一打开就出现在正确位置
            Loaded += (_, _) => UpdateCaret();
        }

        /// <summary>
        /// 没有标题栏（或者即使有，触屏点titlebar有时不跟手）时的兜底拖动方案：
        /// 按钮/输入框会先消费掉点击事件，只有点在空白处才会走到这里，安全不冲突。
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(TextInputDialogViewModel.CursorPosition) or nameof(TextInputDialogViewModel.Text))
                UpdateCaret();
        }

        /// <summary>
        /// 挪动自绘光标（CaretIndicator）到当前 CursorPosition 对应的像素位置，并重新开始闪烁。
        /// IsReadOnly 的 TextBox 不渲染原生光标（不管有没有焦点），所以没法靠 CaretIndex 单独解决，
        /// 只能自己用 GetRectFromCharacterIndex 算出像素位置，叠一根条在输入框上面。
        /// </summary>
        private void UpdateCaret()
        {
            var index = _viewModel.CursorPosition;
            if (index < 0 || index > InputTextBox.Text.Length)
                return;

            InputTextBox.CaretIndex = index; // 顺手同步一下，选中/复制这类原生行为还是有意义的

            var rect = InputTextBox.GetRectFromCharacterIndex(index);
            CaretIndicator.Margin = new Thickness(rect.Left, 0, 0, 0);

            // 每次移动光标都重新开始一轮动画：先实打实显示，停顿一下再开始闪烁，
            // 这样用户操作后能立刻看到光标在哪，不会因为正好闪到"隐"那一帧而看不见
            var blink = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromMilliseconds(600),
            };
            CaretIndicator.BeginAnimation(UIElement.OpacityProperty, blink);
        }

        /// <summary>
        /// 打开字母数字输入弹窗。返回 null 表示用户取消；否则是编辑后的文本，写不写回 PLC 由调用方自己决定。
        /// maxLength 为 null 表示不限制长度。
        /// </summary>
        public static string? ShowDialog(Window? owner, string title, string currentValue, int? maxLength = null)
        {
            var viewModel = new TextInputDialogViewModel(title, currentValue, maxLength);
            var dialog = new TextInputDialog(viewModel, owner);

            if (owner != null)
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
            return viewModel.Result;
        }
    }
}
