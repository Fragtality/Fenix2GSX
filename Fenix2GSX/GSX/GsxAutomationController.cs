using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using Fenix2GSX.Aircraft;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Menu;
using Fenix2GSX.GSX.Services;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX
{
    public enum AutomationState
    {
        SessionStart = 0,
        Preparation = 1,
        Departure = 2,
        PushBack = 3,
        TaxiOut = 4,
        Flight = 5,
        TaxiIn = 6,
        Arrival = 7,
        TurnAround = 8,
    }

    public class GsxAutomationController(GsxController controller)
    {
        protected virtual GsxController Controller { get; } = controller;
        protected virtual AircraftInterface Aircraft => Controller.AircraftInterface;
        protected virtual bool SmartButtonRequest => Aircraft.SmartButtonRequest;
        protected virtual SimConnectManager SimConnect => Fenix2GSX.Instance.AppService.SimConnect;
        protected virtual CancellationToken Token => Controller.Token;
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual Config Config => Controller.Config;
        protected virtual AircraftProfile Profile => Controller.AircraftProfile;
        public virtual string DepartureIcao { get; protected set; } = "";
        public virtual bool IsInitialized { get; protected set; } = false;
        protected virtual bool RunFlag { get; set; } = true;
        public virtual bool IsStarted { get; protected set; } = false;
        public virtual AutomationState State { get; protected set; } = AutomationState.SessionStart;
        public virtual AutomationState LastState { get; protected set; } = AutomationState.SessionStart;
        public virtual bool HasStateChanged => State != LastState;
        protected virtual bool SimGroundState => Controller.IsOnGround;
        public virtual bool IsOnGround { get; protected set; } = true;
        protected virtual int GroundCounter { get; set; } = 0;

        protected virtual IEnumerator DepartureServicesEnumerator { get; set; }
        protected virtual ServiceConfig DepartureServicesCurrent => ((KeyValuePair<int, ServiceConfig>)DepartureServicesEnumerator.Current).Value;
        protected virtual List<GsxService> DepartureServicesCalled { get; } = [];
        public virtual int CountDepartureServicesQueued => Profile?.DepartureServices?.Where(s => s.Value.ServiceActivation != GsxServiceActivation.Skip)?.Count() ?? 0;
        public virtual int CountDepartureServicesCompleted => DepartureServicesCalled?.Where(s => s.State == GsxServiceState.Completed || s.IsSkipped)?.Count() ?? 0;
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => Controller.GsxServices;
        protected virtual GsxServiceReposition ServiceReposition => GsxServices[GsxServiceType.Reposition] as GsxServiceReposition;
        protected virtual GsxServiceRefuel ServiceRefuel => GsxServices[GsxServiceType.Refuel] as GsxServiceRefuel;
        protected virtual GsxServiceJetway ServiceJetway => GsxServices[GsxServiceType.Jetway] as GsxServiceJetway;
        protected virtual GsxServiceStairs ServiceStairs => GsxServices[GsxServiceType.Stairs] as GsxServiceStairs;
        protected virtual GsxServicePushback ServicePushBack => GsxServices[GsxServiceType.Pushback] as GsxServicePushback;
        protected virtual GsxServiceBoarding ServiceBoard => GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding;
        protected virtual GsxServiceDeboarding ServiceDeboard => GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding;
        protected virtual GsxServiceDeice ServiceDeice => GsxServices[GsxServiceType.Deice] as GsxServiceDeice;
        public virtual bool IsGateConnected => ServiceJetway.IsConnected || ServiceStairs.IsConnected;
        public virtual bool ServicesValid => ServiceStairs.State != GsxServiceState.Unknown && ServiceJetway.State != GsxServiceState.Unknown || !IsOnGround;

        public virtual bool ExecutedReposition { get; protected set; } = false;
        public virtual bool DepartureServicesCompleted { get; protected set; } = false;
        public virtual bool GroundEquipmentPlaced { get; protected set; } = false;
        public virtual bool GroundEquipmentClear => !Aircraft.EquipmentChocks && !Aircraft.EquipmentGpu && !Aircraft.EquipmentPca;
        public virtual bool JetwayStairRemoved { get; protected set; } = false;
        public virtual int ChockDelay { get; protected set; } = 0;
        public virtual bool ChockFlashed { get; protected set; } = false;
        public virtual bool CabinDinged { get; protected set; } = false;

        public event Action<AutomationState> OnStateChange;

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                this.OnStateChange += CheckStateChange;
            }
        }

        public virtual void FreeResources()
        {

        }

        public virtual void Reset()
        {
            IsStarted = false;
            RunFlag = true;
            State = AutomationState.SessionStart;

            foreach (var service in GsxServices)
                service.Value.ResetState();

            GroundCounter = 0;
            IsOnGround = true;

            ExecutedReposition = false;
            GroundEquipmentPlaced = false;
            JetwayStairRemoved = false;
            ChockDelay = 0;
            ChockFlashed = false;
            CabinDinged = false;
            DepartureIcao = "";

            DepartureServicesCompleted = false;
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
            GroundCounter = 0;

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

            DepartureServicesCompleted = false;
            DepartureServicesCalled.Clear();
            DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
            DepartureServicesEnumerator.MoveNext();
        }

        public virtual async Task Run()
        {
            IsStarted = true;
            RunFlag = true;
            DepartureServicesEnumerator = Profile.DepartureServices.GetEnumerator();
            DepartureServicesEnumerator.MoveNext();
            foreach (var activation in Profile.DepartureServices.Values)
                activation.ActivationCount = 0;
            Logger.Information($"Automation Service started");

            while (RunFlag && Controller.IsActive && !Controller.Token.IsCancellationRequested)
            {
                CheckGround();
                Logger.Verbose($"Automation Tick - State: {State} | ServicesValid: {ServicesValid}");
                if (Controller.IsGsxRunning && Controller.Menu.FirstReadyReceived && GroundCounter == 0)
                {
                    await EvaluateState();
                    if (Config.RunAutomationService && ServicesValid)
                        await RunServices();
                    if (SmartButtonRequest)
                        Aircraft.ResetSmartButton();
                }

                LastState = State;
                await Task.Delay(Config.StateMachineInterval, Token);
            }
            IsStarted = false;

            Logger.Information($"Automation Service ended");
        }

        public virtual void Stop()
        {
            IsStarted = false;
            RunFlag = false;
        }

        protected virtual void CheckGround()
        {
            if (SimGroundState != IsOnGround && !Controller.IsWalkaround)
            {
                GroundCounter++;
                if (GroundCounter > Config.GroundTicks)
                {
                    GroundCounter = 0;
                    IsOnGround = SimGroundState;
                    Logger.Information($"On Ground State changed: {(IsOnGround ? "On Ground" : "In Flight")}");
                }
            }
            else if (SimGroundState == IsOnGround && GroundCounter > 0)
                GroundCounter = 0;
        }

        protected virtual async void CheckStateChange(AutomationState state)
        {
            if (state == AutomationState.Departure)
                DepartureIcao = await Controller.Flightplan.GetDestinationIcao();
        }

        protected virtual async Task EvaluateState()
        {
            //Session Start => Prep / Push / Taxi-Out / Flight
            if (State == AutomationState.SessionStart)
            {
                if (!Controller.IsOnGround || Config.DebugArrival)
                    StateChange(AutomationState.Flight);
                else if (Aircraft.EnginesRunning || Aircraft.LightBeacon || ServicePushBack.PushStatus > 0)
                {
                    if ((Aircraft.LightBeacon && !Aircraft.EnginesRunning) || ServicePushBack.PushStatus > 0)
                        StateChange(AutomationState.PushBack);
                    else
                        StateChange(AutomationState.TaxiOut);
                }
                else if (Aircraft?.IsFlightPlanLoaded == true)
                    StateChange(AutomationState.Departure);
                else if (Aircraft?.IsLoaded == true)
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
                    StateChange(AutomationState.Departure);
                    ServiceBoard.SetPaxTarget(Aircraft.GetPaxBoarding());
                }
            }
            //Departure => PushBack
            else if (State == AutomationState.Departure)
            {
                if (DepartureServicesCompleted)
                    StateChange(AutomationState.PushBack);
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
                    StateChange(AutomationState.Arrival);
                    ServiceDeboard.SetPaxTarget(Aircraft.GetPaxDeboarding());
                }
            }
            //Arrival => Turnaround
            else if (State == AutomationState.Arrival)
            {
                if (ServiceDeboard.IsCompleted)
                {
                    Aircraft.UnloadOfp();
                    StateChange(AutomationState.TurnAround);
                    if (Config.DingOnTurnaround)
                        Aircraft.DingCabin();
                }
            }
            //Turnaround => Departure
            else if (State == AutomationState.TurnAround)
            {
                if (Aircraft.IsFlightPlanLoaded && IsOnGround && !Aircraft.EnginesRunning)
                {
                    await Controller.ReloadSimbrief();
                    StateChange(AutomationState.Departure);                    
                }
            }
        }

        protected virtual void StateChange(AutomationState newState)
        {
            Logger.Information($"State Change: {State} => {newState}");
            State = newState;
            TaskTools.RunLogged(() => OnStateChange?.Invoke(State), Controller.Token);
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
            if (!ExecutedReposition && !IsGateConnected && !Aircraft.IsFlightPlanLoaded)
            {
                if (Profile.CallReposition)
                {
                    Logger.Information("Automation: Reposition Aircraft on Gate");
                    await ServiceReposition.Call();
                    await Task.Delay(1000, Token);
                }
            }
            ExecutedReposition = ServiceReposition.IsCompleted || IsGateConnected || !Profile.CallReposition || Aircraft.IsFlightPlanLoaded;

            if (ExecutedReposition && Controller.SkippedWalkAround && !GroundEquipmentPlaced && ServicePushBack.PushStatus == 0 && !Aircraft.EnginesRunning)
            {
                Logger.Information("Automation: Placing Ground Equipment");
                Aircraft.SetChocks(false);
                Aircraft.SetChocks(true);
                Aircraft.SetGroundPower(true);
                if (Profile.ConnectPca == 1 || (Profile.ConnectPca == 2 && ServiceJetway.State != GsxServiceState.NotAvailable))
                {
                    Logger.Information($"Connecting PCA");
                    Aircraft.SetPca(true);
                }
                GroundEquipmentPlaced = true;
            }

            if (ExecutedReposition && Controller.SkippedWalkAround && ServiceJetway.IsAvailable && !ServiceJetway.IsConnected && !ServiceJetway.IsCalled
                && (Profile.CallJetwayStairsOnPrep || SmartButtonRequest))
            {
                Logger.Information("Automation: Call Jetway on Preparation");
                await ServiceJetway.Call();
            }

            if (ExecutedReposition && Controller.SkippedWalkAround && ServiceStairs.IsAvailable && !ServiceStairs.IsConnected && !ServiceStairs.IsCalled
                && (Profile.CallJetwayStairsOnPrep || SmartButtonRequest))
            {
                Logger.Information("Automation: Call Stairs on Preparation");
                await ServiceStairs.Call();
            }

            if (!CabinDinged && ExecutedReposition && (ServiceJetway.IsRunning || ServiceStairs.IsRunning))
            {
                if (Config.DingOnStartup)
                    Aircraft.DingCabin();
                CabinDinged = true;
            }
        }

        protected virtual async Task RunDeparture()
        {
            if (Aircraft.IsApuBleedOn && Aircraft.EquipmentPca)
            {
                Logger.Information($"Disconnecting PCA");
                Aircraft.SetPca(false);
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
                else if (Aircraft.IsEfbBoardingCompleted && !ServiceBoard.IsCalled)
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
                            || activation == GsxServiceActivation.AfterRequested && (DepartureServicesCalled.Last()?.State >= GsxServiceState.Requested || DepartureServicesCalled.Last()?.IsSkipped == true || DepartureServicesCalled.Count == 0)
                            || activation == GsxServiceActivation.AfterActive && (DepartureServicesCalled.Last()?.State >= GsxServiceState.Active || DepartureServicesCalled.Last()?.IsSkipped == true || DepartureServicesCalled.Count == 0)
                            || (activation == GsxServiceActivation.AfterPrevCompleted && (DepartureServicesCalled.Last()?.IsCompleted == true || DepartureServicesCalled.Last()?.IsSkipped == true || DepartureServicesCalled.Count == 0))
                            || (activation == GsxServiceActivation.AfterAllCompleted && DepartureServicesCalled.All(s => s.IsCompleted || s.IsSkipped)))
                    {
                        if (DepartureServicesCurrent.ServiceType == GsxServiceType.Boarding)
                            ServiceBoard.SetPaxTarget(Aircraft.GetPaxBoarding());
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
                        Logger.Information($"Automation: Departure Service {DepartureServicesCurrent.ServiceType} called externally");
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
                Aircraft.SetPca(false);
            }

            if (Aircraft.HasOpenDoors && Aircraft.IsFinalReceived && Profile.CloseDoorsOnFinal)
            {
                Logger.Information($"Automation: Close Doors on Final Loadsheet");
                Aircraft.CloseAllDoors();
                await Task.Delay(Config.StateMachineInterval * 2, Token);
            }

            if (!JetwayStairRemoved && IsGateConnected && Aircraft.IsFinalReceived && Profile.RemoveJetwayStairsOnFinal)
            {
                Logger.Information($"Automation: Remove Jetway/Stairs on Final Loadsheet");
                await ServiceJetway.Remove();
                await ServiceStairs.Remove();
                JetwayStairRemoved = true;
                await Task.Delay(Config.StateMachineInterval * 2, Token);
            }

            if (GroundEquipmentPlaced && !Aircraft.IsExternalPowerConnected && Aircraft.IsBrakeSet && Aircraft.LightBeacon)
            {
                if (Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Ground Equipment on Beacon");
                    SetGroundEquip(false);
                }
                GroundEquipmentPlaced = false;
                if (Profile.CallPushbackOnBeacon && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
                {
                    Logger.Information($"Automation: Call Pushback (Beacon / Prepared for Push)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval * 2, Token);
                }
                else if (IsGateConnected && Profile.ClearGroundEquipOnBeacon)
                {
                    Logger.Information($"Automation: Remove Jetway/Stairs on Beacon");
                    await ServiceJetway.Remove();
                    await ServiceStairs.Remove();
                    JetwayStairRemoved = true;
                    await Task.Delay(Config.StateMachineInterval * 2, Token);
                }
            }

            if (ServicePushBack.TugAttachedOnBoarding && Profile.CallPushbackWhenTugAttached > 0 && !ServicePushBack.IsCalled && ServicePushBack.State < GsxServiceState.Requested)
            {
                if (Profile.CallPushbackWhenTugAttached == 1)
                {
                    Logger.Information($"Automation: Call Pushback after Departure Services (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, Token);
                }
                else if (Profile.CallPushbackWhenTugAttached == 2 && Aircraft.IsFinalReceived)
                {
                    Logger.Information($"Automation: Call Pushback after Final LS (Tug already attached)");
                    await ServicePushBack.Call();
                    await Task.Delay(Config.StateMachineInterval, Token);
                }
            }

            if (Aircraft.HasOpenDoors && (ServicePushBack.PushStatus > 0 || ServiceDeice.IsActive || Aircraft.EnginesRunning))
            {
                Logger.Information($"Automation: Close Doors on Pushback");
                Aircraft.CloseAllDoors();
                await Task.Delay(Config.StateMachineInterval, Token);
            }

            if (GroundEquipmentPlaced && (ServicePushBack.PushStatus > 1 || ServicePushBack.State == GsxServiceState.Completed || ServiceDeice.IsActive))
            {
                Logger.Information($"Automation: Remove Ground Equipment on Pushback");
                SetGroundEquip(false);
                GroundEquipmentPlaced = false;
                await Task.Delay(Config.StateMachineInterval, Token);
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
                    Aircraft.SetGroundPower(false);
                }

                if (Aircraft.EquipmentChocks && Aircraft.IsBrakeSet)
                {
                    Logger.Information($"Automation: Remove Chocks {reason}");
                    Aircraft.SetChocks(false);
                }

                if (Aircraft.HasOpenDoors)
                {
                    Logger.Information($"Automation: Close all Doors {reason}");
                    Aircraft.CloseAllDoors();
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
                        SetGroundEquip(false);
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

        protected virtual void SetGroundEquip(bool set)
        {
            Aircraft.SetChocks(set);
            if (!set)
                Aircraft.SetPca(set);
            Aircraft.SetGroundPower(set);
        }

        protected virtual async Task RunArrival()
        {
            if (ChockDelay == 0)
            {
                ChockDelay = new Random().Next(Profile.ChockDelayMin, Profile.ChockDelayMax);
                Logger.Information($"Automation: Placing Chocks on Arrival (ETA {ChockDelay}s)");
                _ = Task.Delay(ChockDelay * 1000).ContinueWith((_) => Aircraft.SetChocks(true));
            }

            if (Aircraft.EquipmentChocks && !ChockFlashed)
            {
                _ = Aircraft.FlashMechCall();
                ChockFlashed = true;
            }

            if (!Aircraft.EquipmentGpu && (IsGateConnected || ServiceDeboard.IsActive))
            {
                Logger.Information($"Automation: Placing GPU on Arrival");
                Aircraft.SetGroundPower(true);
                await Task.Delay(1000, Controller.Token);
            }

            if (!Aircraft.EquipmentPca && Profile.ConnectPca > 0 && (IsGateConnected || ServiceDeboard.IsActive))
            {
                if (Profile.ConnectPca == 1 || (Profile.ConnectPca == 2 && ServiceJetway.State != GsxServiceState.NotAvailable))
                {
                    Logger.Information($"Automation: Placing PCA on Arrival");
                    Aircraft.SetPca(true);
                    await Task.Delay(1000, Controller.Token);
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
                    Logger.Information("Automation: Call Jetway on Preparation");
                    await ServiceJetway.Call();
                }

                if (!ServiceStairs.IsConnected && !ServiceStairs.IsCalled)
                {
                    Logger.Information("Automation: Call Stairs on Preparation");
                    await ServiceStairs.Call();
                }
            }
        }
    }
}
