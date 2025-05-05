using CFIT.AppLogger;
using CFIT.Installer.LibFunc;
using CFIT.Installer.LibWorker;
using System.IO;
using System.Threading;

namespace Installer
{
    public class WorkerInstallUpdate : WorkerAppInstall<Config>
    {
        public bool ResetConfiguration { get; set; } = false;

        public WorkerInstallUpdate(Config config) : base(config)
        {
            SetPropertyFromOption<bool>(Config.OptionResetConfiguration);
        }        

        protected override void CreateFileExclusions()
        {
            
        }

        protected override bool DeleteOldFiles()
        {
            FuncIO.DeleteDirectory(Path.Combine(Config.ProductPath, "log"), true, true);
            FuncIO.DeleteDirectory(InstallerExtractDir, true, true);

            if (File.Exists(Config.ProductConfigPath) && ResetConfiguration)
            {
                Logger.Debug($"Deleting Config File '{Config.ProductConfigPath}'");
                FuncIO.DeleteFile(Config.ProductConfigPath);
            }

            return Directory.Exists(InstallerExtractDir);
        }

        protected override bool CreateDefaultConfig()
        {
            using (var stream = GetAppConfig())
            {
                var confStream = File.Create(Config.ProductConfigPath);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(confStream);
                confStream.Flush(true);
                confStream.Close();
            }
            Thread.Sleep(250);
            return Config.HasConfigFile;
        }

        protected override bool FinalizeSetup()
        {
            string logDir = Path.Combine(Config.ProductPath, "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            return Directory.Exists(logDir);
        }
    }
}
