using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX;
using Fenix2GSX.GSX.Services;
using FenixInterface;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Fenix2GSX.Aircraft
{
    public class AircraftInterface
    {
        protected virtual GsxController Controller { get; }
        protected virtual SimConnectManager SimConnect => Fenix2GSX.Instance.AppService.SimConnect;
        public virtual Config Config => Controller.Config;
        public virtual AircraftProfile Profile => Controller.AircraftProfile;
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => Controller.GsxServices;
        protected virtual FenixAircraftInterface FenixInterface { get; }
        public virtual bool IsInitialized { get; protected set; } = false;
        
        protected virtual ISimResourceSubscription SubAirline { get; set; }
        protected virtual ISimResourceSubscription SubTitle { get; set; }
        protected virtual ISimResourceSubscription SubLivery { get; set; }
        protected virtual ISimResourceSubscription SubSpeed { get; set; }

        public virtual string Airline => SubAirline?.GetString();
        public virtual string Title => !string.IsNullOrWhiteSpace(SubLivery?.GetString()) ? SubLivery.GetString() : SubTitle?.GetString() ?? "";
        public virtual string Registration => FenixInterface.Registration;
        public virtual bool IsFlightPlanLoaded => FenixInterface.IsFlightPlanLoaded;
        public virtual bool IsLoaded => FenixInterface.IsLoaded;
        public virtual bool IsRefueling => FenixInterface.IsRefueling;
        public virtual DisplayUnit UnitAircraft => FenixInterface.UnitAircraft;
        public virtual string SimbriefUser => FenixInterface.SimbriefUser;
        public virtual TimeSpan FlightDuration => FenixInterface.FlightDuration;
        public virtual bool IsEfbBoardingCompleted => !string.IsNullOrWhiteSpace(EfbBoardingState) && (EfbBoardingState?.Equals("ended", System.StringComparison.InvariantCultureIgnoreCase) == true || EfbBoardingState?.Equals("completed", System.StringComparison.InvariantCultureIgnoreCase) == true);
        public virtual string EfbBoardingState => FenixInterface?.EfbBoardingState;
        public virtual double FuelCurrent => FenixInterface.FuelCurrent;
        public virtual double FuelTarget => FenixInterface.FuelTarget;
        public virtual bool SmartButtonRequest => FenixInterface.SmartButtonRequest;
        public virtual int GroundSpeed => (int)SubSpeed.GetNumber();
        public virtual bool EquipmentGpu => FenixInterface.GetGpuState();
        public virtual bool EquipmentPca => FenixInterface.GetPcaState();
        public virtual bool EquipmentChocks => FenixInterface.GetChocksState();
        public virtual bool EnginesRunning => FenixInterface.GetEnginesRunning();
        public virtual bool IsFinalReceived => FenixInterface.GetFinalReceived();
        public virtual bool IsExternalPowerConnected => FenixInterface.GetExternalPowerConnected();
        public virtual bool IsApuBleedOn => FenixInterface.IsApuBleedOn;
        public virtual bool HasOpenDoors => FenixInterface.GetOpenDoors();
        public virtual bool IsBrakeSet => FenixInterface.GetBrake();
        public virtual bool LightNav => FenixInterface.GetLightNav();
        public virtual bool LightBeacon => FenixInterface.GetLightBeacon();

        public AircraftInterface(GsxController controller)
        {
            Controller = controller;
            FenixInterface = new FenixAircraftInterface(Controller);
        }

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                SubAirline = SimStore.AddVariable("ATC AIRLINE", SimUnitType.String);
                SubTitle = SimStore.AddVariable("TITLE", SimUnitType.String);
                if (Sys.GetProcessRunning(Config.BinaryMsfs2024))
                    SubLivery = SimStore.AddVariable("LIVERY NAME", SimUnitType.String);
                SubSpeed = SimStore.AddVariable("GPS GROUND SPEED", SimUnitType.Knots);
                
                SimStore.AddVariable(FenixConstants.VarAcpIntCallCpt, SimUnitType.Number);
                SimStore.AddVariable(FenixConstants.VarAcpIntCallFo, SimUnitType.Number);

                Controller.WalkaroundWasSkipped += OnWalkaroundWasSkipped;
                Controller.AutomationController.OnStateChange += OnAutomationState;
                Controller.GsxServices[GsxServiceType.Stairs].OnStateChanged += OnStairChange;
                (Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel).OnActive += OnRefuelActive;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnActive += OnBoardingActive;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCompleted += OnBoardingCompleted;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnPaxChange += OnPaxChangeBoarding;
                (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCargoChange += OnCargoChangeBoarding;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnActive += OnDeboardingActive;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCompleted += OnDeboardingCompleted;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnPaxChange += OnPaxChangeDeboarding;
                (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCargoChange += OnCargoChangeDeboarding;

                FenixInterface.Init();

                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            FenixInterface.FreeResources();

            Controller.WalkaroundWasSkipped -= OnWalkaroundWasSkipped;
            Controller.AutomationController.OnStateChange -= OnAutomationState;
            Controller.GsxServices[GsxServiceType.Stairs].OnStateChanged -= OnStairChange;
            (Controller.GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel).OnActive -= OnRefuelActive;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnActive -= OnBoardingActive;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCompleted -= OnBoardingCompleted;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnPaxChange -= OnPaxChangeBoarding;
            (Controller.GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding).OnCargoChange -= OnCargoChangeBoarding;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnActive -= OnDeboardingActive;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCompleted -= OnDeboardingCompleted;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnPaxChange -= OnPaxChangeDeboarding;
            (Controller.GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding).OnCargoChange -= OnCargoChangeDeboarding;

            SimStore.Remove(FenixConstants.VarAcpIntCallCpt);
            SimStore.Remove(FenixConstants.VarAcpIntCallFo);

            SimStore.Remove("ATC AIRLINE");
            SimStore.Remove("TITLE");
            if (Sys.GetProcessRunning(Config.BinaryMsfs2024))
                SimStore.Remove("LIVERY NAME");
            SimStore.Remove("GPS GROUND SPEED");
        }

        public virtual void Run()
        {
            FenixInterface.Run();
        }

        public virtual void Stop()
        {
            Reset();
            FenixInterface.Stop();
        }

        public virtual async Task ResetSmartButton()
        {
            await FenixInterface.ResetSmartButton();
        }

        public virtual void Reset()
        {
            FenixInterface.SmartButtonRequest = false;
        }

        public virtual void ResetFlight()
        {
            FenixInterface.ResetFlight();
        }

        public virtual async Task UnloadOfp()
        {
            await FenixInterface.UnloadOfp();
        }

        protected virtual async Task OnWalkaroundWasSkipped()
        {
            await FenixInterface.OnStatePreparation();
        }

        protected virtual void OnAutomationState(AutomationState state)
        {
            if (state == AutomationState.TaxiOut)
                FenixInterface.OnStateTaxiOut();
            else if (state == AutomationState.TaxiIn)
                FenixInterface.OnStateTaxiIn();
            else if (state == AutomationState.Arrival)
                FenixInterface.OnStateArrival();
        }

        protected virtual async void OnStairChange(GsxService service)
        {
            await FenixInterface.OnStairChange((int)GsxServices[GsxServiceType.Stairs].State, (int)GsxServices[GsxServiceType.Jetway].State);
        }

        protected virtual async void OnRefuelActive(GsxService service)
        {
            await FenixInterface.OnRefuelActive();
        }

        protected virtual async void OnBoardingActive(GsxService service)
        {
            await FenixInterface.BoardingStart();
        }

        protected virtual async void OnBoardingCompleted(GsxService service)
        {
            await FenixInterface.BoardingStop();
        }

        protected virtual async void OnPaxChangeBoarding(GsxServiceBoarding service)
        {
            await FenixInterface.OnPaxChangeBoarding(service.PaxTotal);
        }

        protected virtual async void OnCargoChangeBoarding(GsxServiceBoarding service)
        {
            await FenixInterface.OnCargoChangeBoarding(service.CargoPercent);
        }

        protected virtual void OnDeboardingActive(GsxService service)
        {
            FenixInterface.OnDeboardingActive();
        }

        protected virtual async void OnDeboardingCompleted(GsxService service)
        {
            await FenixInterface.OnDeboardingCompleted();
        }

        protected virtual async void OnPaxChangeDeboarding(GsxServiceDeboarding serviceDeboarding)
        {
            await FenixInterface.OnPaxChangeDeboarding(serviceDeboarding.PaxTotal);
        }

        protected virtual async void OnCargoChangeDeboarding(GsxServiceDeboarding service)
        {
            await FenixInterface.OnCargoChangeDeboarding(service.CargoPercent);
        }

        public virtual async Task SetPca(bool set)
        {
            await FenixInterface.SetPca(set);
        }

        public virtual async Task SetChocks(bool set, bool force = false)
        {
            await FenixInterface.SetChocks(set, force);
        }

        public virtual async Task SetGroundPower(bool set, bool force = false)
        {
            await FenixInterface.SetGroundPower(set, force);
        }

        public virtual async Task CloseAllDoors()
        {
            await FenixInterface.CloseAllDoors();
        }

        public virtual int GetPaxBoarding()
        {
            return FenixInterface.GetPaxBoarding();
        }

        public virtual int GetPaxDeboarding()
        {
            return FenixInterface.GetPaxDeboarding();
        }

        public virtual void RefuelStart()
        {
            FenixInterface.RefuelStart();
        }

        public virtual async Task RefuelStop()
        {
            await FenixInterface.RefuelStop();
        }

        public virtual async Task RefuelComplete()
        {
            await FenixInterface.RefuelComplete();
        }

        public virtual async Task DingCabin()
        {
            await FenixInterface.DingCabin();
        }

        public virtual async Task FlashMechCall()
        {
            Logger.Debug($"Flash Mech Indicator");
            int seconds = 10;
            double value;

            for (int i = 0; i <= seconds; i++)
            {
                value = seconds % 2 == 0 ? 1 : 0;
                await SimStore[FenixConstants.VarAcpIntCallCpt].WriteValue(value);
                await SimStore[FenixConstants.VarAcpIntCallFo].WriteValue(value);
                await Task.Delay(1000, Controller.Token);
            }

            await SimStore[FenixConstants.VarAcpIntCallCpt].WriteValue(0);
            await SimStore[FenixConstants.VarAcpIntCallFo].WriteValue(0);
        }
    }
}
