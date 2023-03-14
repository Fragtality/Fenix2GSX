using Serilog;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Fenix2GSX
{
    public static class IPCManager
    {
        public static readonly int waitDuration = 30000;

        public static MobiSimConnect SimConnect { get; set; } = null;

        public static bool WaitForSimulator(CancellationToken cancellationToken)
        {
            bool simRunning = IsSimRunning();
            if (!simRunning && Program.waitForConnect)
            {
                do
                {
                    Log.Logger.Information($"WaitForSimulator: Simulator not started - waiting {waitDuration/1000}s for Sim");
                    Thread.Sleep(waitDuration);
                }
                while (!IsSimRunning() && !cancellationToken.IsCancellationRequested);

                Thread.Sleep(waitDuration);
                return true;
            }
            else if (simRunning)
            {
                Log.Logger.Information($"WaitForSimulator: Simulator started");
                return true;
            }
            else
            {
                Log.Logger.Error($"WaitForSimulator: Simulator not started - aborting");
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

        public static bool WaitForConnection(CancellationToken cancellationToken)
        {
            if (!IsSimRunning())
                return false;

            SimConnect = new MobiSimConnect();
            bool mobiRequested = SimConnect.Connect();

            if (!SimConnect.IsConnected)
            {
                do
                {
                    Log.Logger.Information($"WaitForConnection: Connection not established - waiting {waitDuration / 1000}s for Retry");
                    Thread.Sleep(waitDuration / 2);
                    if (!mobiRequested)
                        mobiRequested = SimConnect.Connect();
                }
                while (!SimConnect.IsConnected && IsSimRunning() && !cancellationToken.IsCancellationRequested);

                return SimConnect.IsConnected;
            }
            else
            {
                Log.Logger.Information($"WaitForFenixBinary: {Program.FenixExecutable} is running");
                return true;
            }
        }

        public static bool WaitForFenixBinary(CancellationToken cancellationToken)
        {
            if (!IsSimRunning())
                return false;

            bool isRunning = IsProcessRunning(Program.FenixExecutable);
            if (!isRunning)
            {
                do
                {
                    Log.Logger.Information($"WaitForFenixBinary: {Program.FenixExecutable} is not running - waiting {waitDuration / 2 / 1000}s for Retry");
                    Thread.Sleep(waitDuration / 2);

                    isRunning = IsProcessRunning(Program.FenixExecutable);
                }
                while (!isRunning && IsSimRunning() && !cancellationToken.IsCancellationRequested);

                return isRunning && IsSimRunning();
            }
            else
            {
                Log.Logger.Information($"WaitForFenixBinary: {Program.FenixExecutable} is running");
                return true;
            }
        }

        public static bool WaitForSessionReady(CancellationToken cancellationToken)
        {
            int waitDuration = 5000;
            SimConnect.SubscribeSimVar("CAMERA STATE", "Enum");
            Thread.Sleep(250);
            bool isReady = IsCamReady();
            while (IsSimRunning() && !isReady && !cancellationToken.IsCancellationRequested)
            {
                Log.Logger.Information($"WaitForSessionReady: Session not ready - waiting {waitDuration / 1000}s for Retry");
                Thread.Sleep(waitDuration);
                isReady = IsCamReady();
            }

            if (!isReady)
            {
                Log.Logger.Error($"WaitForSessionReady: SimConnect or Simulator not available - aborting");
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

        //public static float ReadLVar(string name)
        //{
        //    return SimConnect.ReadLvar(name);
        //}

        //public static float ReadSimVar(string name, string unit)
        //{
        //    return SimConnect.ReadSimVar(name, unit);
        //}
    }
}
