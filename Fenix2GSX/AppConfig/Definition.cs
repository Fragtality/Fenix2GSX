using CFIT.AppFramework.AppConfig;
using System.IO;

namespace Fenix2GSX.AppConfig
{
    public class Definition : ProductDefinitionBase
    {
        public override int BuildConfigVersion { get; } = 6;
        public override string ProductName => "Fenix2GSX";
        public override string ProductExePath => Path.Join(Path.Join(ProductPath, "bin"), ProductExe);
        public override bool ProductVersionCheckDev => true;
        public override bool RequireSimRunning => true;
        public override bool WaitForSim => false;
        public override bool SingleInstance => true;
        public override bool MainWindowShowOnStartup => false;
    }
}
