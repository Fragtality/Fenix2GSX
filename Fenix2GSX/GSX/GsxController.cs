using CFIT.AppFramework.MessageService;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
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
using System.Threading.Tasks;

namespace Fenix2GSX.GSX
{
    public class GsxController : ServiceController<Fenix2GSX, AppService, Config, Definition>, IGsxController
    {
        protected object _lock = new();
        public virtual SimConnectManager SimConnect => Fenix2GSX.Instance.AppService.SimConnect;
        public virtual SimConnectController SimController => Fenix2GSX.Instance.AppService.SimService.Controller;
        public virtual bool IsMsfs2024 => SimController.IsMsfs2024Running;
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
        
        public virtual bool CouatlVarsValid { get; protected set; } = false;
        protected virtual bool CouatlVarsReceived { get; set; } = false;
        public virtual bool CouatlConfigSet { get; protected set; } = false;
        public virtual bool IsProcessRunning => Sys.GetProcessRunning(Config.BinaryGsx2020) || Sys.GetProcessRunning(Config.BinaryGsx2024);
        public virtual bool IsActive { get; protected set; } = false;
        public virtual bool IsGsxRunning => IsProcessRunning && CouatlVarsValid;
        public virtual bool IsOnGround => SimStore["SIM ON GROUND"]?.GetNumber() == 1;
        public virtual bool IsPaused => SimConnect.IsPaused;
        public virtual bool IsWalkaround => CheckWalkAround();
        public virtual bool SkippedWalkAround { get; protected set; } = false;
        public virtual string CouatlState => $"{SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() ?? 0} / {SimStore[GsxConstants.VarCouatlStartProgress]?.GetNumber() ?? 0} / {SimStore[GsxConstants.VarCouatlStartStatus]?.GetNumber() ?? 0}";

        public virtual ISimResourceSubscription SubDoorToggleCargo1 { get; protected set; }
        public virtual ISimResourceSubscription SubDoorToggleCargo2 { get; protected set; }
        public virtual ISimResourceSubscription SubScriptSupress { get; protected set; }
        
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
        }

        protected override void InitReceivers()
        {
            base.InitReceivers();

            SimStore.AddVariable(GsxConstants.VarCouatlStarted).OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartProgress).OnReceived += OnCouatlVariable;
            SimStore.AddVariable(GsxConstants.VarCouatlStartStatus).OnReceived += OnCouatlVariable;
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
            SubScriptSupress = SimStore.AddVariable("L:GSX_AUTO_SUPPRESS_MENUREFRESH");

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
        }

        protected virtual void OnCouatlVariable(ISimResourceSubscription sub, object data)
        {
            lock (_lock)
            {
                if (SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() == 1
                    && SimStore[GsxConstants.VarCouatlStartProgress]?.GetNumber() == 0
                    && SimStore[GsxConstants.VarCouatlStartStatus]?.GetNumber() == 0)
                {
                    if (!CouatlVarsValid)
                    {
                        Logger.Debug($"Couatl Variables valid!");
                        CouatlVarsValid = true;
                        MessageService.Send(MessageGsx.Create<MsgGsxCouatlStarted>(this, true));
                    }
                }
                else
                {
                    if (CouatlVarsValid)
                    {
                        Logger.Debug($"Couatl Variables NOT valid! (started: {SimStore[GsxConstants.VarCouatlStarted]?.GetNumber() ?? 0} | progress: {SimStore[GsxConstants.VarCouatlStartProgress].GetNumber()} | status: {SimStore[GsxConstants.VarCouatlStartStatus]?.GetNumber() ?? 0})");
                        MessageService.Send(MessageGsx.Create<MsgGsxCouatlStopped>(this, true));
                        CouatlVarsValid = false;
                        CouatlConfigSet = false;
                    }
                }

                CouatlVarsReceived = true;
            }
        }

        protected override async Task DoRun()
        {
            try
            {
                while (!AircraftBinary && IsExecutionAllowed && !Token.IsCancellationRequested)
                    await Task.Delay(Config.TimerGsxCheck, Token);
                Logger.Debug($"FenixBinary running");

                if (IsMsfs2024 && SimConnect.CameraState == 30)
                {
                    await Task.Delay(Config.GsxServiceStartDelay, Token);

                    while (SimStore["IS AIRCRAFT"]?.GetNumber() != 0 && SimStore["IS AVATAR"]?.GetNumber() != 1 && IsExecutionAllowed && !Token.IsCancellationRequested)
                        await Task.Delay(Config.TimerGsxCheck, Token);

                    Logger.Debug($"MSFS 2024 Aircraft/Avatar Vars valid");
                }

                AutomationController.Reset();
                AircraftInterface.Run();
                while (!AircraftInterface.IsLoaded && IsExecutionAllowed && !Token.IsCancellationRequested)
                    await Task.Delay(Config.TimerGsxCheck, Token);

                Logger.Debug($"AircraftInterface loaded. Searching Profile ...");
                LoadAircraftProfile();

                Logger.Debug($"GsxService active");
                IsActive = true;
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !Token.IsCancellationRequested)
                {
                    if (!CouatlVarsReceived)
                        OnCouatlVariable(null, null);

                    if (!Menu.FirstReadyReceived && NextMenuStartupCheck <= DateTime.Now && IsGsxRunning)
                    {
                        Logger.Debug($"Menu Startup Check");
                        await Menu.Open(true);
                        NextMenuStartupCheck = DateTime.Now + TimeSpan.FromMilliseconds(Config.TimerGsxStartupMenuCheck);
                    }

                    if (IsGsxRunning && Menu.FirstReadyReceived && !CouatlConfigSet)
                    {
                        SetCouatlConf();
                        CouatlConfigSet = true;
                    }

                    if (!SkippedWalkAround)
                        await SkipWalkaround();

                    if (!AutomationController.IsStarted && Menu.FirstReadyReceived)
                        _ = AutomationController.Run();

                    CheckProcess();

                    await Task.Delay(Config.TimerGsxCheck, Token);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            IsActive = false;

            AircraftInterface.Stop();
            Logger.Debug($"GsxService ended");
        }

        protected virtual void CheckProcess()
        {
            if (CouatlVarsValid && !IsProcessRunning)
            {
                Logger.Debug($"Couatl Process not running!");
                CouatlVarsValid = false;
                MessageService.Send(MessageGsx.Create<MsgGsxCouatlStopped>(this, true));
            }
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
            Logger.Verbose($"ac: {SimStore["IS AIRCRAFT"]?.GetNumber()} | av: {SimStore["IS AVATAR"]?.GetNumber()}");
            if (IsWalkaround && Config.SkipWalkAround)
            {
                Logger.Information("Automation: Skip Walkaround");
                string title = Tools.GetMsfsWindowTitle();
                Sys.SetForegroundWindow(title);
                await Task.Delay(Config.DelayForegroundChange, Token);
                string active = Sys.GetActiveWindowTitle();
                if (active == title)
                {
                    Logger.Debug($"Sending Keystrokes");
                    Tools.SendWalkaroundKeystroke();
                    await Task.Delay(Config.DelayAircraftModeChange, Token);
                }
                else
                    Logger.Debug($"Active Window did not match to '{title}'");
            }
            SkippedWalkAround = CheckSessionReady() && !IsWalkaround;
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
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(15, "", true));
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            await Menu.RunSequence(sequence);
        }

        public override void Stop()
        {
            AutomationController.Stop();
            AircraftInterface.Reset();
            Menu.Reset();

            base.Stop();

            foreach (var service in GsxServices)
                service.Value.ResetState();

            IsActive = false;
            AircraftProfile = null;
            SkippedWalkAround = false;
            CouatlVarsValid = false;
            CouatlVarsReceived = false;
            CouatlConfigSet = false;
            NextMenuStartupCheck = DateTime.MinValue;
        }

        protected override void FreeResources()
        {
            base.FreeResources();

            foreach (var service in GsxServices)
                service.Value.FreeResources();

            Menu.FreeResources();
            AircraftInterface.FreeResources();
            AutomationController.FreeResources();

            SimStore.Remove("L:GSX_AUTO_SUPPRESS_MENUREFRESH");
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
            SimStore.Remove(GsxConstants.VarCouatlStarted);
            SimStore[GsxConstants.VarCouatlStartProgress].OnReceived -= OnCouatlVariable;
            SimStore.Remove(GsxConstants.VarCouatlStartProgress);
            SimStore[GsxConstants.VarCouatlStartStatus].OnReceived -= OnCouatlVariable;
            SimStore.Remove(GsxConstants.VarCouatlStartStatus);
            SimStore.Remove("SIM ON GROUND");
            if (IsMsfs2024)
            {
                SimStore.Remove("IS AIRCRAFT");
                SimStore.Remove("IS AVATAR");
            }
            ReceiverStore.Remove<MsgGsxCouatlStarted>();
            ReceiverStore.Remove<MsgGsxCouatlStopped>();
        }
    }
}
