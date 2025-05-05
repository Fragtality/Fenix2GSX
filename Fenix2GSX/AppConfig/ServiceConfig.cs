using Fenix2GSX.GSX.Services;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fenix2GSX.AppConfig
{
    public class ServiceConfig
    {
        [JsonIgnore]
        public static Dictionary<GsxServiceActivation, string> TextServiceActivations { get; } = new()
        {
            { GsxServiceActivation.Skip, "Skip / Ignore" },
            { GsxServiceActivation.Manual, "Manual by User" },
            { GsxServiceActivation.AfterCalled, "Previous Service called" },
            { GsxServiceActivation.AfterRequested, "Previous Service requested" },
            { GsxServiceActivation.AfterActive, "Previous Service active" },
            { GsxServiceActivation.AfterPrevCompleted, "Previous Service completed" },
            { GsxServiceActivation.AfterAllCompleted, "All Services completed" },
        };

        public virtual GsxServiceType ServiceType { get; set; } = GsxServiceType.Unknown;
        public virtual GsxServiceActivation ServiceActivation { get; set; } = GsxServiceActivation.Manual;
        [JsonIgnore]
        public virtual string ServiceActivationName => TextServiceActivations[ServiceActivation];
        public virtual TimeSpan MinimumFlightDuration { get; set; } = TimeSpan.Zero;
        [JsonIgnore]
        public virtual bool HasDurationConstraint => MinimumFlightDuration > TimeSpan.Zero;

        public ServiceConfig(){ }

        public ServiceConfig(GsxServiceType type, GsxServiceActivation activation) : this(type, activation, TimeSpan.Zero) { }

        public ServiceConfig(GsxServiceType type, GsxServiceActivation activation, TimeSpan duration)
        {
            ServiceType = type;
            ServiceActivation = activation;
            MinimumFlightDuration = duration;
        }
    }
}
