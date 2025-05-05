using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

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
                Controller.AircraftInterface.RefuelComplete();
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
            else if (sub.GetNumber() == 0 && State != GsxServiceState.Unknown)
                Logger.Information($"Fuel Hose disconnected");
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
            else
                return state;
        }
    }
}
