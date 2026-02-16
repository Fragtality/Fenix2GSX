using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServicePushback(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Pushback;
        public virtual ISimResourceSubscription SubDepartService { get; protected set; }
        public virtual ISimResourceSubscription SubPushStatus { get; protected set; }
        protected override ISimResourceSubscription SubStateVar => SubDepartService;
        public virtual bool IsPinInserted => SubBypassPin.GetNumber() == 1;
        public virtual int PushStatus => (int)SubPushStatus.GetNumber();
        public virtual bool IsTugConnected => SubPushStatus.GetNumber() == 3 || SubPushStatus.GetNumber() == 4;
        public virtual bool TugAttachedOnBoarding { get; protected set; } = false;
        public virtual ISimResourceSubscription SubBypassPin { get; protected set; }

        public event Action<GsxServicePushback> OnBypassPin;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(5, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override GsxMenuSequence InitCancelSequence()
        {
            return new GsxMenuSequence();
        }

        protected override void InitSubscriptions()
        {
            SubDepartService = SimStore.AddVariable(GsxConstants.VarServiceDeparture);
            SubDepartService.OnReceived += OnStateChange;
            SubPushStatus = SimStore.AddVariable(GsxConstants.VarPusbackStatus);
            SubPushStatus.OnReceived += OnPushChange;

            SubBypassPin = SimStore.AddVariable(GsxConstants.VarBypassPin);
            SubBypassPin.OnReceived += NotifyBypassPin;
        }

        protected virtual void OnPushChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            var state = sub.GetNumber();
            Logger.Debug($"Push Status Change: {state}");
            if (!TugAttachedOnBoarding && state > 0 && (Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Active || Controller.GsxServices[GsxServiceType.Boarding].State == GsxServiceState.Requested))
            {
                Logger.Information($"Tug attaching during Boarding");
                TugAttachedOnBoarding = true;
                Controller.Menu.SuppressMenuRefresh = false;
            }
        }

        protected virtual void NotifyBypassPin(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            TaskTools.RunLogged(() => OnBypassPin?.Invoke(this), Controller.Token);
        }

        protected override void DoReset()
        {
            TugAttachedOnBoarding = false;
        }

        public override void FreeResources()
        {
            SubDepartService.OnReceived -= OnStateChange;
            SubBypassPin.OnReceived -= NotifyBypassPin;
            SubPushStatus.OnReceived -= OnPushChange;

            SimStore.Remove(GsxConstants.VarServiceDeparture);
            SimStore.Remove(GsxConstants.VarBypassPin);
            SimStore.Remove(GsxConstants.VarPusbackStatus);
        }

        public override async Task Call()
        {
            if (PushStatus == 0 || !CheckCalled())
                await base.Call();
            else if (PushStatus > 0 && PushStatus < 5)
            {
                var sequence = new GsxMenuSequence();
                sequence.Commands.Add(new(5, GsxConstants.MenuGate, true) { NoHide = true });
                await Controller.Menu.RunSequence(sequence);
            }
        }

        public override async Task Cancel(int option = -1)
        {
            await EndPushback();
        }

        public virtual async Task EndPushback(int selection = 1)
        {
            Logger.Debug($"End Pushback ({PushStatus})");
            if (PushStatus < 5)
                return;

            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(selection, GsxConstants.MenuPushbackInterrupt, true));
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());
            await Controller.Menu.RunSequence(sequence);
        }
    }
}
