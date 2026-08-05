using System.ComponentModel;
using System.Windows;
using oovv_ads_control.ViewModels.Dialogs;

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

            viewModel.RequestClose += (_, _) => DialogResult = viewModel.Result != null;

            // TextBox.CaretIndex 不是依赖属性，没法用 XAML Binding，光标位置在这里手动同步
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateCaret();
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
