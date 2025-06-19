using CFIT.AppFramework.UI.ViewModels;
using Fenix2GSX.AppConfig;
using System;
using System.Linq;
using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Profiles
{
    public partial class ViewProfiles : UserControl, IView
    {
        protected virtual ModelProfiles ViewModel { get; }
        protected virtual ViewModelSelector<AircraftProfile, AircraftProfile> ViewModelSelector => ViewModel.ViewModelSelector;

        public ViewProfiles()
        {
            InitializeComponent();
            
            ViewModel = new(AppService.Instance, SelectorProfiles);
            this.DataContext = ViewModel;

            InputType.ItemsSource = ModelProfiles.MatchTypes;

            ViewModelSelector.BindTextElement(InputName, nameof(AircraftProfile.Name));
            ViewModelSelector.BindMember(InputType, nameof(AircraftProfile.MatchType), null, ProfileMatchType.Default);
            ViewModelSelector.BindTextElement(InputMatchString, nameof(AircraftProfile.MatchString));

            ButtonAdd.Command = ViewModelSelector.BindAddUpdateButton(ButtonAdd, ImageAdd, GetItem, IsItemValid);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputName);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputType);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputMatchString);
            ViewModelSelector.AddUpdateCommand.Executed += () => AppService.Instance?.Config?.NotifyPropertyChanged(nameof(Config.CurrentProfile));

            ButtonRemove.Command = ViewModelSelector.BindRemoveButton(ButtonRemove, ViewModel.IsSelectionNonDefault);
            ViewModelSelector.RemoveCommand.Subscribe(InputName);
            ViewModelSelector.RemoveCommand.Subscribe(InputType);
            ViewModelSelector.RemoveCommand.Subscribe(InputMatchString);
            ViewModelSelector.RemoveCommand.Executed += () => ViewModel.CheckActiveProfile();

            ButtonSetActive.Command = ViewModel.SetActiveCommand;
        }

        protected virtual AircraftProfile GetItem()
        {
            try
            {
                return new AircraftProfile() { Name = InputName.Text, MatchType = (ProfileMatchType)InputType.SelectedValue, MatchString = InputMatchString.Text };
            }
            catch
            {
                return default;
            }
        }

        public virtual bool IsItemValid()
        {
            bool baseCheck = !string.IsNullOrWhiteSpace(InputName?.Text) && InputType?.SelectedValue is ProfileMatchType type && type != ProfileMatchType.Default && !string.IsNullOrWhiteSpace(InputMatchString?.Text);
            if (!baseCheck)
                return false;

            if (InputName?.Text?.Equals(ViewModelSelector?.SelectedItem?.Name, StringComparison.InvariantCultureIgnoreCase) == true)
                return true;
            else
                return ViewModelSelector?.ItemsSource?.Source?.Any(p => p.Name.Equals(InputName?.Text, StringComparison.InvariantCultureIgnoreCase)) == false;
        }

        public virtual void Start()
        {
            SelectorProfiles.SelectedItem = AppService.Instance?.Config?.CurrentProfile;
            ViewModel.Start();
        }

        public virtual void Stop()
        {
            ViewModel?.Stop();
        }
    }
}
