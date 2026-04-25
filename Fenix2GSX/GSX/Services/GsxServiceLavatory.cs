using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceLavatory(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Lavatory;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubLavatoryService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubLavatoryService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(3, GsxConstants.MenuAdditionalServices));
            sequence.Commands.Add(GsxMenuCommand.Operator());
            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(3, GsxConstants.MenuAdditionalServices));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Open());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubLavatoryService = SimStore.AddVariable(GsxConstants.VarServiceLavatory);
            SubLavatoryService?.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubLavatoryService?.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceLavatory);
        }
    }
}
