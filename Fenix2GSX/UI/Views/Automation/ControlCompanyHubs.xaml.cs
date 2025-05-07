using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ControlCompanyHubs : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual ViewModelSelector<string, string> ViewModelSelector { get; }

        public ControlCompanyHubs(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModelSelector = new(ListHubs, ViewModel.CompanyHubs, AppWindow.IconLoader);
            ViewModelSelector.BindTextElement(InputHub);

            ButtonAdd.Command = ViewModelSelector.BindAddUpdateButton(ButtonAdd, ImageAdd, () => InputHub?.Text ?? "", () => !string.IsNullOrWhiteSpace(InputHub?.Text ?? ""));
            ViewModelSelector.AddUpdateCommand.Subscribe(InputHub);
            ViewModelSelector.AddUpdateCommand.Bind(InputHub);
            ButtonRemove.Command = ViewModelSelector.BindRemoveButton(ButtonRemove, () => ListHubs?.SelectedValue is string str && !string.IsNullOrWhiteSpace(str));

            ButtonUp.Command = new CommandWrapper(() => ViewModel.CompanyHubs.MoveItem(ListHubs.SelectedIndex, -1), () => ListHubs?.SelectedIndex != -1).Subscribe(ListHubs);
            ButtonDown.Command = new CommandWrapper(() => ViewModel.CompanyHubs.MoveItem(ListHubs.SelectedIndex, 1), () => ListHubs?.SelectedIndex != -1).Subscribe(ListHubs);
        }
    }
}
