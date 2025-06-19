using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceRefuel(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Refuel;
        public virtual ISimResourceSubscription SubRefuelService { get; protected set; }
        public virtual ISimResourceSubscription SubRefuelHose { get; protected set; }
        public virtual bool IsRefueling => IsActive && SubRefuelHose.GetNumber() == 1;
        protected virtual bool CompleteNotified { get; set; } = false;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(3, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubRefuelService = SimStore.AddVariable(GsxConstants.VarServiceRefuel);
            SubRefuelHose = SimStore.AddVariable(GsxConstants.VarServiceRefuelHose);

            SubRefuelService.OnReceived += OnStateChange;
            SubRefuelHose.OnReceived += OnHoseChange;
        }

        protected override void OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                WasActive = true;
                NotifyActive();
            }
            else if (sub.GetNumber() == 1 && WasActive && !CompleteNotified)
            {
                Logger.Information($"{Type} Service completed");
                CompleteNotified = true;
                _ = Controller.AircraftInterface.RefuelComplete();
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        protected virtual void OnHoseChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (sub.GetNumber() == 1 && State != GsxServiceState.Unknown && State != GsxServiceState.Completed)
            {
                Logger.Information($"Fuel Hose connected");
                if (State == GsxServiceState.Active)
                    Controller.AircraftInterface.RefuelStart();
            }
            else if (sub.GetNumber() == 0 && State != GsxServiceState.Unknown && WasActive)
            {
                if (Controller.AircraftProfile.RefuelFinishOnHose && Controller.AircraftInterface.IsRefueling)
                {
                    Logger.Information($"Fuel Hose disconnected while Refuel still in Progress - stopping Refuel");
                    if (!Controller.SimConnect.IsPaused)
                        _ = Controller.AircraftInterface.RefuelStop();
                    else
                        _ = DelayedFuelStop();
                }
                else if (Controller.AircraftInterface.IsRefueling)
                    Logger.Information($"Fuel Hose disconnected while Refuel still in Progress - Refuel continues");
                else
                    Logger.Information($"Fuel Hose disconnected");
            }
        }

        protected virtual async Task DelayedFuelStop()
        {
            Logger.Information($"Waiting for Sim to be unpaused");
            while (Controller.SimConnect.IsPaused)
            {
                Logger.Debug($"Waiting for Sim to be unpaused");
                await Task.Delay(Controller.Config.CheckInterval, Controller.Token);
            }
            await Controller.AircraftInterface.RefuelStop();
        }

        protected override void DoReset()
        {
            CompleteNotified = false;
        }

        public override void FreeResources()
        {
            SubRefuelService.OnReceived -= OnStateChange;
            SubRefuelHose.OnReceived -= OnHoseChange;

            SimStore.Remove(GsxConstants.VarServiceRefuel);
            SimStore.Remove(GsxConstants.VarServiceRefuelHose);
        }

        protected override GsxServiceState GetState()
        {
            var state = ReadState(SubRefuelService);
            if (state == GsxServiceState.Callable && WasActive)
                return GsxServiceState.Completed;
            else if (state == GsxServiceState.Active && !WasActive)
            {
                Logger.Debug($"Setting WasActive for Refuel");
                WasActive = true;
                return state;
            }
            else
                return state;
        }
    }
}
