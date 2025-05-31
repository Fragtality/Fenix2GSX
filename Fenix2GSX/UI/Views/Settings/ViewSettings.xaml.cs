using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Settings
{
    public partial class ViewSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }

        public ViewSettings()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ViewModel.BindStringNumber(nameof(ViewModel.FenixWeightBag), InputBagWeight);
            ViewModel.BindStringNumber(nameof(ViewModel.FuelResetDefaultKg), InputFuelDefault);
            ViewModel.BindStringNumber(nameof(ViewModel.FuelCompareVariance), InputFuelVariance);
            ViewModel.BindStringInteger(nameof(ViewModel.CargoPercentChangePerSec), InputCargoRate);
            ViewModel.BindStringInteger(nameof(ViewModel.DoorCargoDelay), InputDoorCargoDelay);
        }

        public virtual void Start()
        {
            
        }

        public virtual void Stop()
        {

        }
    }
}
