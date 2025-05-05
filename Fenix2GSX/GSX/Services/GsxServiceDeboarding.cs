using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceDeboarding(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Deboarding;
        public virtual ISimResourceSubscription SubDeboardService { get; protected set; }
        public virtual int PaxTarget => (int)SubPaxTarget.GetNumber();
        public virtual ISimResourceSubscription SubPaxTarget { get; protected set; }
        public virtual int PaxTotal => (int)SubPaxTotal.GetNumber();
        public virtual ISimResourceSubscription SubPaxTotal { get; protected set; }
        public virtual int CargoPercent => (int)SubCargoPercent.GetNumber();
        public virtual ISimResourceSubscription SubCargoPercent { get; protected set; }

        public event Action<GsxServiceDeboarding> OnPaxChange;
        public event Action<GsxServiceDeboarding> OnCargoChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(1, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubDeboardService = SimStore.AddVariable(GsxConstants.VarServiceDeboarding);
            SubDeboardService.OnReceived += OnStateChange;

            SubPaxTarget = SimStore.AddVariable(GsxConstants.VarPaxTarget);
            SubPaxTotal = SimStore.AddVariable(GsxConstants.VarPaxTotalDeboard);
            SubPaxTotal.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore.AddVariable(GsxConstants.VarCargoPercentDeboard);
            SubCargoPercent.OnReceived += NotifyCargoChange;

            SimStore.AddVariable(GsxConstants.VarNoCrewDeboard);
            SimStore.AddVariable(GsxConstants.VarNoPilotsDeboard);
        }

        protected override void DoReset()
        {

        }

        public override void FreeResources()
        {
            SubDeboardService.OnReceived -= OnStateChange;

            SimStore.Remove(GsxConstants.VarServiceDeboarding);
            SimStore.Remove(GsxConstants.VarPaxTarget);
            SimStore.Remove(GsxConstants.VarPaxTotalDeboard);
            SimStore.Remove(GsxConstants.VarCargoPercentDeboard);
            SimStore.Remove(GsxConstants.VarNoCrewDeboard);
            SimStore.Remove(GsxConstants.VarNoPilotsDeboard);
        }

        protected override GsxServiceState GetState()
        {
            return ReadState(SubDeboardService);
        }

        public virtual bool SetPaxTarget(int num)
        {
            if (Profile.SkipCrewQuestion)
            {
                SimStore[GsxConstants.VarNoCrewDeboard].WriteValue(1);
                SimStore[GsxConstants.VarNoPilotsDeboard].WriteValue(1);
            }

            return SubPaxTarget.WriteValue(num);
        }

        protected virtual void NotifyPaxChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (State != GsxServiceState.Active)
            {
                Logger.Debug($"Ignoring Pax Change - Service not active");
                return;
            }

            var pax = sub.GetNumber();
            if (pax < 0 || pax > PaxTarget)
            {
                Logger.Warning($"Ignoring Pax Change - Value received: {pax}");
                return;
            }

            OnPaxChange?.Invoke(this);
        }

        protected virtual void NotifyCargoChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (State != GsxServiceState.Active)
            {
                Logger.Debug($"Ignoring Cargo Change - Service not active");
                return;
            }

            var cargo = sub.GetNumber();
            if (cargo < 0 || cargo > 100)
            {
                Logger.Warning($"Ignoring Cargo Change - Value received: {cargo}");
                return;
            }

            OnCargoChange?.Invoke(this);
        }
    }
}
