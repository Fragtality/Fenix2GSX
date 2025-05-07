using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ModelAutomation : ModelBase<AircraftProfile>
    {
        protected virtual DispatcherTimer UpdateTimer { get; }
        public override AircraftProfile Source => GsxController?.AircraftProfile;

        public ModelAutomation(AppService appService) : base(appService.GsxService?.AircraftProfile, appService)
        {
            UpdateTimer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100),
            };
            UpdateTimer.Tick += UpdateTimer_Tick;

            DepartureServices = new ModelDepartureServices(this) { AddAllowed = false };
            DepartureServices.CollectionChanged += (_, _) => SaveConfig();

            OperatorPreferences = new ModelOperatorPreferences(this);
            OperatorPreferences.CollectionChanged += (_, _) => SaveConfig();

            CompanyHubs = new ModelCompanyHubs(this);
            CompanyHubs.CollectionChanged += (_, _) => SaveConfig();
        }

        protected virtual void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            DepartureServices.NotifyCollectionChanged();
            OperatorPreferences.NotifyCollectionChanged();
            CompanyHubs.NotifyCollectionChanged();
            UpdateTimer.Stop();
        }

        protected override void InitializeModel()
        {
            GsxController.ProfileChanged += OnProfileChanged;
            Config.PropertyChanged += OnPropertyChanged;
        }

        protected virtual void OnProfileChanged(AircraftProfile profile)
        {
            NotifyPropertyChanged(string.Empty);
            UpdateTimer.Start();
        }

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(DisplayUnit))
            {
                NotifyPropertyChanged(nameof(RefuelRateKgSec));
            }
        }

        public virtual string ProfileName => Source?.Name;

        //Gate & Doors
        public virtual bool DoorStairHandling { get => Source.DoorStairHandling; set => SetModelValue<bool>(value); }
        public virtual bool DoorStairIncludeL2 { get => Source.DoorStairIncludeL2; set => SetModelValue<bool>(value); }
        public virtual bool DoorCargoHandling { get => Source.DoorCargoHandling; set => SetModelValue<bool>(value); }
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
        public virtual int ConnectPca { get => Source.ConnectPca; set => SetModelValue<int>(value); }
        public virtual int ChockDelayMin { get => Source.ChockDelayMin; set => SetModelValue<int>(value); }
        public virtual int ChockDelayMax { get => Source.ChockDelayMax; set => SetModelValue<int>(value); }

        //GSX Services
        public virtual bool CallReposition { get => Source.CallReposition; set => SetModelValue<bool>(value); }
        public virtual bool CallDeboardOnArrival { get => Source.CallDeboardOnArrival; set => SetModelValue<bool>(value); }
        public virtual ModelDepartureServices DepartureServices { get; }
        public virtual Dictionary<GsxServiceActivation, string> TextServiceActivations => ServiceConfig.TextServiceActivations;
        public virtual Dictionary<GsxServiceConstraint, string> TextServiceConstraints => ServiceConfig.TextServiceConstraints;

        public virtual double RefuelRateKgSec { get => ConvertKgToDisplayUnit(Source.RefuelRateKgSec); set => SetModelValue<double>(ConvertFromDisplayUnitKg(value)); }
        public virtual bool UseFixedRefuelRate => !UseRefuelTimeTarget;
        public virtual bool UseRefuelTimeTarget { get => Source.UseRefuelTimeTarget; set { SetModelValue<bool>(value); NotifyPropertyChanged(nameof(UseFixedRefuelRate)); } }
        public virtual int RefuelTimeTargetSeconds { get => Source.RefuelTimeTargetSeconds; set => SetModelValue<int>(value); }
        public virtual bool SkipFuelOnTankering { get => Source.SkipFuelOnTankering; set => SetModelValue<bool>(value); }

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
        public virtual int FinalDelayMin { get => Source.FinalDelayMin; set => SetModelValue<int>(value); }
        public virtual int FinalDelayMax { get => Source.FinalDelayMax; set => SetModelValue<int>(value); }
        public virtual bool FuelSaveLoadFob { get => Source.FuelSaveLoadFob; set => SetModelValue<bool>(value); }
        public virtual bool RandomizePax { get => Source.RandomizePax; set => SetModelValue<bool>(value); }
        public virtual double ChancePerSeat { get => Source.ChancePerSeat * 100.0; set => SetModelValue<double>(value / 100.0); }
    }
}
