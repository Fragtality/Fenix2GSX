using Fenix2GSX.GSX.Services;
using FenixInterface;
using System.Collections.Generic;

namespace Fenix2GSX.AppConfig
{
    public enum ProfileMatchType
    {
        Default = 0,
        Airline = 1,
        Title = 2,
        Registration = 3,
    }

    public class AircraftProfile : IAircraftProfile
    {
        public virtual string Name { get; set; } = "default";
        public virtual ProfileMatchType MatchType { get; set; } = ProfileMatchType.Default;
        public virtual string MatchString { get; set; } = "";

        public virtual void Copy(AircraftProfile profile)
        {
            Name = profile.Name;
            MatchType = profile.MatchType;
            MatchString = profile.MatchString;
        }

        public override string ToString()
        {
            if (MatchType != ProfileMatchType.Default)
                return $"{Name}: {MatchType} ~ '{MatchString}'";
            else
                return $"{Name}: {MatchType}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is AircraftProfile profile)
                return this.Name.Equals(profile.Name);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0 ^ MatchType.GetHashCode() ^ MatchString?.GetHashCode() ?? 0;
        }

        //Settings
        public virtual int ConnectPca { get; set; } = 2; // 0 => false | 1 => true | 2 => only on jetway stand
        public virtual bool DoorStairHandling { get; set; } = true;
        public virtual bool DoorStairIncludeL2 { get; set; } = false;
        public virtual bool DoorCargoHandling { get; set; } = true;
        public virtual int FinalDelayMin { get; set; } = 90;
        public virtual int FinalDelayMax { get; set; } = 150;
        public virtual int ChockDelayMin { get; set; } = 10;
        public virtual int ChockDelayMax { get; set; } = 20;
        public virtual bool FuelSaveLoadFob { get; set; } = true;
        public virtual bool RandomizePax { get; set; } = true;
        public virtual double ChancePerSeat { get; set; } = 0.025;
        public virtual double RefuelRateKgSec { get; set; } = 28;
        public virtual bool UseRefuelTimeTarget { get; set; } = false;
        public virtual int RefuelTimeTargetSeconds { get; set; } = 300;
        public virtual bool DoorsCargoKeepOpenOnLoaded { get; set; } = false;
        public virtual bool DoorsCargoKeepOpenOnUnloaded { get; set; } = false;
        public virtual bool OperatorAutoSelect { get; set; } = true;
        public virtual List<string> OperatorPreferences { get; set; } = [];
        public virtual bool SkipFuelOnTankering { get; set; } = true;
        public virtual bool CallReposition { get; set; } = true;
        public virtual bool CallJetwayStairsOnPrep { get; set; } = true;
        public virtual bool CallJetwayStairsDuringDeparture { get; set; } = false;
        public virtual bool CallJetwayStairsOnArrival { get; set; } = true;
        public virtual int RemoveStairsAfterDepature { get; set; } = 2; // 0 => false | 1 => true | 2 => only on jetway stand
        public virtual int AttachTugDuringBoarding { get; set; } = 1; // 0 => not answer | 1 => no | 2 => yes
        public virtual int CallPushbackWhenTugAttached { get; set; } = 2; // 0 => false | 1 => after Departure Services | 2 => after Final LS
        public virtual bool SkipCrewQuestion { get; set; } = true;
        public virtual bool SkipFollowMe { get; set; } = true;
        public virtual bool KeepDirectionMenuOpen { get; set; } = true;
        public virtual bool CloseDoorsOnFinal { get; set; } = true;
        public virtual bool RemoveJetwayStairsOnFinal { get; set; } = true;
        public virtual bool CallPushbackOnBeacon { get; set; } = false;
        public virtual bool ClearGroundEquipOnBeacon { get; set; } = true;
        public virtual bool CallDeboardOnArrival { get; set; } = true;
        public virtual bool AnswerCabinCallGround { get; set; } = true;
        public virtual int DelayCabinCallGround { get; set; } = 4000;
        public virtual bool AnswerCabinCallAir { get; set; } = true;
        public virtual int DelayCabinCallAir { get; set; } = 2500;
        public virtual SortedDictionary<int, ServiceConfig> DepartureServices { get; set; } = new()
        {
            { 0, new ServiceConfig(GsxServiceType.Refuel, GsxServiceActivation.AfterCalled) },
            { 1, new ServiceConfig(GsxServiceType.Catering, GsxServiceActivation.AfterCalled) },
            { 2, new ServiceConfig(GsxServiceType.Lavatory, GsxServiceActivation.Skip) },
            { 3, new ServiceConfig(GsxServiceType.Water, GsxServiceActivation.AfterRequested) },
            { 4, new ServiceConfig(GsxServiceType.Boarding, GsxServiceActivation.AfterAllCompleted) },
        };
    }
}
