using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceReposition(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Reposition;
        protected override ISimResourceSubscription SubStateVar => null;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.Commands.Add(GsxMenuCommand.Select(10, GsxConstants.MenuGate));
            sequence.Commands.Add(GsxMenuCommand.Select(1, GsxConstants.MenuParkingSelect));
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Wait());
            sequence.Commands.Add(GsxMenuCommand.Open());
            sequence.IgnoreGsxState = true;

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void InitSubscriptions()
        {

        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {

        }

        protected override GsxServiceState GetState()
        {
            if (SequenceResult)
                return GsxServiceState.Completed;
            else
                return GsxServiceState.Callable;
        }

        protected override bool CheckCalled()
        {
            IsCalled = SequenceResult;
            return IsCalled;
        }

        protected override void SetStateVariable(GsxServiceState state)
        {
            
        }
    }
}
