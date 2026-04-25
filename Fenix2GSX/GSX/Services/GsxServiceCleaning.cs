using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System;

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
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(5, GsxConstants.MenuAdditionalServices));
            sequence.Commands.Add(GsxMenuCommand.Operator());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(5, GsxConstants.MenuAdditionalServices));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Open());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubCleaningService = SimStore.AddVariable(GsxConstants.VarServiceCleaning);
            SubCleaningService?.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubCleaningService?.OnReceived -= OnStateChange;

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
                ActivationTime = DateTime.Now;
                NotifyActive();
            }
        }
    }
}
