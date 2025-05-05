using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{

    public partial class ControlOperatorSelection : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual ViewModelSelector<string, string> ViewModelSelector { get; }

        public ControlOperatorSelection(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModelSelector = new(ListOperators, ViewModel.OperatorPreferences, AppWindow.IconLoader);
            ViewModelSelector.BindTextElement(InputOperator);

            ButtonAdd.Command = ViewModelSelector.BindAddUpdateButton(ButtonAdd, ImageAdd, () => InputOperator?.Text ?? "", () => !string.IsNullOrWhiteSpace(InputOperator?.Text ?? ""));
            ViewModelSelector.AddUpdateCommand.Subscribe(InputOperator);
            ViewModelSelector.AddUpdateCommand.Bind(InputOperator);
            ButtonRemove.Command = ViewModelSelector.BindRemoveButton(ButtonRemove, () => ListOperators?.SelectedValue is string str && !string.IsNullOrWhiteSpace(str));

            ButtonUp.Command = new CommandWrapper(() => ViewModel.OperatorPreferences.MoveItem(ListOperators.SelectedIndex, -1), () => ListOperators?.SelectedIndex != -1).Subscribe(ListOperators);
            ButtonDown.Command = new CommandWrapper(() => ViewModel.OperatorPreferences.MoveItem(ListOperators.SelectedIndex, 1), () => ListOperators?.SelectedIndex != -1).Subscribe(ListOperators);
        }
    }
}
