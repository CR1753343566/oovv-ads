using System.Windows;
using oovv_ads_control.ViewModels.Dialogs;

namespace oovv_ads_control.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for NumericInputDialog.xaml
    /// </summary>
    public partial class NumericInputDialog : Window
    {
        private NumericInputDialog(NumericInputDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += (_, _) => DialogResult = viewModel.Result.HasValue;
        }

        /// <summary>
        /// 打开数值输入弹窗。actualValue/currentSetpoint 只是调用这一刻的快照，弹窗打开期间不会跟着 PLC 刷新。
        /// 返回 null 表示用户取消；否则是通过范围校验的新设定值，写不写回 PLC 由调用方自己决定。
        /// </summary>
        public static double? ShowDialog(Window? owner, string parameterName, string unit,
            double actualValue, double currentSetpoint, double minimum, double maximum)
        {
            var viewModel = new NumericInputDialogViewModel(parameterName, unit, actualValue, currentSetpoint, minimum, maximum);
            var dialog = new NumericInputDialog(viewModel);

            if (owner != null)
                dialog.Owner = owner;
            else
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            dialog.ShowDialog();
            return viewModel.Result;
        }

        private void NumericKeypad_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
