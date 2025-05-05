using CFIT.AppFramework;
using CFIT.AppFramework.UI.NotifyIcon;
using CommunityToolkit.Mvvm.Input;

namespace Fenix2GSX.UI.NotifyIcon
{
    public partial class NotifyIconModelExt(ISimApp simApp) : NotifyIconViewModel(simApp)
    {
        protected override void CreateItems()
        {
            base.CreateItems();
            Items.Add(new(null, null));
            Items.Add(new($"Restart {SimApp.DefinitionBase.ProductName}", RestartAppCommand));
            Items.Add(new($"Restart {SimApp.DefinitionBase.ProductName} and GSX", RestartAppGsxCommand));
        }

        [RelayCommand]
        public virtual void RestartApp()
        {
            try { (SimApp as Fenix2GSX).AppService.ResetRequested = AppResetRequest.App; } catch { }
        }

        [RelayCommand]
        public virtual void RestartAppGsx()
        {
            try { (SimApp as Fenix2GSX).AppService.ResetRequested = AppResetRequest.AppGsx; } catch { }
        }
    }
}
