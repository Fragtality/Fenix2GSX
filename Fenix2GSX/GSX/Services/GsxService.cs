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
        public virtual GsxServiceState State => GetState();
        public virtual bool IsCalling => CallSequence.IsExecuting;
        public virtual bool IsRunning => State == GsxServiceState.Requested || State == GsxServiceState.Active;
        public virtual bool IsActive => State == GsxServiceState.Active;
        public virtual bool IsCompleted => State == GsxServiceState.Completed;
        public virtual bool IsSkipped { get; set; } = false;
        public virtual bool WasActive { get; protected set; } = false;
        public virtual bool WasCompleted { get; protected set; } = false;

        public event Func<GsxService, Task> OnActive;
        public event Func<GsxService, Task> OnCompleted;
        public event Func<GsxService, Task> OnStateChanged;

        public GsxService(GsxController controller)
        {
            Controller = controller;
            CallSequence = InitCallSequence();
            InitSubscriptions();
            Controller.GsxServices.Add(Type, this);
        }

        protected abstract GsxMenuSequence InitCallSequence();

        protected abstract void InitSubscriptions();

        protected virtual void OnStateChange(ISimResourceSubscription sub, object data)
        {
            if (!IsFenixAircraft)
                return;

            if (sub.GetNumber() == 4)
            {
                Logger.Information($"{Type} Service requested");
            }
            else if (sub.GetNumber() == 5)
            {
                Logger.Information($"{Type} Service active");
                WasActive = true;
                NotifyActive();
            }
            else if (sub.GetNumber() == 6)
            {
                Logger.Information($"{Type} Service completed");
                WasActive = true;
                WasCompleted = true;
                NotifyCompleted();
            }
            NotifyStateChange();
        }

        public virtual void ResetState()
        {
            IsCalled = false;
            IsSkipped = false;
            WasActive = false;
            WasCompleted = false;
            CallSequence.Reset();
            DoReset();
        }

        protected abstract void DoReset();

        public abstract void FreeResources();

        protected static GsxServiceState ReadState(ISimResourceSubscription sub)
        {
            try
            {
                return (GsxServiceState)sub.GetValue<int>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return GsxServiceState.Unknown;
            }
        }

        protected abstract GsxServiceState GetState();

        protected virtual bool CheckCalled()
        {
            return IsRunning;
        }

        public virtual async Task Call()
        {
            if (IsCalled)
                return;

            if (await DoCall() == false)
                return;
            await Task.Delay(Controller.Config.DelayServiceStateChange, Controller.Token);
            IsCalled = CheckCalled();
        }

        protected virtual async Task<bool> DoCall()
        {
            bool result = await Controller.Menu.RunSequence(CallSequence);
            Logger.Debug($"{Type} Sequence completed: Success {result}");
            return result;
        }

        protected virtual void NotifyStateChange()
        {
            if (!IsFenixAircraft)
                return;

            if (State == GsxServiceState.Unknown)
            {
                Logger.Debug($"Ignoring State Change - State is unknown");
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
