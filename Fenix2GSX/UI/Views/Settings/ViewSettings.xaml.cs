using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Settings
{
    public partial class ViewSettings : UserControl, IView
    {
        protected virtual ModelSettings ViewModel { get; }
        protected virtual ViewModelSelector<KeyValuePair<string, double>, string> ViewModelSelector { get; }

        public ViewSettings()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            ViewModel.BindStringNumber(nameof(ViewModel.FenixWeightBag), InputBagWeight, "15", new ValidationRuleRange<double>(1, 15));
            ViewModel.BindStringNumber(nameof(ViewModel.FuelResetDefaultKg), InputFuelDefault, "3000", new ValidationRuleRange<double>(1000, 6000));
            ViewModel.BindStringNumber(nameof(ViewModel.FuelCompareVariance), InputFuelVariance, "25", new ValidationRuleRange<double>(10, 100));
            ViewModel.BindStringInteger(nameof(ViewModel.CargoPercentChangePerSec), InputCargoRate, "5", new ValidationRuleRange<int>(1, 25));
            ViewModel.BindStringInteger(nameof(ViewModel.DoorCargoOpenDelay), InputDoorCargoOpenDelay, "2", new ValidationRuleRange<int>(1, 90));
            ViewModel.BindStringInteger(nameof(ViewModel.RefuelPanelOpenDelay), InputRefuelOpenDelay, "10", new ValidationRuleRange<int>(1, 90));
            ViewModel.BindStringInteger(nameof(ViewModel.RefuelPanelCloseDelay), InputRefuelCloseDelay, "42", new ValidationRuleRange<int>(1, 180));
            ViewModel.BindStringInteger(nameof(ViewModel.GsxMenuStartupMaxFail), InputGsxMaxFail, "4", new ValidationRuleRange<int>(1,16));

            ViewModelSelector = new(ListSavedFuel, ViewModel.ModelSavedFuel);
            ViewModelSelector.BindRemoveButton(ButtonRemove);
        }

        public virtual void Start()
        {
            
        }

        public virtual void Stop()
        {

        }
    }
}
