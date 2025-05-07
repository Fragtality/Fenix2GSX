using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using CoreAudio;
using Fenix2GSX.Aircraft;
using Fenix2GSX.Audio;
using FenixInterface;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fenix2GSX.AppConfig
{
    public enum DisplayUnit
    {
        KG = 0,
        LB = 1,
    }

    public class Config : AppConfigBase<Definition>, IConfig, INotifyPropertyChanged
    {
        public virtual double WeightConversion { get; set; } = 2.2046226218;
        public virtual float CargoDistMain { get; set; } = 4000.0f / 9440.0f;
        public virtual float CargoDistBulk { get; set; } = 1440.0f / 9440.0f;
        public virtual string BinaryGsx2020 { get; set; } = "Couatl64_MSFS";
        public virtual string BinaryGsx2024 { get; set; } = "Couatl64_MSFS2024";
        public virtual string Msfs2024WindowTitle { get; set; } = "Microsoft Flight Simulator 2024 - ";
        public virtual string FenixAircraftString { get; set; } = "FNX_3";
        public virtual string FenixBinary { get; set; } = "FenixSystem";
        public virtual int UiRefreshInterval { get; set; } = 500;
        public virtual double FenixWeightBag { get; set; } = 15;
        public virtual double FuelCompareVariance { get; set; } = 25;
        public virtual int TimerGsxCheck { get; set; } = 1000;
        public virtual int TimerGsxStartupMenuCheck { get; set; } = 5000;
        public virtual int DelayGsxBinaryStart { get; set; } = 7500;
        public virtual bool RunGsxService { get; set; } = true;
        public virtual bool RestartGsxOnTaxiIn { get; set; } = false;
        public virtual bool RunAudioService { get; set; } = true;
        public virtual string AudioDebugFile { get; set; } = "log\\AudioDebug.txt";
        public virtual DataFlow AudioDeviceFlow { get; set; } = DataFlow.Render;
        public virtual DeviceState AudioDeviceState { get; set; } = DeviceState.Active;
        public virtual int AudioServiceRunInterval { get; set; } = 1000;
        public virtual int AudioProcessStartupDelay { get; set; } = 2000;
        public virtual int AudioDeviceCheckInterval { get; set; } = 5000;
        public virtual int AudioProcessMaxSearchCount { get; set; } = 30;
        public virtual bool AudioSynchSessionOnCountChange { get; set; } = false;
        public virtual List<string> AudioDeviceBlacklist { get; set; } = [];
        public virtual AcpSide AudioAcpSide { get; set; } = AcpSide.CPT;
        public virtual List<AudioMapping> AudioMappings { get; set; } = 
        [
            new(AudioChannel.VHF1, "", "vPilot"),
            new(AudioChannel.VHF1, "", "BeyondATC"),
            new(AudioChannel.VHF1, "", "Pilot2ATC_2021"),
            new(AudioChannel.INT, "", "Couatl64_MSFS"),
            new(AudioChannel.INT, "", "Couatl64_MSFS2024"),
            new(AudioChannel.CAB, "", "FlightSimulator"),
            new(AudioChannel.CAB, "", "FlightSimulator2024"),
        ];
        public virtual Dictionary<AudioChannel, double> AudioStartupVolumes { get; set; } = new()
        {
            { AudioChannel.VHF1, 1.0 },
            { AudioChannel.VHF2, -1.0 },
            { AudioChannel.VHF3, -1.0 },
            { AudioChannel.HF1, -1.0 },
            { AudioChannel.HF2, -1.0 },
            { AudioChannel.INT, 1.0 },
            { AudioChannel.CAB, 1.0 },
            { AudioChannel.PA, -1.0 },
        };
        public virtual Dictionary<AudioChannel, bool> AudioStartupUnmute { get; set; } = new()
        {
            { AudioChannel.VHF1, true },
            { AudioChannel.VHF2, false },
            { AudioChannel.VHF3, false },
            { AudioChannel.HF1, false },
            { AudioChannel.HF2, false },
            { AudioChannel.INT, true },
            { AudioChannel.CAB, true },
            { AudioChannel.PA, false },
        };
        public virtual int GsxServiceStartDelay { get; set; } = 4000;
        public virtual int GroundTicks { get; set; } = 2;
        public virtual int DelayForegroundChange { get; set; } = 1250;
        public virtual int DelayAircraftModeChange { get; set; } = 1250;
        public virtual int MenuCheckInterval { get; set; } = 250;
        public virtual int MenuOpenTimeout { get; set; } = 5000;
        public virtual int EfbCheckInterval { get; set; } = 1500;
        public virtual bool EfbResetOnStartup { get; set; } = true;        
        public virtual DisplayUnit WeightDisplayUnit { get; set; } = DisplayUnit.KG;
        [JsonIgnore]
        public virtual string WeightDisplayUnitString => WeightDisplayUnit.ToString().ToLowerInvariant();
        public virtual double FuelResetDefaultKg { get; set; } = 3000;
        public virtual bool FuelRoundUp100 { get; set; } = true;
        public virtual Dictionary<string, double> FuelFobSaved { get; set; } = [];
        public virtual int CargoPercentChangePerSec { get; set; } = 5;
        public virtual int DoorCargoFwdBoardDelay { get; set; } = 30;
        public virtual int DoorCargoAftBoardDelay { get; set; } = 2;
        public virtual int DoorCargoFwdDeboardDelay { get; set; } = 8;
        public virtual int DoorCargoAftDeboardDelay { get; set; } = 2;
        public virtual bool InterceptGpuAndChocksOnBeacon { get; set; } = true;
        public virtual int OperatorWaitTimeout { get; set; } = 1500;
        public virtual int OperatorSelectTimeout { get; set; } = 10000;
        public virtual bool SkipWalkAround { get; set; } = true;
        public virtual bool RunAutomationService { get; set; } = true;
        public virtual bool DebugArrival { get; set; } = false;
        public virtual int StateMachineInterval { get; set; } = 500;
        public virtual int DelayServiceStateChange { get; set; } = 500;
        public virtual int SpeedTresholdTaxiOut { get; set; } = 2;
        public virtual int SpeedTresholdTaxiIn { get; set; } = 30;

        public virtual List<AircraftProfile> AircraftProfiles { get; set; } = new()
        {
            { new AircraftProfile() }
        };

        public override void SaveConfiguration()
        {
            SaveConfiguration<Config>(this, ConfigFile);
        }

        protected override void InitConfiguration()
        {
            if (!AircraftProfiles.Any(p => p.Name == "default"))
            {
                AircraftProfiles.Add(new AircraftProfile());
                this.SaveConfiguration();
            }
        }

        protected override void UpdateConfiguration(int buildConfigVersion)
        {

        }

        public virtual void SetFuelFob(string registration, double fuel)
        {
            if (FuelFobSaved.ContainsKey(registration))
                FuelFobSaved[registration] = fuel;
            else
                FuelFobSaved.Add(registration, fuel);

            SaveConfiguration();
        }

        public virtual double GetFuelFob(string registration)
        {
            if (FuelFobSaved.TryGetValue(registration, out double fuel))
                return fuel;
            else
                return FuelResetDefaultKg;
        }

        public virtual AircraftProfile GetAircraftProfile(AircraftInterface aircraft)
        {
            if (aircraft.IsLoaded)
            {
                var query = AircraftProfiles.Where(p => p.MatchType == ProfileMatchType.Registration && aircraft.Registration.Equals(p.MatchString, System.StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                {
                    var profile = query.First();
                    Logger.Information($"Loading Profile '{profile.Name}' (matched by Registration {profile.MatchString})");
                    return profile;
                }

                query = AircraftProfiles.Where(p => p.MatchType == ProfileMatchType.Title && aircraft.Title.Contains(p.MatchString, System.StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                {
                    var profile = query.First();
                    Logger.Information($"Loading Profile '{profile.Name}' (matched by Title {profile.MatchString})");
                    return profile;
                }

                query = AircraftProfiles.Where(p => p.MatchType == ProfileMatchType.Airline && aircraft.Airline.StartsWith(p.MatchString, System.StringComparison.InvariantCultureIgnoreCase));
                if (query.Any())
                {
                    var profile = query.First();
                    Logger.Information($"Loading Profile '{profile.Name}' (matched by Airline {profile.MatchString})");
                    return profile;
                }
            }

            Logger.Information($"Loading default Aircraft Profile");
            return AircraftProfiles.Where(p => p.Name == "default").First() ?? new AircraftProfile();
        }

        public virtual double ConvertKgToDisplayUnit(double kg)
        {
            if (WeightDisplayUnit == DisplayUnit.KG)
                return kg;
            else
                return kg * WeightConversion;
        }

        public virtual double ConvertLbToDisplayUnit(double lb)
        {
            if (WeightDisplayUnit == DisplayUnit.LB)
                return lb;
            else
                return lb / WeightConversion;
        }

        public virtual double ConvertFromDisplayUnitKg(double value)
        {
            if (WeightDisplayUnit == DisplayUnit.KG)
                return value;
            else
                return value / WeightConversion;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonIgnore]
        public virtual DisplayUnit DisplayUnit
        {
            get => WeightDisplayUnit;
            set
            {
                WeightDisplayUnit = value;
                SaveConfiguration();
                NotifyDisplayUnit();
            }
        }

        public virtual void NotifyDisplayUnit()
        {
            NotifyPropertyChanged(nameof(DisplayUnit));
            NotifyPropertyChanged(nameof(WeightDisplayUnit));
            NotifyPropertyChanged(nameof(WeightDisplayUnitString));
        }
    }
}
