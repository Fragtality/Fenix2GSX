using CFIT.AppFramework.UI.ViewModels;
using Fenix2GSX.AppConfig;
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
            ViewModelSelector.BindMember(InputType, nameof(AircraftProfile.MatchType));
            ViewModelSelector.BindTextElement(InputMatchString, nameof(AircraftProfile.MatchString));

            ButtonAdd.Command = ViewModelSelector.BindAddUpdateButton(ButtonAdd, ImageAdd, GetItem, IsItemValid);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputName);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputType);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputMatchString);

            ButtonRemove.Command = ViewModelSelector.BindRemoveButton(ButtonRemove, ViewModel.IsSelectionNonDefault);
            ViewModelSelector.RemoveCommand.Executed += () => ViewModel.CheckActiveProfile();
            ViewModelSelector.RemoveCommand.Subscribe(InputName);
            ViewModelSelector.RemoveCommand.Subscribe(InputType);
            ViewModelSelector.RemoveCommand.Subscribe(InputMatchString);

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
            return !string.IsNullOrWhiteSpace(InputName?.Text) && InputType?.SelectedValue is ProfileMatchType type && type != ProfileMatchType.Default && !string.IsNullOrWhiteSpace(InputMatchString?.Text);
        }

        public virtual void Start()
        {
            ViewModel.Start();
        }

        public virtual void Stop()
        {
            ViewModel?.Stop();
        }
    }
}
