using Fenix2GSX.AppConfig;
using System.ComponentModel;

namespace Fenix2GSX.UI.Views.Settings
{
    public partial class ModelSettings(AppService appService) : ModelBase<Config>(appService.Config, appService)
    {
        protected override void InitializeModel()
        {
            Config.PropertyChanged += OnPropertyChanged;
        }

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(DisplayUnit))
            {
                NotifyPropertyChanged(nameof(FenixWeightBag));
                NotifyPropertyChanged(nameof(FuelResetDefaultKg));
                NotifyPropertyChanged(nameof(FuelCompareVariance));
            }
        }

        public virtual double FenixWeightBag { get => ConvertKgToDisplayUnit(Source.FenixWeightBag); set => SetModelValue<double>(ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelResetDefaultKg { get => ConvertKgToDisplayUnit(Source.FuelResetDefaultKg); set => SetModelValue<double>(ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelCompareVariance { get => ConvertKgToDisplayUnit(Source.FuelCompareVariance); set => SetModelValue<double>(ConvertFromDisplayUnitKg(value)); }
        public virtual bool FuelRoundUp100 { get => Source.FuelRoundUp100; set => SetModelValue<bool>(value); }
        public virtual bool DingOnStartup { get => Source.DingOnStartup; set => SetModelValue<bool>(value); }
        public virtual bool DingOnFinal { get => Source.DingOnFinal; set => SetModelValue<bool>(value); }
        public virtual bool DingOnTurnaround { get => Source.DingOnTurnaround; set => SetModelValue<bool>(value); }
        public virtual int CargoPercentChangePerSec { get => Source.CargoPercentChangePerSec; set => SetModelValue<int>(value); }
        public virtual int DoorCargoDelay { get => Source.DoorCargoDelay; set => SetModelValue<int>(value); }
        public virtual bool SkipWalkAround { get => Source.SkipWalkAround; set => SetModelValue<bool>(value); }
        public virtual bool RestartGsxOnTaxiIn { get => Source.RestartGsxOnTaxiIn; set => SetModelValue<bool>(value); }
        public virtual bool EfbResetOnStartup { get => Source.EfbResetOnStartup; set => SetModelValue<bool>(value); }
        public virtual bool RunGsxService { get => Source.RunGsxService; set => SetModelValue<bool>(value); }
    }
}
