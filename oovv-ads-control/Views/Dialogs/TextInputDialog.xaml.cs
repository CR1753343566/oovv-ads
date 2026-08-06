using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        private TextInputDialog(TextInputDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // 占屏幕宽 80% / 高 50%，按 SystemParameters 算而不是写死像素——万一屏幕分辨率变了也不用改代码
            Width = SystemParameters.PrimaryScreenWidth * 0.8;
            Height = SystemParameters.PrimaryScreenHeight * 0.5;

            // HeaderGrid 跟键盘共用同一套 AlphanumericKeyboardLayout.ColumnCount 列坐标系，
            // 列定义在这里生成，XAML 里的 Grid.Column/ColumnSpan 才有意义
            for (int i = 0; i < AlphanumericKeyboardLayout.ColumnCount; i++)
                HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition());

            viewModel.RequestClose += (_, _) => DialogResult = viewModel.Result != null;

            // TextBox.CaretIndex 不是依赖属性，没法用 XAML Binding，光标位置在这里手动同步
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateCaret();
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

        private void UpdateCaret()
        {
            var index = _viewModel.CursorPosition;
            if (index >= 0 && index <= InputTextBox.Text.Length)
                InputTextBox.CaretIndex = index;
        }

        /// <summary>
        /// 打开字母数字输入弹窗。返回 null 表示用户取消；否则是编辑后的文本，写不写回 PLC 由调用方自己决定。
        /// maxLength 为 null 表示不限制长度。
        /// </summary>
        public static string? ShowDialog(Window? owner, string title, string currentValue, int? maxLength = null)
        {
            var viewModel = new TextInputDialogViewModel(title, currentValue, maxLength);
            var dialog = new TextInputDialog(viewModel);

            if (owner != null)
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
            return viewModel.Result;
        }
    }
}
