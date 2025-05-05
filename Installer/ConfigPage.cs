using CFIT.Installer.Product;
using CFIT.Installer.UI.Behavior;
using CFIT.Installer.UI.Config;

namespace Installer
{
    public class ConfigPage : PageConfig
    {
        public Config Config { get { return BaseConfig as Config; } }

        public override void CreateConfigItems()
        {
            ConfigItemHelper.CreateCheckboxDesktopLink(Config, ConfigBase.OptionDesktopLink, Items);
            ConfigItemHelper.CreateRadioAutoStart(Config, Items);
            if (Config.Mode == SetupMode.UPDATE)
                Items.Add(new ConfigItemCheckbox("Reset Configuration", "Reset App Configuration to Default (only for Troubleshooting)", Config.OptionResetConfiguration, Config));
            if (Config.GetOption<bool>(Config.StateRemoveMobiAllowed))
                Items.Add(new ConfigItemCheckbox("Remove Mobiflight Module", "Remove the Mobiflight Module from MSFS' Community Folder (not required anymore for Fenix2GSX).\n\nATTENTION: Make sure no other Application is using it before removing it!\n(You will only see this Option if MobiFlight Connector and PilotsDeck are not detected as installed)", Config.OptionRemoveMobiflight, Config));
        }
    }
}
