using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.Installer.Product;
using System;
using System.IO;

namespace Installer
{
    public class Config : ConfigBase
    {
        public override string ProductName { get { return "Fenix2GSX"; } }
        public override string ProductExePath { get { return Path.Combine(ProductPath, "bin", ProductExe); } }
        public virtual string InstallerExtractDir { get { return Path.Combine(ProductPath, "bin"); } }

        public static readonly string OptionResetConfiguration = "ResetConfiguration";
        public static readonly string OptionRemoveMobiflight = "RemoveMobiflight";
        public static readonly string StateRemoveMobiAllowed = "RemoveMobiAllowed";

        //Worker: .NET
        public virtual bool NetRuntimeDesktop { get; set; } = true;
        public virtual string NetVersion { get; set; } = "8.0.19";
        public virtual bool CheckMajorEqual { get; set; } = true;
        public virtual string NetUrl { get; set; } = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.19/windowsdesktop-runtime-8.0.19-win-x64.exe";
        public virtual string NetInstaller { get; set; } = "windowsdesktop-runtime-8.0.19-win-x64.exe";

        public override void CheckInstallerOptions()
        {
            base.CheckInstallerOptions();

            //ResetConfig
            SetOption(OptionResetConfiguration, false);

            //Removal of Mobi Flight
            SetOption(OptionRemoveMobiflight, false);
            if (MobiInstalled() || PilotsdeckInstalled())
                SetOption(StateRemoveMobiAllowed, false);
            else
                SetOption(StateRemoveMobiAllowed, true);
        }

        public static bool MobiInstalled()
        {
            bool result;
            try
            {
                result = !string.IsNullOrWhiteSpace(Sys.GetRegistryValue<string>(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MobiFlight Connector", "UninstallString"));
            }
            catch (Exception ex)
            {
                result = false;
                Logger.LogException(ex);                
            }
            return result;
        }

        public static bool PilotsdeckInstalled()
        {
            bool result;
            try
            {
                result = File.Exists(Path.Combine(Sys.FolderAppDataRoaming(), @"Elgato\StreamDeck\Plugins\com.extension.pilotsdeck.sdPlugin\PilotsDeck.exe"));
            }
            catch (Exception ex)
            {
                result = false;
                Logger.LogException(ex);
            }
            return result;
        }
    }
}
