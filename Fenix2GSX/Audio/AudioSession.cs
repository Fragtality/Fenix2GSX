using CFIT.AppLogger;
using CFIT.AppTools;
using CFIT.SimConnectLib.SimResources;
using CoreAudio;
using Fenix2GSX.AppConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fenix2GSX.Audio
{
    public class AudioSession(AudioController controller, AudioMapping mapping)
    {
        protected virtual AudioController Controller { get; } = controller;
        public virtual AudioMapping Mapping => new(Channel, Device, Binary, UseLatch);
        public virtual AudioChannel Channel { get; } = mapping.Channel;
        public virtual string Device { get; } = mapping.Device;
        public virtual string Binary { get; } = mapping.Binary;
        public virtual bool UseLatch { get; } = mapping.UseLatch;
        public virtual uint ProcessId { get; protected set; } = 0;
        public virtual int ProcessCount { get; protected set; } = 0;
        public virtual bool IsActive => ProcessId > 0 && Controller.HasInitialized && Controller.IsExecutionAllowed;
        public virtual bool IsRunning => Sys.GetProcessRunning(Binary);
        public virtual int SearchCounter { get; set; } = 0;
        public virtual ConcurrentDictionary<string, float> SavedVolumes { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SavedMutes { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SynchedSessionsVolume { get; } = [];
        public virtual ConcurrentDictionary<string, bool> SynchedSessionsMute { get; } = [];
        public virtual List<AudioSessionControl2> SessionControls { get; } = [];
        public virtual ConcurrentDictionary<AcpSide, ISimResourceSubscription> SubVolume { get; } = [];
        public virtual ConcurrentDictionary<AcpSide, ISimResourceSubscription> SubMute { get; } = [];

        public override string ToString()
        {
            return $"{Channel}: '{Binary}' @ '{(string.IsNullOrWhiteSpace(Device) ? "all" : Device)}'";
        }

        public virtual int CheckProcess(bool force = false)
        {
            bool running = IsRunning;
            int result = 0;

            if (!running && ProcessId != 0 || force)
            {
                if (!force)
                    Logger.Debug($"Binary '{Binary}' stopped");
                else
                    Logger.Verbose($"Binary '{Binary}' stopped");
                ClearSimSubscriptions();
                ProcessId = 0;
                SessionControls.Clear();
                result = -1;
            }

            if (running && ProcessId == 0)
            {
                ProcessId = (uint)Sys.GetProcess(Binary).Id;
                Logger.Debug($"Binary '{Binary}' started (PID: {ProcessId})");
                SetSimSubscriptions();
                result = 1;
            }
            else if (running && ProcessId > 0)
            {
                int count = Process.GetProcessesByName(Binary)?.Length ?? 0;
                if (ProcessCount != count)
                {
                    result = 1;
                    Logger.Debug($"Process Count changed");
                }
                ProcessCount = count;
            }

            return result;
        }

        public virtual void RestoreVolumes()
        {
            var savedVolumes = SavedVolumes.ToArray();
            foreach (var ident in savedVolumes)
            {
                var query = SessionControls.Where(s => s.SessionInstanceIdentifier == ident.Key);
                if (query.Any())
                {
                    Logger.Debug($"Restore Volume for Instance '{ident.Key}' to {ident.Value} (AudioSession {this})");
                    try
                    {
                        query.First().SimpleAudioVolume.MasterVolume = ident.Value;
                        SavedVolumes.Remove(ident.Key);
                    }
                    catch { }
                }
            }
            SavedVolumes.Clear();

            var savedMutes = SavedMutes.ToArray();
            foreach (var ident in savedMutes)
            {
                var query = SessionControls.Where(s => s.SessionInstanceIdentifier == ident.Key);
                if (query.Any())
                {
                    Logger.Debug($"Restore Mute for Instance '{ident.Key}' to {ident.Value} (AudioSession {this})");
                    try
                    {
                        query.First().SimpleAudioVolume.Mute = ident.Value;
                        SavedMutes.Remove(ident.Key);
                    }
                    catch { }
                }
            }
            SavedMutes.Clear();

            SynchedSessionsVolume.Clear();
            SynchedSessionsMute.Clear();
        }

        public virtual void SetSessionList(List<AudioSessionControl2> list)
        {
            SessionControls.Clear();
            SearchCounter = 0;

            foreach (var item in list)
            {
                SavedVolumes.TryAdd(item.SessionInstanceIdentifier, item.SimpleAudioVolume.MasterVolume);
                SavedMutes.TryAdd(item.SessionInstanceIdentifier, item.SimpleAudioVolume.Mute);
                SessionControls.Add(item);
            }
        }

        public virtual void SetSimSubscriptions()
        {
            SetSimSubscription(AcpSide.CPT, SubVolume, AudioController.VarsVolumeKnobsCapt, OnVolumeChange);
            SetSimSubscription(AcpSide.FO, SubVolume, AudioController.VarsVolumeKnobsFo, OnVolumeChange);
            SetSimSubscription(AcpSide.CPT, SubMute, AudioController.VarsVolumeLatchSwitchesCapt, OnMuteChange);
            SetSimSubscription(AcpSide.FO, SubMute, AudioController.VarsVolumeLatchSwitchesFo, OnMuteChange);
        }

        protected virtual void SetSimSubscription(AcpSide side, ConcurrentDictionary<AcpSide, ISimResourceSubscription> dict, ConcurrentDictionary<AudioChannel, string> varDict, Action<ISimResourceSubscription,object> action)
        {
            var sub = Controller.SimStore[varDict[Channel]];
            sub.OnReceived += action;
            dict.Add(side, sub);
        }

        public virtual void ClearSimSubscriptions()
        {
            foreach (var sub in SubVolume)
                try { sub.Value.OnReceived -= OnVolumeChange; } catch { }
            foreach (var sub in SubMute)
                try { sub.Value.OnReceived -= OnMuteChange; } catch { }

            SubVolume.Clear();
            SubMute.Clear();
        }

        public virtual void SynchControls()
        {
            if (SubVolume.TryGetValue(Controller.Config.AudioAcpSide, out var subVol))
                OnVolumeChange(subVol, null);
            if (SubMute.TryGetValue(Controller.Config.AudioAcpSide, out var subMute))
                OnMuteChange(subMute, null);
        }

        protected virtual void OnVolumeChange(ISimResourceSubscription sub, object data)
        {
            if (!IsActive || SessionControls.Count == 0 || !SubVolume.ContainsKey(Controller.Config.AudioAcpSide))
                return;
            if (sub.Name != SubVolume[Controller.Config.AudioAcpSide].Name)
                return;

            float value = sub.GetValue<float>();
            if (value < 0 || value > 1.0f)
            {
                Logger.Debug($"Invalid Value Range for '{sub.Name}': {value}");
                return;
            }

            try
            {
                if (data != null || Controller.Config.AudioSynchSessionOnCountChange)
                    SessionControls.ForEach(ctrl => ctrl.SimpleAudioVolume.MasterVolume = value);
                else
                {
                    foreach (var ctrl in SessionControls)
                    {
                        if (Controller.ResetVolumes || !SynchedSessionsVolume.ContainsKey(ctrl.SessionInstanceIdentifier))
                        {
                            ctrl.SimpleAudioVolume.MasterVolume = value;
                            SynchedSessionsVolume.TryAdd(ctrl.SessionInstanceIdentifier, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected virtual void OnMuteChange(ISimResourceSubscription sub, object data)
        {
            if (!IsActive || SessionControls.Count == 0 || !UseLatch || !SubMute.ContainsKey(Controller.Config.AudioAcpSide))
                return;
            if (sub.Name != SubMute[Controller.Config.AudioAcpSide].Name)
                return;

            float value = sub.GetValue<float>();
            if (value < 0)
            {
                Logger.Debug($"Invalid Value Range for '{sub.Name}': {value}");
                return;
            }
            bool mute = value == 0.0f;

            try
            {
                if (data != null || Controller.Config.AudioSynchSessionOnCountChange)
                    SessionControls.ForEach(ctrl => ctrl.SimpleAudioVolume.Mute = mute);
                else
                {
                    foreach (var ctrl in SessionControls)
                    {
                        if (Controller.ResetVolumes || !SynchedSessionsMute.ContainsKey(ctrl.SessionInstanceIdentifier))
                        {
                            ctrl.SimpleAudioVolume.Mute = mute;
                            SynchedSessionsMute.TryAdd(ctrl.SessionInstanceIdentifier, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
