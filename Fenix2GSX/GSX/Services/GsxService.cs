using CFIT.AppFramework.ResourceStores;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Menu;
using System;
using System.Threading.Tasks;

namespace Fenix2GSX.GSX.Services
{
    public enum GsxServiceType
    {
        Unknown = 0,
        Reposition = 1,
        Refuel = 2,
        Catering = 3,
        Boarding = 4,
        Pushback = 5,
        Deice = 6,
        Deboarding = 7,
        GPU = 8,
        Water = 9,
        Lavatory = 10,
        Jetway = 11,
        Stairs = 12,
        Cleaning = 13,
    }

    public enum GsxServiceActivation
    {
        Skip = 0,
        Manual = 1,
        AfterCalled = 2,
        AfterRequested = 3,
        AfterActive = 4,
        AfterPrevCompleted = 5,
        AfterAllCompleted = 6,
    }

    public enum GsxServiceConstraint
    {
        NoneAlways = 0,
        FirstLeg = 1,
        TurnAround = 2,
        CompanyHub = 3,
        NonCompanyHub = 4,
        TurnOnHub = 5,
        TurnOnNonHub = 6,
    }

    public enum GsxServiceState
    {
        Unknown = 0,
        Callable = 1,
        NotAvailable = 2,
        Bypassed = 3,
        Requested = 4,
        Active = 5,
        Completed = 6,
        Completing = 7,
    }

    public abstract class GsxService
    {
        public abstract GsxServiceType Type { get; }
        public virtual GsxController Controller { get; }
        protected virtual SimStore SimStore => Controller.SimStore;
        protected virtual ReceiverStore ReceiverStore => Controller.ReceiverStore;
        protected virtual AircraftProfile Profile => Controller.AircraftProfile;
        protected virtual bool IsFenixAircraft => AppService.Instance.IsFenixAircraft;

        public virtual bool IsCalled { get; protected set; } = false;
        protected virtual bool SequenceResult => CallSequence?.IsSuccess ?? false;
        protected virtual GsxMenuSequence CallSequence { get; }
        protected virtual GsxMenuSequence CancelSequence { get; set; }
        protected abstract ISimResourceSubscription SubStateVar { get; }
        public virtual GsxServiceState State => GetState();
        public virtual bool IsCalling => CallSequence.IsExecuting;
        public virtual bool IsRunning => State == GsxServiceState.Requested || State == GsxServiceState.Active;
        public virtual bool IsActive => State == GsxServiceState.Active;
        public virtual bool IsCompleted => State == GsxServiceState.Completed;
        public virtual bool IsCompleting => State == GsxServiceState.Completing;
        protected virtual double NumStateCompleted { get; } = 6;
        public virtual bool IsSkipped { get; set; } = false;
        public virtual bool WasActive { get; protected set; } = false;
        public virtual DateTime ActivationTime { get; protected set; } = DateTime.MaxValue;
        public virtual bool WasCompleted { get; protected set; } = false;

        public event Func<GsxService, Task> OnActive;
        public event Func<GsxService, Task> OnCompleted;
        public event Func<GsxService, Task> OnStateChanged;

        public GsxService(GsxController controller)
        {
            Controller = controller;
            CallSequence = InitCallSequence();
            CancelSequence = InitCancelSequence();
            InitSubscriptions();
            Controller.GsxServices.Add(Type, this);
        }

        protected abstract GsxMenuSequence InitCallSequence();

        protected abstract GsxMenuSequence InitCancelSequence();

        protected abstract void InitSubscriptions();

        protected virtual bool EvaluateComplete(ISimResourceSubscription sub)
        {
            return sub?.GetNumber() == NumStateCompleted && WasActive;
        }

        protected virtual void RunStateRequested()
        {

        }

        protected virtual void RunStateActive()
        {
            WasActive = true;
            ActivationTime = DateTime.Now;
            NotifyActive();
        }

        protected virtual void RunStateCompleted()
        {

        }

        protected virtual void OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
                RunStateRequested();
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                RunStateActive();
            }
            else if (EvaluateComplete(sub))
            {
                Logger.Information($"{Type} Service completed");
                WasCompleted = true;
                RunStateCompleted();
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        public virtual void ResetState(bool resetVariable = false)
        {
            IsCalled = false;
            IsSkipped = false;
            WasActive = false;
            ActivationTime = DateTime.MaxValue;
            WasCompleted = false;
            CallSequence.Reset();
            if (resetVariable)
                SetStateVariable(GsxServiceState.Callable);
            DoReset();
        }

        protected abstract void DoReset();

        public abstract void FreeResources();

        protected virtual GsxServiceState ReadState()
        {
            try
            {
                return (GsxServiceState)SubStateVar.GetValue<int>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected virtual GsxServiceState GetState()
        {
            var state = ReadState();

            if ((state == GsxServiceState.Callable || state == GsxServiceState.Bypassed || state == (GsxServiceState)NumStateCompleted) && WasActive)
                return GsxServiceState.Completed;
            else
                return state;
        }

        protected virtual void SetStateVariable(GsxServiceState state)
        {
            Logger.Debug($"Resetting State L-Var for Service {Type} to '{state}'");
            SubStateVar.WriteValue((int)state);
        }

        protected virtual bool CheckCalled()
        {
            IsCalled = IsRunning;
            return IsCalled;
        }

        public virtual async Task Call()
        {
            if (CheckCalled())
                return;

            if (await DoCall() == false)
                return;
            await Task.Delay(Controller.Config.DelayServiceStateChange, Controller.Token);
            CheckCalled();
        }

        protected virtual async Task<bool> DoCall()
        {
            bool result = await Controller.Menu.RunSequence(CallSequence);
            Logger.Debug($"{Type} Sequence completed: Success {result}");
            return result;
        }

        public virtual async Task Cancel(int option = -1)
        {
            if (option == -1)
                option = Controller.AircraftProfile.SmartButtonAbortService;

            var sequence = new GsxMenuSequence();
            sequence.Commands.AddRange(CancelSequence.Commands);
            sequence.Commands.Add(new(option, GsxConstants.MenuCancelService) { WaitReady = true });
            sequence.Commands.Add(GsxMenuCommand.CreateDummy());

            Logger.Debug($"Executing Cancel Sequence for Service {Type}");
            await Controller.Menu.RunSequence(sequence);
        }

        protected virtual void NotifyStateChange()
        {
            if (!IsFenixAircraft)
                return;

            if (State == GsxServiceState.Unknown)
            {
                Logger.Debug($"Ignoring State Change - State for {Type} is unknown");
                return;
            }

            Logger.Debug($"Notify State Change for {Type}: {State}");
            TaskTools.RunLogged(() => OnStateChanged?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyActive()
        {
            if (!IsFenixAircraft)
                return;

            Logger.Debug($"Notify Active for {Type}: {State}");
            TaskTools.RunLogged(() => OnActive?.Invoke(this), Controller.Token);
        }

        protected virtual void NotifyCompleted()
        {
            if (!IsFenixAircraft)
                return;

            Logger.Debug($"Notify Completed for {Type}: {State}");
            TaskTools.RunLogged(() => OnCompleted?.Invoke(this), Controller.Token);
        }

        public virtual void ForceComplete()
        {
            if (!WasCompleted)
            {
                Logger.Debug($"Force Complete for {Type}");
                WasCompleted = true;
                NotifyCompleted();
                NotifyStateChange();
            }
        }
    }
}
