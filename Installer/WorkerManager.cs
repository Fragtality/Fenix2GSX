using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using CFIT.Installer.Product;

namespace Installer
{
    public class WorkerManager : WorkerManagerBase
    {
        public Config Config { get { return BaseConfig as Config; } }

        public WorkerManager(Config config) : base(config)
        {

        }

        protected void CreateInstallUpdateTasks(SetupMode key)
        {
            WorkerQueues[key].Enqueue(new WorkerDotNet<Config>(Config));
            if (Config?.GetOption<bool>(Config.StateRemoveMobiAllowed) == true)
            {
                var worker = new WorkerPackagePaths<Config>(Config);
                worker.SearchSimulators.Add(Simulator.MSFS2020);
                worker.SearchSimulators.Add(Simulator.MSFS2024);
                WorkerQueues[key].Enqueue(worker);
            }
            WorkerQueues[key].Enqueue(new WorkerInstallUpdate(Config));
            WorkerQueues[key].Enqueue(new WorkerAutoStart<Config>(Config));
            if (Config?.GetOption<bool>(ConfigBase.OptionDesktopLink) == true)
                WorkerQueues[key].Enqueue(new WorkerDesktopLinkCreate<Config>(Config));
            if (Config?.GetOption<bool>(Config.OptionRemoveMobiflight) == true)
                WorkerQueues[key].Enqueue(new WorkerRemoveMobi(Config));
        }

        protected override void CreateInstallTasks()
        {
            CreateInstallUpdateTasks(SetupMode.INSTALL);
        }

        protected override void CreateRemovalTasks()
        {
            WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerAppRemove<Config>(Config));
            if (Config.Mode == SetupMode.REMOVE)
                Config.SetOption(ConfigBase.OptionAutoStartTargets, SimAutoStart.NOAUTO);
            WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerAutoStart<Config>(Config));
            WorkerQueues[SetupMode.REMOVE].Enqueue(new WorkerDesktopLinkRemove<Config>(Config));
        }

        protected override void CreateUpdateTasks()
        {
            CreateInstallUpdateTasks(SetupMode.UPDATE);
        }
    }
}
