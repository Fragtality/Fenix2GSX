using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Session;

namespace Fenix2GSX
{
    public class ServiceController
    {
        private int flightState = 0; //0
        private bool planePositioned = false;
        private bool connectCalled = false;
        private bool pcaCalled = false;
        private bool refueling = false;
        private bool refuelPaused = false;
        private bool refuelFinished = false; //f
        private bool cateringFinished = false; //f
        private bool refuelRequested = false; //f
        private bool cateringRequested = false;
        private bool boarding = false;
        private bool boardingRequested = false;
        private bool boardFinished = false;
        private bool finalLoadsheetSend = false;
        private bool equipmentRemoved = false;
        private bool deboarding = false;
        private int delayCounter = 0;
        private int delay = 0;
        private string flightPlanID = "0";

        private bool testArrival = Program.testArrival;

        private MobiSimConnect SimConnect;
        private FenixContoller FenixController;
        private InputSimulator InputSimulator;
        private IAudioSession gsxAudioSession = null;
        private int gsxAudioVolume = -1;
        private int gsxAudioMute = -1;

        public int Interval { get; set; } = 1000;

        public ServiceController(MobiSimConnect simConnect, IEnumerable<CoreAudioDevice> devices)
        {
            SimConnect = simConnect;
            SimConnect.SubscribeSimVar("SIM ON GROUND", "Bool");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_CATERING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_REFUELING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_DEPARTURE_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_DEICING_STATE");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL");
            SimConnect.SubscribeLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO");
            SimConnect.SubscribeLvar("FSDT_GSX_BOARDING_CARGO_PERCENT");
            SimConnect.SubscribeLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT");
            SimConnect.SubscribeLvar("FSDT_GSX_FUELHOSE_CONNECTED");
            SimConnect.SubscribeLvar("FSDT_VAR_EnginesStopped");
            SimConnect.SubscribeLvar("FSDT_GSX_COUATL_STARTED");
            SimConnect.SubscribeLvar("FSDT_GSX_JETWAY");
            SimConnect.SubscribeLvar("FSDT_GSX_STAIRS");
            SimConnect.SubscribeLvar("S_MIP_PARKING_BRAKE");
            SimConnect.SubscribeLvar("S_OH_EXT_LT_BEACON");
            SimConnect.SubscribeLvar("I_OH_ELEC_EXT_PWR_L");
            
            FenixController = new();
            InputSimulator = new();

            if (Program.gsxVolumeControl && devices != null)
            {
                SimConnect.SubscribeLvar("I_ASP_INT_REC");
                SimConnect.SubscribeLvar("A_ASP_INT_VOLUME");

                foreach (var device in devices)
                {
                    if (device.SessionController != null)
                    {
                        foreach (var session in device.SessionController.ActiveSessions())
                        {
                            if (!string.IsNullOrWhiteSpace(session.ExecutablePath) && session.ExecutablePath.Contains("couatl64_MSFS"))
                            {
                                gsxAudioSession = session;
                                break;
                            }
                        }
                    }
                }
            }

            if (testArrival)
                FenixController.Update(true);
        }

        public void ResetAudio()
        {
            if (Program.gsxVolumeControl && gsxAudioSession != null)
            {
                gsxAudioSession.Volume = 100;
                gsxAudioSession.IsMuted = false;
            }
        }

        public void ControlAudio()
        {
            if (!Program.gsxVolumeControl || gsxAudioSession == null)
                return;

            int volume = (int)(SimConnect.ReadLvar("A_ASP_INT_VOLUME") * 100.0f);
            int muted = (int)SimConnect.ReadLvar("I_ASP_INT_REC");

            if (volume >= 0 && volume != gsxAudioVolume)
            {
                gsxAudioSession.Volume = volume;
                gsxAudioVolume = volume;
            }

            if (muted >= 0 && muted != gsxAudioMute)
            {
                gsxAudioSession.IsMuted = muted == 0;
                gsxAudioMute = muted;
            }
        }

        public void RunServices()
        {
            bool simOnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            FenixController.Update(false);

            //Pre-Flight - First-Flight
            if (flightState == 0 && simOnGround)
            {
                if (testArrival)
                {
                    flightState = 3; // in Flight
                    FenixController.Update(true);
                    Log.Logger.Information("Test Arrival: Plane is in 'Flight'");
                    return;
                }
                Interval = 1000;

                if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Log.Logger.Information("Couatl Engine not running");
                    return;
                }

                if (Program.repositionPlane && !planePositioned)
                {
                    if (Program.operatorDelay > 0)
                        OperatorDelay("before Reposition");
                    Log.Logger.Information("Repositioning Plane");
                    MenuOpenByVar();
                    MenuItem(0);
                    MenuItem(1);
                    planePositioned = true;
                    Thread.Sleep(3000);
                    return;
                }

                if (Program.autoConnect && !connectCalled)
                {
                    if (Program.operatorDelay > 0 && !Program.repositionPlane)
                        OperatorDelay("before Service Call");
                    CallJetwayStairs(Program.operatorDelay > 0);
                    connectCalled = true;
                    return;
                }

                if (Program.connectPCA && !pcaCalled)
                {
                    Log.Logger.Information("Connecting PCA");
                    FenixController.SetServicePCA(true);
                    pcaCalled = true;
                    return;
                }

                if (FenixController.IsFlightplanLoaded())
                {
                    flightState = 1;
                    flightPlanID = FenixController.flightPlanID;
                    int numPax = FenixController.GetPaxPlanned();
                    SetPassengers(numPax);
                    Log.Logger.Information($"Current State: Preflight - At Depature Gate. Passengers Count set to {numPax}.");
                }
            }
            //Special Case: loaded in Flight
            if (flightState == 0 && !simOnGround)
            {
                FenixController.Update(true);
                SetPassengers(FenixController.GetPaxPlanned());
                flightPlanID = FenixController.flightPlanID;

                flightState = 3;
                Interval = 180000;
                Log.Logger.Information("Current State: Flight");
                return;
            }

            //At Depature Gate
            int refuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            int cateringState = (int)SimConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            if (flightState == 1 && (!refuelFinished || !boardFinished))
            {
                Interval = 1000;
                if (Program.autoRefuel)
                {
                    if (!refuelRequested && refuelState != 6)
                    {
                        Log.Logger.Information("Calling Refuel Service");
                        MenuOpenByVar();
                        MenuItem(3);
                        refuelRequested = true;
                        return;
                    }

                    if (Program.callCatering && !cateringRequested && cateringState != 6)
                    {
                        Log.Logger.Information("Calling Catering Service");
                        MenuOpenByVar();
                        MenuItem(2);
                        cateringRequested = true;
                        return;
                    }
                }

                if (!cateringFinished && cateringState == 6)
                {
                    cateringFinished = true;
                    Log.Logger.Information("Catering finished");
                }

                if (Program.autoBoarding)
                { 
                    if (!boardingRequested && refuelFinished && ((Program.callCatering && cateringFinished) || !Program.callCatering))
                    {
                        if (delayCounter == 0)
                            Log.Logger.Information("Waiting 90s before calling Boarding");

                        if (delayCounter < 90)
                            delayCounter++;
                        else
                        {
                            Log.Logger.Information("Calling Boarding Service");
                            MenuOpenByVar();
                            MenuItem(4);
                            delayCounter = 0;
                            boardingRequested = true;
                        }
                        return;
                    }
                }

                if (!refueling && !refuelFinished && refuelState == 5)
                {
                    refueling = true;
                    refuelPaused = true;
                    Log.Logger.Information("Fuel Service active.");
                    FenixController.RefuelStart();
                }
                else if (refueling)
                {
                    if (SimConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                    {
                        if (refuelPaused)
                        {
                            Log.Logger.Information("Fuel Hose connected - refueling");
                            refuelPaused = false;
                        }

                        if (FenixController.Refuel())
                        {
                            refueling = false;
                            refuelFinished = true;
                            refuelPaused = false;
                            FenixController.RefuelStop();
                            Log.Logger.Information("Refuel completed.");
                        }
                    }
                    else
                    {
                        if (!refuelPaused && !refuelFinished)
                        {
                            Log.Logger.Information("Fuel Hose disconnected - waiting for next Truck.");
                            refuelPaused = true;
                        }
                    }
                }

                if (!boarding && !boardFinished && SimConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") >= 4)
                {
                    boarding = true;
                    FenixController.BoardingStart();
                    Log.Logger.Information("Boarding Service active.");
                }
                else if (boarding)
                {
                    if (FenixController.Boarding((int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL"), (int)SimConnect.ReadLvar("FSDT_GSX_BOARDING_CARGO_PERCENT")))
                    {
                        boarding = false;
                        boardFinished = true;
                        FenixController.BoardingStop();
                        Log.Logger.Information("Boarding completed.");
                    }
                }

                return;
            }

            //At Depature -> Prepare Depature
            if (flightState == 1 && refuelFinished && boardFinished)
            {
                if (!finalLoadsheetSend)
                {
                    if (delay == 0)
                    {
                        delay = new Random().Next(90, 150);
                        delayCounter = 0;
                        Log.Logger.Information($"Sending Final Loadsheet in {delay}s");
                    }

                    if (delayCounter < delay)
                    {
                        delayCounter++;
                        return;
                    }
                    else
                    {
                        Log.Logger.Information($"Sending Final Loadsheet!");
                        FenixController.TriggerFinal();
                        finalLoadsheetSend = true;
                    }
                }
                else if (!equipmentRemoved)
                {
                    equipmentRemoved = SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1 && SimConnect.ReadLvar("I_OH_ELEC_EXT_PWR_L") == 0;
                    if (equipmentRemoved)
                    {
                        Log.Logger.Information("Preparing for Pushback, disconnecting");
                        if (SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)
                        {
                            MenuOpenByVar();
                            Log.Logger.Information("Calling Jetway");
                            MenuItem(6);
                        }
                        FenixController.SetServiceChocks(false);
                        FenixController.SetServicePCA(false);
                        FenixController.SetServiceGPU(false);
                    }
                }
                else //At Depature -> Prepare Depature
                {
                    flightState = 2;
                    Log.Logger.Information("Current State: Departing");
                    delay = 0;
                    delayCounter = 0;
                    Interval = 60000;
                }
                
                return;
            }

            if (flightState <= 3)
            {
                //Taxi Out -> Flight
                if (flightState <= 2 && !simOnGround)
                {
                    flightState = 3;
                    if (flightState <= 1) //in flight restart
                    {
                        Log.Logger.Information("In-Flight restart detected");
                        FenixController.Update(true);
                        flightPlanID = FenixController.flightPlanID;
                    }
                    Log.Logger.Information("Current State: Flight");
                    Interval = 180000;
                    
                    return;
                }

                //Flight -> Taxi In
                if (flightState == 3 && simOnGround)
                {
                    flightState = 4;
                    Log.Logger.Information("Current State: Arriving");

                    Interval = 2500;
                    if (testArrival)
                        flightPlanID = FenixController.flightPlanID;
                    pcaCalled = false;
                    connectCalled = false;
                                        
                    return;
                }
            }

            //Taxi In -> At Arrival Gate
            int deboard_state = (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
            if (flightState == 4 && SimConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1)
            {
                if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Log.Logger.Information("Couatl Engine not running");
                    return;
                }

                if (Program.autoConnect && !connectCalled)
                {
                    CallJetwayStairs(Program.operatorDelay > 0);
                    connectCalled = true;
                    return;
                }

                if (SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1)
                    return;

                if (Program.connectPCA && !pcaCalled)
                {
                    Log.Logger.Information("Connecting PCA");
                    FenixController.SetServicePCA(true);
                    pcaCalled = true;
                }

                FenixController.SetServiceChocks(true);
                FenixController.SetServiceGPU(true);
                SetPassengers(FenixController.GetPaxPlanned());

                flightState = 5;
                Log.Logger.Information("Current State: At Arrival Gate");

                if (Program.autoDeboarding && deboard_state < 4)
                {
                    Log.Logger.Information("Calling Deboarding Service");
                    MenuOpenByVar();
                    MenuItem(1);
                }

                return;
            }

            //At Arrival Gate
            if (flightState == 5 && deboard_state >= 4)
            {
                if (!deboarding)
                {
                    deboarding = true;
                    FenixController.DeboardingStart();
                    Interval = 1000;
                    Log.Logger.Information($"Pax on Board: {FenixController.GetPaxCurrent()}");
                    return;
                }
                else if (deboarding)
                {
                    if (FenixController.Deboarding((int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL"), (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")) || deboard_state == 6)
                    {
                        deboarding = false;
                        Log.Logger.Information("Deboarding finished.");
                        FenixController.DeboardingStop();
                        Log.Logger.Information("Current State: Turn-Around - Waiting for new Flightplan");
                        flightState = 6;
                        Interval = 10000;
                        return;
                    }
                }
            }

            //Pre-Flight - Turn-Around
            if (flightState == 6)
            {
                if (FenixController.IsFlightplanLoaded() && FenixController.flightPlanID != flightPlanID)
                {
                    flightPlanID = FenixController.flightPlanID;
                    flightState = 1;
                    planePositioned = true;
                    connectCalled = true;
                    pcaCalled = true;
                    refueling = false;
                    refuelPaused = false;
                    refuelFinished = false;
                    cateringFinished = false;
                    refuelRequested = false;
                    cateringRequested = false;
                    boarding = false;
                    boardingRequested = false;
                    boardFinished = false;
                    finalLoadsheetSend = false;
                    equipmentRemoved = false;
                    deboarding = false;
                    delayCounter = 0;
                    delay = 0;

                    int numPax = FenixController.GetPaxPlanned();
                    SetPassengers(numPax);
                    Log.Logger.Information($"Current State: Preflight - At Depature Gate. Passengers Count set to {numPax}.");
                }
            }
        }

        private void SetPassengers(int numPax)
        {
            SimConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", numPax);
            if (Program.disableCrew)
            {
                Log.Logger.Information("Crew Boarding disabled");
                SimConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_CREW_NOT_BOARDING", 1);
            }
        }

        private void OperatorDelay(string msg = "for Operator Selection")
        {
            Log.Logger.Information($"Sleeping {Program.operatorDelay}s {msg}");
            Thread.Sleep((int)(Program.operatorDelay * 1000));
        }

        private void CallJetwayStairs(bool useDelay)
        {
            MenuOpenByVar();

            if (SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)
            {
                Log.Logger.Information("Calling Jetway");
                MenuItem(6);
                if (useDelay)
                    OperatorDelay();

                if (SimConnect.ReadLvar("FSDT_GSX_STAIRS") != 2)
                {
                    MenuOpenByVar();
                    Log.Logger.Information("Calling Stairs");
                    MenuItem(7);
                }
            }
            else
            {
                Log.Logger.Information("Calling Stairs");
                MenuItem(7);
                if (useDelay)
                    OperatorDelay();
            }
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private void SetForeground()
        {
            var msfsProc = Process.GetProcessesByName("FlightSimulator").FirstOrDefault();
            if (msfsProc != null)
                SetForegroundWindow(msfsProc.MainWindowHandle);
            Thread.Sleep(250);
        }

        private void MenuOpenByKey()
        {
            SetForeground();
            //Log.Logger.Debug("Opening Menu by Key");
            InputSimulator.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.LCONTROL, VirtualKeyCode.LSHIFT }, VirtualKeyCode.F12);
        }

        private void MenuOpenByVar()
        {
            MenuOpenByKey();
            //SetForeground();
            //Log.Logger.Debug("Opening Menu by Var");
            SimConnect.WriteLvar("FSDT_GSX_MENU_OPEN", 1);
            Thread.Sleep(3000);
        }

        private void MenuItem(int index)
        {
            //Log.Logger.Debug($"Selecting Item {index}");
            InputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_0 + index);
            Thread.Sleep(250);
        }
    }
}
