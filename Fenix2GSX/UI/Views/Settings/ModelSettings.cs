using Fenix2GSX.AppConfig;
using FenixInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace Fenix2GSX.UI.Views.Settings
{
    public partial class ModelSettings : ModelBase<Config>
    {
        protected virtual DispatcherTimer UnitUpdateTimer { get; }
        public virtual Dictionary<DisplayUnit, string> DisplayUnitDefaultItems { get; } = new()
        {
            { DisplayUnit.KG, "kg" },
            { DisplayUnit.LB, "lb" },
        };
        public virtual Dictionary<DisplayUnitSource, string> DisplayUnitSourceItems { get; } = new()
        {
            { DisplayUnitSource.App, "App" },
            { DisplayUnitSource.Aircraft, "Aircraft" },
        };

        public ModelSettings(AppService appService) : base(appService.Config, appService)
        {
            UnitUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UnitUpdateTimer.Tick += UnitUpdateTimer_Tick;
        }

        protected override void InitializeModel()
        {
            Config.PropertyChanged += OnConfigPropertyChanged;
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                && !UnitUpdateTimer.IsEnabled)
                UnitUpdateTimer.Start();
        }

        protected virtual void UnitUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            NotifyPropertyChanged(nameof(FenixWeightBag));
            NotifyPropertyChanged(nameof(FuelResetDefaultKg));
            NotifyPropertyChanged(nameof(FuelCompareVariance));
            InhibitConfigSave = false;
            UnitUpdateTimer.Stop();
        }

        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;
        public virtual DisplayUnit DisplayUnitDefault { get => Source.DisplayUnitDefault; set { SetModelValue<DisplayUnit>(value); Config.EvaluateDisplayUnit(); } }
        public virtual DisplayUnitSource DisplayUnitSource { get => Source.DisplayUnitSource; set { SetModelValue<DisplayUnitSource>(value); Config.EvaluateDisplayUnit(); } }
        public virtual double FenixWeightBag { get => Config.ConvertKgToDisplayUnit(Source.FenixWeightBag); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelResetDefaultKg { get => Config.ConvertKgToDisplayUnit(Source.FuelResetDefaultKg); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual double FuelCompareVariance { get => Config.ConvertKgToDisplayUnit(Source.FuelCompareVariance); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
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
        public virtual bool OpenAppWindowOnStart { get => Source.OpenAppWindowOnStart; set => SetModelValue<bool>(value); }
    }
}
