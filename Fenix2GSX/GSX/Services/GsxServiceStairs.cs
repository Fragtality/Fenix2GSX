using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceStairs(GsxController controller) : GsxService(controller)
    {

        public override GsxServiceType Type => GsxServiceType.Stairs;
        public virtual ISimResourceSubscription SubService { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubService;
        public virtual ISimResourceSubscription SubOperating { get; protected set; }

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        public virtual bool IsConnected => SubService.GetNumber() == (int)GsxServiceState.Active && SubOperating.GetNumber() < 3;
        public virtual bool IsOperating => SubService.GetNumber() == (int)GsxServiceState.Requested || SubOperating.GetNumber() > 3;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(7, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubService = SimStore.AddVariable(GsxConstants.VarServiceStairs);
            SubOperating = SimStore.AddVariable(GsxConstants.VarServiceStairsOperation);
            SubService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubService.OnReceived -= OnStateChange;
            SimStore.Remove(GsxConstants.VarServiceStairs);
            SimStore.Remove(GsxConstants.VarServiceStairsOperation);
        }

        protected override bool CheckCalled()
        {
            IsCalled = IsOperating || IsRunning;
            return IsCalled;
        }

        protected override async Task<bool> DoCall()
        {
            if (IsAvailable)
                return await base.DoCall();
            else
                return true;
        }

        public virtual async Task Remove()
        {
            if (!IsConnected || !IsAvailable || IsOperating)
                return;

            await DoCall();
        }

        protected override void OnStateChange(ISimResourceSubscription sub, object data)
        {
            base.OnStateChange(sub, data);
            if (State == GsxServiceState.Callable && IsCalled)
            {
                Logger.Debug($"Reset IsCalled for Stairs");
                IsCalled = false;
            }
        }
    }
}
