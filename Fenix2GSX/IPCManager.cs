using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Fenix2GSX
{
    public static class IPCManager
    {
        public static readonly int waitDuration = 30000;

        public static MobiSimConnect SimConnect { get; set; } = null;

        public static bool WaitForSimulator(ServiceModel model)
        {
            bool simRunning = IsSimRunning();
            if (!simRunning && model.WaitForConnect)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator not started - waiting {waitDuration / 1000}s for Sim");
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !model.CancellationRequested);

                Thread.Sleep(waitDuration);
                return true;
            }
            else if (simRunning)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSimulator", $"Simulator started");
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSimulator", $"Simulator not started - aborting");
                return false;
            }
        }

        public static bool IsProcessRunning(string name)
        {
            Process proc = Process.GetProcessesByName(name).FirstOrDefault();
            return proc != null;
        }

        public static bool IsSimRunning()
        {
            return IsProcessRunning("FlightSimulator");
        }
        
        public static bool WaitForConnection(ServiceModel model)
        {
            if (!IsSimRunning())
                return false;

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();

            if (!SimConnect.IsConnected)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"Connection not established - waiting {waitDuration / 1000}s for Retry");
                    Thread.Sleep(waitDuration / 2);
                    if (!mobiRequested)
                        mobiRequested = SimConnect.Connect();
                }
                while (!SimConnect.IsConnected && IsSimRunning() && !model.CancellationRequested);

                return SimConnect.IsConnected;
            }
            else
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForConnection", $"{model.FenixExecutable} is running");
                return true;
            }
        }
        
        public static bool WaitForFenixBinary(ServiceModel model)
        {
            if (!IsSimRunning())
                return false;

            bool isRunning = IsProcessRunning(model.FenixExecutable);
            if (!isRunning)
            {
                do
                {
                    Logger.Log(LogLevel.Information, "IPCManager:WaitForFenixBinary", $"{model.FenixExecutable} is not running - waiting {waitDuration / 2 / 1000}s for Retry");
                    Thread.Sleep(waitDuration / 2);

                    isRunning = IsProcessRunning(model.FenixExecutable);
                }
                while (!isRunning && IsSimRunning() && !model.CancellationRequested);

                return isRunning && IsSimRunning();
            }
            else
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForFenixBinary", $"{model.FenixExecutable} is running");
                return true;
            }
        }
        
        public static bool WaitForSessionReady(ServiceModel model)
        {
            int waitDuration = 5000;
            SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");
            Thread.Sleep(250);
            bool isReady = IsCamReady();
            while (IsSimRunning() && !isReady && !model.CancellationRequested)
            {
                Logger.Log(LogLevel.Information, "IPCManager:WaitForSessionReady", $"Session not ready - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                Logger.Log(LogLevel.Error, "IPCManager:WaitForSessionReady", $"SimConnect or Simulator not available - aborting");
                return false;
            }

            return true;
        }

        public static bool IsCamReady()
        {
            float value = SimConnect.ReadSimVar("CAMERA STATE", "Enum");

            return value >= 2 && value <= 5;
        }

        public static void CloseSafe()
        {
            try
            {
                if (SimConnect != null)
                {
                    SimConnect.Disconnect();
                    SimConnect = null;
                }
            }
            catch { }
        }
    }
}
