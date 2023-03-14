using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CefSharp;
using CefSharp.OffScreen;
using AudioSwitcher.AudioApi.CoreAudio;

namespace Fenix2GSX
{
    public class Program
    {
        public static readonly string FenixExecutable = Convert.ToString(ConfigurationManager.AppSettings["FenixExecutable"]) ?? "FenixSystem";
        public static readonly string logFilePath = Convert.ToString(ConfigurationManager.AppSettings["logFilePath"]) ?? "Fenix2GSX.log";
        public static readonly string logLevel = Convert.ToString(ConfigurationManager.AppSettings["logLevel"]) ?? "Debug";
        public static readonly bool waitForConnect = Convert.ToBoolean(ConfigurationManager.AppSettings["waitForConnect"]);
        public static readonly bool testArrival = Convert.ToBoolean(ConfigurationManager.AppSettings["testArrival"]);
        public static readonly bool gsxVolumeControl = Convert.ToBoolean(ConfigurationManager.AppSettings["gsxVolumeControl"]);
        public static readonly bool disableCrew = Convert.ToBoolean(ConfigurationManager.AppSettings["disableCrew"]);
        public static readonly bool repositionPlane = Convert.ToBoolean(ConfigurationManager.AppSettings["repositionPlane"]);
        public static readonly bool autoConnect = Convert.ToBoolean(ConfigurationManager.AppSettings["autoConnect"]);
        public static readonly bool connectPCA = Convert.ToBoolean(ConfigurationManager.AppSettings["connectPCA"]);
        public static readonly bool autoRefuel = Convert.ToBoolean(ConfigurationManager.AppSettings["autoRefuel"]);
        public static readonly bool callCatering = Convert.ToBoolean(ConfigurationManager.AppSettings["callCatering"]);
        public static readonly bool autoBoarding = Convert.ToBoolean(ConfigurationManager.AppSettings["autoBoarding"]);
        public static readonly bool autoDeboarding = Convert.ToBoolean(ConfigurationManager.AppSettings["autoDeboarding"]);
        public static readonly float refuelRateKGS = Convert.ToSingle(ConfigurationManager.AppSettings["refuelRateKGS"]);

        public static CoreAudioController AudioController = null;
        public static IEnumerable<CoreAudioDevice> AudioDevices = null;

        private static CancellationToken cancellationToken;
        private static bool cancelRequested = false;

        static void Main()
        {
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
            Log.Information($"Program: Fenix2GSX started! Log Level: {logLevel} Log File: {logFilePath}");

            try
            {
                Log.Information("Initializing CefBrowser ...");
                var settings = new CefSettings();
                if (!Cef.IsInitialized)
                    Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);

                if (gsxVolumeControl)
                {
                    Log.Information("Initializing AudioController ...");
                    AudioController = new CoreAudioController();
                    AudioDevices = AudioController.GetPlaybackDevices();
                }

                CancellationTokenSource cancellationTokenSource = new();
                cancellationToken = cancellationTokenSource.Token;

                while (!cancellationToken.IsCancellationRequested && !cancelRequested)
                {
                    if (Wait())
                    {
                        MainLoop();
                    }
                    else
                    {
                        if (!IPCManager.IsSimRunning())
                        {
                            cancelRequested = true;
                            Log.Logger.Error($"Program: Session aborted, Retry not possible - exiting Program");
                        }
                        else
                        {
                            Reset();
                            Log.Logger.Information($"Program: Session aborted, Retry possible - Waiting for new Session");
                        }
                    }
                }

                IPCManager.CloseSafe();
                Cef.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Program: Critical Exception occured: {ex.Source} - {ex.Message}");
            }

            Log.Information($"Program: FNX2PLD terminated.");
        }

        private static void MainLoop()
        {
            var controller = new ServiceController(IPCManager.SimConnect, AudioDevices);
            Thread.Sleep(5000);
            int elapsedMS = controller.Interval;
            int delay = 100;
            while (!cancellationToken.IsCancellationRequested && IPCManager.IsProcessRunning(FenixExecutable) && IPCManager.IsSimRunning())
            {
                try
                {
                    if (elapsedMS >= controller.Interval)
                    {
                        controller.RunServices();
                        elapsedMS = 0;
                    }

                    if (gsxVolumeControl)
                        controller.ControlAudio();
                    
                    Thread.Sleep(delay);
                    elapsedMS += delay;
                }
                catch (Exception ex)
                {
                    Log.Logger.Error($"Program: Critical Exception during MainLoop() {ex.GetType()} {ex.Message} {ex.Source}");
                }
            }
            
            Log.Logger.Information($"Program: MainLoop ended.");
            if (Program.gsxVolumeControl)
            {
                Log.Logger.Information("Resetting GSX Audio");
                controller.ResetAudio();
            }
        }

        private static bool Wait()
        {
            if (!IPCManager.WaitForSimulator(cancellationToken))
                return false;

            if (!IPCManager.WaitForConnection(cancellationToken))
                return false;

            if (!IPCManager.WaitForFenixBinary(cancellationToken))
                return false;

            if (!IPCManager.WaitForSessionReady(cancellationToken))
                return false;

            return true;
        }

        private static void Reset()
        {
            try
            {
                IPCManager.SimConnect?.Disconnect();
                IPCManager.SimConnect = null;
            }
            catch
            {
                Log.Logger.Error($"Program: Exception during Reset()");
            }
        }
    }
}