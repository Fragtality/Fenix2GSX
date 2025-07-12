using CFIT.AppFramework.UI.ValueConverter;
using CFIT.AppFramework.UI.ViewModels;
using Fenix2GSX.AppConfig;
using Fenix2GSX.Audio;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Fenix2GSX.UI.Views.Audio
{
    public partial class ModelAppMappings(ModelAudio modelAudio) : ViewModelCollection<AudioMapping, AudioMapping>(modelAudio.Source.AudioMappings, (s) => s, (s) => s != null)
    {
        protected virtual ModelAudio ModelAudio { get; } = modelAudio;
        public override ICollection<AudioMapping> Source => ModelAudio.Source.AudioMappings;

        protected override void InitializeMemberBindings()
        {
            base.InitializeMemberBindings();

            CreateMemberBinding<AudioChannel, AudioChannel>(nameof(AudioMapping.Channel), new NoneConverter());
            CreateMemberBinding<string, string>(nameof(AudioMapping.DeviceName), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(AudioMapping.UseLatch), new NoneConverter());
            CreateMemberBinding<bool, bool>(nameof(AudioMapping.OnlyActive), new NoneConverter());
        }

        public override bool UpdateSource(AudioMapping oldItem, AudioMapping newItem)
        {
            try
            {
                oldItem.Channel = newItem.Channel;
                oldItem.Device = newItem.Device;
                oldItem.Binary = newItem.Binary;
                oldItem.UseLatch = newItem.UseLatch;
                oldItem.OnlyActive = newItem.OnlyActive;
                return true;
            }
            catch { }

            return false;
        }

        public override void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e = null)
        {
            ModelAudio.Source.AudioMappings.Sort();
            base.NotifyCollectionChanged(e);
        }
    }
}
