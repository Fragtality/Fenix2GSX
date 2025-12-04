using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fenix2GSX.Aircraft;
using Fenix2GSX.AppConfig;
using Fenix2GSX.Audio;
using Fenix2GSX.GSX;
using Fenix2GSX.GSX.Menu;
using Fenix2GSX.GSX.Services;
using FenixInterface;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fenix2GSX.UI.Views.Monitor
{
    public partial class ModelMonitor(AppService source) : ViewModelBase<AppService>(source)
    {
        protected virtual DispatcherTimer UpdateTimer { get; set; }
        protected virtual bool ForceRefresh { get; set; } = false;
        protected static SolidColorBrush ColorValid { get; } = new(Colors.Green);
        protected static SolidColorBrush ColorInvalid { get; } = new(Colors.Red);
        protected virtual Config Config => this.Source.Config;
        protected virtual SimConnectController SimConnectController => this.Source.SimService.Controller;
        protected virtual SimConnectManager SimConnect => this.Source.SimConnect;
        protected virtual GsxController GsxController => this.Source.GsxService;
        protected virtual AudioController AudioController => this.Source.AudioService;
        public virtual ObservableCollection<string> MessageLog { get; } = [];
        protected virtual GsxAutomationController AutomationController => GsxController?.AutomationController;
        protected virtual AircraftInterface AircraftInterface => GsxController?.AircraftInterface;
        protected virtual ConcurrentDictionary<GsxServiceType, GsxService> GsxServices => GsxController?.GsxServices;
        protected virtual GsxServiceBoarding GsxServiceBoard => GsxServices[GsxServiceType.Boarding] as GsxServiceBoarding;
        protected virtual GsxServiceDeboarding GsxServiceDeboard => GsxServices[GsxServiceType.Deboarding] as GsxServiceDeboarding;
        protected virtual GsxServicePushback GsxServicePushBack => GsxServices[GsxServiceType.Pushback] as GsxServicePushback;

        protected override void InitializeModel()
        {
            UpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(AppService.Instance.Config.UiRefreshInterval),
            };
            UpdateTimer.Tick += OnUpdate;
        }

        public virtual void Start()
        {
            ForceRefresh = true;
            UpdateTimer.Start();
        }

        public virtual void Stop()
        {
            UpdateTimer?.Stop();
        }

        [RelayCommand]
        public virtual void LogDir()
        {
            try { Process.Start(new ProcessStartInfo(Path.Join(Config.Definition.ProductPath, Config.Definition.ProductLogPath)) { UseShellExecute = true }); } catch { }
        }

        protected virtual void UpdateBoolState(string propertyValue, string propertyColor, bool value, bool reverseColor = false)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (this.GetPropertyValue<bool>(propertyValue) != value || ForceRefresh)
                {
                    this.SetPropertyValue<bool>(propertyValue, value);
                    UpdateColor(propertyColor, value, reverseColor);
                }
            }
            catch { }
        }

        protected virtual void UpdateColor(string propertyColor, bool state, bool reverseColor = false)
        {
            try
            {
                if (reverseColor)
                    this.SetPropertyValue<SolidColorBrush>(propertyColor, state ? ColorInvalid : ColorValid);
                else
                    this.SetPropertyValue<SolidColorBrush>(propertyColor, state ? ColorValid : ColorInvalid);
            }
            catch { }
        }

        protected virtual void UpdateState<T>(string propertyValue, T value)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (!this.GetPropertyValue<T>(propertyValue)?.Equals(value) == true || ForceRefresh)
                    this.SetPropertyValue<T>(propertyValue, value);
            }
            catch { }
        }

        protected virtual void OnUpdate(object? sender, EventArgs e)
        {
            try { UpdateSim(); } catch (Exception ex) { Logger.LogException(ex); }
            try { UpdateGsx(); } catch { }
            try { UpdateApp(); } catch { }
            try { UpdateLog(); } catch { }
            ForceRefresh = false;
        }

        protected virtual void UpdateSim()
        {
            UpdateBoolState(nameof(SimRunning), nameof(SimRunningColor), SimConnectController.IsSimRunning);
            UpdateBoolState(nameof(SimConnected), nameof(SimConnectedColor), SimConnect.IsSimConnected);
            UpdateBoolState(nameof(SimSession), nameof(SimSessionColor), SimConnect.IsSessionRunning && !SimConnect.IsSessionStopped);

            UpdateBoolState(nameof(SimPaused), nameof(SimPausedColor), SimConnect.IsPaused, true);
            UpdateBoolState(nameof(SimWalkaround), nameof(SimWalkaroundColor), GsxController?.IsWalkaround ?? false, true);
            UpdateState<long>(nameof(CameraState), SimConnect.CameraState);

            UpdateState<string>(nameof(SimVersion), SimConnect.SimVersionString);

            UpdateState<string>(nameof(AircraftString), SimConnect.AircraftString);
        }

        [ObservableProperty]
        protected bool _SimRunning = false;
        [ObservableProperty]
        protected SolidColorBrush _SimRunningColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimConnected = false;
        [ObservableProperty]
        protected SolidColorBrush _SimConnectedColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimSession = false;
        [ObservableProperty]
        protected SolidColorBrush _SimSessionColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimPaused = false;
        [ObservableProperty]
        protected SolidColorBrush _SimPausedColor = ColorInvalid;

        [ObservableProperty]
        protected bool _SimWalkaround = false;
        [ObservableProperty]
        protected SolidColorBrush _SimWalkaroundColor = ColorInvalid;

        [ObservableProperty]
        protected long _CameraState = 0;

        [ObservableProperty]
        protected string _SimVersion = "";

        [ObservableProperty]
        protected string _AircraftString = "";

        protected virtual void UpdateGsx()
        {
            UpdateBoolState(nameof(GsxRunning), nameof(GsxRunningColor), GsxController.CheckBinaries());
            UpdateState<string>(nameof(GsxStarted), $"{GsxController?.CouatlLastStarted ?? 0} | {GsxController?.CouatlLastProgress ?? 100}");
            UpdateColor(nameof(GsxStartedColor), GsxController.CouatlVarsValid);
            UpdateState<GsxMenuState>(nameof(GsxMenu), GsxController.Menu.MenuState);
            UpdateState<int>(nameof(GsxPaxTarget), GsxServiceBoard?.SubPaxTarget?.GetValue<int>() ?? 0);
            UpdateState<string>(nameof(GsxPaxTotal), $"{GsxServiceBoard?.SubPaxTotal?.GetValue<int>() ?? 0} | {GsxServiceDeboard?.SubPaxTotal?.GetValue<int>() ?? 0}");
            UpdateState<string>(nameof(GsxCargoProgress), $"{GsxServiceBoard?.SubCargoPercent?.GetValue<int>() ?? 0} | {GsxServiceDeboard?.SubCargoPercent?.GetValue<int>() ?? 0}");

            UpdateState<GsxServiceState>(nameof(ServiceReposition), GsxServices[GsxServiceType.Reposition].State);
            UpdateState<GsxServiceState>(nameof(ServiceRefuel), GsxServices[GsxServiceType.Refuel].State);
            UpdateState<GsxServiceState>(nameof(ServiceCatering), GsxServices[GsxServiceType.Catering].State);
            UpdateState<GsxServiceState>(nameof(ServiceLavatory), GsxServices[GsxServiceType.Lavatory].State);
            UpdateState<GsxServiceState>(nameof(ServiceWater), GsxServices[GsxServiceType.Water].State);
            UpdateState<GsxServiceState>(nameof(ServiceCleaning), GsxServices[GsxServiceType.Cleaning].State);
            UpdateState<GsxServiceState>(nameof(ServiceGpu), GsxServices[GsxServiceType.GPU].State);

            UpdateState<GsxServiceState>(nameof(ServiceBoarding), GsxServices[GsxServiceType.Boarding].State);
            UpdateState<GsxServiceState>(nameof(ServiceDeboarding), GsxServices[GsxServiceType.Deboarding].State);
            UpdateState<string>(nameof(ServicePushback), $"{GsxServicePushBack.State} ({GsxServicePushBack.PushStatus})");
            UpdateState<GsxServiceState>(nameof(ServiceJetway), GsxServices[GsxServiceType.Jetway].State);
            UpdateState<GsxServiceState>(nameof(ServiceStairs), GsxServices[GsxServiceType.Stairs].State);
        }

        [ObservableProperty]
        protected bool _GsxRunning = false;
        [ObservableProperty]
        protected SolidColorBrush _GsxRunningColor = ColorInvalid;

        [ObservableProperty]
        protected string _GsxStarted = "";
        [ObservableProperty]
        protected SolidColorBrush _GsxStartedColor = ColorInvalid;

        [ObservableProperty]
        protected GsxMenuState _GsxMenu = GsxMenuState.UNKNOWN;

        [ObservableProperty]
        protected int _GsxPaxTarget = 0;

        [ObservableProperty]
        protected string _GsxPaxTotal = "0 | 0";

        [ObservableProperty]
        protected string _GsxCargoProgress = "0 | 0";

        [ObservableProperty]
        protected GsxServiceState _ServiceReposition = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceRefuel = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceCatering = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceLavatory = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceWater = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceCleaning = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceGpu = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceBoarding = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceDeboarding = GsxServiceState.Unknown;

        [ObservableProperty]
        protected string _ServicePushback = $"{GsxServiceState.Unknown} (0)";

        [ObservableProperty]
        protected GsxServiceState _ServiceJetway = GsxServiceState.Unknown;

        [ObservableProperty]
        protected GsxServiceState _ServiceStairs = GsxServiceState.Unknown;

        protected virtual void UpdateApp()
        {
            UpdateBoolState(nameof(AppGsxController), nameof(AppGsxControllerColor), GsxController.IsActive);
            UpdateBoolState(nameof(AppAircraftBinary), nameof(AppAircraftBinaryColor), GsxController.AircraftBinary);
            UpdateBoolState(nameof(AppAircraftInterface), nameof(AppAircraftInterfaceColor), AircraftInterface.IsLoaded);
            UpdateBoolState(nameof(AppAutomationController), nameof(AppAutomationControllerColor), AutomationController.IsStarted);
            UpdateBoolState(nameof(AppAudioController), nameof(AppAudioControllerColor), AudioController.IsActive);

            UpdateState<AutomationState>(nameof(AppAutomationState), AutomationController?.State ?? AutomationState.SessionStart);
            UpdateState<string>(nameof(AppAutomationDepartureServices), $"{AutomationController?.ServiceCountCompleted ?? 0} / {AutomationController?.ServiceCountRunning ?? 0} / {AutomationController?.ServiceCountTotal ?? 0}" ?? "0 / 0 / 0");

            try
            {
                UpdateState<bool>(nameof(AppOnGround), AutomationController?.IsOnGround ?? true);
                UpdateState<bool>(nameof(AppEnginesRunning), AircraftInterface?.EnginesRunning ?? false);
                UpdateState<bool>(nameof(AppInMotion), AircraftInterface?.GroundSpeed > Config.SpeedTresholdTaxiOut);
            }
            catch { }

            UpdateState<string>(nameof(AppProfile), GsxController?.AircraftProfile?.ToString() ?? "");
            UpdateState<string>(nameof(AppAircraft), $"{AircraftInterface?.Airline ?? ""} / {AircraftInterface?.Title ?? ""} / {AircraftInterface?.Registration ?? ""}");
        }

        [ObservableProperty]
        protected bool _AppGsxController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppGsxControllerColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAircraftBinary = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAircraftBinaryColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAircraftInterface = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAircraftInterfaceColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAutomationController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAutomationControllerColor = ColorInvalid;

        [ObservableProperty]
        protected bool _AppAudioController = false;
        [ObservableProperty]
        protected SolidColorBrush _AppAudioControllerColor = ColorInvalid;

        [ObservableProperty]
        protected AutomationState _AppAutomationState = AutomationState.SessionStart;

        [ObservableProperty]
        protected string _AppAutomationDepartureServices = "0 / 0";

        [ObservableProperty]
        protected bool _AppOnGround = true;

        [ObservableProperty]
        protected bool _AppEnginesRunning = false;

        [ObservableProperty]
        protected bool _AppInMotion = false;

        [ObservableProperty]
        protected string _AppProfile = "";

        [ObservableProperty]
        protected string _AppAircraft = "Airline / Title / Registration";

        protected virtual void UpdateLog()
        {
            if (Logger.Messages.IsEmpty)
                NotifyPropertyChanged(nameof(MessageLog));

            while (!Logger.Messages.IsEmpty)
            {
                MessageLog.Add(Logger.Messages.Dequeue());
                if (MessageLog.Count > 12)
                    MessageLog.RemoveAt(0);
            }
        }
    }
}
