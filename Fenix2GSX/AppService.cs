using CFIT.AppFramework.Messages;
using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.Definitions;
using Fenix2GSX.AppConfig;
using Fenix2GSX.Audio;
using Fenix2GSX.GSX;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fenix2GSX
{
    public enum AppResetRequest
    {
        None = 0,
        App = 1,
        AppGsx = 2,
    }

    public class AppService : AppService<Fenix2GSX, AppService, Config, Definition>
    {
        public virtual CancellationTokenSource RequestTokenSource { get; protected set; }
        public virtual CancellationToken RequestToken { get; protected set; }
        public virtual GsxController GsxService { get; protected set; }
        public virtual AudioController AudioService { get; protected set; }
        public virtual AppResetRequest ResetRequested {  get; set; } = AppResetRequest.None;
        public virtual bool IsSessionInitializing { get; protected set; } = false;
        public virtual bool IsSessionInitialized { get; protected set; } = false;
        public virtual bool SessionStopRequested { get; protected set; } = false;
        public virtual bool IsFenixAircraft => SimConnect.AircraftString.Contains(Config.FenixAircraftString, StringComparison.InvariantCultureIgnoreCase);

        public AppService(Config config) : base(config)
        {
            RefreshToken();
        }

        protected virtual void RefreshToken()
        {
            RequestTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Fenix2GSX.Instance.Token);
            RequestToken = RequestTokenSource.Token;
        }

        protected override void CreateServiceControllers()
        {
            GsxService = new GsxController(Config);
            AudioService = new AudioController(Config);
        }

        protected override Task InitReceivers()
        {
            base.InitReceivers();
            ReceiverStore.Add<MsgSessionReady>().OnMessage += OnSessionReady;
            ReceiverStore.Add<MsgSessionEnded>().OnMessage += OnSessionEnded;
            return Task.CompletedTask;
        }

        protected virtual void OnSessionEnded(MsgSessionEnded obj)
        {
            SessionStopRequested = true;

            try
            {
                Logger.Debug($"Cancel Request Token");
                RequestTokenSource.Cancel();

                if (GsxService.IsActive)
                {
                    Logger.Debug($"Stop GsxService");
                    GsxService.Stop();
                }

                if (AudioService.IsActive)
                {
                    Logger.Debug($"Stop AudioService");
                    AudioService.Stop();
                }

                Config.SetDisplayUnit(Config.DisplayUnitDefault);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            IsSessionInitialized = false;
        }

        protected virtual void OnSessionReady(MsgSessionReady obj)
        {
            if (!IsFenixAircraft || IsSessionInitializing || IsSessionInitialized)
                return;
            IsSessionInitializing = true;
            SessionStopRequested = false;            

            try
            {
                Logger.Debug($"Refresh Token");
                RefreshToken();

                if (App.Config.RunGsxService)
                {
                    Logger.Debug($"Start GsxService");
                    GsxService.Start();
                }

                if (App.Config.RunAudioService)
                {
                    Logger.Debug($"Start AudioService");
                    AudioService.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            IsSessionInitialized = true;
            IsSessionInitializing = false;
        }

        public virtual async Task RestartGsx()
        {
            Logger.Debug($"Kill Couatl Process");
            Sys.KillProcess(App.Config.BinaryGsx2020);
            Sys.KillProcess(App.Config.BinaryGsx2024);

            Logger.Debug($"Wait for Binary Start ({Config.DelayGsxBinaryStart}ms) ...");
            await Task.Delay(Config.DelayGsxBinaryStart, Token);

            if (SimService.Manager.GetSimVersion() == SimVersion.MSFS2020 && !Sys.GetProcessRunning(App.Config.BinaryGsx2020))
            {
                Logger.Debug($"Starting Process {App.Config.BinaryGsx2020}");
                string dir = Path.Join(GsxService.PathInstallation, "couatl64");
                Sys.StartProcess(Path.Join(dir, $"{App.Config.BinaryGsx2020}.exe"), dir);
            }

            if (SimService.Manager.GetSimVersion() == SimVersion.MSFS2024 && !Sys.GetProcessRunning(App.Config.BinaryGsx2024))
            {
                Logger.Debug($"Starting Process {App.Config.BinaryGsx2024}");
                string dir = Path.Join(GsxService.PathInstallation, "couatl64");
                Sys.StartProcess(Path.Join(dir, $"{App.Config.BinaryGsx2024}.exe"), dir);
            }

            await Task.Delay(Config.DelayGsxBinaryStart, Token);
        }

        protected override async Task MainLoop()
        {
            await Task.Delay(App.Config.TimerGsxCheck, Token);

            if (ResetRequested > AppResetRequest.None)
            {
                Logger.Debug($"Reset was requested: {ResetRequested}");
                OnSessionEnded(null);
                if (ResetRequested == AppResetRequest.App)
                    await Task.Delay(2500, Token);
                else
                    await RestartGsx();
                OnSessionReady(null);
                ResetRequested = AppResetRequest.None;
            }
        }

        protected override Task FreeResources()
        {
            base.FreeResources();
            ReceiverStore.Remove<MsgSessionReady>().OnMessage -= OnSessionReady;
            ReceiverStore.Remove<MsgSessionEnded>().OnMessage -= OnSessionEnded;
            return Task.CompletedTask;
        }
    }
}
