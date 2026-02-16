using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceDeice(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Deice;
        public virtual ISimResourceSubscription SubDeiceService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDeiceService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(2, GsxConstants.MenuAdditionalServices) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(2, GsxConstants.MenuAdditionalServices) { WaitReady = true });

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubDeiceService = SimStore.AddVariable(GsxConstants.VarServiceDeice);
            SubDeiceService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubDeiceService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceDeice);
        }
    }
}
