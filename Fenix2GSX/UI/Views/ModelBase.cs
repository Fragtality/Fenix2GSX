using CFIT.AppFramework.UI.ViewModels;
using CFIT.SimConnectLib;
using Fenix2GSX.Aircraft;
using Fenix2GSX.AppConfig;
using Fenix2GSX.Audio;
using Fenix2GSX.GSX;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Fenix2GSX.UI.Views
{
    public abstract partial class ModelBase<TObject>(TObject source, AppService appService) : ViewModelBase<TObject>(source) where TObject : class
    {
        protected virtual AppService AppService { get; } = appService;
        protected virtual Config Config => AppService.Config;
        protected virtual SimConnectController SimConnectController => AppService.SimService.Controller;
        protected virtual SimConnectManager SimConnect => AppService.SimConnect;
        protected virtual GsxController GsxController => AppService.GsxService;
        protected virtual AudioController AudioController => AppService.AudioService;
        protected virtual AircraftInterface AircraftInterface => GsxController?.AircraftInterface;
        protected virtual AircraftProfile AircraftProfile => GsxController?.AircraftProfile;

        public virtual DisplayUnit DisplayUnit { get => Config.DisplayUnit; set { Config.WeightDisplayUnit = value; NotifyDisplayUnit(); Config.NotifyDisplayUnit(); } }
        public virtual string DisplayUnitString => Config.WeightDisplayUnitString.ToString().ToLowerInvariant();
        public virtual Dictionary<DisplayUnit, string> TextDisplayUnit { get; } = new()
        {
            { DisplayUnit.KG, "kg" },
            { DisplayUnit.LB, "lb" },
        };

        public virtual double ConvertKgToDisplayUnit(double kg)
        {
            return Config.ConvertKgToDisplayUnit(kg);
        }

        public virtual double ConvertLbToDisplayUnit(double lb)
        {
            return Config.ConvertLbToDisplayUnit(lb);
        }

        public virtual double ConvertFromDisplayUnitKg(double value)
        {
            return Config.ConvertFromDisplayUnitKg(value);
        }

        public virtual void SaveConfig()
        {
            Config.SaveConfiguration();
        }

        public virtual void SetModelValue<T>(T value, Func<T, ValidationContext, ValidationResult> validator = null, Action<T, T> callback = null, [CallerMemberName] string propertyName = null!)
        {
            SetSourceValue(value, validator, callback, propertyName);
            SaveConfig();
        }

        public virtual void NotifyDisplayUnit()
        {
            NotifyPropertyChanged(nameof(DisplayUnit));
            NotifyPropertyChanged(nameof(DisplayUnitString));
        }
    }
}
