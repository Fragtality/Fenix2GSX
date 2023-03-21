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
                float.TryParse(Interface.FenixGetVariable("aircraft.fuel.total.amount.kg"), out fuelCurrent);
                float.TryParse(Interface.FenixGetVariable("aircraft.refuel.fuelTarget"), out fuelPlanned);
                fuelUnits = Interface.FenixGetVariable("system.config.Units.Weight");
                if (fuelUnits == "LBS")
                    fuelPlanned /= 2.205f;

                string str = Interface.FenixGetVariable("fenix.efb.loadingStatus");
                if (!string.IsNullOrWhiteSpace(str))
                    int.TryParse(str.Substring(1), out cargoPlanned);

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
                Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.fuel.total.amount.kg", 2500.0f));
                fuelCurrent = 2500;
            }
        }

        public bool Refuel()
        {
            if (fuelCurrent + Model.RefuelRate < fuelPlanned)
                fuelCurrent += Model.RefuelRate;
            else
                fuelCurrent = fuelPlanned;

            Interface.FenixPost(FenixInterface.MsgMutation("float", "aircraft.fuel.total.amount.kg", fuelCurrent));

            return fuelCurrent == fuelPlanned;
        }

        public void RefuelStop()
        {

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
            ChangePassengers(paxCurrent - paxLast, true);
            paxLast = paxCurrent;

            ChangeCargo(cargoCurrent);
            cargoLast = cargoCurrent;

            return paxCurrent == GetPaxPlanned() && cargoCurrent == 100;
        }

        private void ChangePassengers(int num, bool onboard)
        {
            if (num <= 0)
                return;

            for (int i = paxLast; i < paxLast + num && i < GetPaxPlanned(); i++)
            {
                paxCurrent[paxSeats[i]] = onboard;
            }

            SendSeatString();
        }

        private void SendSeatString()
        {
            string seatString = "";
            bool first = true;
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
            paxLast = 0;
            cargoLast = 100;
            paxSeats = new int[GetPaxPlanned()];
            int n = 0;
            for (int i = 0; i < paxPlanned.Length; i++)
            {
                if (paxPlanned[i])
                {
                    paxSeats[n] = i;
                    n++;
                }
            }
        }

        public bool Deboarding(int paxCurrent, int cargoCurrent)
        {
            ChangePassengers(paxCurrent - paxLast, false);
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
            SendSeatString();
            paxSeats = null;
        }
    }
}
