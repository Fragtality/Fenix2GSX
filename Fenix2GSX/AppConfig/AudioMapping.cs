using Fenix2GSX.Audio;
using System;
using System.Text.Json.Serialization;

namespace Fenix2GSX.AppConfig
{
    public class AudioMapping : IComparable<AudioMapping>
    {
        public virtual AudioChannel Channel { get; set; }
        public virtual string Device { get; set; }
        public virtual string Binary { get; set; }
        public virtual bool UseLatch { get; set; }
        public virtual bool OnlyActive { get; set; } = true;

        public AudioMapping() { }

        public AudioMapping(AudioChannel channel, string device, string binary, bool useLatch = true, bool onlyActive = true)
        {
            Channel = channel;
            Device = device;
            Binary = binary;
            UseLatch = useLatch;
            OnlyActive = onlyActive;
        }

        [JsonIgnore]
        public virtual string DeviceName { get => string.IsNullOrWhiteSpace(Device) ? "All" : Device; set { Device = string.IsNullOrWhiteSpace(value) || value == "All" ? "" : value; } }

        public int CompareTo(AudioMapping? other)
        {
            return Channel.CompareTo(other.Channel);
        }

        public override string ToString()
        {
            return $"Channel: {Channel} - Binary '{Binary}' @ Device '{DeviceName}' (UseLatch: {UseLatch} | OnlyActive: {OnlyActive})";
        }
    }
}
