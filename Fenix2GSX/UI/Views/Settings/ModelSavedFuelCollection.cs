using CFIT.AppFramework.UI.ViewModels;
using System;
using System.Collections.Generic;

namespace Fenix2GSX.UI.Views.Settings
{
    public partial class ModelSavedFuelCollection : ViewModelDictionary<string, double, string>
    {
        public override ICollection<KeyValuePair<string, double>> Source => AppService.Instance?.Config?.FuelFobSaved ?? [];
        
        public ModelSavedFuelCollection() : base(AppService.Instance?.Config?.FuelFobSaved ?? [],
                    (kv) => $"{kv.Key} ({Math.Round(AppService.Instance.Config.ConvertKgToDisplayUnit(kv.Value), 2)} {AppService.Instance.Config.DisplayUnitCurrentString})",
                    (kv) => !string.IsNullOrWhiteSpace(kv.Key))
        {
            AddAllowed = false;
            UpdatesAllowed = false;
        }
    }
}
