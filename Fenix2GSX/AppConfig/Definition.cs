using CFIT.AppFramework.AppConfig;
using System.IO;

namespace Fenix2GSX.AppConfig
{
    public class Definition : ProductDefinitionBase
    {
        public override int BuildConfigVersion { get; } = 16;
        public override string ProductName => "Fenix2GSX";
        public override string ProductExePath => Path.Join(Path.Join(ProductPath, "bin"), ProductExe);
        public override bool ProductVersionCheckDev => true;
        public override bool RequireSimRunning => false;
        public override bool WaitForSim => true;
        public override bool SingleInstance => true;
        public override bool MainWindowShowOnStartup => AppService.Instance?.Config?.OpenAppWindowOnStart == true || AppService.Instance?.Config?.ForceOpen == true;
    }
}
