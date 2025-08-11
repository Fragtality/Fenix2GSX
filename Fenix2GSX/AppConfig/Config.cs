using CFIT.AppFramework.AppConfig;
using CFIT.AppLogger;
using CoreAudio;
using Fenix2GSX.Aircraft;
using Fenix2GSX.Audio;
using FenixInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fenix2GSX.AppConfig
{
    public class Config : AppConfigBase<Definition>, IConfig, INotifyPropertyChanged
    {
        public virtual bool OpenAppWindowOnStart { get; set; } = false;
        [JsonIgnore]
        public virtual bool ForceOpen { get; set; } = false;
        public virtual double WeightConversion { get; set; } = 2.2046226218;
        public virtual float CargoDistMain { get; set; } = 4000.0f / 9440.0f;
        public virtual float CargoDistBulk { get; set; } = 1440.0f / 9440.0f;
        public virtual string BinaryGsx2020 { get; set; } = "Couatl64_MSFS";
        public virtual string BinaryGsx2024 { get; set; } = "Couatl64_MSFS2024";
        public virtual string Msfs2024WindowTitle { get; set; } = "Microsoft Flight Simulator 2024 - ";
        public virtual string FenixAircraftString { get; set; } = "FNX_3";
        public virtual string FenixBinary { get; set; } = "FenixSystem";
        public virtual string SimbriefUrlBase { get; set; } = "https://www.simbrief.com";
        public virtual string SimbriefUrlPathName { get; set; } = "/api/xml.fetcher.php?username={0}&json=v2";
        public virtual string SimbriefUrlPathId { get; set; } = "/api/xml.fetcher.php?userid={0}&json=v2";
        public virtual int UiRefreshInterval { get; set; } = 500;
        public virtual double FenixWeightBag { get; set; } = 15;
        public virtual double FuelCompareVariance { get; set; } = 25;
        public virtual int TimerGsxCheck { get; set; } = 1000;
        public virtual int TimerGsxProcessCheck { get; set; } = 5000;
        public virtual int TimerGsxStartupMenuCheck { get; set; } = 5000;
        public virtual int GsxMenuStartupMaxFail { get; set; } = 4;
        public virtual bool RestartGsxStartupFail { get; set; } = false;
        public virtual int DelayGsxBinaryStart { get; set; } = 2000;
        public virtual bool RunGsxService { get; set; } = true;
        public virtual bool RestartGsxOnTaxiIn { get; set; } = false;
        public virtual bool RunAudioService { get; set; } = true;
        public virtual string AudioDebugFile { get; set; } = "log\\AudioDebug.txt";
        public virtual DataFlow AudioDeviceFlow { get; set; } = DataFlow.Render;
        public virtual DeviceState AudioDeviceState { get; set; } = DeviceState.Active;
        public virtual int AudioServiceRunInterval { get; set; } = 1000;
        public virtual int AudioProcessCheckInterval { get; set; } = 2500;
        public virtual int AudioProcessStartupDelay { get; set; } = 2000;
        public virtual int AudioDeviceCheckInterval { get; set; } = 60000;
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
        public virtual int MenuOpenTimeout { get; set; } = 2500;
        public virtual int EfbCheckInterval { get; set; } = 1500;
        public virtual bool DingOnStartup { get; set; } = true;
        public virtual bool DingOnFinal { get; set; } = true;
        public virtual bool DingOnTurnaround { get; set; } = true;
        [JsonIgnore]
        public virtual DisplayUnit DisplayUnitCurrent { get; set; }
        [JsonIgnore]
        public string DisplayUnitCurrentString => DisplayUnitCurrent.ToString().ToLowerInvariant();
        public virtual DisplayUnit DisplayUnitDefault { get; set; }
        public virtual DisplayUnitSource DisplayUnitSource { get; set; }
        public virtual double FuelResetDefaultKg { get; set; } = 3000;
        public virtual bool FuelRoundUp100 { get; set; } = true;
        public virtual int RefuelPanelCloseDelay { get; set; } = 42;
        public virtual int RefuelPanelOpenDelay { get; set; } = 10;
        public virtual Dictionary<string, double> FuelFobSaved { get; set; } = [];
        public virtual int CargoPercentChangePerSec { get; set; } = 5;
        public int DoorCargoDelay { get; set; } = 16;
        public int DoorCargoOpenDelay { get; set; } = 2; 
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

        [JsonIgnore]
        public virtual AircraftProfile CurrentProfile => AppService.Instance?.GsxService?.AircraftProfile;
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
            if (ConfigVersion < 7 && buildConfigVersion >= 7)
            {
                AudioDeviceCheckInterval = 60000;
                DoorCargoDelay = 16;
                SizeLimit = 10 * 1024 * 1024;
            }

            if (ConfigVersion < 9 && buildConfigVersion >= 9)
            {
                SizeLimit = 10 * 1024 * 1024;
            }

            if (ConfigVersion < 10 && buildConfigVersion >= 10)
            {
                DelayGsxBinaryStart = 2000;
                MenuOpenTimeout = 2500;
            }

            if (ConfigVersion < 15 && buildConfigVersion >= 15)
            {
                foreach (var profile in AircraftProfiles)
                {
                    if (profile.UseRefuelTimeTarget)
                        profile.RefuelMethod = RefuelMethod.DynamicRate;
                    else
                        profile.RefuelMethod = RefuelMethod.FixedRate;
                }
            }

            if (ConfigVersion < 16 && buildConfigVersion >= 16)
            {
                GsxMenuStartupMaxFail = 4;
            }
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
                foreach (var profile in AircraftProfiles)
                {
                    if (profile.MatchType != ProfileMatchType.Registration)
                        continue;
                    var strings = profile.MatchString.Split('|');
                    foreach (var s in strings)
                    {
                        if (aircraft.Registration.Equals(s, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Information($"Loading Profile '{profile.Name}' (matched on Registration - '{aircraft.Registration}' equals '{s}')");
                            return profile;
                        }
                    }
                }

                foreach (var profile in AircraftProfiles)
                {
                    if (profile.MatchType != ProfileMatchType.Title)
                        continue;
                    var strings = profile.MatchString.Split('|');
                    foreach (var s in strings)
                    {
                        if (aircraft.Title.Contains(s, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Information($"Loading Profile '{profile.Name}' (matched on Title/Livery - '{aircraft.Title}' contains '{s}')");
                            return profile;
                        }
                    }
                }

                foreach (var profile in AircraftProfiles)
                {
                    if (profile.MatchType != ProfileMatchType.Airline)
                        continue;
                    var strings = profile.MatchString.Split('|');
                    foreach (var s in strings)
                    {
                        if (!AppService.Instance.GsxService.IsMsfs2024 && aircraft.Airline.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Information($"Loading Profile '{profile.Name}' (matched on Airline - '{aircraft.Airline}' starts with '{s}')");
                            return profile;
                        }
                        else if (AppService.Instance.GsxService.IsMsfs2024 && aircraft.Title.Contains(s, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Logger.Information($"Loading Profile '{profile.Name}' (matched on Livery - '{aircraft.Title}' contains '{s}')");
                            return profile;
                        }
                    }
                }
            }

            Logger.Information($"Loading default Aircraft Profile");
            return AircraftProfiles.Where(p => p.Name == "default").First() ?? new AircraftProfile();
        }

        public virtual void SetDisplayUnit(DisplayUnit displayUnit)
        {
            bool notify = DisplayUnitCurrent != displayUnit;
            DisplayUnitCurrent = displayUnit;
            if (notify)
                NotifyDisplayUnit();
        }

        public virtual double ConvertKgToDisplayUnit(double kg)
        {
            if (DisplayUnitCurrent == DisplayUnit.KG)
                return kg;
            else
                return kg * WeightConversion;
        }

        public virtual double ConvertLbToDisplayUnit(double lb)
        {
            if (DisplayUnitCurrent == DisplayUnit.LB)
                return lb;
            else
                return lb / WeightConversion;
        }

        public virtual double ConvertFromDisplayUnitKg(double value)
        {
            if (DisplayUnitCurrent == DisplayUnit.KG)
                return value;
            else
                return value / WeightConversion;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void NotifyDisplayUnit()
        {
            NotifyPropertyChanged(nameof(DisplayUnitCurrent));
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
        }

        public virtual void EvaluateDisplayUnit()
        {
            if (DisplayUnitSource == DisplayUnitSource.App && DisplayUnitCurrent != DisplayUnitDefault)
            {
                DisplayUnitCurrent = DisplayUnitDefault;
                NotifyDisplayUnit();
            }
            else if (AppService.Instance?.SimConnect?.IsSessionRunning == true
                    && DisplayUnitSource == DisplayUnitSource.Aircraft && AppService.Instance?.GsxService?.AircraftInterface?.IsLoaded == true
                    && AppService.Instance?.GsxService?.AircraftInterface?.UnitAircraft != DisplayUnitCurrent)
            {
                DisplayUnitCurrent = AppService.Instance.GsxService.AircraftInterface.UnitAircraft;
                NotifyDisplayUnit();
            }
            else if (AppService.Instance?.SimConnect?.IsSessionRunning == false && DisplayUnitCurrent != DisplayUnitDefault)
            {
                DisplayUnitCurrent = DisplayUnitDefault;
                NotifyDisplayUnit();
            }
        }
    }
}
