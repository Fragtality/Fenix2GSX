using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using Fenix2GSX.Aircraft;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Menu;
using Fenix2GSX.GSX.Services;
using FenixInterface;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX
{
    //public enum AutomationState
    //{
    //    SessionStart = 0,
    //    Preparation = 1,
    //    Departure = 2,
    //    PushBack = 3,
    //    TaxiOut = 4,
    //    Flight = 5,
    //    TaxiIn = 6,
    //    Arrival = 7,
    //    TurnAround = 8,
    //}

    public class GsxAutomationController(GsxController controller)
    {
        protected virtual GsxController Controller { get; } = controller;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        protected virtual AircraftInterface Aircraft => Controller.AircraftInterface;
        protected virtual bool SmartButtonRequest => Aircraft.SmartButtonRequest;
        protected virtual SimConnectManager SimConnect => Fenix2GSX.Instance.AppService.SimConnect;
        protected virtual CancellationToken Token => Controller.Token;
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual Config Config => Controller.Config;
        protected virtual AircraftProfile Profile => Controller.AircraftProfile;
        public virtual string DepartureIcao { get; protected set; } = "";
        public virtual bool IsInitialized { get; protected set; } = false;
        public virtual bool RunFlag { get; protected set; } = false;
        public virtual bool IsStarted { get; protected set; } = false;
        public virtual AutomationState State { get; protected set; } = AutomationState.SessionStart;
        public virtual AutomationState LastState { get; protected set; } = AutomationState.SessionStart;
        public virtual bool HasStateChanged => State != LastState;
        protected virtual bool SimGroundState => Controller.IsOnGround;
        public virtual bool IsOnGround { get; set; } = true;

        protected virtual IEnumerator DepartureServicesEnumerator { get; set; }
        protected virtual ServiceConfig DepartureServicesCurrent => ((KeyValuePair<int, ServiceConfig>)DepartureServicesEnumerator.Current).Value;
        protected virtual List<GsxService> DepartureServicesCalled { get; } = [];
        public virtual int ServiceCountCompleted { get; protected set; } = 0;
        public virtual int ServiceCountRunning { get; protected set; } = 0;
        public virtual int ServiceCountTotal { get; protected set; } = 0;
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => Controller.GsxServices;
        protected virtual GsxServiceReposition ServiceReposition => GsxServices[GsxServiceType.Reposition] as GsxServiceReposition;
        protected virtual GsxServiceRefuel ServiceRefuel => GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel;
        protected virtual GsxServiceCatering ServiceCatering => GsxServices[GsxServiceType.Catering] as GsxServiceCatering;
        protected virtual GsxServiceJetway ServiceJetway => GsxServices[GsxServiceType.Jetway] as GsxServiceJetway;
        protected virtual GsxServiceStairs ServiceStairs => GsxServices[GsxServiceType.Stairs] as GsxServiceStairs;
        protected virtual GsxServicePushback ServicePushBack => GsxServices[GsxServiceType.Pushback] as GsxServicePushback;
        protected virtual GsxServiceBoarding ServiceBoard => GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding;
        protected virtual GsxServiceDeboarding ServiceDeboard => GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding;
        protected virtual GsxServiceDeice ServiceDeice => GsxServices[GsxServiceType.Deice] as GsxServiceDeice;
        public virtual bool IsGateConnected => ServiceJetway.IsConnected || ServiceStairs.IsConnected;
        public virtual bool HasDepartBypassed => Controller.GsxServices[GsxServiceType.Refuel].State == GsxServiceState.Bypassed || Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Bypassed;
        public virtual bool ServicesValid => ServiceStairs.State != GsxServiceState.Unknown || ServiceJetway.State != GsxServiceState.Unknown || !IsOnGround;

        public virtual bool ExecutedReposition { get; protected set; } = false;
        public virtual bool DepartureServicesCompleted { get; protected set; } = false;
        public virtual bool GroundEquipmentPlaced { get; protected set; } = false;
        public virtual bool GroundEquipmentClear => !Aircraft.EquipmentChocks && !Aircraft.EquipmentGpu && !Aircraft.EquipmentPca;
        public virtual bool JetwayStairRemoved { get; protected set; } = false;
        public virtual int ChockDelay { get; protected set; } = 0;
        public virtual bool ChockFlashed { get; protected set; } = false;
        public virtual bool CabinDinged { get; protected set; } = false;
        public virtual string FlightPlanId => Aircraft.FenixInterface.FlightPlanId;
        public virtual string OfpArrivalId { get; protected set; } = "0";
        public virtual bool RunDepartureOnArrival { get; protected set; } = false;

        public event Action<AutomationState> OnStateChange;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                Controller.MsgCouatlStarted.OnMessage += OnCouatlStarted;
                Controller.MsgCouatlStopped.OnMessage += OnCouatlStopped;
                IsInitialized = true;
            }
        }

        public virtual void FreeResources()
        {
            Controller.MsgCouatlStarted.OnMessage -= OnCouatlStarted;
            Controller.MsgCouatlStopped.OnMessage -= OnCouatlStopped;
        }

        public virtual void Reset()
        {
            IsStarted = false;
            RunFlag = false;
            State = AutomationState.SessionStart;

            foreach (var service in GsxServices)
                service.Value.ResetState();

            IsOnGround = true;

            ExecutedReposition = false;
            GroundEquipmentPlaced = false;
            JetwayStairRemoved = false;
            ChockDelay = 0;
            ChockFlashed = false;
            CabinDinged = false;
            DepartureIcao = "";
            OfpArrivalId = "0";
            ServiceCountRunning = 0;
            ServiceCountCompleted = 0;
            ServiceCountTotal = 0;

            DepartureServicesCompleted = false;
            RunDepartureOnArrival = false;
            DepartureServicesCalled?.Clear();
            if (Profile?.DepartureServices != null)
            {
                DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
                DepartureServicesEnumerator.MoveNext();

                foreach (var activation in Profile.DepartureServices.Values)
                    activation.ActivationCount = 0;
            }
        }

        protected virtual void ResetFlight()
        {
            Controller.Menu.ResetFlight();
            foreach (var service in GsxServices)
                service.Value.ResetState();

            Aircraft.ResetFlight();
            GroundEquipmentPlaced = false;
            JetwayStairRemoved = false;
            ChockDelay = 0;
            ChockFlashed = false;
            CabinDinged = false;
            DepartureIcao = "";
            OfpArrivalId = "0";

            DepartureServicesCompleted = false;
            RunDepartureOnArrival = false;
            DepartureServicesCalled.Clear();
            DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
            DepartureServicesEnumerator.MoveNext();
        }

        public virtual async Task Run()
        {
            try
            {
                IsStarted = true;
                RunFlag = true;
                DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
                DepartureServicesEnumerator.MoveNext();
                foreach (var activation in Profile.DepartureServices.Values)
                    activation.ActivationCount = 0;
                Logger.Information($"Automation Service started");

                while (RunFlag && Controller.IsActive && !Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                {
                    Logger.Verbose($"Automation Tick - State: {State} | ServicesValid: {ServicesValid}");
                    if (Controller.IsGsxRunning && Controller.CanAutomationRun)
                    {
                        await EvaluateState();
                        if (Config.RunAutomationService && ServicesValid && Controller.Menu.FirstReadyReceived)
                            await RunServices();
                        if (SmartButtonRequest)
                            await Aircraft.ResetSmartButton();
                    }

                    LastState = State;
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
                IsStarted = false;
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            Logger.Information($"Automation Service ended");
        }

        public virtual void Stop()
        {
            IsStarted = false;
            RunFlag = false;
        }

        protected virtual async Task EvaluateState()
        {
            ServiceCountRunning = 0;
            ServiceCountCompleted = 0;
            ServiceCountTotal = 0;

            //Session Start => Prep / Push / Taxi-Out / Flight
            if (State == AutomationState.SessionStart)
            {
                if (!IsOnGround || Config.DebugArrival)
                {
                    Logger.Debug($"Starting in {AutomationState.Flight} - IsOnGround {Controller.IsOnGround} | DebugArrival {Config.DebugArrival}");
                    StateChange(AutomationState.Flight);
                }
                else if (Aircraft.EnginesRunning || Aircraft.LightBeacon || ServicePushBack.PushStatus > 0)
                {
                    if ((Aircraft.LightBeacon && !Aircraft.EnginesRunning) || ServicePushBack.PushStatus > 0)
                    {
                        Logger.Debug($"Starting in {AutomationState.PushBack} - Beacon {Aircraft.LightBeacon} | PushStatus {ServicePushBack.PushStatus > 0}");
                        StateChange(AutomationState.PushBack);
                    }
                    else
                    {
                        Logger.Debug($"Starting in {AutomationState.TaxiOut} - EnginesRunning {Aircraft.EnginesRunning}");
                        StateChange(AutomationState.TaxiOut);
                    }
                }
                else if (Aircraft?.IsFlightPlanLoaded == true)
                {
                    if (string.IsNullOrWhiteSpace(DepartureIcao))
                        DepartureIcao = await Controller.Flightplan.GetDestinationIcao();
                    if (!string.IsNullOrWhiteSpace(DepartureIcao) && Controller?.Menu?.IsGateMenu == true)
                        StateChange(AutomationState.Departure);
                }
                else if (Aircraft?.IsLoaded == true && Controller?.SkippedWalkAround == true && Controller?.Menu?.IsGateMenu == true)
                    StateChange(AutomationState.Preparation);
            }
            //intercept Flight
            else if (State < AutomationState.Flight && !IsOnGround)
            {
                StateChange(AutomationState.Flight);
                ResetFlight();
            }
            //intercept TaxiOut
            else if (State < AutomationState.TaxiOut && Aircraft.EnginesRunning && ServicePushBack.State != GsxServiceState.Active && Aircraft.GroundSpeed > 1)
            {
                Logger.Debug($"Intercepting Taxi Out!");
                StateChange(AutomationState.TaxiOut);
            }
            //Preparation => Departure
            else if (State == AutomationState.Preparation)
            {
                if (ExecutedReposition && Aircraft.IsFlightPlanLoaded && IsGateConnected)
                {
                    CabinDinged = false;
                    if (string.IsNullOrWhiteSpace(DepartureIcao))
                        DepartureIcao = await Controller.Flightplan.GetDestinationIcao();
                    if (!string.IsNullOrWhiteSpace(DepartureIcao))
                    {
                        StateChange(AutomationState.Departure);
                        await ServiceBoard.SetPaxTarget(Aircraft.GetPaxBoarding());
                    }
                }
            }
            //Departure => PushBack
            else if (State == AutomationState.Departure)
            {
                bool pushbackTrigger = ServicePushBack.PushStatus > 0 && (ServicePushBack.IsActive || HasDepartBypassed);
                if (DepartureServicesCompleted || pushbackTrigger)
                {
                    if (pushbackTrigger)
                        Logger.Information($"Pushback Service already running - skipping Departure");
                    StateChange(AutomationState.PushBack);
                }
                else
                {
                    foreach (var serviceCfg in Profile.DepartureServices.Values)
                    {
                        if (serviceCfg.ServiceActivation == GsxServiceActivation.Skip)
                            continue;

                        if (Controller.GsxServices[serviceCfg.ServiceType].IsActive)
                            ServiceCountRunning++;
                        else if (Controller.GsxServices[serviceCfg.ServiceType].IsRunning)
                            ServiceCountRunning++;
                        else if (Controller.GsxServices[serviceCfg.ServiceType].IsCompleted || Controller.GsxServices[serviceCfg.ServiceType].IsSkipped)
                            ServiceCountCompleted++;

                        ServiceCountTotal++;
                    }
                }
            }
            //PushBack => TaxiOut
            else if (State == AutomationState.PushBack)
            {
                if ((ServicePushBack.IsCompleted && Aircraft.EnginesRunning && GroundEquipmentClear)
                    || (GroundEquipmentClear && !Controller.IsWalkaround && Aircraft.EnginesRunning && ServicePushBack.PushStatus == 0 && Aircraft.GroundSpeed >= Config.SpeedTresholdTaxiOut))
                    StateChange(AutomationState.TaxiOut);
            }
            //Flight => TaxiIn
            else if (State == AutomationState.Flight)
            {
                if (IsOnGround && Aircraft.GroundSpeed < Config.SpeedTresholdTaxiIn)
                {
                    if (Config.RestartGsxOnTaxiIn)
                    {
                        Logger.Information($"Restarting GSX on Taxi-In");
                        await AppService.Instance.RestartGsx();
                    }
                    StateChange(AutomationState.TaxiIn);
                }
            }
            //TaxiIn => Arrival
            else if (State == AutomationState.TaxiIn)
            {
                if (!Aircraft.EnginesRunning && Aircraft.IsBrakeSet && !Aircraft.LightBeacon)
                {
                    await Controller.Menu.OpenHide();
                    OfpArrivalId = FlightPlanId;
                    StateChange(AutomationState.Arrival);
                    await ServiceDeboard.SetPaxTarget(Aircraft.GetPaxDeboarding());
                }
            }
            //Arrival => Turnaround (or Departure)
            else if (State == AutomationState.Arrival)
            {
                if (ServiceDeboard.IsCompleted)
                {
                    if (!RunDepartureOnArrival)
                        await SetTurnaround();
                    else
                    {
                        await Aircraft.UnloadOfp(false);
                        Controller.Menu.SuppressMenuRefresh = false;
                        await SkipTurn(AutomationState.Departure);
                    }
                }
            }
            //Turnaround => Departure
            else if (State == AutomationState.TurnAround)
            {
                if (Aircraft.IsFlightPlanLoaded && IsOnGround && !Aircraft.EnginesRunning)
                {
                    await SkipTurn(AutomationState.Departure);
                }
                else if (SmartButtonRequest)
                {
                    Logger.Information("Skip Turnaround Phase (SmarButton Request)");
                    await SkipTurn(AutomationState.Departure);
                }
                else if (ServiceRefuel.IsRunning || ServiceCatering.IsRunning || ServiceBoard.IsRunning)
                {
                    Logger.Warning($"Departure Services already running! Skipping Turnaround");
                    await SkipTurn(AutomationState.Departure);
                }
                else if (ServicePushBack.IsRunning)
                {
                    Logger.Warning($"Pushback Service already running! Skipping Turnaround");
                    await SkipTurn(AutomationState.PushBack);
                }
            }
        }

        protected virtual async Task SetTurnaround()
        {
            await Aircraft.UnloadOfp();
            StateChange(AutomationState.TurnAround);
            if (Config.DingOnTurnaround)
                await Aircraft.DingCabin();
            Controller.Menu.SuppressMenuRefresh = false;
        }

        protected virtual async Task SkipTurn(AutomationState state)
        {
            await Controller.ReloadSimbrief();
            if (string.IsNullOrWhiteSpace(DepartureIcao))
                DepartureIcao = await Controller.Flightplan.GetDestinationIcao();
            StateChange(AutomationState.Departure);
        }

        protected virtual void StateChange(AutomationState newState)
        {
            Logger.Information($"State Change: {State} => {newState}");
            State = newState;
            TaskTools.RunLogged(() => OnStateChange?.Invoke(State), RequestToken);
        }

        public virtual async void OnCouatlStarted(MsgGsxCouatlStarted msg)
        {
            try
            {
                if (!IsStarted)
                    return;

                if (State == AutomationState.Departure && !DepartureServicesCompleted)
                {
                    if (!DepartureServicesEnumerator.CheckEnumeratorValid())
                    {
                        Controller.GsxServices[GsxServiceType.Boarding].ForceComplete();
                        Logger.Information($"GSX Restart on last Departure Service detected - skip to Pushback");
                        StateChange(AutomationState.PushBack);
                    }
                    else if (DepartureServicesCalled.Any(s => s.Type == GsxServiceType.Refuel && s.WasActive && !s.WasCompleted) && !RunDepartureOnArrival)
                    {
                        Logger.Information($"GSX Restart during Departure Service detected - completing Refuel");
                        Controller.GsxServices[GsxServiceType.Refuel].ForceComplete();
                    }
                }

                if (State == AutomationState.Arrival && ServiceDeboard.WasActive && !ServiceDeboard.WasCompleted)
                {
                    Controller.GsxServices[GsxServiceType.Deboarding].ForceComplete();
                    Logger.Information($"GSX Restart on Deboarding Service detected - skip to Turnaround");
                    await SetTurnaround();
                    await Aircraft.OnDeboardingCompleted(Controller.GsxServices[GsxServiceType.Deboarding]);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
        }

        public virtual void OnCouatlStopped(MsgGsxCouatlStopped msg)
        {
            if (!IsStarted)
                return;
        }

        protected virtual async Task RunServices()
        {
            if (State == AutomationState.Preparation)
            {
                await RunPreparation();
            }
            else if (State == AutomationState.Departure)
            {
                await RunDeparture();
            }
            else if (State == AutomationState.PushBack)
            {
                await RunPushback();
            }
            else if (State == AutomationState.Arrival)
            {
                await RunArrival();
            }
        }

        protected virtual async Task RunPreparation()
        {
            if (!ExecutedReposition && !IsGateConnected && !Aircraft.IsFlightPlanLoaded && !Controller.Menu.WarpedToGate)
            {
                if (Profile.CallReposition)
                {
                    Logger.Information("Automation: Reposition Aircraft on Gate");
                    await ServiceReposition.Call();
                    await Task.Delay(2000, RequestToken);
                }
            }
            ExecutedReposition = ServiceReposition.IsCompleted || IsGateConnected || !Profile.CallReposition || Aircraft.IsFlightPlanLoaded || Controller.Menu.WarpedToGate;

            if (ExecutedReposition && Controller.SkippedWalkAround && !GroundEquipmentPlaced && ServicePushBack.PushStatus == 0 && !Aircraft.EnginesRunning)
            {
                if (!Profile.CallReposition)
                    await Task.Delay(3000);
                Logger.Information("Automation: Placing Ground Equipment");
                await Aircraft.SetChocks(false, true);
                await Task.Delay(1000);
                await Aircraft.SetChocks(true);
                await Aircraft.SetGroundPower(true);
                if (Profile.ConnectPca == 1 || (Profile.ConnectPca == 2 && ServiceJetway.State != GsxServiceState.NotAvailable))
                {
                    Logger.Information($"Connecting PCA");
                    await Aircraft.SetPca(true);
                }
                GroundEquipmentPlaced = true;
            }

            if (ExecutedReposition && Controller.SkippedWalkAround && ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled
                && (Profile.CallJetwayStairsOnPrep || SmartButtonRequest))
            {
                Logger.Information("Automation: Call Jetway on Preparation");
                await Controller.AircraftInterface.FenixInterface.SetStairsFwd(false);
                await ServiceJetway.Call();
            }

            if (ExecutedReposition && Controller.SkippedWalkAround && ServiceStairs.IsAvailable && !ServiceStairs.IsConnected && !ServiceStairs.IsCalled
                && (Profile.CallJetwayStairsOnPrep || SmartButtonRequest))
            {
                Logger.Information("Automation: Call Stairs on Preparation");
                await Controller.AircraftInterface.FenixInterface.SetStairsAft(false);
                await ServiceStairs.Call();
            }

            if (!CabinDinged && ExecutedReposition && (ServiceJetway.IsRunning || ServiceStairs.IsRunning))
            {
                if (Config.DingOnStartup)
                    await Aircraft.DingCabin();
                CabinDinged = true;
            }
        }

        protected virtual async Task RunDeparture()
        {
            if (Aircraft.IsApuBleedOn && Aircraft.EquipmentPca && State == AutomationState.Departure)
            {
                Logger.Information($"Disconnecting PCA");
                await Aircraft.SetPca(false);
            }

            if (!DepartureServicesCompleted)
            {
                if (!DepartureServicesEnumerator.CheckEnumeratorValid())
                {
                    DepartureServicesCompleted = DepartureServicesCalled.All(s => s.IsCompleted || s.IsSkipped);
                    if (DepartureServicesCompleted)
                    {
                        Logger.Information($"Automation: All Departure Services completed");
                        if (ServiceStairs.IsConnected && (ServiceJetway.IsConnected && Profile.RemoveStairsAfterDepature == 2) || (!ServiceJetway.IsConnected && Profile.RemoveStairsAfterDepature == 1))
                        {
                            Logger.Information($"Automation: Remove Stairs after Departure Services");
                            await ServiceStairs.Remove();
                        }
                    }
                }
                else if (Aircraft.IsEfbBoardingCompleted && !ServiceBoard.IsCalled && State == AutomationState.Departure)
                {
                    Logger.Information($"EFB Boarding detected - skipping all Departure Services {Aircraft.EfbBoardingState}");
                    DepartureServicesCompleted = true;
                }
            }

            if (!DepartureServicesCompleted && !IsGateConnected && Profile.CallJetwayStairsDuringDeparture)
            {
                if (ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled)
                {
                    Logger.Information("Automation: Call Jetway during Departure");
                    await ServiceJetway.Call();
                }

                if (ServiceStairs.IsAvailable && !ServiceStairs.IsConnected && !ServiceStairs.IsCalled)
                {
                    Logger.Information("Automation: Call Stairs during Departure");
                    await ServiceStairs.Call();
                }
            }

            if (DepartureServicesEnumerator.CheckEnumeratorValid() && !DepartureServicesCompleted)
            {
                GsxService current = GsxServices[DepartureServicesCurrent.ServiceType];
                GsxServiceActivation activation = DepartureServicesCurrent.ServiceActivation;
                if (!current.IsCalled && !current.IsCompleted && !current.IsRunning && (!DepartureServicesCurrent.HasDurationConstraint || Aircraft.FlightDuration >= DepartureServicesCurrent.MinimumFlightDuration))
                {
                    if (Profile.SkipFuelOnTankering && activation != GsxServiceActivation.Skip && current is GsxServiceRefuel && Aircraft.FuelTarget > 0 && Aircraft.FuelCurrent >= Aircraft.FuelTarget - Config.FuelCompareVariance)
                    {
                        Logger.Information($"Automation: Skip Refuel because FOB is greater than planned");
                        MoveDepartureQueue(current, true);
                    }
                    else if (activation == GsxServiceActivation.Skip)
                    {
                        Logger.Debug($"Skipping Service {DepartureServicesCurrent.ServiceType}");
                        DepartureServicesEnumerator.MoveNext();
                    }
                    else if (DepartureServicesCurrent.ActivationCount > 0 && DepartureServicesCurrent.ServiceConstraint == GsxServiceConstraint.FirstLeg)
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Constraint '{DepartureServicesCurrent.ServiceConstraintName}'");
                        MoveDepartureQueue(current, true);
                    }
                    else if (DepartureServicesCurrent.ActivationCount == 0 && DepartureServicesCurrent.ServiceConstraint == GsxServiceConstraint.TurnAround)
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Constraint '{DepartureServicesCurrent.ServiceConstraintName}'");
                        MoveDepartureQueue(current, true);
                    }
                    else if (DepartureServicesCurrent.ServiceConstraint == GsxServiceConstraint.CompanyHub && !Profile.IsCompanyHub(DepartureIcao))
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Constraint '{DepartureServicesCurrent.ServiceConstraintName}'");
                        MoveDepartureQueue(current, true);
                    }
                    else if (SmartButtonRequest
                            || activation == GsxServiceActivation.AfterCalled
                            || (activation == GsxServiceActivation.AfterRequested && (DepartureServicesCalled?.Count == 0 || DepartureServicesCalled?.SafeLast()?.State >= GsxServiceState.Requested || DepartureServicesCalled?.SafeLast()?.IsSkipped == true))
                            || (activation == GsxServiceActivation.AfterActive && (DepartureServicesCalled?.Count == 0 || DepartureServicesCalled?.SafeLast()?.State >= GsxServiceState.Active || DepartureServicesCalled?.SafeLast()?.IsSkipped == true))
                            || (activation == GsxServiceActivation.AfterPrevCompleted && (DepartureServicesCalled?.Count == 0 || DepartureServicesCalled?.SafeLast()?.IsCompleted == true || DepartureServicesCalled?.SafeLast()?.IsSkipped == true))
                            || (activation == GsxServiceActivation.AfterAllCompleted && (DepartureServicesCalled?.Count == 0 || DepartureServicesCalled.All(s => s.IsCompleted || s.IsSkipped))))
                    {
                        if (DepartureServicesCurrent.ServiceType == GsxServiceType.Boarding)
                            await ServiceBoard.SetPaxTarget(Aircraft.GetPaxBoarding());

                        Logger.Information($"Automation: Call Departure Service {DepartureServicesCurrent.ServiceType}");
                        await current.Call();
                        if (current.IsCalled)
                            MoveDepartureQueue(current);
                    }
                }
                else
                {
                    bool skipped = false;
                    if (current.IsCompleted)
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} already completed");
                    else if (current.IsCalled || current.IsRunning)
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} called externally");
                    }
                    else
                    {
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} skipped due to Time Constraint");
                        skipped = true;
                    }

                    MoveDepartureQueue(current, skipped);
                }
            }
        }

        protected virtual void MoveDepartureQueue(GsxService service, bool asSkipped = false)
        {
            DepartureServicesCalled.Add(service);
            DepartureServicesCurrent.ActivationCount++;
            if (asSkipped)
                service.IsSkipped = true;
            DepartureServicesEnumerator.MoveNext();
        }

        protected virtual async Task RunPushback()
        {
            if (Aircraft.IsApuBleedOn && Aircraft.EquipmentPca)
            {
                Logger.Information($"Disconnecting PCA");
                await Aircraft.SetPca(false);
            }

            if (Aircraft.HasOpenDoors && !Aircraft.FenixInterface.CargoDoorsMoving() && Aircraft.IsFinalReceived && Profile.CloseDoorsOnFinal)
            {
                Logger.Information($"Automation: Close Doors on Final Loadsheet");
                await Aircraft.CloseAllDoors();
                await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
            }

            if (Profile.GradualGroundEquipRemoval)
            {
                if (Aircraft.EquipmentGpu && !Aircraft.IsExternalPowerConnected)
                {
                    Logger.Information($"Automation: Removing GPU on External Power disconnect");
                    await Aircraft.SetGroundPower(false);
                }

                if (Aircraft.EquipmentChocks && Aircraft.IsBrakeSet && !Aircraft.EquipmentGpu)
                {
                    Logger.Information($"Automation: Removing Chocks on Parking Brake set");
                    await Aircraft.SetChocks(false);
                }
            }

            if (!JetwayStairRemoved && IsGateConnected && Aircraft.IsFinalReceived && Profile.RemoveJetwayStairsOnFinal)
            {
                Logger.Information($"Automation: Remove Jetway/Stairs on Final Loadsheet");
                await ServiceJetway.Remove();
                await ServiceStairs.Remove();
                JetwayStairRemoved = true;
                await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
            }

            if (GroundEquipmentPlaced && !Aircraft.IsExternalPowerConnected && Aircraft.IsBrakeSet && Aircraft.LightBeacon)
            {
                if (Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Ground Equipment on Beacon");
                    await SetGroundEquip(false);
                    GroundEquipmentPlaced = false;
                }
                
                if (Profile.CallPushbackOnBeacon && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
                {
                    Logger.Information($"Automation: Call Pushback (Beacon / Prepared for Push)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                }
                else if (IsGateConnected && Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Jetway/Stairs on Beacon");
                    await ServiceJetway.Remove();
                    await ServiceStairs.Remove();
                    JetwayStairRemoved = true;
                    await Task.Delay(Config.StateMachineInterval * 2, RequestToken);
                }
            }
            else if (GroundEquipmentClear)
                GroundEquipmentPlaced = false;

            if (ServicePushBack.TugAttachedOnBoarding && Profile.CallPushbackWhenTugAttached > 0 && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
            {
                if (Profile.CallPushbackWhenTugAttached == 1)
                {
                    Logger.Information($"Automation: Call Pushback after Departure Services (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
                else if (Profile.CallPushbackWhenTugAttached == 2 && Aircraft.IsFinalReceived)
                {
                    Logger.Information($"Automation: Call Pushback after Final LS (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, RequestToken);
                }
            }

            if (Aircraft.HasOpenDoors && !Aircraft.FenixInterface.CargoDoorsMoving() 
                && ((ServicePushBack.PushStatus > 0 && ServicePushBack.IsRunning) || ServiceDeice.IsActive || Aircraft.EnginesRunning))
            {
                if ((ServicePushBack.PushStatus > 0 && ServicePushBack.IsRunning))
                    Logger.Information($"Automation: Close Doors on Pushback");
                else if (ServiceDeice.IsActive)
                    Logger.Information($"Automation: Close Doors on Deice");
                else
                    Logger.Information($"Automation: Close Doors on Engine running");

                await Aircraft.CloseAllDoors();
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }

            if (GroundEquipmentPlaced && ((ServicePushBack.PushStatus > 1 && ServicePushBack.IsRunning) || ServiceDeice.IsActive))
            {
                if (!ServiceDeice.IsActive)
                    Logger.Information($"Automation: Remove Ground Equipment on Pushback");
                else
                    Logger.Information($"Automation: Remove Ground Equipment on Deice");

                await SetGroundEquip(false);
                GroundEquipmentPlaced = false;
                await Task.Delay(Config.StateMachineInterval, RequestToken);
            }

            if ((ServicePushBack.PushStatus > 1 || ServiceDeice.IsActive || Aircraft.EnginesRunning) && !GroundEquipmentPlaced)
            {
                string reason = "for Pushback";
                if (ServiceDeice.IsActive)
                    reason = "for De-Ice";
                else if (Aircraft.EnginesRunning)
                    reason = "because Engines Running";

                if (Aircraft.EquipmentGpu && !Aircraft.IsExternalPowerConnected)
                {
                    Logger.Information($"Automation: Remove GPU {reason}");
                    await Aircraft.SetGroundPower(false);
                }

                if (Aircraft.EquipmentChocks && Aircraft.IsBrakeSet)
                {
                    Logger.Information($"Automation: Remove Chocks {reason}");
                    await Aircraft.SetChocks(false);
                }

                if (Aircraft.HasOpenDoors && !Aircraft.FenixInterface.CargoDoorsMoving())
                {
                    Logger.Information($"Automation: Close all Doors {reason}");
                    await Aircraft.CloseAllDoors();
                }
            }

            if (SmartButtonRequest)
            {
                Logger.Debug($"INT/RAD on Push ({ServicePushBack.PushStatus})");
                if (!ServicePushBack.IsCalled || (ServicePushBack.IsTugConnected && Controller.Menu.MenuState == GsxMenuState.TIMEOUT))
                {
                    await ServicePushBack.Call();
                    if (GroundEquipmentPlaced)
                    {
                        Logger.Information($"Automation: Remove Ground Equipment on INT/RAD");
                        await SetGroundEquip(false);
                        GroundEquipmentPlaced = false;
                    }
                }
                else if (ServicePushBack.PushStatus >= 5 && ServicePushBack.PushStatus < 9)
                    await ServicePushBack.EndPushback();
            }
            else if (Profile.KeepDirectionMenuOpen && ServicePushBack.State == GsxServiceState.Callable && ServicePushBack.IsTugConnected && Controller.Menu.MenuState == GsxMenuState.TIMEOUT)
            {
                Logger.Debug($"Reopen Direction Menu");
                await ServicePushBack.Call();
                await Controller.Menu.MsgMenuReady.ReceiveAsync();
                if (Controller.Menu.MatchTitle(GsxConstants.MenuPushbackInterrupt) && Controller.Menu.MenuLines[2].StartsWith(GsxConstants.MenuPushbackChange, StringComparison.InvariantCultureIgnoreCase))
                    await Controller.Menu.Select(3, false, false);
            }
        }

        protected virtual async Task SetGroundEquip(bool set)
        {
            await Aircraft.SetChocks(set);
            if (!set)
                await Aircraft.SetPca(set);
            await Aircraft.SetGroundPower(set);
        }

        protected virtual async Task RunArrival()
        {
            if (ChockDelay == 0)
            {
                ChockDelay = new Random().Next(Profile.ChockDelayMin, Profile.ChockDelayMax);
                Logger.Information($"Automation: Placing Chocks on Arrival (ETA {ChockDelay}s)");
                _ = Task.Delay(ChockDelay * 1000).ContinueWith((_) => Aircraft.SetChocks(true));
                _ = Task.Delay(60000, RequestToken).ContinueWith(async (_) =>
                {
                    if (!Aircraft.EquipmentGpu)
                    {
                        Logger.Warning($"Failback: Setting GPU after Deboard was called");
                        await Aircraft.SetGroundPower(true, true);
                    }
                });
            }

            if (Aircraft.EquipmentChocks && !ChockFlashed)
            {
                _ = Aircraft.FlashMechCall();
                ChockFlashed = true;
            }

            if (!Aircraft.EquipmentGpu && (IsGateConnected || ServiceDeboard.IsActive))
            {
                Logger.Information($"Automation: Placing GPU on Arrival");
                await Aircraft.SetGroundPower(true);
                await Task.Delay(1000, RequestToken);
            }

            if (!Aircraft.EquipmentPca && Profile.ConnectPca > 0 && (IsGateConnected || ServiceDeboard.IsActive))
            {
                if (Profile.ConnectPca == 1 || (Profile.ConnectPca == 2 && ServiceJetway.State != GsxServiceState.NotAvailable))
                {
                    Logger.Information($"Automation: Placing PCA on Arrival");
                    await Aircraft.SetPca(true);
                    await Task.Delay(1000, RequestToken);
                }
            }

            GroundEquipmentPlaced = Aircraft.EquipmentGpu && Aircraft.EquipmentChocks;

            if (!ServiceDeboard.IsCalled &&
                (Profile.CallDeboardOnArrival || (!Profile.CallDeboardOnArrival && SmartButtonRequest)))
            {
                if (SmartButtonRequest)
                    Logger.Information("Call Deboard by INT/RAD");
                else
                    Logger.Information("Automation: Call Deboard on Arrival");
                await ServiceDeboard.Call();
            }
            else if (!ServiceDeboard.IsCalled && !IsGateConnected && Profile.CallJetwayStairsOnArrival)
            {
                if (ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled)
                {
                    Logger.Information("Automation: Call Jetway on Arrival");
                    await ServiceJetway.Call();
                }

                if (!ServiceStairs.IsConnected && !ServiceStairs.IsCalled)
                {
                    Logger.Information("Automation: Call Stairs on Arrival");
                    await ServiceStairs.Call();
                }
            }

            if (Profile.RunDepartureOnArrival && !RunDepartureOnArrival && ServiceDeboard.IsActive && OfpArrivalId != FlightPlanId)
            {
                Logger.Information("Automation: Run Departure Services during Arrival");
                RunDepartureOnArrival = true;
            }

            if (RunDepartureOnArrival && DepartureServicesCurrent.ServiceType != GsxServiceType.Boarding)
            {
                await RunDeparture();
            }
        }
    }
}
