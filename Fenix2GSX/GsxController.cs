using CoreAudio;
using Microsoft.Win32;
using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Fenix2GSX
{
    public enum FlightState
    {
        PREP = 0,
        DEPATURE,
        TAXIOUT,
        FLIGHT,
        TAXIIN,
        ARRIVAL,
        TURNAROUND
    }

    public class GsxController
    {
        private readonly string pathMenuFile = @"\MSFS\fsdreamteam-gsx-pro\html_ui\InGamePanels\FSDT_GSX_Panel\menu";
        private readonly string registryPath = @"HKEY_CURRENT_USER\SOFTWARE\FSDreamTeam";
        private readonly string registryValue = @"root";
        private string menuFile = "";

        private FlightState state = FlightState.PREP;
        private bool planePositioned = false;
        private bool connectCalled = false;
        private bool pcaCalled = false;
        private bool refueling = false;
        private bool refuelPaused = false;
        private bool refuelFinished = false;
        private bool cateringFinished = false;
        private bool refuelRequested = false;
        private bool cateringRequested = false;
        private bool boarding = false;
        private bool boardingRequested = false;
        private bool boardFinished = false;
        private bool finalLoadsheetSend = false;
        private bool equipmentRemoved = false;
        private bool pcaRemoved = false;
        private bool deboarding = false;
        private int delayCounter = 0;
        private int delay = 0;
        private string flightPlanID = "0";
        private int paxPlanned = 0;
        private bool firstRun = true;

        private MobiSimConnect SimConnect;
        private FenixContoller FenixController;
        private ServiceModel Model;
        private AudioSessionControl2 gsxAudioSession = null;
        private float gsxAudioVolume = -1;
        private int gsxAudioMute = -1;

        public int Interval { get; set; } = 1000;

        public GsxController(ServiceModel model)
        {
            Model = model;

            SimConnect = IPCManager.SimConnect;
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
            SimConnect.SubscribeLvar("I_OH_ELEC_APU_START_U");
            SimConnect.SubscribeLvar("S_OH_PNEUMATIC_APU_BLEED");

            FenixController = new(Model);

            if (Model.GsxVolumeControl)
            {
                SimConnect.SubscribeLvar("I_ASP_INT_REC");
                SimConnect.SubscribeLvar("A_ASP_INT_VOLUME");
                SimConnect.SubscribeLvar("I_FCU_TRACK_FPA_MODE");
                SimConnect.SubscribeLvar("I_FCU_HEADING_VS_MODE");

                MMDeviceEnumerator deviceEnumerator = new(Guid.NewGuid());
                var devices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                foreach (var device in devices)
                {
                    foreach (var session in device.AudioSessionManager2.Sessions)
                    {
                        Process p = Process.GetProcessById((int)session.ProcessID);
                        if (p.ProcessName == "Couatl64_MSFS")
                        {
                            gsxAudioSession = session;
                            break;
                        }
                    }

                    if (gsxAudioSession != null)
                        break;
                }
            }

            string regPath = (string)Registry.GetValue(registryPath, registryValue, null) + pathMenuFile;
            if (Path.Exists(regPath))
                menuFile = regPath;

            if (Model.TestArrival)
                FenixController.Update(true);
        }

        public void ResetAudio()
        {
            if (gsxAudioSession != null)
            {
                gsxAudioSession.SimpleAudioVolume.MasterVolume = 1.0f;
                gsxAudioSession.SimpleAudioVolume.Mute = false;
            }
        }

        public void ControlAudio()
        {
            if (!Model.GsxVolumeControl || gsxAudioSession == null)
                return;

            if (!(SimConnect.ReadLvar("I_FCU_TRACK_FPA_MODE") == 1 || SimConnect.ReadLvar("I_FCU_HEADING_VS_MODE") == 1))
            {
                ResetAudio();
                return;
            }

            float volume = SimConnect.ReadLvar("A_ASP_INT_VOLUME");
            int muted = (int)SimConnect.ReadLvar("I_ASP_INT_REC");
            if (volume >= 0 && volume != gsxAudioVolume)
            {
                gsxAudioSession.SimpleAudioVolume.MasterVolume = volume;
                gsxAudioVolume = volume;
            }

            if (muted >= 0 && muted != gsxAudioMute)
            {
                gsxAudioSession.SimpleAudioVolume.Mute = muted == 0;
                gsxAudioMute = muted;
            }
        }

        public void RunServices()
        {
            bool simOnGround = SimConnect.ReadSimVar("SIM ON GROUND", "Bool") != 0.0f;
            FenixController.Update(false);

            //PREPARATION (On-Ground and Engines not running)
            if (state == FlightState.PREP && simOnGround && !FenixController.enginesRunning)
            {
                if (Model.TestArrival)
                {
                    state = FlightState.FLIGHT;
                    FenixController.Update(true);
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Test Arrival - Plane is in 'Flight'");
                    return;
                }
                Interval = 1000;

                if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Couatl Engine not running");
                    return;
                }

                if (Model.RepositionPlane && !planePositioned)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Waiting {Model.RepositionDelay}s before Repositioning ...");
                    FenixController.SetServiceChocks(true);
                    Thread.Sleep((int)(Model.RepositionDelay * 1000.0f));
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Repositioning Plane");
                    MenuOpen();
                    MenuItem(10);
                    Thread.Sleep(1500);
                    MenuItem(1);
                    planePositioned = true;
                    Thread.Sleep(1500);
                    return;
                }

                if (Model.AutoConnect && !connectCalled)
                {
                    CallJetwayStairs();
                    connectCalled = true;
                    return;
                }

                if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Connecting PCA");
                    FenixController.SetServicePCA(true);
                    pcaCalled = true;
                    return;
                }

                if (firstRun)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Setting GPU and Chocks");
                    FenixController.SetServiceChocks(true);
                    FenixController.SetServiceGPU(true);
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State: Preparation (Waiting for Flightplan import)");
                    firstRun = false;
                }

                if (FenixController.IsFlightplanLoaded())
                {
                    state = FlightState.DEPATURE;
                    flightPlanID = FenixController.flightPlanID;
                    SetPassengers(FenixController.GetPaxPlanned());
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Preparation -> Depature (Waiting for Refueling and Boarding)");
                }
            }
            //Special Case: loaded in Flight or with Engines Running
            if (state == FlightState.PREP && (!simOnGround || FenixController.enginesRunning))
            {
                FenixController.Update(true);
                flightPlanID = FenixController.flightPlanID;

                state = FlightState.FLIGHT;
                Interval = 180000;
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Current State is Flight.");

                if (simOnGround && FenixController.enginesRunning)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Starting on Runway - Removing Ground Equipment");
                    FenixController.SetServiceChocks(false);
                    FenixController.SetServiceGPU(false);
                }
                    
                return;
            }

            //DEPATURE - Boarding & Refueling
            int refuelState = (int)SimConnect.ReadLvar("FSDT_GSX_REFUELING_STATE");
            int cateringState = (int)SimConnect.ReadLvar("FSDT_GSX_CATERING_STATE");
            if (state == FlightState.DEPATURE && (!refuelFinished || !boardFinished))
            {
                Interval = 1000;
                if (Model.AutoRefuel)
                {
                    if (!refuelRequested && refuelState != 6)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Refuel Service");
                        MenuOpen();
                        MenuItem(3);
                        refuelRequested = true;
                        return;
                    }

                    if (Model.CallCatering && !cateringRequested && cateringState != 6)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Catering Service");
                        MenuOpen();
                        MenuItem(2);
                        OperatorSelection();
                        cateringRequested = true;
                        return;
                    }
                }

                if (!cateringFinished && cateringState == 6)
                {
                    cateringFinished = true;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Catering finished");
                }

                if (Model.AutoBoarding)
                {
                    if (!boardingRequested && refuelFinished && ((Model.CallCatering && cateringFinished) || !Model.CallCatering))
                    {
                        if (delayCounter == 0)
                            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Waiting 90s before calling Boarding");

                        if (delayCounter < 90)
                            delayCounter++;
                        else
                        {
                            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Boarding Service");
                            SetPassengers(FenixController.GetPaxPlanned());
                            MenuOpen();
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
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Fuel Service active");
                    FenixController.RefuelStart();
                }
                else if (refueling)
                {
                    if (SimConnect.ReadLvar("FSDT_GSX_FUELHOSE_CONNECTED") == 1)
                    {
                        if (refuelPaused)
                        {
                            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Fuel Hose connected - refueling");
                            refuelPaused = false;
                        }

                        if (FenixController.Refuel())
                        {
                            refueling = false;
                            refuelFinished = true;
                            refuelPaused = false;
                            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Refuel completed");
                        }
                    }
                    else
                    {
                        if (!refuelPaused && !refuelFinished)
                        {
                            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Fuel Hose disconnected - waiting for next Truck");
                            refuelPaused = true;
                        }
                    }
                }

                if (!boarding && !boardFinished && SimConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") >= 4)
                {
                    boarding = true;
                    FenixController.BoardingStart();
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Boarding Service active");
                }
                else if (boarding)
                {
                    if (FenixController.Boarding((int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_BOARDING_TOTAL"), (int)SimConnect.ReadLvar("FSDT_GSX_BOARDING_CARGO_PERCENT")) || SimConnect.ReadLvar("FSDT_GSX_BOARDING_STATE") == 6)
                    {
                        boarding = false;
                        boardFinished = true;
                        FenixController.BoardingStop();
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Boarding completed");
                    }
                }

                return;
            }

            //DEPATURE - Loadsheet & Ground-Equipment
            if (state == FlightState.DEPATURE && refuelFinished && boardFinished)
            {
                if (Model.ConnectPCA && !pcaRemoved)
                {
                    if (IPCManager.SimConnect.ReadLvar("I_OH_ELEC_APU_START_U") != 0 && IPCManager.SimConnect.ReadLvar("S_OH_PNEUMATIC_APU_BLEED") != 0)
                    {
                        FenixController.SetServicePCA(false);
                        pcaRemoved = true;
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"APU Bleed enabled - removing PCA");
                    }
                }

                if (!finalLoadsheetSend)
                {
                    if (delay == 0)
                    {
                        delay = new Random().Next(90, 150);
                        delayCounter = 0;
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Sending Final Loadsheet in {delay}s");
                    }

                    if (delayCounter < delay)
                    {
                        delayCounter++;
                        return;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Sending Final Loadsheet");
                        FenixController.TriggerFinal();
                        finalLoadsheetSend = true;
                    }
                }
                else if (!equipmentRemoved)
                {
                    equipmentRemoved = SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1 && SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1 && SimConnect.ReadLvar("I_OH_ELEC_EXT_PWR_L") == 0;
                    if (equipmentRemoved)
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Preparing for Pushback - removing Equipment");
                        //WORKAROUND
                        if (SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)
                        {
                            MenuOpen();
                            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Removing Jetway");
                            MenuItem(6);
                        }
                        FenixController.SetServiceChocks(false);
                        FenixController.SetServicePCA(false);
                        FenixController.SetServiceGPU(false);
                    }
                }
                else //DEPARTURE -> TAXIOUT
                {
                    state = FlightState.TAXIOUT;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Depature -> Taxi-Out");
                    delay = 0;
                    delayCounter = 0;
                    Interval = 60000;
                }

                return;
            }

            if (state <= FlightState.FLIGHT)
            {
                //TAXIOUT -> FLIGHT
                if (state <= FlightState.TAXIOUT && !simOnGround)
                {
                    if (state <= FlightState.DEPATURE) //in flight restart
                    {
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"In-Flight restart detected");
                        FenixController.Update(true);
                        flightPlanID = FenixController.flightPlanID;
                    }
                    state = FlightState.FLIGHT;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Taxi-Out -> Flight");
                    Interval = 180000;

                    return;
                }

                //FLIGHT -> TAXIIN
                if (state == FlightState.FLIGHT && simOnGround)
                {
                    state = FlightState.TAXIIN;
                    //FenixController.Update(true);
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Flight -> Taxi-In (Waiting for Engines stopped and Beacon off)");

                    Interval = 2500;
                    if (Model.TestArrival)
                        flightPlanID = FenixController.flightPlanID;
                    pcaCalled = false;
                    connectCalled = false;

                    return;
                }
            }

            //TAXIIN -> ARRIVAL - Ground Equipment
            int deboard_state = (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_STATE");
            if (state == FlightState.TAXIIN && SimConnect.ReadLvar("FSDT_VAR_EnginesStopped") == 1 && SimConnect.ReadLvar("S_MIP_PARKING_BRAKE") == 1)
            {
                if (SimConnect.ReadLvar("FSDT_GSX_COUATL_STARTED") != 1)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Couatl Engine not running");
                    return;
                }

                if (Model.AutoConnect && !connectCalled)
                {
                    CallJetwayStairs();
                    connectCalled = true;
                    return;
                }

                if (SimConnect.ReadLvar("S_OH_EXT_LT_BEACON") == 1)
                    return;

                if (Model.ConnectPCA && !pcaCalled && (!Model.PcaOnlyJetways || (Model.PcaOnlyJetways && SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)))
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Connecting PCA");
                    FenixController.SetServicePCA(true);
                    pcaCalled = true;
                }

                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Setting GPU and Chocks");
                FenixController.SetServiceChocks(true);
                FenixController.SetServiceGPU(true);
                SetPassengers(FenixController.GetPaxPlanned());

                state = FlightState.ARRIVAL;
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Taxi-In -> Arrival (Waiting for Deboarding)");

                if (Model.AutoDeboarding && deboard_state < 4)
                {
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Deboarding Service");
                    SetPassengers(FenixController.GetPaxPlanned());
                    MenuOpen();
                    MenuItem(1);
                    if (!Model.AutoConnect)
                        OperatorSelection();
                }

                return;
            }

            //ARRIVAL - Deboarding
            if (state == FlightState.ARRIVAL && deboard_state >= 4)
            {
                if (!deboarding)
                {
                    deboarding = true;
                    FenixController.DeboardingStart();
                    Interval = 1000;
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Deboarding Service active");
                    return;
                }
                else if (deboarding)
                {
                    if (SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") != paxPlanned)
                    {
                        Logger.Log(LogLevel.Warning, "GsxController:RunServices", $"Passenger changed during Boarding! Trying to reset Number ...");
                        SimConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", paxPlanned);
                    }

                    int paxCurrent = (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS") - (int)SimConnect.ReadLvar("FSDT_GSX_NUMPASSENGERS_DEBOARDING_TOTAL");
                    if (FenixController.Deboarding(paxCurrent, (int)SimConnect.ReadLvar("FSDT_GSX_DEBOARDING_CARGO_PERCENT")) || deboard_state == 6 || deboard_state == 1)
                    {
                        deboarding = false;
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Deboarding finished (GSX State {deboard_state})");
                        FenixController.DeboardingStop();
                        Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Arrival -> Turn-Around (Waiting for new Flightplan)");
                        state = FlightState.TURNAROUND;
                        Interval = 10000;
                        return;
                    }
                }
            }

            //Pre-Flight - Turn-Around
            if (state == FlightState.TURNAROUND)
            {
                if (FenixController.IsFlightplanLoaded() && FenixController.flightPlanID != flightPlanID)
                {
                    flightPlanID = FenixController.flightPlanID;
                    state = FlightState.DEPATURE;
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
                    pcaRemoved = false;
                    deboarding = false;
                    delayCounter = 0;
                    paxPlanned = 0;
                    delay = 0;

                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"State Change: Turn-Around -> Depature (Waiting for Refueling and Boarding)");
                }
            }
        }

        private void SetPassengers(int numPax)
        {
            SimConnect.WriteLvar("FSDT_GSX_NUMPASSENGERS", numPax);
            paxPlanned = numPax;
            Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Passenger Count set to {numPax}");
            if (Model.DisableCrew)
            {
                SimConnect.WriteLvar("FSDT_GSX_CREW_NOT_DEBOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_CREW_NOT_BOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_DEBOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_PILOTS_NOT_BOARDING", 1);
                SimConnect.WriteLvar("FSDT_GSX_NUMCREW", 0);
                SimConnect.WriteLvar("FSDT_GSX_NUMPILOTS", 0);
                SimConnect.WriteLvar("FSDT_GSX_CREW_ON_BOARD", 1);
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Crew Boarding disabled");
            }
        }

        private void CallJetwayStairs()
        {
            MenuOpen();

            if (SimConnect.ReadLvar("FSDT_GSX_JETWAY") != 2)
            {
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Jetway");
                MenuItem(6);
                OperatorSelection();

                if (SimConnect.ReadLvar("FSDT_GSX_STAIRS") != 2)
                {
                    Thread.Sleep(1500);
                    MenuOpen();
                    Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Stairs");
                    MenuItem(7);
                }
            }
            else
            {
                Logger.Log(LogLevel.Information, "GsxController:RunServices", $"Calling Stairs");
                MenuItem(7);
                OperatorSelection();
            }
        }

        private void OperatorSelection()
        {
            Thread.Sleep(1500);

            int result = IsOperatorSelectionActive();
            if (result == -1)
            {
                Logger.Log(LogLevel.Information, "GsxController:OperatorSelection", $"Waiting {Model.OperatorDelay}s for Operator Selection");
                Thread.Sleep((int)(Model.OperatorDelay * 1000));
            }
            else if (result == 1)
            {
                Logger.Log(LogLevel.Information, "GsxController:OperatorSelection", $"Operator Selection active, choosing Option 1");
                MenuItem(1);
            }
            else
                Logger.Log(LogLevel.Information, "GsxController:OperatorSelection", $"No Operator Selection needed");
        }

        private int IsOperatorSelectionActive()
        {
            int result = -1;

            if (menuFile != "")
            {
                string[] lines = File.ReadLines(menuFile).ToArray();
                if (lines.Length > 1)
                {
                    if (lines[1] != "Request Deboarding")
                        result = 1;
                    else
                        result = 0;
                }
            }

            return result;
        }

        private void MenuOpen()
        {

            SimConnect.IsGsxMenuReady = false;
            SimConnect.WriteLvar("FSDT_GSX_MENU_OPEN", 1);
            while (!SimConnect.IsGsxMenuReady) { }
        }

        private void MenuItem(int index, bool waitForMenu = false)
        {
            if (waitForMenu)
            {
                SimConnect.IsGsxMenuReady = false;
                while (!SimConnect.IsGsxMenuReady) { }
            }
            SimConnect.WriteLvar("FSDT_GSX_MENU_CHOICE", index - 1);
        }
    }
}
