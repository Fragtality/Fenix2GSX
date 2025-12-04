using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceCleaning(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Cleaning;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubCleaningService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCleaningService;

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

        protected override void RunStateRequested()
        {
            base.RunStateRequested();
            WasActive = true;
            NotifyActive();
        }

        protected override void RunStateActive()
        {
            if (!WasActive)
            {
                WasActive = true;
                NotifyActive();
            }
        }
    }
}
