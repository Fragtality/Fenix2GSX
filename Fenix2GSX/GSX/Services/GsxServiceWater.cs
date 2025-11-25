using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceWater(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Water;
        protected override double NumStateCompleted { get; } = 1;
        public virtual ISimResourceSubscription SubWaterService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubWaterService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(8, GsxConstants.MenuGate, true));
            sequence.Commands.Add(new(4, GsxConstants.MenuAdditionalServices) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateReset());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubWaterService = SimStore.AddVariable(GsxConstants.VarServiceWater);
            SubWaterService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubWaterService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceWater);
        }

        protected override bool CheckCalled()
        {
            return SequenceResult;
        }
    }
}
