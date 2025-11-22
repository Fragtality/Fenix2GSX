using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceCleaning(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Cleaning;
        public virtual ISimResourceSubscription SubCleaningService { get; protected set; }

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(5, GsxConstants.MenuAdditionalServices) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubCleaningService = SimStore.AddVariable(GsxConstants.VarServiceCleaning);
            SubCleaningService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubCleaningService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceCleaning);
        }

        protected override bool CheckCalled()
        {
            return SequenceResult;
        }

        protected override void OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
                WasActive = true;
                NotifyActive();
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                if (!WasActive)
                {
                    WasActive = true;
                    NotifyActive();
                }
            }
            else if (sub.GetNumber() == 1 && WasActive)
            {
                Logger.Information($"{Type} Service completed");
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        protected override GsxServiceState GetState()
        {
            var state = ReadState(SubCleaningService);
            if (state == GsxServiceState.Callable && WasActive)
                return GsxServiceState.Completed;
            else
                return state;
        }
    }
}
