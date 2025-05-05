using CFIT.Installer.LibFunc;
using CFIT.Installer.Product;
using CFIT.Installer.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Installer
{
    public class WorkerRemoveMobi : TaskWorker<Config>
    {
        public virtual Dictionary<Simulator, string[]> MsfsPackagePaths { get; set; }
        public static readonly string ModuleName = "mobiflight-event-module";

        public WorkerRemoveMobi(Config config) : base(config, "Remove MobiFlight Module", "Checking Package Paths ...")
        {
            Model.DisplayInSummary = false;
            Model.DisplayCompleted = true;
        }

        protected override async Task<bool> DoRun()
        {
            int result = 0;

            if (MsfsPackagePaths == null || MsfsPackagePaths.Count <= 0)
            {
                if (!Config.HasOption(ConfigBase.OptionPackagePaths, out Dictionary<Simulator, string[]> paths) || paths?.Count == 0)
                {
                    Model.SetError($"No Package Paths for MSFS set in Options - abort!");
                    return false;
                }

                MsfsPackagePaths = paths;
            }

            if (MsfsPackagePaths.ContainsKey(Simulator.MSFS2020))
            {
                foreach (var packagePath in MsfsPackagePaths[Simulator.MSFS2020])
                {
                    string path = $@"{packagePath}\{ModuleName}";
                    if (Directory.Exists(path))
                    {
                        Model.SetState($"Removing MobiFlight Module for MSFS2020");
                        Directory.Delete(path, true);
                        if (Directory.Exists(path))
                            result--;
                    }
                }
            }

            await Task.Delay(150);

            if (MsfsPackagePaths.ContainsKey(Simulator.MSFS2024))
            {
                foreach (var packagePath in MsfsPackagePaths[Simulator.MSFS2024])
                {
                    string path = $@"{packagePath}\{ModuleName}";
                    if (Directory.Exists(path))
                    {
                        Model.SetState($"Removing MobiFlight Module for MSFS2024");
                        Directory.Delete(path, true);
                        if (Directory.Exists(path))
                            result--;
                    }
                }
            }

            if (result >= 0)
                Model.SetSuccess("MobiFlight Module removed from Community Folder.");

            return result >= 0;
        }
    }
}
