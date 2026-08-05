using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using oovv_ads_control.ViewModels.Dialogs;

namespace oovv_ads_control.Views.Controls
{
    /// <summary>
    /// Interaction logic for AlphanumericKeypad.xaml
    /// </summary>
    public partial class AlphanumericKeypad : UserControl
    {
        private readonly List<(Button Button, KeyDefinition Key)> _characterButtons = new();

        public AlphanumericKeypad()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            BuildKeys();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;

            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += OnViewModelPropertyChanged;

            RefreshCharacterLabels();
        }

        /// <summary>Shift/Caps 状态变了，字符键显示的字符要跟着刷新（大小写、符号切换）。</summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(TextInputDialogViewModel.IsShiftActive)
                or nameof(TextInputDialogViewModel.IsCapsLockActive))
            {
                RefreshCharacterLabels();
            }
        }

        private void BuildKeys()
        {
            for (int i = 0; i < AlphanumericKeyboardLayout.ColumnCount; i++)
                KeyboardGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var keyButtonStyle = (Style)FindResource("KeyButtonStyle");

            foreach (var key in AlphanumericKeyboardLayout.Keys)
            {
                var button = new Button { Style = keyButtonStyle, Content = key.Normal };
                Grid.SetRow(button, key.Row);
                Grid.SetColumn(button, key.Column);
                Grid.SetColumnSpan(button, key.ColumnSpan);
                button.Click += (_, _) => OnKeyPressed(key);

                KeyboardGrid.Children.Add(button);

                if (key.Kind == KeyKind.Character)
                    _characterButtons.Add((button, key));
            }
        }

        private void RefreshCharacterLabels()
        {
            if (DataContext is not TextInputDialogViewModel vm)
                return;

            foreach (var (button, key) in _characterButtons)
                button.Content = vm.ResolveLabel(key.Normal, key.Shifted, key.IsLetter);
        }

        private void OnKeyPressed(KeyDefinition key)
        {
            if (DataContext is not TextInputDialogViewModel vm)
                return;

            switch (key.Kind)
            {
                case KeyKind.Character:
                    vm.InsertCharCommand.Execute(vm.ResolveLabel(key.Normal, key.Shifted, key.IsLetter));
                    break;
                case KeyKind.Space:
                    vm.InsertCharCommand.Execute(" ");
                    break;
                case KeyKind.Tab:
                    vm.InsertCharCommand.Execute("\t");
                    break;
                case KeyKind.Backspace:
                    vm.BackspaceCommand.Execute(null);
                    break;
                case KeyKind.Delete:
                    vm.DeleteCommand.Execute(null);
                    break;
                case KeyKind.ArrowLeft:
                    vm.MoveLeftCommand.Execute(null);
                    break;
                case KeyKind.ArrowRight:
                    vm.MoveRightCommand.Execute(null);
                    break;
                case KeyKind.Home:
                    vm.HomeCommand.Execute(null);
                    break;
                case KeyKind.End:
                    vm.EndCommand.Execute(null);
                    break;
                case KeyKind.CapsLock:
                    vm.ToggleCapsCommand.Execute(null);
                    break;
                case KeyKind.Shift:
                    vm.ToggleShiftCommand.Execute(null);
                    break;
                case KeyKind.Insert:
                    vm.ToggleInsertCommand.Execute(null);
                    break;
                case KeyKind.Enter:
                    if (vm.SaveCommand.CanExecute(null))
                        vm.SaveCommand.Execute(null);
                    break;
                case KeyKind.ArrowUp:
                case KeyKind.ArrowDown:
                case KeyKind.Ctrl:
                case KeyKind.Alt:
                    // 参考图上有这几个键，但单行文本输入没有对应语义（不是多行编辑器，也没有接组合键），
                    // 先做成纯展示、按下没反应，以后真需要了再接。
                    break;
            }
        }
    }
}
