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
using System.Threading.Tasks;

namespace Fenix2GSX
{
    public enum AppResetRequest
    {
        None = 0,
        App = 1,
        AppGsx = 2,
    }

    public class AppService(Config config) : AppService<Fenix2GSX, AppService, Config, Definition>(config)
    {
        public virtual GsxController GsxService { get; protected set; }
        public virtual AudioController AudioService { get; protected set; }
        public virtual AppResetRequest ResetRequested {  get; set; } = AppResetRequest.None;
        public virtual bool IsFenixAircraft => SimConnect.AircraftString.Contains(Config.FenixAircraftString, StringComparison.InvariantCultureIgnoreCase);

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

        protected virtual void OnSessionReady(MsgSessionReady obj)
        {
            if (!IsFenixAircraft)
                return;

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

        public virtual async Task RestartGsx()
        {
            Logger.Debug($"Kill Couatl Process");
            Sys.KillProcess(App.Config.BinaryGsx2020);
            Sys.KillProcess(App.Config.BinaryGsx2024);

            Logger.Debug($"Wait for Binary Start ({Config.DelayGsxBinaryStart}ms) ...");
            await Task.Delay(Config.DelayGsxBinaryStart, App.Token);

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

            await Task.Delay(Config.DelayGsxBinaryStart, App.Token);
        }

        protected override async Task MainLoop()
        {
            await Task.Delay(App.Config.TimerGsxCheck, App.Token);

            if (ResetRequested > AppResetRequest.None)
            {
                Logger.Debug($"Reset was requested: {ResetRequested}");
                OnSessionEnded(null);
                if (ResetRequested == AppResetRequest.App)
                    await Task.Delay(1500, App.Token);
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
