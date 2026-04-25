using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceGpu(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.GPU;
        public virtual ISimResourceSubscription SubGpuService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubGpuService;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(8, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuAdditionalServices));
            sequence.Commands.Add(GsxMenuCommand.Operator());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void InitSubscriptions()
        {
            SubGpuService = SimStore.AddVariable(GsxConstants.VarServiceGpu);
            SubGpuService?.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubGpuService?.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceGpu);
        }

        public override async Task Cancel(int option = -1)
        {
            await DoCall();
        }
    }
}
