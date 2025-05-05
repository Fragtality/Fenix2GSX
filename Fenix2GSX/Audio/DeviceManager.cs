using CFIT.AppLogger;
using CFIT.AppTools;
using CoreAudio;
using Fenix2GSX.AppConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Fenix2GSX.Audio
{
    public class DeviceManager(AudioController controller)
    {
        protected virtual AudioController Controller { get; } = controller;
        protected virtual Config Config => Controller.Config;
        protected virtual MMDeviceEnumerator DeviceEnumerator { get; } = new(Guid.NewGuid());
        public virtual ConcurrentDictionary<string, MMDevice> Devices { get; } = [];
        protected virtual DateTime LastDeviceScan { get; set; } = DateTime.MinValue;
        protected virtual int LastDeviceCount { get; set; } = 0;
        protected virtual int SessionCount => Devices.Sum(d => d.Value.AudioSessionManager2.Sessions.Count);
        protected virtual int LastSessionCount { get; set; } = 0;

        public event Action DevicesChanged;

        protected virtual void Add(List<MMDevice> devices)
        {
            foreach (var device in devices)
                Devices.Add(device.DeviceFriendlyName, device);
        }

        public virtual void Clear()
        {
            Devices.Clear();
        }

        public virtual bool Scan(bool force = false)
        {
            bool result = false;

            try
            {
                if (DateTime.Now >= LastDeviceScan + TimeSpan.FromMilliseconds(Config.AudioDeviceCheckInterval) || force)
                {
                    if (force)
                        Logger.Debug($"Scanning Audio Devices");
                    else
                        Logger.Verbose($"Scanning Audio Devices");
                    var deviceList = EnumerateDevices(out int sessionCount);

                    if (LastDeviceCount != deviceList.Count || LastSessionCount != sessionCount || force)
                    {
                        Logger.Debug($"Device Enumeration needed - DeviceCount {LastDeviceCount != deviceList.Count} | SessionCount {LastSessionCount != sessionCount} | Forced {force}");
                        result = true;
                        Clear();
                        Add(deviceList);
                    }

                    LastSessionCount = SessionCount;
                    LastDeviceCount = Devices.Count;
                    LastDeviceScan = DateTime.Now;
                }

                if (result)
                    DevicesChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return result;
        }

        protected virtual List<MMDevice> EnumerateDevices(out int sessionCount)
        {
            List<MMDevice> devices = [];
            sessionCount = 0;
            var deviceList = DeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in deviceList)
            {
                try
                {
                    if (Config.AudioDeviceBlacklist.Where(d => d.StartsWith(device.DeviceFriendlyName, StringComparison.InvariantCultureIgnoreCase)).Any())
                    {
                        Logger.Debug($"Ignoring Device '{device.DeviceFriendlyName}' (on Blacklist)");
                        continue;
                    }

                    Logger.Verbose($"Testing Sessions on '{device.DeviceFriendlyName}'");
                    foreach (var session in device.AudioSessionManager2.Sessions)
                        Logger.Verbose($"Name: {session.DisplayName} | ID: {session.ProcessID} | SessionInstance: {session.SessionInstanceIdentifier}");
                    sessionCount += device.AudioSessionManager2.Sessions.Count;

                    devices.Add(device);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.Debug($"Ignoring Device '{device.DeviceFriendlyName}' (raised Exception)");
                    Config.AudioDeviceBlacklist.Add(device.DeviceFriendlyName);
                }
            }

            return devices;
        }

        public virtual List<string> GetDeviceNames()
        {
            List<string> devices = [];
            var deviceList = DeviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in deviceList)
            {
                try
                {
                    Logger.Verbose($"Testing Sessions on '{device.DeviceFriendlyName}'");
                    foreach (var session in device.AudioSessionManager2.Sessions)
                        Logger.Verbose($"Name: {session.DisplayName} | ID: {session.ProcessID}");

                    devices.Add(device.DeviceFriendlyName);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    Logger.Debug($"Device '{device.DeviceFriendlyName}' raised an Exception");
                }
            }

            return devices;
        }

        public virtual List<AudioSessionControl2> GetAudioSessions(AudioSession audioSession)
        {
            List<AudioSessionControl2> list = [];
            bool allDevices = string.IsNullOrWhiteSpace(audioSession.Device);
            try
            {
                foreach (var device in Devices.Values)
                {
                    if (!allDevices && !device.DeviceInterfaceFriendlyName.Equals(audioSession.Device, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var query = device.AudioSessionManager2.Sessions.Where(s => (s.ProcessID == audioSession.ProcessId || s.SessionInstanceIdentifier.Contains($"{audioSession.Binary}.exe", StringComparison.InvariantCultureIgnoreCase)) && s.State == AudioSessionState.AudioSessionStateActive);
                    if (query.Any())
                    {
                        list.AddRange(query);
                        Logger.Debug($"Found {list.Count} Sessions on Device '{device.DeviceFriendlyName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return list;
        }
    }
}
