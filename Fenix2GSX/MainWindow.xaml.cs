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
            chkConnectPCA.IsChecked = serviceModel.ConnectPCA;
            chkPcaOnlyJetway.IsChecked = serviceModel.PcaOnlyJetways;
            chkAutoRefuel.IsChecked = serviceModel.AutoRefuel;
            chkCallCatering.IsChecked = serviceModel.CallCatering;
            chkAutoBoard.IsChecked = serviceModel.AutoBoarding;
            chkAutoDeboard.IsChecked = serviceModel.AutoDeboarding;
            txtRepositionDelay.Text = serviceModel.RepositionDelay.ToString(CultureInfo.InvariantCulture);
            txtRefuelRate.Text = serviceModel.RefuelRate.ToString(CultureInfo.InvariantCulture);
            if (serviceModel.RefuelUnit == "KGS")
            {
                unitKGS.IsChecked = true;
                unitLBS.IsChecked = false;
            }
            else
            {
                unitKGS.IsChecked = false;
                unitLBS.IsChecked = true;
            }
            txtVhf1VolumeApp.Text = serviceModel.Vhf1VolumeApp;
        }

        protected void UpdateLogArea()
        {
            while (Logger.MessageQueue.Count > 0)
            {
                
                if (lineCounter > 4)
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

            if (IPCManager.SimConnect != null && IPCManager.SimConnect.IsReady)
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

        private void txtRepositionDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtRepositionDelay.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("repositionDelay", Convert.ToString(txtRepositionDelay.Text, CultureInfo.InvariantCulture));
        }

        private void txtRefuelRate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(txtRefuelRate.Text, CultureInfo.InvariantCulture, out _))
                serviceModel.SetSetting("refuelRate", Convert.ToString(txtRefuelRate.Text, CultureInfo.InvariantCulture));
        }

        private void txtVhf1VolumeApp_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtVhf1VolumeApp.Text))
                serviceModel.SetSetting("vhf1VolumeApp", txtVhf1VolumeApp.Text);
        }

        private void units_Click(object sender, RoutedEventArgs e)
        {
            if (!float.TryParse(txtRefuelRate.Text, CultureInfo.InvariantCulture, out float fuelRate))
                return;

            if (sender == unitKGS && serviceModel.RefuelUnit == "LBS")
            {
                fuelRate /= FenixContoller.weightConversion;
                serviceModel.SetSetting("refuelRate", Convert.ToString(fuelRate, CultureInfo.InvariantCulture));
                serviceModel.SetSetting("refuelUnit", "KGS");
                txtRefuelRate.Text = Convert.ToString(fuelRate, CultureInfo.InvariantCulture);
            }
            else if (sender == unitLBS && serviceModel.RefuelUnit == "KGS")
            {
                fuelRate *= FenixContoller.weightConversion;
                serviceModel.SetSetting("refuelRate", Convert.ToString(fuelRate, CultureInfo.InvariantCulture));
                serviceModel.SetSetting("refuelUnit", "LBS");
                txtRefuelRate.Text = Convert.ToString(fuelRate, CultureInfo.InvariantCulture);
            }
        }
    }
}
