using CFIT.AppFramework;
using CFIT.AppFramework.UI.NotifyIcon;

namespace Fenix2GSX.UI.NotifyIcon
{
    public partial class NotifyIconModelExt(ISimApp simApp) : NotifyIconViewModel(simApp)
    {
        protected override void CreateItems()
        {
            base.CreateItems();
            Items.Add(new(null, null));
        }
    }
}
