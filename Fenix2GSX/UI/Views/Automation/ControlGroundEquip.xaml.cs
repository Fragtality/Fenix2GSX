using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ControlGroundEquip : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlGroundEquip(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindStringInteger(nameof(ViewModel.ChockDelayMin), InputChockMinimum, "10");
            ViewModel.BindStringInteger(nameof(ViewModel.ChockDelayMax), InputChockMaximum, "20");
        }
    }
}
