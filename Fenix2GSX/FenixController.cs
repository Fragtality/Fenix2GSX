using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Fenix2GSX
{
    public class FenixContoller
    {
        private FenixInterface Interface;
        private ServiceModel Model;
        
        private float fuelCurrent = 0;
        private float fuelPlanned = 0;
        private string fuelUnits = "KG";
        
        private bool[] paxPlanned;
        private int[] paxSeats;
        private bool[] paxCurrent;
        private int paxLast;
        
        private int cargoPlanned;
        private const float cargoDistMain = 4000.0f / 9440.0f;
        private const float cargoDistBulk = 1440.0f / 9440.0f;
        private int cargoLast;

        public string flightPlanID = "0";
        public bool enginesRunning = false;
        public static readonly float weightConversion = 2.205f;

        public FenixContoller(ServiceModel model)
        { 
            Interface = new();
            paxCurrent = new bool[162];
            paxSeats = null;
            Model = model;
        }

        public void Update(bool forceCurrent)
        {
            try
            {
                float.TryParse(Interface.FenixGetVariable("aircraft.engine1.raw"), out float engine1);
                float.TryParse(Interface.FenixGetVariable("aircraft.engine2.raw"), out float engine2);
                enginesRunning = engine1 > 18.0f || engine2 > 18.0f;

                float.TryParse(Interface.FenixGetVariable("aircraft.fuel.total.amount.kg"), out fuelCurrent);
                float.TryParse(Interface.FenixGetVariable("aircraft.refuel.fuelTarget"), out fuelPlanned);
                fuelUnits = Interface.FenixGetVariable("system.config.Units.Weight");
                if (fuelUnits == "LBS")
                    fuelPlanned /= weightConversion;

                string str = Interface.FenixGetVariable("fenix.efb.loadingStatus");
                if (!string.IsNullOrWhiteSpace(str))
                    int.TryParse(str[1..], out cargoPlanned);

                JObject result = JObject.Parse(Interface.FenixGet(FenixInterface.MsgQuery("fenix.efb.passengers.booked", "queryResult")));
                paxPlanned = result["data"]["dataRef"]["queryResult"]["value"].ToObject<bool[]>();
                if (forceCurrent)
                    paxCurrent = paxPlanned;

            
                result = JObject.Parse(Interface.FenixGet(FenixInterface.MsgQuery("fenix.efb.flightTimestampJSON", "queryResult")));
                string innerJson = result["data"]["dataRef"]["queryResult"]["value"].ToString();
                result = JObject.Parse(innerJson);
                innerJson = result["fenixTimes"]["PRELIM_EDNO"].ToString();
                if (flightPlanID != innerJson)
                {
                    Logger.Log(LogLevel.Information, "FenixContoller:Update", $"New FlightPlan with ID {innerJson} detected!");
                    flightPlanID = innerJson;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "FenixContoller:Update", $"Exception during Update {ex.Message}");
            }
        }

        public bool IsFlightplanLoaded()
        {
            return !string.IsNullOrEmpty(Interface.FenixGetVariable("fenix.efb.prelimloadsheet"));
        }

        public int GetPaxPlanned()
        {
            return paxPlanned.Count(i => i);
        }

        public int GetPaxCurrent()
        {
            return paxCurrent.Count(i => i);
        }

        public float GetFuelPlanned()
        {
            return fuelPlanned;
        }

        public float GetFuelCurrent()
        {
            return fuelCurrent;
        }

        public void SetServicePCA(bool enable)
        {
            Interface.FenixPost(FenixInterface.MsgMutation("bool", "groundservice.preconditionedAir", enable));
        }

        public void SetServiceChocks(bool enable)
        {
            Interface.FenixPost(FenixInterface.MsgMutation("bool", "fenix.efb.chocks", enable));
        }

        public void SetServiceGPU(bool enable)
        {
            Interface.FenixPost(FenixInterface.MsgMutation("bool", "groundservice.groundpower", enable));
        }

        public void TriggerFinal()
        {
            Interface.TriggerFinalOnEFB();
            Interface.FenixPost(FenixInterface.MsgMutation("bool", "doors.entry.left.fwd", false));
        }

        public void RefuelStart()
        {
            if (fuelCurrent > fuelPlanned)
            {
                Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.fuel.total.amount.kg", 3000.0f));
                fuelCurrent = 3000;
            }
        }

        public bool Refuel()
        {
            float step = Model.GetFuelRateKGS();

            if (fuelCurrent + step < fuelPlanned)
                fuelCurrent += step;
            else
                fuelCurrent = fuelPlanned;

            Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.fuel.total.amount.kg", fuelCurrent));

            return fuelCurrent == fuelPlanned;
        }

        public void BoardingStart()
        {
            paxLast = 0;
            cargoLast = 0;
            paxSeats = new int[GetPaxPlanned()];
            int n = 0;
            for (int i=0; i < paxPlanned.Length; i++)
            {
                if (paxPlanned[i])
                {
                    paxSeats[n] = i;
                    n++;
                }
            }
        }

        public bool Boarding(int paxCurrent, int cargoCurrent)
        {
            BoardPassengers(paxCurrent - paxLast);
            paxLast = paxCurrent;

            ChangeCargo(cargoCurrent);
            cargoLast = cargoCurrent;

            return paxCurrent == GetPaxPlanned() && cargoCurrent == 100;
        }

        private void BoardPassengers(int num)
        {
            if (num < 0)
            {
                Logger.Log(LogLevel.Debug, "FenixContoller:BoardPassengers", $"Passenger Num was below 0!");
                return;
            }
            else if (num > 15)
            {
                Logger.Log(LogLevel.Debug, "FenixContoller:BoardPassengers", $"Passenger Num was above 15!");
                return;
            }
            else
                Logger.Log(LogLevel.Debug, "FenixContoller:BoardPassengers", $"(num {num}) (current {GetPaxCurrent()}) (planned ({GetPaxPlanned()}))");

            int n = 0;
            for (int i = paxLast; i < paxLast + num && i < GetPaxPlanned(); i++)
            {
                paxCurrent[paxSeats[i]] = true;
                n++;
            }

            if (n > 0)
                SendSeatString();
        }

        private void SendSeatString(bool force = false)
        {
            string seatString = "";
            bool first = true;

            if (GetPaxCurrent() == 0 && !force)
                return;

            foreach (var pax in paxCurrent)
            {
                if (first)
                {
                    if (pax)
                        seatString = "true";
                    else
                        seatString = "false";
                    first = false;
                }
                else
                {
                    if (pax)
                        seatString += ",true";
                    else
                        seatString += ",false";
                }
            }
            Logger.Log(LogLevel.Debug, "FenixContoller:SendSeatString", seatString);
            Interface.FenixPost(FenixInterface.MsgMutation("string", "aircraft.passengers.seatOccupation.string", seatString));
        }

        private void ChangeCargo(int cargoCurrent)
        {
            if (cargoCurrent == cargoLast)
                return;

            float cargo = (float)cargoPlanned * (float)(cargoCurrent / 100.0f);
            Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.cargo.forward.amount", (float)cargo * cargoDistMain));
            Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.cargo.aft.amount", (float)cargo * cargoDistMain));
            Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.cargo.bulk.amount", (float)cargo * cargoDistBulk));
        }

        public void BoardingStop()
        {
            paxSeats = null;
            Interface.FenixPost(FenixInterface.MsgMutation("string", "fenix.efb.boardingStatus", "ended"));
        }

        public void DeboardingStart()
        {
            Logger.Log(LogLevel.Debug, "FenixContoller:DeboardingStart", $"(planned {GetPaxPlanned()}) (current {GetPaxCurrent()})");
            paxLast = GetPaxPlanned();
            if (GetPaxCurrent() != GetPaxPlanned())
                paxCurrent = paxPlanned;
            cargoLast = 100;            
        }

        private void DeboardPassengers(int num)
        {
            if (num < 0)
            {
                Logger.Log(LogLevel.Debug, "FenixContoller:DeboardPassengers", $"Passenger Num was below 0!");
                return;
            }
            else if (num > 15)
            {
                Logger.Log(LogLevel.Debug, "FenixContoller:DeboardPassengers", $"Passenger Num was above 15!");
                return;
            }
            else
                Logger.Log(LogLevel.Debug, "FenixContoller:DeboardPassengers", $"(num {num}) (current {GetPaxCurrent()}) (planned ({GetPaxPlanned()}))");

            int n = 0;
            for (int i = 0; i < paxCurrent.Length && n < num; i++)
            {
                if (paxCurrent[i])
                {
                    paxCurrent[i] = false;
                    n++;
                }
            }

            if (n > 0)
                SendSeatString();
        }

        public bool Deboarding(int paxCurrent, int cargoCurrent)
        {
            DeboardPassengers(paxLast - paxCurrent);
            paxLast = paxCurrent;

            cargoCurrent = 100 - cargoCurrent;
            ChangeCargo(cargoCurrent);
            cargoLast = cargoCurrent;

            return paxCurrent == 0 && cargoCurrent == 0;
        }

        public void DeboardingStop()
        {
            ChangeCargo(0);
            for (int i = 0; i < paxCurrent.Length; i++)
                paxCurrent[i] = false;
            Logger.Log(LogLevel.Debug, "FenixController:DeboardingStop", "Sending SeatString");
            SendSeatString(true);
            paxCurrent = new bool[162];
            paxSeats = null;
        }
    }
}
