using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using CFIT.AppLogger;
using CFIT.AppTools;
using CommunityToolkit.Mvvm.ComponentModel;
using Fenix2GSX.Aircraft;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Fenix2GSX.UI.Views.Profiles
{
    public partial class ModelProfiles : ViewModelBase<AppService>
    {
        protected virtual Selector Selector { get; }
        protected virtual ModelProfileCollection ProfileCollection { get; }
        public virtual ViewModelSelector<AircraftProfile, AircraftProfile> ViewModelSelector { get; }
        protected virtual DispatcherTimer UpdateTimer { get; set; }
        protected virtual Config Config => this.Source.Config;
        protected virtual GsxController GsxController => this.Source.GsxService;
        protected virtual AircraftInterface AircraftInterface => GsxController?.AircraftInterface;
        protected virtual bool ForceRefresh { get; set; } = false;
        public virtual ICommandWrapper SetActiveCommand { get; }
        public virtual ICommandWrapper CloneCommand { get; }

        public ModelProfiles(AppService source, Selector selector) : base(source)
        {
            Selector = selector;
            ProfileCollection = new();
            ViewModelSelector = new(Selector, ProfileCollection, AppWindow.IconLoader);

            Selector.SelectionChanged += (_, _) => NotifyPropertyChanged(nameof(IsEditAllowed));
            ProfileCollection.CreateMemberBinding<ProfileMatchType, ProfileMatchType>(nameof(AircraftProfile.MatchType), null);
            ProfileCollection.CollectionChanged += OnCollectionChanged;

            SetActiveCommand = new CommandWrapper(() => GsxController.SetAircraftProfile((Selector?.SelectedValue as AircraftProfile)?.Name), () => Selector?.SelectedValue is AircraftProfile);
            SetActiveCommand.Subscribe(Selector);

            CloneCommand = new CommandWrapper(CloneProfile, () => Selector?.SelectedValue is AircraftProfile profile && profile.MatchType != ProfileMatchType.Default);
            CloneCommand.Subscribe(Selector);
        }

        protected virtual void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Config.SaveConfiguration();
        }

        public virtual void CheckActiveProfile()
        {
            if (!Config.AircraftProfiles.Any(p => p.Name == GsxController.AircraftProfile?.Name))
                GsxController.LoadAircraftProfile();
        }

        protected virtual void CloneProfile()
        {
            try
            {
                Logger.Debug($"Cloning Profile ...");
                if (Selector?.SelectedValue is not AircraftProfile profile)
                {
                    Logger.Warning($"The selected Value is not an AircraftProfile");
                    return;
                }

                if (profile.MatchType == ProfileMatchType.Default)
                {
                    Logger.Warning($"Can not clone Default Profiles");
                    return;
                }

                string json = JsonSerializer.Serialize<AircraftProfile>(profile);
                var clone = JsonSerializer.Deserialize<AircraftProfile>(json);
                clone.Name = $"Clone of {profile.Name}";

                if (Config.AircraftProfiles.Any(p => p.Name.Equals(clone.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Logger.Warning($"The Profile '{clone.Name}' is already configured");
                    return;
                }

                Config.AircraftProfiles.Add(clone);
                ProfileCollection.NotifyCollectionChanged();
                Logger.Debug($"Cloned Profile '{profile.Name}'");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected override void InitializeModel()
        {
            UpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(AppService.Instance.Config.UiRefreshInterval),
            };
            UpdateTimer.Tick += OnUpdate;
        }

        public virtual void Start()
        {
            ForceRefresh = true;
            UpdateTimer.Start();
        }

        public virtual void Stop()
        {
            UpdateTimer?.Stop();
        }

        protected virtual void UpdateState<T>(string propertyValue, T value)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyValue) || (object)value == null)
                    return;

                if (!this.GetPropertyValue<T>(propertyValue)?.Equals(value) == true || ForceRefresh)
                    this.SetPropertyValue<T>(propertyValue, value);
            }
            catch { }
        }

        protected virtual void OnUpdate(object? sender, EventArgs e)
        {
            try { UpdateState<string>(nameof(CurrentAirline), AircraftInterface?.Airline); } catch { }
            try { UpdateState<string>(nameof(CurrentRegistration), AircraftInterface?.Registration); } catch { }
            try { UpdateState<string>(nameof(CurrentTitle), AircraftInterface?.Title); } catch { }
            try { UpdateState<string>(nameof(CurrentProfile), GsxController?.AircraftProfile?.ToString() ?? ""); } catch { }
            ForceRefresh = false;
        }

        [ObservableProperty]
        protected string _CurrentAirline = "";

        [ObservableProperty]
        protected string _CurrentRegistration = "";

        [ObservableProperty]
        protected string _CurrentTitle = "";

        [ObservableProperty]
        protected string _CurrentProfile = "";

        public static Dictionary<ProfileMatchType, string> MatchTypes { get; } = new()
        {
            {ProfileMatchType.Default, "Default" },
            {ProfileMatchType.Airline, "Airline" },
            {ProfileMatchType.Title, "Title" },
            {ProfileMatchType.Registration, "Registration" },
        };

        public virtual bool IsSelectionNonDefault()
        {
            return !IsSelectionDefault();
        }

        public virtual bool IsEditAllowed => !IsSelectionDefault() || Selector?.SelectedValue == null;

        public virtual bool IsSelectionDefault()
        {
            return (Selector?.SelectedValue is AircraftProfile profile && profile.MatchType == ProfileMatchType.Default);
        }
    }
}
