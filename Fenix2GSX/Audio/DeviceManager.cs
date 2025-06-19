using CFIT.AppLogger;
using CFIT.AppTools;
using CoreAudio;
using Fenix2GSX.AppConfig;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        protected virtual void Add(Dictionary<string, MMDevice> devices)
        {
            foreach (var device in devices)
                Devices.Add(device.Key, device.Value);
        }

        public virtual void Clear()
        {
            Devices.Clear();
        }

        public virtual bool Scan()
        {
            bool result = false;

            try
            {
                if (DateTime.Now >= LastDeviceScan + TimeSpan.FromMilliseconds(Config.AudioDeviceCheckInterval))
                {
                    Logger.Debug($"Scanning Audio Devices");
                    var deviceList = EnumerateDevices(out int sessionCount);

                    if (LastDeviceCount != deviceList.Count || LastSessionCount != sessionCount)
                    {
                        Logger.Debug($"Device Enumeration needed - DeviceCount {LastDeviceCount != deviceList.Count} | SessionCount {LastSessionCount != sessionCount}");
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

        protected virtual Dictionary<string, MMDevice> EnumerateDevices(out int sessionCount)
        {
            Dictionary<string, MMDevice> devices = [];
            sessionCount = 0;
            MMDeviceCollection deviceList = null;
            try
            {
                deviceList = DeviceEnumerator.EnumerateAudioEndPoints(Config.AudioDeviceFlow, Config.AudioDeviceState);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            if (deviceList == null)
                return devices;

            foreach (var device in deviceList)
            {
                try
                {
                    string deviceName = device.DeviceFriendlyName;
                    if (Config.AudioDeviceBlacklist.Where(d => d.StartsWith(deviceName, StringComparison.InvariantCultureIgnoreCase)).Any())
                    {
                        Logger.Debug($"Ignoring Device '{deviceName}' (on Blacklist)");
                        continue;
                    }

                    if (Config.LogLevel == LogLevel.Verbose)
                    {
                        Logger.Verbose($"Testing Sessions on '{deviceName}'");
                        foreach (var session in device.AudioSessionManager2.Sessions)
                            Logger.Verbose($"Name: {session.DisplayName} | ID: {session.ProcessID} | SessionInstance: {session.SessionInstanceIdentifier}");
                    }
                    sessionCount += device.AudioSessionManager2.Sessions.Count;

                    devices.Add(deviceName, device);
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
            MMDeviceCollection deviceList = null;
            try
            {
                deviceList = DeviceEnumerator.EnumerateAudioEndPoints(Config.AudioDeviceFlow, Config.AudioDeviceState);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            if (deviceList == null)
                return devices;

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
                        if (Config.LogLevel == LogLevel.Verbose)
                            Logger.Verbose($"Found {list.Count} Sessions on Device '{device.DeviceFriendlyName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return list;
        }

        public virtual void WriteDebugInformation()
        {
            try
            {
                StringBuilder debugInfo = new();
                
                try
                {
                    debugInfo.AppendLine($"Configured Audio Mappings: {Config.AudioMappings.Count}");
                    int i = 0;
                    foreach (var mapping in Config.AudioMappings)
                        debugInfo.AppendLine($"\tMapping #{i++} - {mapping}");
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Mapping Enumeration raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                }

                try
                {
                    debugInfo.AppendLine("");
                    debugInfo.AppendLine("Process Enumeration ...");
                    int i = 0;
                    foreach (var mapping in Config.AudioMappings)
                    {
                        var proc = Sys.GetProcess(mapping.Binary);
                        var procId = proc?.Id ?? 0;
                        var procRunning = proc?.ProcessName == mapping.Binary;
                        debugInfo.AppendLine($"\tProcess for Mapping #{i++} - Binary '{mapping.Binary}' (Running: {procRunning} | ID: {procId})");
                    }
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Process Enumeration raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                }

                MMDeviceCollection deviceList = null;
                try
                {
                    debugInfo.AppendLine("");
                    deviceList = DeviceEnumerator.EnumerateAudioEndPoints(Config.AudioDeviceFlow, Config.AudioDeviceState);
                    debugInfo.AppendLine($"EnumerateAudioEndPoints(): Enumerated {deviceList.Count} Audio Devices (Flow: {Config.AudioDeviceFlow} | State: {Config.AudioDeviceState}).");
                }
                catch (Exception ex)
                {
                    debugInfo.AppendLine($"Device Enumeration raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                }
                if (deviceList == null)
                    return;

                foreach (var device in deviceList)
                {
                    try
                    {
                        debugInfo.AppendLine($"Scanning Device '{device.DeviceFriendlyName}' (Sessions: {device?.AudioSessionManager2?.Sessions?.Count} | Blacklisted: {Config.AudioDeviceBlacklist.Where(d => d.StartsWith(device.DeviceFriendlyName, StringComparison.InvariantCultureIgnoreCase)).Any()})");
                        int i = 1;
                        foreach (var session in device.AudioSessionManager2.Sessions)
                            debugInfo.AppendLine($"\tSession #{i++} - Name: {session.DisplayName} | ID: {session.ProcessID} | SessionInstance: {session.SessionInstanceIdentifier}");
                    }
                    catch (Exception ex)
                    {
                        debugInfo.AppendLine($"Device raised Exception: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}");
                    }
                }
            
                File.WriteAllText(Config.AudioDebugFile, debugInfo.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}
