using CFIT.AppFramework.UI.ViewModels;
using Fenix2GSX.AppConfig;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fenix2GSX.UI.Views.Profiles
{
    public partial class ModelProfileCollection() : ViewModelCollection<AircraftProfile, AircraftProfile>(AppService.Instance?.Config?.AircraftProfiles ?? [], (i) => i, (p) => !string.IsNullOrWhiteSpace(p?.Name))
    {
        public override ICollection<AircraftProfile> Source => AppService.Instance?.Config?.AircraftProfiles ?? [];

        public override bool UpdateSource(AircraftProfile oldItem, AircraftProfile newItem)
        {
            try
            {
                if (Contains(oldItem))
                {
                    if (oldItem.Name.Equals(newItem?.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        oldItem.Copy(newItem);
                        return true;
                    }
                    else if (!Source.Where(p => p.Name.Equals(newItem.Name, StringComparison.InvariantCultureIgnoreCase)).Any())
                    {
                        oldItem.Copy(newItem);
                        return true;
                    }
                    else
                        return false;
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
