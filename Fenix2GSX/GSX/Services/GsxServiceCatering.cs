using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceCatering(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Catering;
        public virtual ISimResourceSubscription SubCaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubCaterService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(2, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubCaterService = SimStore.AddVariable(GsxConstants.VarServiceCatering);
            SubCaterService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {
            
        }

        public override void FreeResources()
        {
            SubCaterService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceCatering);
        }
    }
}
