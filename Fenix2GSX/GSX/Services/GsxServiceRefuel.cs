using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceRefuel(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Refuel;
        public virtual ISimResourceSubscription SubRefuelService { get; protected set; }
        public virtual ISimResourceSubscription SubRefuelHose { get; protected set; }
        public virtual bool IsRefueling => IsActive && SubRefuelHose.GetNumber() == 1;
        public virtual bool WasHoseConnected { get; protected set; } = false;
        protected virtual bool CompleteNotified { get; set; } = false;

        public event Func<bool, Task> OnHoseConnection;

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
                WasHoseConnected = false;
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                WasActive = true;
                CompleteNotified = false;
                NotifyActive();
            }
            else if (sub.GetNumber() == 1 && WasActive && !CompleteNotified)
            {
                Logger.Information($"{Type} Service completed");
                WasCompleted = true;
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        protected override void NotifyCompleted()
        {
            CompleteNotified = true;
            base.NotifyCompleted();
        }

        protected virtual void OnHoseChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (sub.GetNumber() == 1 && State != GsxServiceState.Unknown && State != GsxServiceState.Completed)
            {
                Logger.Debug($"Fuel Hose connected");
                if (State == GsxServiceState.Active)
                {
                    WasHoseConnected = true;
                    TaskTools.RunLogged(() => OnHoseConnection?.Invoke(true), Controller.Token);
                }
            }
            else if (sub.GetNumber() == 0 && State != GsxServiceState.Unknown && WasActive)
            {
                Logger.Debug($"Fuel Hose disconnected");
                TaskTools.RunLogged(() => OnHoseConnection?.Invoke(false), Controller.Token);
            }
        }

        protected override void DoReset()
        {
            CompleteNotified = false;
            WasHoseConnected = false;
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
            if ((state == GsxServiceState.Callable && WasActive) || (state == GsxServiceState.Callable && WasCompleted))
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
