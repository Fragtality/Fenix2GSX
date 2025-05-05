﻿using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.GSX.Menu;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public class GsxServiceJetway(GsxController controller) : GsxService(controller)
    {

        public override GsxServiceType Type => GsxServiceType.Jetway;
        public virtual ISimResourceSubscription SubService { get; protected set; }
        public virtual ISimResourceSubscription SubOperating { get; protected set; }

        public virtual bool IsAvailable => State != GsxServiceState.NotAvailable;
        public virtual bool IsConnected => SubService.GetNumber() == (int)GsxServiceState.Active && SubOperating.GetNumber() < 3;
        public virtual bool IsOperating => SubService.GetNumber() == (int)GsxServiceState.Requested || SubOperating.GetNumber() > 3;

        protected override GsxMenuSequence InitCallSequence()
        {
            var sequence = new GsxMenuSequence();
            sequence.Commands.Add(new(6, GsxConstants.MenuGate, true));
            sequence.Commands.Add(GsxMenuCommand.CreateOperator());
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            return sequence;
        }

        protected override void InitSubscriptions()
        {
            SubService = SimStore.AddVariable(GsxConstants.VarServiceJetway);
            SubOperating = SimStore.AddVariable(GsxConstants.VarServiceJetwayOperation);
            SubService.OnReceived += OnStateChange;
        }

        protected override void DoReset()
        {
            
        }

        public override void FreeResources()
        {
            SubService.OnReceived -= OnStateChange;
            SimStore.Remove(GsxConstants.VarServiceJetway);
            SimStore.Remove(GsxConstants.VarServiceJetwayOperation);
        }

        protected override GsxServiceState GetState()
        {
            return ReadState(SubService);
        }

        protected override bool CheckCalled()
        {
            return IsOperating || IsConnected;
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
    }
}
