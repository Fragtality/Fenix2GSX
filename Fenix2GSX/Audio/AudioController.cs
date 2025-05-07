using CFIT.AppFramework.Services;
using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib;
using CFIT.SimConnectLib.SimResources;
using Fenix2GSX.AppConfig;
using FenixInterface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fenix2GSX.Audio
{
    public enum AudioChannel
    {
        VHF1,
        VHF2,
        VHF3,
        HF1,
        HF2,
        INT,
        CAB,
        PA
    }

    public enum AcpSide
    {
        CPT = 0,
        FO = 1
    }

    public class AudioController : ServiceController<Fenix2GSX, AppService, Config, Definition>
    {
        public virtual SimConnectManager SimConnect => Fenix2GSX.Instance.AppService.SimConnect;
        protected virtual ISimResourceSubscription SubPlanePowered { get; set; }
        public virtual bool IsActive { get; protected set; } = false;
        public virtual bool IsPlanePowered => SubPlanePowered.GetNumber() > 0;
        public virtual bool HasInitialized { get; protected set; } = false;
        public virtual DeviceManager DeviceManager { get; }
        public virtual SessionManager SessionManager { get; }
        public virtual bool ResetVolumes { get; set; } = false;
        public virtual bool ResetMappings { get; set; } = false;
        public static ConcurrentDictionary<AudioChannel, string> VarsVolumeKnobsCapt { get; } = new()
        {
            { AudioChannel.VHF1, "L:A_ASP_VHF_1_VOLUME" },
            { AudioChannel.VHF2, "L:A_ASP_VHF_2_VOLUME" },
            { AudioChannel.VHF3, "L:A_ASP_VHF_3_VOLUME" },
            { AudioChannel.HF1, "L:A_ASP_HF_1_VOLUME" },
            { AudioChannel.HF2, "L:A_ASP_HF_2_VOLUME" },
            { AudioChannel.INT, "L:A_ASP_INT_VOLUME" },
            { AudioChannel.CAB, "L:A_ASP_CAB_VOLUME" },
            { AudioChannel.PA, "L:A_ASP_PA_VOLUME" },
        };
        public static ConcurrentDictionary<AudioChannel, string> VarsVolumeLatchSwitchesCapt { get; } = new()
        {
            { AudioChannel.VHF1, "L:S_ASP_VHF_1_REC_LATCH" },
            { AudioChannel.VHF2, "L:S_ASP_VHF_2_REC_LATCH" },
            { AudioChannel.VHF3, "L:S_ASP_VHF_3_REC_LATCH" },
            { AudioChannel.HF1, "L:S_ASP_HF_1_REC_LATCH" },
            { AudioChannel.HF2, "L:S_ASP_HF_2_REC_LATCH" },
            { AudioChannel.INT, "L:S_ASP_INT_REC_LATCH" },
            { AudioChannel.CAB, "L:S_ASP_CAB_REC_LATCH" },
            { AudioChannel.PA, "L:S_ASP_PA_REC_LATCH" },
        };
        public static ConcurrentDictionary<AudioChannel, string> VarsVolumeKnobsFo { get; } = new()
        {
            { AudioChannel.VHF1, "L:A_ASP2_VHF_1_VOLUME" },
            { AudioChannel.VHF2, "L:A_ASP2_VHF_2_VOLUME" },
            { AudioChannel.VHF3, "L:A_ASP2_VHF_3_VOLUME" },
            { AudioChannel.HF1, "L:A_ASP2_HF_1_VOLUME" },
            { AudioChannel.HF2, "L:A_ASP2_HF_2_VOLUME" },
            { AudioChannel.INT, "L:A_ASP2_INT_VOLUME" },
            { AudioChannel.CAB, "L:A_ASP2_CAB_VOLUME" },
            { AudioChannel.PA, "L:A_ASP2_PA_VOLUME" },
        };
        public static ConcurrentDictionary<AudioChannel, string> VarsVolumeLatchSwitchesFo { get; } = new()
        {
            { AudioChannel.VHF1, "L:S_ASP2_VHF_1_REC_LATCH" },
            { AudioChannel.VHF2, "L:S_ASP2_VHF_2_REC_LATCH" },
            { AudioChannel.VHF3, "L:S_ASP2_VHF_3_REC_LATCH" },
            { AudioChannel.HF1, "L:S_ASP2_HF_1_REC_LATCH" },
            { AudioChannel.HF2, "L:S_ASP2_HF_2_REC_LATCH" },
            { AudioChannel.INT, "L:S_ASP2_INT_REC_LATCH" },
            { AudioChannel.CAB, "L:S_ASP2_CAB_REC_LATCH" },
            { AudioChannel.PA, "L:S_ASP2_PA_REC_LATCH" },
        };
        protected virtual List<ConcurrentDictionary<AudioChannel, string>> AllVars { get; } = [];

        public AudioController(Config config) : base(config)
        {
            AllVars.Add(VarsVolumeKnobsCapt);
            AllVars.Add(VarsVolumeLatchSwitchesCapt);
            AllVars.Add(VarsVolumeKnobsFo);
            AllVars.Add(VarsVolumeLatchSwitchesFo);

            DeviceManager = new(this);
            SessionManager = new(this);
        }

        protected override void InitReceivers()
        {
            base.InitReceivers();

            SubPlanePowered = SimStore.AddVariable(FenixConstants.VarPowered);
            foreach (var dict in AllVars)
                foreach (var name in dict.Values)
                    SimStore.AddVariable(name);
        }

        protected override void FreeResources()
        {
            base.FreeResources();

            SimStore.Remove(FenixConstants.VarPowered);
            foreach (var dict in AllVars)
                foreach (var name in dict.Values)
                    SimStore.Remove(name);

            DeviceManager.Clear();
        }

        protected virtual void SetStartupVolumes()
        {
            try
            {
                foreach (var channel in Enum.GetValues<AudioChannel>())
                {
                    if (Config.AudioStartupVolumes[channel] >= 0.0)
                    {
                        var knobVars = Config.AudioAcpSide == AcpSide.CPT ? VarsVolumeKnobsCapt : VarsVolumeKnobsFo;
                        SimStore[knobVars[channel]].WriteValue(Config.AudioStartupVolumes[channel]);
                    }

                    if (Config.AudioStartupUnmute[channel])
                    {
                        var latchVars = Config.AudioAcpSide == AcpSide.CPT ? VarsVolumeLatchSwitchesCapt : VarsVolumeLatchSwitchesFo;
                        SimStore[latchVars[channel]].WriteValue(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected override async Task DoRun()
        {
            while (!IsPlanePowered && IsExecutionAllowed && !Token.IsCancellationRequested)
                await Task.Delay(Config.AudioServiceRunInterval, Token);

            Logger.Debug($"Aircraft powered. AudioService active");
            try
            {
                SessionManager.RegisterMappings();
                bool rescanNeeded = false;
                IsActive = true;
                SetStartupVolumes();
                while (SimConnect.IsSessionRunning && IsExecutionAllowed && !Token.IsCancellationRequested)
                {
                    rescanNeeded = SessionManager.HasInactiveSessions || SessionManager.HasEmptySearches || ResetMappings;
                    if (ResetMappings)
                    {
                        SessionManager.UnregisterMappings();
                        SessionManager.RegisterMappings();
                        ResetMappings = false;
                    }

                    if (SessionManager.CheckProcesses(rescanNeeded))
                    {
                        rescanNeeded = true;
                        await Task.Delay(Config.AudioProcessStartupDelay, Token);
                    }

                    if (DeviceManager.Scan(rescanNeeded))
                        rescanNeeded = true;

                    HasInitialized = true;

                    SessionManager.CheckSessions(rescanNeeded);
                    if (ResetVolumes)
                        SessionManager.SynchControls();

                    ResetVolumes = false;
                    rescanNeeded = false;
                    await Task.Delay(Config.AudioServiceRunInterval, Token);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException)
                    Logger.LogException(ex);
            }
            IsActive = false;
            SessionManager.UnregisterMappings();

            Logger.Debug($"AudioService ended");
        }

        public override void Stop()
        {
            base.Stop();

            try { SessionManager.RestoreVolumes(); } catch { }
            IsActive = false;
            HasInitialized = false;
            DeviceManager.Clear();
            SessionManager.Clear();
        }
    }
}
