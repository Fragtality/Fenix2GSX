using CFIT.AppFramework.UI.ViewModels;
using Fenix2GSX.AppConfig;
using System.Collections.Generic;

namespace Fenix2GSX.UI.Views.Profiles
{
    public partial class ModelProfileCollection(ICollection<AircraftProfile> source) : ViewModelCollection<AircraftProfile, AircraftProfile>(source, (i) => i, (p) => !string.IsNullOrWhiteSpace(p?.Name))
    {
        public override bool UpdateSource(AircraftProfile oldItem, AircraftProfile newItem)
        {
            try
            {
                if (Contains(oldItem))
                {
                    oldItem.Copy(newItem);
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
