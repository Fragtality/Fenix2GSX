using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ControlGateDoors : UserControl
    {
        protected virtual ModelAutomation ViewModel { get; }

        public ControlGateDoors(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
        }
    }
}
