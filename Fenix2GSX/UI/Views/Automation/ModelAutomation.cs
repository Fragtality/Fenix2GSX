using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Services;
using FenixInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ModelAutomation : ModelBase<AircraftProfile>
    {
        protected virtual DispatcherTimer ProfileUpdateTimer { get; }
        protected virtual DispatcherTimer UnitUpdateTimer { get; }
        public override AircraftProfile Source => GsxController?.AircraftProfile;

        public ModelAutomation(AppService appService) : base(appService.GsxService?.AircraftProfile, appService)
        {
            ProfileUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            ProfileUpdateTimer.Tick += ProfileUpdateTimer_Tick;

            UnitUpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UnitUpdateTimer.Tick += UnitUpdateTimer_Tick;

            DepartureServices = new ModelDepartureServices(this) { AddAllowed = false };
            DepartureServices.CollectionChanged += (_, _) => SaveConfig();

            OperatorPreferences = new ModelOperatorPreferences(this);
            OperatorPreferences.CollectionChanged += (_, _) => SaveConfig();

            CompanyHubs = new ModelCompanyHubs(this);
            CompanyHubs.CollectionChanged += (_, _) => SaveConfig();
        }

        protected virtual void ProfileUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            DepartureServices.NotifyCollectionChanged();
            OperatorPreferences.NotifyCollectionChanged();
            CompanyHubs.NotifyCollectionChanged();
            InhibitConfigSave = false;
            ProfileUpdateTimer.Stop();
        }

        protected virtual void UnitUpdateTimer_Tick(object? sender, EventArgs e)
        {
            InhibitConfigSave = true;
            NotifyPropertyChanged(nameof(RefuelRateKgSec));
            NotifyPropertyChanged(nameof(DisplayUnitCurrentString));
            InhibitConfigSave = false;
            UnitUpdateTimer.Stop();
        }

        protected override void InitializeModel()
        {
            GsxController.ProfileChanged += OnProfileChanged;
            Config.PropertyChanged += OnConfigPropertyChanged;
        }

        protected virtual void OnProfileChanged(AircraftProfile profile)
        {
            NotifyPropertyChanged(string.Empty);
            ProfileUpdateTimer.Start();
        }

        protected virtual void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if ((e?.PropertyName == nameof(Config.DisplayUnitCurrent) || e?.PropertyName == nameof(Config.DisplayUnitCurrentString))
                && !UnitUpdateTimer.IsEnabled)
                UnitUpdateTimer.Start();
            if (e?.PropertyName == nameof(Config.CurrentProfile))
                NotifyPropertyChanged(nameof(ProfileName));
        }

        protected virtual void NotifyRefuelMethod()
        {
            NotifyPropertyChanged(nameof(RefuelFixedVisible));
            NotifyPropertyChanged(nameof(RefuelDynamicVisible));
            NotifyPropertyChanged(nameof(RefuelPanelVisible));
        }

        public virtual string ProfileName => Config?.CurrentProfile?.Name ?? "";
        public virtual string DisplayUnitCurrentString => Config.DisplayUnitCurrentString;

        //Gate & Doors
        public virtual bool DoorStairHandling { get => Source.DoorStairHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorStairIncludeL2 { get => Source.DoorStairIncludeL2; set => SetModelValue<bool>(value); }
        public virtual bool DoorCargoHandling { get => Source.DoorCargoHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorOpenBoardActive { get => Source.DoorOpenBoardActive; set => SetModelValue<bool>(value); }
        public virtual bool DoorsCargoKeepOpenOnLoaded { get => Source.DoorsCargoKeepOpenOnLoaded; set => SetModelValue<bool>(value); }
        public virtual bool DoorsCargoKeepOpenOnUnloaded { get => Source.DoorsCargoKeepOpenOnUnloaded; set => SetModelValue<bool>(value); }
        public virtual bool CloseDoorsOnFinal { get => Source.CloseDoorsOnFinal; set => SetModelValue<bool>(value); }

        public virtual bool CallJetwayStairsOnPrep { get => Source.CallJetwayStairsOnPrep; set => SetModelValue<bool>(value); }
        public virtual bool CallJetwayStairsDuringDeparture { get => Source.CallJetwayStairsDuringDeparture; set => SetModelValue<bool>(value); }
        public virtual bool CallJetwayStairsOnArrival { get => Source.CallJetwayStairsOnArrival; set => SetModelValue<bool>(value); }
        public virtual int RemoveStairsAfterDepature { get => Source.RemoveStairsAfterDepature; set => SetModelValue<int>(value); }
        public virtual bool RemoveJetwayStairsOnFinal { get => Source.RemoveJetwayStairsOnFinal; set => SetModelValue<bool>(value); }

        //Ground Equipment
        public virtual bool ClearGroundEquipOnBeacon { get => Source.ClearGroundEquipOnBeacon; set => SetModelValue<bool>(value); }
        public virtual bool GradualGroundEquipRemoval { get => Source.GradualGroundEquipRemoval; set => SetModelValue<bool>(value); }
        public virtual bool ConnectGpuWithApuRunning { get => Source.ConnectGpuWithApuRunning; set => SetModelValue<bool>(value); }
        public virtual int ConnectPca { get => Source.ConnectPca; set => SetModelValue<int>(value); }
        public virtual bool PcaOverride { get => Source.PcaOverride; set => SetModelValue<bool>(value); }
        public virtual int ChockDelayMin { get => Source.ChockDelayMin;
            set
            { 
                if (value < ChockDelayMax)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(ChockDelayMin));
            }
        }
        public virtual int ChockDelayMax { get => Source.ChockDelayMax;
            set
            {
                if (value > ChockDelayMin)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(ChockDelayMax));
            }
        }

        //GSX Services
        public virtual bool CallReposition { get => Source.CallReposition; set => SetModelValue<bool>(value); }
        public virtual bool CallDeboardOnArrival { get => Source.CallDeboardOnArrival; set => SetModelValue<bool>(value); }
        public virtual bool RunDepartureOnArrival { get => Source.RunDepartureOnArrival; set => SetModelValue<bool>(value); }
        public virtual ModelDepartureServices DepartureServices { get; }
        public virtual Dictionary<GsxServiceActivation, string> TextServiceActivations => ServiceConfig.TextServiceActivations;
        public virtual Dictionary<GsxServiceConstraint, string> TextServiceConstraints => ServiceConfig.TextServiceConstraints;

        public virtual RefuelMethod RefuelMethod { get => Source.RefuelMethod; set { SetModelValue<RefuelMethod>(value); NotifyRefuelMethod(); } }
        public virtual Dictionary<RefuelMethod, string> RefuelMethodOptions { get; } = new()
        {
            { RefuelMethod.FixedRate, "Fixed Rate" },
            { RefuelMethod.DynamicRate, "Dynamic Rate" },
            { RefuelMethod.RefuelPanel, "Refuel Panel" },
        };
        public virtual double RefuelRateKgSec { get => Config.ConvertKgToDisplayUnit(Source.RefuelRateKgSec); set => SetModelValue<double>(Config.ConvertFromDisplayUnitKg(value)); }
        public virtual bool RefuelFixedVisible => RefuelMethod == RefuelMethod.FixedRate;
        public virtual bool RefuelDynamicVisible => RefuelMethod == RefuelMethod.DynamicRate;
        public virtual bool RefuelPanelVisible => RefuelMethod == RefuelMethod.RefuelPanel;
        public virtual int RefuelTimeTargetSeconds { get => Source.RefuelTimeTargetSeconds; set => SetModelValue<int>(value); }
        public virtual bool SkipFuelOnTankering { get => Source.SkipFuelOnTankering; set => SetModelValue<bool>(value); }
        public virtual bool RefuelFinishOnHose { get => Source.RefuelFinishOnHose; set => SetModelValue<bool>(value); }

        public virtual Dictionary<int, string> TugOptions { get; } = new()
        {
            { 0, "Not Answer" },
            { 1, "No" },
            { 2, "Yes" },
        };
        public virtual int AttachTugDuringBoarding { get => Source.AttachTugDuringBoarding; set => SetModelValue<int>(value); }
        public virtual int CallPushbackWhenTugAttached { get => Source.CallPushbackWhenTugAttached; set => SetModelValue<int>(value); }
        public virtual bool CallPushbackOnBeacon { get => Source.CallPushbackOnBeacon; set => SetModelValue<bool>(value); }

        //Operator Selection
        public virtual bool OperatorAutoSelect { get => Source.OperatorAutoSelect; set => SetModelValue<bool>(value); }
        public virtual ModelOperatorPreferences OperatorPreferences { get; }

        //Company Hubs
        public virtual ModelCompanyHubs CompanyHubs { get; }

        //Skip Questions
        public virtual bool SkipCrewQuestion { get => Source.SkipCrewQuestion; set => SetModelValue<bool>(value); }
        public virtual bool SkipFollowMe { get => Source.SkipFollowMe; set => SetModelValue<bool>(value); }
        public virtual bool KeepDirectionMenuOpen { get => Source.KeepDirectionMenuOpen; set => SetModelValue<bool>(value); }
        public virtual bool AnswerCabinCallGround { get => Source.AnswerCabinCallGround; set => SetModelValue<bool>(value); }
        public virtual bool AnswerCabinCallAir { get => Source.AnswerCabinCallAir; set => SetModelValue<bool>(value); }

        //Aircraft / OFP
        public virtual int FinalDelayMin { get => Source.FinalDelayMin;
            set
            {
                if (value < FinalDelayMax)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(FinalDelayMin));
            }
        }
        public virtual int FinalDelayMax { get => Source.FinalDelayMax;
            set
            {
                if (value > FinalDelayMin)
                    SetModelValue<int>(value);
                else
                    OnPropertyChanged(nameof(FinalDelayMax));
            }
        }
        public virtual bool FuelSaveLoadFob { get => Source.FuelSaveLoadFob; set => SetModelValue<bool>(value); }
        public virtual bool RandomizePax { get => Source.RandomizePax; set => SetModelValue<bool>(value); }
        public virtual double ChancePerSeat { get => Source.ChancePerSeat * 100.0; set => SetModelValue<double>(value / 100.0); }
    }
}
