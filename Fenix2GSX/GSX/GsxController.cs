using CFIT.AppFramework.MessageService;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.Definitions;
using CFIT.SimConnectLib.SimResources;
using CFIT.SimConnectLib.SimVars;
using Fenix2GSX.Aircraft;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Menu;
using Fenix2GSX.GSX.Services;
using FenixInterface;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX
{
    public class GsxController : ServiceController<Fenix2GSX, AppService, Config, Definition>, IGsxController
    {
        protected bool _lock = false;
        public virtual CancellationToken RequestToken => AppService.Instance.RequestToken;
        public virtual SimConnectManager SimConnect => Fenix2GSX.Instance.AppService.SimConnect;
        public virtual SimConnectController SimController => Fenix2GSX.Instance.AppService.SimService.Controller;
        public virtual bool IsMsfs2024 => SimConnect.GetSimVersion() == SimVersion.MSFS2024;
        public virtual string PathInstallation { get; }
        public virtual GsxMenu Menu { get; }
        protected virtual DateTime NextMenuStartupCheck { get; set; } = DateTime.MinValue;
        public virtual AircraftInterface AircraftInterface { get; }
        public virtual Flightplan Flightplan { get; } = new Flightplan();
        public virtual bool AircraftBinary => Sys.GetProcessRunning(Config.FenixBinary);
        public virtual IConfig IConfig => Config;
        public virtual AircraftProfile AircraftProfile { get; protected set; } = null;
        public event Action<AircraftProfile> ProfileChanged;
        public virtual IAircraftProfile IAircraftProfile => AircraftProfile;
        public virtual GsxAutomationController AutomationController { get; }
        public virtual MessageReceiver<MsgGsxCouatlStarted> MsgCouatlStarted { get; protected set; }
        public virtual MessageReceiver<MsgGsxCouatlStopped> MsgCouatlStopped { get; protected set; }
        public virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices { get; } = [];
        public virtual bool IsRefuelActive => GsxServices[GsxServiceType.Refuel].State == GsxServiceState.Active;
        
        public virtual bool CouatlVarsValid { get; protected set; } = false;
        public virtual int CouatlLastProgress { get; protected set; } = 0;
        public virtual int CouatlLastStarted { get; protected set; } = 0;
        public virtual int CouatlLastSimbrief { get; protected set; } = 0;
        public virtual DateTime CouatlInhibitStateChanges { get; protected set; } = DateTime.MinValue;
        public virtual int CouatlInvalidCount { get; protected set; } = 0;
        protected virtual bool CouatlVarsReceived { get; set; } = false;
        public virtual bool CouatlConfigSet { get; protected set; } = false;
        public virtual bool IsProcessRunning { get; protected set; } = false;
        protected virtual DateTime NextProcessCheck { get; set; } = DateTime.MinValue;
        public virtual bool IsActive { get; protected set; } = false;
        public virtual bool IsAutomationStarted => AutomationController?.IsStarted == true;
        public virtual AutomationState AutomationState => AutomationController?.State ?? AutomationState.SessionStart;
        public virtual bool IsGsxRunning => IsProcessRunning && CouatlVarsValid;
        public virtual bool IsOnGround => SimStore["SIM ON GROUND"]?.GetNumber() == 1;
        public virtual bool FirstGroundCheck { get; protected set; } = true;
        public virtual bool IsAirStart { get; protected set; } = false;
        public virtual bool CanAutomationRun => Menu.FirstReadyReceived || IsAirStart || AircraftInterface?.EnginesRunning == true;
        protected virtual int GroundCounter { get; set; } = 0;
        public virtual bool IsPaused => SimConnect.IsPaused;
        public virtual bool IsWalkaround => CheckWalkAround();
        public virtual bool SkippedWalkAround { get; protected set; } = false;
        public virtual bool WalkAroundSkipActive { get; protected set; } = false;
        public virtual bool WalkaroundNotified { get; protected set; } = false;
        public event Func<Task> WalkaroundWasSkipped;

        public virtual ISimResourceSubscription SubDoorToggleCargo1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo2 { get; protected set; }
        
        public GsxController(Config config) : base(config)
        {
            PathInstallation = Sys.GetRegistryValue<string>(GsxConstants.RegPath, GsxConstants.RegValue, null) ?? GsxConstants.PathDefault;
            Menu = new(this);
            AircraftInterface = new(this);
            AutomationController = new(this);
            SetAircraftProfile("default");
        }

        public virtual void SetAircraftProfile(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                AircraftProfile = Config.AircraftProfiles.First(p => p.Name == name);
                Logger.Debug($"Using Profile {AircraftProfile}");
                ProfileChanged?.Invoke(AircraftProfile);
            }
        }

        public virtual void LoadAircraftProfile()
        {
            SetAircraftProfile(Config?.GetAircraftProfile(AircraftInterface)?.Name);
        }

        protected virtual void InitGsxServices()
        {
            _ = new GsxServiceReposition(this);
            _ = new GsxServiceRefuel(this);
            _ = new GsxServiceCatering(this);
            _ = new GsxServiceJetway(this);
            _ = new GsxServiceStairs(this);
            _ = new GsxServiceBoarding(this);
            _ = new GsxServiceDeboarding(this);
            _ = new GsxServicePushback(this);
            _ = new GsxServiceGpu(this);
            _ = new GsxServiceDeice(this);
            _ = new GsxServiceLavatory(this);
            _ = new GsxServiceWater(this);
            _ = new GsxServiceCleaning(this);
        }

        protected override Task InitReceivers()
        {
            base.InitReceivers();

            SimStore.AddVariable(GsxConstants.VarCouatlStarted).OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlSimbrief).OnReceived += OnCouatlSimbrief;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg5).OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg6).OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProg7).OnReceived += OnCouatlVariable;

            SimStore.AddVariable("SIM ON GROUND", SimUnitType.Bool);
            if (IsMsfs2024)
            {
                SimStore.AddVariable("IS AIRCRAFT", SimUnitType.Number);
                SimStore.AddVariable("IS AVATAR", SimUnitType.Number);
            }
            MsgCouatlStarted = ReceiverStore.Add<MsgGsxCouatlStarted>();
            MsgCouatlStopped = ReceiverStore.Add<MsgGsxCouatlStopped>();

            SubDoorToggleCargo1 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo1);
            SubDoorToggleCargo2 = SimStore.AddVariable(GsxConstants.VarDoorToggleCargo2);

            SimStore.AddVariable(GsxConstants.VarReadProgFuel).OnReceived += OnConfigChange;
            SimStore.AddVariable(GsxConstants.VarReadCustFuel).OnReceived += OnConfigChange;
            SimStore.AddVariable(GsxConstants.VarReadAutoMode).OnReceived += OnConfigChange;
            SimStore.AddVariable(GsxConstants.VarSetProgFuel);
            SimStore.AddVariable(GsxConstants.VarSetCustFuel);
            SimStore.AddVariable(GsxConstants.VarSetAutoMode);

            InitGsxServices();
            Menu.Init();
            AircraftInterface.Init();
            AutomationController.Init();
            return Task.CompletedTask;
        }

        protected virtual void OnCouatlSimbrief(ISimResourceSubscription sub, object data)
        {
            try
            {
                int state = (int)sub.GetNumber();
                if (CouatlLastSimbrief == 1 && state == 0 && CouatlVarsValid && CouatlInhibitStateChanges < DateTime.Now)
                {
                    Logger.Debug("Simbrief Refresh detected - inhibiting Couatl State Changes for 5s");
                    CouatlInhibitStateChanges = DateTime.Now + TimeSpan.FromSeconds(5);
                }

                CouatlLastSimbrief = state;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                CouatlInhibitStateChanges = DateTime.MinValue;
                CouatlLastSimbrief = 0;
            }
        }

        protected virtual void OnCouatlVariable(ISimResourceSubscription sub, object data)
        {
            while (_lock && !Token.IsCancellationRequested) { }
            _lock = true;

            try
            {
                CouatlLastStarted = (int)(SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() ?? 0);
                CouatlLastProgress = (int)Math.Max(
                    Math.Max(SimStore[GsxConstants.VarCouatlStartProg5]?.GetNumber() ?? 100, SimStore[GsxConstants.VarCouatlStartProg6]?.GetNumber() ?? 100),
                    SimStore[GsxConstants.VarCouatlStartProg7]?.GetNumber() ?? 100
                    );
                if (CouatlLastStarted == 1 && CouatlLastProgress == 0)
                {
                    if (!CouatlVarsValid)
                    {
                        Logger.Debug($"Couatl Variables valid!");
                        CouatlVarsValid = true;
                        CouatlInvalidCount = 0;
                        MessageService.Send(MessageGsx.Create<MsgGsxCouatlStarted>(this, true));
                    }
                }
                else if (CouatlVarsValid)
                {
                    if (CouatlInhibitStateChanges < DateTime.Now)
                    {
                        Logger.Debug($"Couatl Variables NOT valid! (started: {CouatlLastStarted} / progress: {CouatlLastProgress})");
                        MessageService.Send(MessageGsx.Create<MsgGsxCouatlStopped>(this, true));
                        CouatlVarsValid = false;
                        CouatlConfigSet = false;
                    }
                    else
                        Logger.Debug("Couatl Variables invalid-Change ignored");
                }

                CouatlVarsReceived = true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            finally
            {
                _lock = false;
            }            
        }

        protected virtual void CheckGround()
        {
            if (FirstGroundCheck)
            {
                AutomationController.IsOnGround = IsOnGround;
                FirstGroundCheck = false;
                IsAirStart = !AutomationController.IsOnGround;
                if (IsAirStart)
                    Logger.Debug($"Air Start detected");
            }
            else if (AutomationController.IsOnGround != IsOnGround && !IsWalkaround)
            {
                GroundCounter++;
                if (GroundCounter > Config.GroundTicks)
                {
                    GroundCounter = 0;
                    AutomationController.IsOnGround = IsOnGround;
                    Logger.Information($"On Ground State changed: {(AutomationController.IsOnGround ? "On Ground" : "In Flight")}");
                }
            }
            else if (AutomationController.IsOnGround == IsOnGround && GroundCounter > 0)
                GroundCounter = 0;
        }

        protected override async Task DoRun()
        {
            try
            {
                Menu.Reset();

                while (!AircraftBinary && IsExecutionAllowed && !Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                    await Task.Delay(Config.TimerGsxCheck, Token);
                Logger.Debug($"FenixBinary running");
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                if (IsMsfs2024 && SimConnect.CameraState == 30)
                {
                    await Task.Delay(Config.GsxServiceStartDelay, Token);

                    while (((SimStore["IS AIRCRAFT"]?.GetNumber() == 0 && SimStore["IS AVATAR"]?.GetNumber() == 0)
                        || (SimStore["IS AIRCRAFT"]?.GetNumber() == 1 && SimStore["IS AVATAR"]?.GetNumber() == 1))
                        && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                        await Task.Delay(Config.TimerGsxCheck, RequestToken);

                    Logger.Debug($"MSFS 2024 Aircraft/Avatar Vars valid");
                }
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                AutomationController.Reset();
                AircraftInterface.Run();
                while (!AircraftInterface.IsLoaded && IsExecutionAllowed && !Token.IsCancellationRequested && !RequestToken.IsCancellationRequested)
                    await Task.Delay(Config.TimerGsxCheck, Token);
                if (!IsExecutionAllowed || RequestToken.IsCancellationRequested)
                    return;

                Logger.Debug($"AircraftInterface loaded. Searching Profile ...");
                LoadAircraftProfile();

                Logger.Debug($"GsxService active (VarsReceived: {CouatlVarsReceived} | FirstReady: {Menu.FirstReadyReceived})");
                IsActive = true;
                Logger.Information($"GsxController active - waiting for Menu to be ready");
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
                {
                    if (Config.LogLevel == LogLevel.Verbose)
                        Logger.Verbose($"Controller Tick - VarsReceived: {CouatlVarsReceived} | FirstReady: {Menu.FirstReadyReceived} | VarsValid: {CouatlVarsValid} | IsGsxRunning: {IsGsxRunning}");
                    CheckGround();

                    if (!CouatlVarsReceived && IsProcessRunning)
                        OnCouatlVariable(null, null);

                    if (!SkippedWalkAround && !WalkAroundSkipActive)
                    {
                        if (AutomationController.IsOnGround && !AircraftInterface.EnginesRunning && AutomationController.State == AutomationState.SessionStart && !AircraftProfile.SkipWalkAround && AircraftProfile.PlaceFenixStairsWalkaround && !AircraftInterface.FenixInterface.StairsFwd)
                            await AircraftInterface.FenixInterface.SetStairsFwd(true);
                        else if (AutomationController.IsOnGround)
                            _ = SkipWalkaround();
                        else
                            SkippedWalkAround = true;
                    }

                    if (!Menu.FirstReadyReceived && IsProcessRunning && NextMenuStartupCheck <= DateTime.Now && AutomationController.IsOnGround)
                    {
                        if (CouatlVarsReceived && CouatlLastStarted == 1 && IsProcessRunning)
                        {
                            Logger.Information($"Trying to open GSX Menu ...");
                            await Menu.OpenHide();
                            await Task.Delay(1000, RequestToken);
                        }

                        if (!CouatlVarsValid || !IsProcessRunning)
                        {
                            CouatlInvalidCount++;
                            Logger.Warning($"GSX Menu is not starting #{CouatlInvalidCount}");
                            if (CouatlInvalidCount > Config.GsxMenuStartupMaxFail && Config.RestartGsxStartupFail)
                            {
                                Logger.Information($"Restarting GSX ...");
                                await AppService.Instance.RestartGsx();
                                CouatlInvalidCount = 0;
                                await Task.Delay(Config.GsxServiceStartDelay, RequestToken);
                            }
                        }

                        NextMenuStartupCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxStartupMenuCheck);
                    }

                    if (IsGsxRunning && Menu.FirstReadyReceived && !CouatlConfigSet)
                    {
                        SetCouatlConf();
                        CouatlConfigSet = true;
                    }

                    if (!AutomationController.IsStarted && CanAutomationRun && !AutomationController.RunFlag)
                        _ = AutomationController.Run();
                    else if (AutomationController.IsStarted && SkippedWalkAround && !WalkaroundNotified)
                    {
                        if (AutomationController.IsOnGround && AutomationController.State < AutomationState.Departure)
                            await TaskTools.RunLogged(async () => await WalkaroundWasSkipped?.Invoke());
                        WalkaroundNotified = true;
                    }

                    CheckProcess();

                    await Task.Delay(Config.TimerGsxCheck, RequestToken);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }

            try
            {
                IsActive = false;
                await Stop();
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            
            Logger.Debug($"GsxService ended");
        }

        protected virtual void CheckProcess()
        {
            if (NextProcessCheck <= DateTime.Now)
            {
                IsProcessRunning = CheckBinaries();
                NextProcessCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxProcessCheck);
            }

            if (CouatlVarsValid && !IsProcessRunning)
            {
                Logger.Debug($"Couatl Process not running!");
                CouatlVarsValid = false;
                MessageService.Send(MessageGsx.Create<MsgGsxCouatlStopped>(this, true));
            }
        }

        public virtual bool CheckBinaries()
        {
            var version = SimConnect.GetSimVersion();
            if (version == SimVersion.MSFS2020)
                return Sys.GetProcessRunning(Config.BinaryGsx2020);
            else if (version == SimVersion.MSFS2024)
                return Sys.GetProcessRunning(Config.BinaryGsx2024);
            else
                return false;
        }

        protected virtual bool CheckWalkAround()
        {
            if (IsMsfs2024)
                return SimStore["IS AVATAR"]?.GetNumber() == 1;
            else
                return false;
        }

        protected virtual bool CheckSessionReady()
        {
            return SimConnect.CameraState < 11;
        }

        protected virtual async Task SkipWalkaround()
        {
            WalkAroundSkipActive = true;
            Logger.Verbose($"ac: {SimStore["IS AIRCRAFT"]?.GetNumber()} | av: {SimStore["IS AVATAR"]?.GetNumber()}");
            while (IsWalkaround && !SkippedWalkAround && AircraftProfile.SkipWalkAround && IsExecutionAllowed && !RequestToken.IsCancellationRequested)
            {
                Logger.Information("Automation: Skip Walkaround");
                string title = Tools.GetMsfsWindowTitle();
                Sys.SetForegroundWindow(title);
                await Task.Delay(Config.DelayForegroundChange, RequestToken);
                string active = Sys.GetActiveWindowTitle();
                if (active == title)
                {
                    Logger.Debug($"Sending Keystrokes");
                    Tools.SendWalkaroundKeystroke();
                    await Task.Delay(Config.DelayAircraftModeChange, RequestToken);
                }
                else
                    Logger.Debug($"Active Window did not match to '{title}'");

                if (IsWalkaround)
                    await Task.Delay(Config.TimerGsxCheck, Token);
                
                SkippedWalkAround = CheckSessionReady() && !IsWalkaround;
            }
            SkippedWalkAround = CheckSessionReady() && !IsWalkaround;
            WalkAroundSkipActive = false;
        }

        protected virtual void OnConfigChange(ISimResourceSubscription sub, object data)
        {
            if (!IsGsxRunning || !Menu.FirstReadyReceived || !AircraftInterface.IsLoaded)
                return;

            if (sub.Name == GsxConstants.VarReadProgFuel && sub.GetNumber() > 0)
            {
                Logger.Debug($"Resetting GSX Setting {sub.Name}");
                SimStore[GsxConstants.VarSetProgFuel].WriteValue(-1);
            }

            if (sub.Name == GsxConstants.VarReadCustFuel && sub.GetNumber() > 0)
            {
                Logger.Debug($"Resetting GSX Setting {sub.Name}");
                SimStore[GsxConstants.VarSetCustFuel].WriteValue(-1);
            }

            if (sub.Name == GsxConstants.VarReadAutoMode && sub.GetNumber() > 0)
            {
                Logger.Debug($"Resetting GSX Setting {sub.Name}");
                SimStore[GsxConstants.VarSetAutoMode].WriteValue(-1);
            }
        }

        protected virtual void SetCouatlConf()
        {
            Logger.Debug($"Set GSX Settings");
            SimStore[GsxConstants.VarSetProgFuel].WriteValue(-1);
            SimStore[GsxConstants.VarSetCustFuel].WriteValue(-1);
            SimStore[GsxConstants.VarSetAutoMode].WriteValue(-1);
        }

        public virtual async Task ReloadSimbrief()
        {
            Logger.Information($"Refreshing GSX SimBrief/VDGS Data");
            Logger.Debug("Simbrief Refresh - inhibiting Couatl State Changes for 7.5s");
            CouatlInhibitStateChanges = DateTime.Now + TimeSpan.FromSeconds(7.5);
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(15, "", true));
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            await Menu.RunSequence(sequence);
        }

        public override Task Stop()
        {
            AutomationController.Stop();
            AircraftInterface.Reset();
            AircraftInterface.Stop();
            Menu.Reset();

            base.Stop();

            foreach (var service in GsxServices)
                service.Value.ResetState();

            IsActive = false;
            AircraftProfile = null;
            SkippedWalkAround = false;
            WalkaroundNotified = false;
            WalkAroundSkipActive = false;
            CouatlVarsValid = false;
            CouatlLastProgress = 0;
            CouatlLastStarted = 0;
            CouatlInvalidCount = 0;
            CouatlVarsReceived = false;
            CouatlConfigSet = false;
            GroundCounter = 0;
            FirstGroundCheck = true;
            IsAirStart = false;
            NextMenuStartupCheck = DateTime.MinValue;
            return Task.CompletedTask;
        }

        protected override Task FreeResources()
        {
            base.FreeResources();

            foreach (var service in GsxServices)
                service.Value.FreeResources();

            Menu.FreeResources();
            AircraftInterface.FreeResources();
            AutomationController.FreeResources();

            SimStore[GsxConstants.VarReadProgFuel].OnReceived -= OnConfigChange;
            SimStore[GsxConstants.VarReadCustFuel].OnReceived -= OnConfigChange;
            SimStore[GsxConstants.VarReadAutoMode].OnReceived -= OnConfigChange;
            SimStore.Remove(GsxConstants.VarReadProgFuel);
            SimStore.Remove(GsxConstants.VarReadCustFuel);
            SimStore.Remove(GsxConstants.VarReadAutoMode);
            SimStore.Remove(GsxConstants.VarSetProgFuel);
            SimStore.Remove(GsxConstants.VarSetCustFuel);
            SimStore.Remove(GsxConstants.VarSetAutoMode);
            SimStore.Remove(GsxConstants.VarDoorToggleCargo1);
            SimStore.Remove(GsxConstants.VarDoorToggleCargo2);

            SimStore[GsxConstants.VarCouatlStarted].OnReceived -= OnCouatlVariable;
            SimStore[GsxConstants.VarCouatlSimbrief].OnReceived -= OnCouatlSimbrief;
            SimStore[GsxConstants.VarCouatlStartProg5].OnReceived -= OnCouatlVariable;
            SimStore[GsxConstants.VarCouatlStartProg6].OnReceived -= OnCouatlVariable;
            SimStore[GsxConstants.VarCouatlStartProg7].OnReceived -= OnCouatlVariable;
            SimStore.Remove(GsxConstants.VarCouatlStarted);
            SimStore.Remove(GsxConstants.VarCouatlStartProg5);
            SimStore.Remove(GsxConstants.VarCouatlStartProg6);
            SimStore.Remove(GsxConstants.VarCouatlStartProg7);

            SimStore.Remove("SIM ON GROUND");
            if (IsMsfs2024)
            {
                SimStore.Remove("IS AIRCRAFT");
                SimStore.Remove("IS AVATAR");
            }
            ReceiverStore.Remove<MsgGsxCouatlStarted>();
            ReceiverStore.Remove<MsgGsxCouatlStopped>();

            return Task.CompletedTask;
        }
    }
}
