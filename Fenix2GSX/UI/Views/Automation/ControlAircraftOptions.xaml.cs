using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ControlAircraftOptions : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlAircraftOptions(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;

            ViewModel.BindStringInteger(nameof(ViewModel.FinalDelayMin), InputFinalMinimum, "90");
            ViewModel.BindStringInteger(nameof(ViewModel.FinalDelayMax), InputFinalMaximum, "150");
            ViewModel.BindStringNumber(nameof(ViewModel.ChancePerSeat), InputChanceSeat, "2");
        }
    }
}
