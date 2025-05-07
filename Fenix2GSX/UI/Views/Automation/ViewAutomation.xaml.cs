using System.Collections.Generic;
using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Automation
{
    public enum SettingControl
    {
        GateDoors = 0,
        GroundEquip = 1,
        GsxServices = 2,
        OperatorSelection = 3,
        CompanyHubs = 4,
        SkipQuestions = 5,
        AircraftOptions = 6,
    }

    public partial class ViewAutomation : UserControl, IView
    {
        protected virtual ModelAutomation ViewModel { get; }
        protected virtual Dictionary<SettingControl, UserControl> SettingControls { get; } = [];
        protected static Dictionary<SettingControl, string> SettingGroups { get; } = new()
        {
            { SettingControl.GateDoors, "Gate & Doors" },
            { SettingControl.GroundEquip, "Ground Equipment" },
            { SettingControl.GsxServices, "GSX Services" },
            { SettingControl.OperatorSelection, "Operator Selection" },
            { SettingControl.CompanyHubs, "Company Hubs" },
            { SettingControl.SkipQuestions, "Skip Questions" },
            { SettingControl.AircraftOptions, "Aircraft Options" },
        };

        public ViewAutomation()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            SettingControls.Add(SettingControl.GateDoors, new ControlGateDoors(ViewModel));
            SettingControls.Add(SettingControl.GroundEquip, new ControlGroundEquip(ViewModel));
            SettingControls.Add(SettingControl.GsxServices, new ControlGsxServices(ViewModel));
            SettingControls.Add(SettingControl.OperatorSelection, new ControlOperatorSelection(ViewModel));
            SettingControls.Add(SettingControl.CompanyHubs, new ControlCompanyHubs(ViewModel));
            SettingControls.Add(SettingControl.SkipQuestions, new ControlSkipQuestions(ViewModel));
            SettingControls.Add(SettingControl.AircraftOptions, new ControlAircraftOptions(ViewModel));

            SelectorSettingGroup.ItemsSource = SettingGroups;
            SelectorSettingGroup.SelectionChanged += OnSelectionChanged;
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectorSettingGroup?.SelectedValue is SettingControl controlKey && SettingControls.TryGetValue(controlKey, out var control))
                ViewSettingGroup.Content = control;
        }

        public virtual void Start()
        {
            if (SelectorSettingGroup?.SelectedValue is not SettingControl)
                SelectorSettingGroup.SelectedIndex = 0;
        }

        public virtual void Stop()
        {
            
        }
    }
}
