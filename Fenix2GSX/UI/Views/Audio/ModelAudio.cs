using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreAudio;
using Fenix2GSX.AppConfig;
using Fenix2GSX.Audio;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Fenix2GSX.UI.Views.Audio
{
    public partial class ModelAudio : ModelBase<Config>
    {
        public ICommand CommandDebugInfo { get; } = new RelayCommand(() => AppService.Instance.AudioService.DeviceManager.WriteDebugInformation());

        public ModelAudio(AppService appService) : base(appService.Config, appService)
        {
            AppMappingCollection = new(this);
            AppMappingCollection.CollectionChanged += (_, _) => { SaveConfig(); AudioController.ResetMappings = true; };

            BlacklistCollection = new(this);
            BlacklistCollection.CollectionChanged += (_, _) => SaveConfig();
        }

        protected override void InitializeModel()
        {
            this.PropertyChanged += OnPropertyChanged;
            AudioController.DeviceManager.DevicesChanged += () => NotifyPropertyChanged(nameof(AudioDevices));
        }

        protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(CurrentChannel))
            {
                NotifyPropertyChanged(nameof(SetStartupVolume));
                NotifyPropertyChanged(nameof(StartupVolume));
                NotifyPropertyChanged(nameof(StartupUnmute));
            }
        }

        public virtual Dictionary<AcpSide, string> AcpSideOptions { get; } = new()
        {
            { AcpSide.CPT, "Captain" },
            { AcpSide.FO, "First Officer" },
        };

        public virtual AcpSide AudioAcpSide { get => Source.AudioAcpSide; set { SetModelValue<AcpSide>(value); AudioController.ResetVolumes = true; } }

        [ObservableProperty]
        protected AudioChannel _CurrentChannel = AudioChannel.VHF1;

        public virtual bool SetStartupVolume
        {
            get => Source.AudioStartupVolumes[CurrentChannel] >= 0.0;
            set
            {
                double setValue = value ? 1.0 : -1.0;
                Source.AudioStartupVolumes[CurrentChannel] = setValue;
                Source.SaveConfiguration();
                OnPropertyChanged(nameof(SetStartupVolume));
                OnPropertyChanged(nameof(StartupVolume));
            }
        }

        public virtual double StartupVolume
        {
            get => (Source.AudioStartupVolumes[CurrentChannel] >= 0.0 ? Source.AudioStartupVolumes[CurrentChannel] * 100.0 : 0);
            set
            {
                Source.AudioStartupVolumes[CurrentChannel] = value / 100.0;
                Source.SaveConfiguration();
                OnPropertyChanged(nameof(StartupVolume));
            }
        }

        public virtual bool StartupUnmute
        {
            get => Source.AudioStartupUnmute[CurrentChannel];
            set
            {
                Source.AudioStartupUnmute[CurrentChannel] = value;
                Source.SaveConfiguration();
                OnPropertyChanged(nameof(StartupUnmute));
            }
        }

        public virtual ModelAppMappings AppMappingCollection { get; }

        public virtual List<string> AudioDevices
        {
            get
            {
                var list = new List<string> { "All" };
                list.AddRange([.. AudioController.DeviceManager.GetDeviceNames()]);

                return list;
            }
        }

        public virtual ModelDeviceBlacklist BlacklistCollection { get; }

        public virtual Dictionary<DataFlow, string> DeviceDataFlows { get; } = new()
        {
            { DataFlow.Render, DataFlow.Render.ToString() },
            { DataFlow.Capture, DataFlow.Capture.ToString() },
            { DataFlow.All, DataFlow.All.ToString() },
        };
        public virtual DataFlow AudioDeviceFlow { get => Source.AudioDeviceFlow; set { SetModelValue<DataFlow>(value); AudioController.ResetMappings = true; } }

        public virtual Dictionary<DeviceState, string> DeviceStates { get; } = new()
        {
            { DeviceState.Active, DeviceState.Active.ToString() },
            { DeviceState.Disabled, DeviceState.Disabled.ToString() },
            { DeviceState.NotPresent, DeviceState.NotPresent.ToString() },
            { DeviceState.Unplugged, DeviceState.Unplugged.ToString() },
            { DeviceState.MaskAll, DeviceState.MaskAll.ToString() },
        };
        public virtual DeviceState AudioDeviceState { get => Source.AudioDeviceState; set { SetModelValue<DeviceState>(value); AudioController.ResetMappings = true; } }
    }
}
