using CFIT.AppFramework;
using CFIT.AppLogger;
using Fenix2GSX.AppConfig;
using Fenix2GSX.UI;
using Fenix2GSX.UI.NotifyIcon;
using System;

namespace Fenix2GSX
{
    public class Fenix2GSX(Type windowType) : SimApp<Fenix2GSX, AppService, Config, Definition>(windowType, typeof(NotifyIconModelExt))
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                var app = new Fenix2GSX(typeof(AppWindow));
                return app.Start(args);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return -1;
            }
        }
    }
}
