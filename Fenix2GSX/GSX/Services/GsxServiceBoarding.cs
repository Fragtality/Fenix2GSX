﻿using CFIT.AppLogger;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceBoarding(GsxController controller) : GsxService(controller)
    {
        public override GsxServiceType Type => GsxServiceType.Boarding;
        public virtual ISimResourceSubscription SubBoardService { get; protected set; }
        public virtual int PaxTarget => (int)SubPaxTarget.GetNumber();
        public virtual ISimResourceSubscription SubPaxTarget { get; protected set; }
        public virtual int PaxTotal => (int)SubPaxTotal.GetNumber();
        public virtual ISimResourceSubscription SubPaxTotal { get; protected set; }
        public virtual int CargoPercent => (int)SubCargoPercent.GetNumber();
        public virtual ISimResourceSubscription SubCargoPercent { get; protected set; }

        public event Action<GsxServiceBoarding> OnPaxChange;
        public event Action<GsxServiceBoarding> OnCargoChange;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(4, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubBoardService = SimStore.AddVariable(GsxConstants.VarServiceBoarding);
            SubBoardService.OnReceived += OnStateChange;

            SubPaxTarget = SimStore.AddVariable(GsxConstants.VarPaxTarget);
            SubPaxTotal = SimStore.AddVariable(GsxConstants.VarPaxTotalBoard);
            SubPaxTotal.OnReceived += NotifyPaxChange;
            SubCargoPercent = SimStore.AddVariable(GsxConstants.VarCargoPercentBoard);
            SubCargoPercent.OnReceived += NotifyCargoChange;

            SimStore.AddVariable(GsxConstants.VarNoCrewBoard);
            SimStore.AddVariable(GsxConstants.VarNoPilotsBoard);
        }

        protected override void DoReset()
        {

        }

        public override async Task Call()
        {
            await base.Call();
        }

        protected override void NotifyStateChange()
        {
            base.NotifyStateChange();

            if (IsFenixAircraft && State == GsxServiceState.Active)
                Controller.SubScriptSupress.WriteValue(1);
        }

        public override void FreeResources()
        {
            SubBoardService.OnReceived -= OnStateChange;
            SubPaxTotal.OnReceived -= NotifyPaxChange;
            SubCargoPercent.OnReceived -= NotifyCargoChange;

            SimStore.Remove(GsxConstants.VarServiceBoarding);
            SimStore.Remove(GsxConstants.VarPaxTarget);
            SimStore.Remove(GsxConstants.VarPaxTotalBoard);
            SimStore.Remove(GsxConstants.VarCargoPercentBoard);
            SimStore.Remove(GsxConstants.VarNoCrewBoard);
            SimStore.Remove(GsxConstants.VarNoPilotsBoard);
        }

        protected override GsxServiceState GetState()
        {
            return ReadState(SubBoardService);
        }

        public virtual bool SetPaxTarget(int num)
        {
            if (Profile.SkipCrewQuestion)
            {
                SimStore[GsxConstants.VarNoCrewBoard].WriteValue(1);
                SimStore[GsxConstants.VarNoPilotsBoard].WriteValue(1);
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

        protected override void NotifyCompleted()
        {
            if (!IsFenixAircraft)
                return;

            base.NotifyCompleted();
            Controller.SubScriptSupress.WriteValue(0);
        }
    }
}
