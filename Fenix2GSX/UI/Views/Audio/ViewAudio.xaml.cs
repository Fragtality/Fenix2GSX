using CFIT.AppFramework.UI.Validations;
using CFIT.AppFramework.UI.ViewModels;
using CFIT.AppLogger;
using CFIT.AppTools;
using Fenix2GSX.AppConfig;
using Fenix2GSX.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fenix2GSX.UI.Views.Audio
{
    public partial class ViewAudio : UserControl, IView
    {
        protected virtual ModelAudio ViewModel { get; }
        protected virtual ViewModelSelector<AudioMapping, AudioMapping> ViewModelMappings { get; }
        protected virtual ViewModelSelector<string, string> ViewModelBlacklist { get; }
        public virtual bool HasSelection => GridAudioMappings?.SelectedIndex != -1;

        public ViewAudio()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;

            SelectorCurrentChannel.ItemsSource = Enum.GetValues<AudioChannel>();
            ViewModel.BindStringNumber(nameof(ViewModel.StartupVolume), InputStartupVolume, "100", new ValidationRuleRange<double>(0.0, 100));

            ViewModelMappings = new(GridAudioMappings, ViewModel.AppMappingCollection, AppWindow.IconLoader);
            ButtonAddMapping.Command = ViewModelMappings.BindAddUpdateButton(ButtonAddMapping, ImageAddMapping, GetMappingItem);
            ButtonRemoveMapping.Command = ViewModelMappings.BindRemoveButton(ButtonRemoveMapping);

            SelectorMappingChannel.ItemsSource = Enum.GetValues<AudioChannel>();
            ViewModelMappings.BindMember(SelectorMappingChannel, nameof(AudioMapping.Channel));
            ViewModelMappings.AddUpdateCommand.Subscribe(SelectorCurrentChannel);

            ViewModelMappings.BindTextElement(InputMappingApp, nameof(AudioMapping.Binary));
            ViewModelMappings.AddUpdateCommand.Subscribe(InputMappingApp);

            SelectorMappingDevice.ItemsSource = ViewModel.AudioDevices;
            ViewModelMappings.BindMember(SelectorMappingDevice, nameof(AudioMapping.DeviceName));
            ViewModelMappings.AddUpdateCommand.Subscribe(SelectorMappingDevice);

            ViewModelMappings.BindMember(CheckboxMappingMute, nameof(AudioMapping.UseLatch));
            ViewModelMappings.AddUpdateCommand.Subscribe(CheckboxMappingMute);

            ViewModelMappings.BindMember(CheckboxOnlyActive, nameof(AudioMapping.OnlyActive));
            ViewModelMappings.AddUpdateCommand.Subscribe(CheckboxOnlyActive);

            GridAudioMappings.SizeChanged += OnGridSizeChanged;

            ViewModelBlacklist = new(ListDeviceBlacklist, ViewModel.BlacklistCollection, AppWindow.IconLoader);
            ButtonAddDevice.Command = ViewModelBlacklist.BindAddUpdateButton(ButtonAddDevice, ImageAddDevice, GetDeviceItem);
            ButtonRemoveDevice.Command = ViewModelBlacklist.BindRemoveButton(ButtonRemoveDevice);

            ViewModelBlacklist.BindTextElement(InputDevice);
            ViewModelBlacklist.AddUpdateCommand.Subscribe(InputDevice);

            CommandBinding copyCommandBinding = new(ApplicationCommands.Copy,
                            (_, _) => { InputDevice.Text = SelectorMappingDevice.SelectedValue as string; ViewModelBlacklist.AddUpdateCommand.NotifyCanExecuteChanged(); });
            CommandBindings.Add(copyCommandBinding);
            SelectorMappingDevice.CommandBindings.Add(copyCommandBinding);

            ListDeviceBlacklist.SizeChanged += OnListSizeChanged;

            InputMappingApp.KeyUp += InputMappingApp_KeyUp;
            ListActiveProcesses.SelectionChanged += OnProcessSelected;
            InputMappingApp.GotFocus += OnBinaryInputFocused;
            InputMappingApp.LostFocus += OnBinaryInputUnfocused;
        }

        protected virtual void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                int offset = 4;
                SelectorMappingChannel.Width = GridAudioMappings.Columns[0].ActualWidth - offset;
                InputMappingApp.Width = GridAudioMappings.Columns[1].ActualWidth - offset;
                ListActiveProcesses.Width = GridAudioMappings.Columns[1].ActualWidth - offset;
                SelectorMappingDevice.Width = GridAudioMappings.Columns[2].ActualWidth - offset;
                PanelMute.Width = GridAudioMappings.Columns[3].ActualWidth;
                PanelActive.Width = GridAudioMappings.Columns[4].ActualWidth;
            }
            catch { }
        }

        protected virtual void OnListSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                InputDevice.Width = ListDeviceBlacklist.ActualWidth;
            }
            catch { }
        }

        protected virtual AudioMapping GetMappingItem()
        {
            try
            {
                if (SelectorMappingChannel?.SelectedValue is AudioChannel channel
                    && SelectorMappingDevice?.SelectedValue is string device && !string.IsNullOrWhiteSpace(device)
                    && !string.IsNullOrWhiteSpace(InputMappingApp?.Text)
                    && CheckboxMappingMute?.IsChecked is bool unmute
                    && CheckboxOnlyActive?.IsChecked is bool onlyActive)
                    return new AudioMapping(channel, (device == "All" ? "" : device), InputMappingApp?.Text, unmute, onlyActive);
            }
            catch { }

            return null;
        }

        protected virtual string GetDeviceItem()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(InputDevice?.Text))
                    return InputDevice?.Text;
            }
            catch { }

            return null;
        }

        protected virtual void OnBinaryInputFocused(object sender, RoutedEventArgs e)
        {
            ListActiveProcesses.ItemsSource = GetProcesses(InputMappingApp?.Text);
            ListActiveProcesses.Visibility = Visibility.Visible;
        }

        protected virtual void InputMappingApp_KeyUp(object sender, KeyEventArgs e)
        {
            if (!Sys.IsEnter(e))
            {
                ListActiveProcesses.ItemsSource = GetProcesses(InputMappingApp?.Text);
            }
        }

        protected virtual void OnProcessSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ListActiveProcesses?.SelectedIndex != -1 && ListActiveProcesses?.SelectedValue is string str)
            {
                InputMappingApp.Text = str;
                ListActiveProcesses.ItemsSource = null;
                ListActiveProcesses.Visibility = Visibility.Collapsed;
            }
        }

        protected virtual void OnBinaryInputUnfocused(object sender, RoutedEventArgs e)
        {
            ListActiveProcesses.Visibility = Visibility.Collapsed;
        }

        protected virtual List<string> GetProcesses(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return [.. Process.GetProcesses().Select(p => p.ProcessName)];
                else
                    return [.. Process.GetProcesses().Where(p => p.ProcessName.Contains(name, StringComparison.InvariantCultureIgnoreCase)).Select(p => p.ProcessName)];
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return [];
            }
        }

        public virtual void Start()
        {
            SelectorMappingDevice.ItemsSource = null;
            SelectorMappingDevice.ItemsSource = ViewModel.AudioDevices;
            SelectorMappingDevice.SelectedIndex = 0;
        }

        public virtual void Stop()
        {
            
        }
    }
}
