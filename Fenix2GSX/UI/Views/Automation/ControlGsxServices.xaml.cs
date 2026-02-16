using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppFramework.UI.ViewModels.Commands;
using Fenix2GSX.AppConfig;
using Fenix2GSX.GSX.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ControlGsxServices : UserControl, INotifyPropertyChanged
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual ViewModelSelector<ServiceConfig, ServiceConfig> ViewModelSelector { get; }
        protected virtual TimeSpanConverter TimeSpanConverter { get; } = new TimeSpanConverter();
        public virtual bool HasSelection => GridDepartureServices?.SelectedIndex != -1;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ControlGsxServices(ModelAutomation viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            this.DataContext = ViewModel;
            ImageEdit.Source = AppWindow.IconLoader.LoadIcon("edit");

            ViewModel.BindStringNumber(nameof(ViewModel.RefuelRateKgSec), InputRefuelRate, "28");
            ViewModel.BindStringInteger(nameof(ViewModel.RefuelTimeTargetSeconds), InputTimeTarget, "150");

            ViewModelSelector = new(GridDepartureServices, ViewModel.DepartureServices, AppWindow.IconLoader);
            ViewModelSelector.BindTextElement(LabelServiceName, nameof(ServiceConfig.ServiceType));
            ViewModelSelector.BindMember(SelectorActivation, nameof(ServiceConfig.ServiceActivation));
            ViewModelSelector.BindTextElement(InputActivateAt, nameof(ServiceConfig.MaxTimeBeforeDeparture), "0", TimeSpanConverter);
            ViewModelSelector.BindTextElement(InputDuration, nameof(ServiceConfig.MinimumFlightDuration), "0", TimeSpanConverter);
            ViewModelSelector.BindMember(SelectorConstraint, nameof(ServiceConfig.ServiceConstraint));
            ViewModelSelector.BindTextElement(InputRunTime, nameof(ServiceConfig.MaxRunTime), "0", TimeSpanConverter);

            ViewModelSelector.BindAddUpdateButton(ButtonEdit, null, GetItem, () => HasSelection);
            ViewModelSelector.AddUpdateCommand.Subscribe(SelectorActivation);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputActivateAt);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputDuration);
            ViewModelSelector.AddUpdateCommand.Subscribe(SelectorConstraint);
            ViewModelSelector.AddUpdateCommand.Subscribe(InputRunTime);

            GridDepartureServices.SelectionChanged += (_, _) => NotifyPropertyChanged(nameof(HasSelection));

            ButtonUp.Command = new CommandWrapper(() => ViewModel.DepartureServices.MoveItem(GridDepartureServices.SelectedIndex, -1), () => GridDepartureServices?.SelectedIndex != -1).Subscribe(GridDepartureServices);
            ButtonDown.Command = new CommandWrapper(() => ViewModel.DepartureServices.MoveItem(GridDepartureServices.SelectedIndex, 1), () => GridDepartureServices?.SelectedIndex != -1).Subscribe(GridDepartureServices);

            GridDepartureServices.SizeChanged += OnGridSizeChanged;
        }

        protected virtual void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                LabelServiceName.Width = GridDepartureServices.Columns[0].ActualWidth;
                SelectorActivation.Width = GridDepartureServices.Columns[1].ActualWidth;
                SelectorConstraint.Width = GridDepartureServices.Columns[3].ActualWidth - 10;
            }
            catch { }
        }

        protected virtual ServiceConfig GetItem()
        {
            try
            {
                if (GridDepartureServices?.SelectedValue is ServiceConfig serviceConfig
                    && SelectorActivation?.SelectedValue is GsxServiceActivation activation
                    && !string.IsNullOrWhiteSpace(InputActivateAt?.Text)
                    && !string.IsNullOrWhiteSpace(InputDuration?.Text)
                    && SelectorConstraint?.SelectedValue is GsxServiceConstraint constraint
                    && !string.IsNullOrWhiteSpace(InputRunTime?.Text))
                    
                    return new ServiceConfig(serviceConfig.ServiceType,
                                             activation,
                                             (TimeSpan)TimeSpanConverter.ConvertBack(InputDuration.Text, typeof(TimeSpan), null, null),
                                             constraint,
                                             (TimeSpan)TimeSpanConverter.ConvertBack(InputActivateAt.Text, typeof(TimeSpan), null, null),
                                             (TimeSpan)TimeSpanConverter.ConvertBack(InputRunTime.Text, typeof(TimeSpan), null, null));
            }
            catch { }
            
            return null;
        }

        public virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void NotifyUpdate()
        {
            NotifyPropertyChanged(string.Empty);
        }
    }
}
