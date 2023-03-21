using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fenix2GSX
{
    public partial class MainWindow : Window
    {
        protected NotifyIconViewModel notifyModel;
        protected ServiceModel serviceModel;
        protected DispatcherTimer timer;
        protected int lineCounter = 0;

        public MainWindow(NotifyIconViewModel notifyModel, ServiceModel serviceModel)
        {
            InitializeComponent();
            this.notifyModel = notifyModel;
            this.serviceModel = serviceModel;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += OnTick;
        }

        protected void LoadSettings()
        {
            chkWaitForConnect.IsChecked = serviceModel.WaitForConnect;
            chkGsxVolumeControl.IsChecked = serviceModel.GsxVolumeControl;
            chkDisableCrewBoarding.IsChecked = serviceModel.DisableCrew;
            chkAutoReposition.IsChecked = serviceModel.RepositionPlane;
            chkAutoConnect.IsChecked = serviceModel.AutoConnect;
            txtOperatorDelay.Text = Convert.ToString(serviceModel.OperatorDelay, CultureInfo.InvariantCulture);
            chkConnectPCA.IsChecked = serviceModel.ConnectPCA;
            chkPcaOnlyJetway.IsChecked = serviceModel.PcaOnlyJetways;
            chkAutoRefuel.IsChecked = serviceModel.AutoRefuel;
            chkCallCatering.IsChecked = serviceModel.CallCatering;
            chkAutoBoard.IsChecked = serviceModel.AutoBoarding;
            chkAutoDeboard.IsChecked = serviceModel.AutoDeboarding;
            txtRefuelRate.Text = Convert.ToString(serviceModel.RefuelRate, CultureInfo.InvariantCulture);
        }

        protected void UpdateLogArea()
        {
            while (Logger.MessageQueue.Count > 0)
            {
                
                if (lineCounter > 3)
                    txtLogMessages.Text = txtLogMessages.Text[(txtLogMessages.Text.IndexOf('\n') + 1)..];
                txtLogMessages.Text += Logger.MessageQueue.Dequeue().ToString() + "\n";
                lineCounter++;
            }
        }

        protected void UpdateStatus()
        {
            if (serviceModel.IsSimRunning)
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatMSFS.Foreground = new SolidColorBrush(Colors.Red);

            if (IPCManager.SimConnect.IsReady)
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSimConnect.Foreground = new SolidColorBrush(Colors.Red);

            if (serviceModel.IsFenixRunning)
                lblConnStatFenix.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatFenix.Foreground = new SolidColorBrush(Colors.Red);

            if (serviceModel.IsSessionRunning)
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.DarkGreen);
            else
                lblConnStatSession.Foreground = new SolidColorBrush(Colors.Red);
        }

        protected void OnTick(object sender, EventArgs e)
        {
            UpdateLogArea();
            UpdateStatus();
        }

        protected void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                notifyModel.CanExecuteHideWindow = false;
                notifyModel.CanExecuteShowWindow = true;
                timer.Stop();
            }
            else
            {
                LoadSettings();
                timer.Start();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void chkWaitForConnect_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("waitForConnect", chkWaitForConnect.IsChecked.ToString().ToLower());
        }

        private void chkGsxVolumeControl_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("gsxVolumeControl", chkGsxVolumeControl.IsChecked.ToString().ToLower());
        }

        private void chkAutoReposition_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("repositionPlane", chkAutoReposition.IsChecked.ToString().ToLower());
        }

        private void chkAutoConnect_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoConnect", chkAutoConnect.IsChecked.ToString().ToLower());
        }

        private void chkConnectPCA_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("connectPCA", chkConnectPCA.IsChecked.ToString().ToLower());
        }

        private void chkPcaOnlyJetway_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("pcaOnlyJetway", chkPcaOnlyJetway.IsChecked.ToString().ToLower());
        }

        private void chkDisableCrewBoarding_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("disableCrew", chkDisableCrewBoarding.IsChecked.ToString().ToLower());
        }

        private void chkAutoRefuel_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoRefuel", chkAutoRefuel.IsChecked.ToString().ToLower());
        }

        private void chkCallCatering_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("callCatering", chkCallCatering.IsChecked.ToString().ToLower());
        }

        private void chkAutoBoard_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoBoarding", chkAutoBoard.IsChecked.ToString().ToLower());
        }

        private void chkAutoDeboard_Click(object sender, RoutedEventArgs e)
        {
            serviceModel.SetSetting("autoDeboarding", chkAutoDeboard.IsChecked.ToString().ToLower());
        }

        private void txtOperatorDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtOperatorDelay.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("operatorDelay", Convert.ToString(txtOperatorDelay.Text, CultureInfo.InvariantCulture));
        }

        private void txtRefuelRate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtRefuelRate.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("refuelRateKGS", Convert.ToString(txtRefuelRate.Text, CultureInfo.InvariantCulture));
        }
    }
}
