using CefSharp;
using CefSharp.OffScreen;
using H.NotifyIcon;
using Serilog;
using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Fenix2GSX
{
    public partial class App : Application
    {
        private ServiceModel Model;
        private ServiceController Controller;

        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Model = new();
            InitLog();
            InitSystray();
            InitCef();

            Controller = new(Model);
            Task.Run(Controller.Run);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
            timer.Start();

            MainWindow = new MainWindow(notifyIcon.DataContext as NotifyIconViewModel, Model);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Model.CancellationRequested = true;
            notifyIcon?.Dispose();
            Cef.Shutdown();
            base.OnExit(e);

            Logger.Log(LogLevel.Information, "App:OnExit", "Fenix2GSX exiting ...");
        }

        protected void OnTick(object sender, EventArgs e)
        {
            if (Model.ServiceExited)
            {
                Current.Shutdown();
            }
        }

        protected void InitLog()
        {
            string logFilePath = Model.GetSetting("logFilePath", "Fenix2GSX.log");
            string logLevel = Model.GetSetting("logLevel", "Debug");
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration().WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3,
                                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message} {NewLine}{Exception}");
            if (logLevel == "Warning")
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == "Debug")
                loggerConfiguration.MinimumLevel.Debug();
            else
                loggerConfiguration.MinimumLevel.Information();
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Information($"-----------------------------------------------------------------------");
            Logger.Log(LogLevel.Information, "App:InitLog", $"Fenix2GSX started! Log Level: {logLevel} Log File: {logFilePath}");
        }

        protected void InitSystray()
        {
            Logger.Log(LogLevel.Information, "App:InitSystray", $"Creating SysTray Icon ...");
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            notifyIcon.Icon = GetIcon("phoenix.ico");
            notifyIcon.ForceCreate();
        }

        protected void InitCef()
        {
            Logger.Log(LogLevel.Information, "App:InitCef", $"Initializing Cef Browser ...");
            var settings = new CefSettings();
            if (!Cef.IsInitialized)
                Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }

        public Icon GetIcon(string filename)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fenix2GSX.{filename}");
            return new Icon(stream);
        }
    }
}
