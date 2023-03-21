using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using System.Windows;

namespace Fenix2GSX
{
    public partial class NotifyIconViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowWindowCommand))]
        public bool canExecuteShowWindow = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(HideWindowCommand))]
        public bool canExecuteHideWindow;

        [RelayCommand(CanExecute = nameof(CanExecuteShowWindow))]
        public void ShowWindow()
        {
            Application.Current.MainWindow.Show(disableEfficiencyMode: true);
            CanExecuteShowWindow = false;
            CanExecuteHideWindow = true;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteHideWindow))]
        public void HideWindow()
        {
            Application.Current.MainWindow.Hide(enableEfficiencyMode: true);
            CanExecuteShowWindow = true;
            CanExecuteHideWindow = false;
        }

        [RelayCommand]
        public void ExitApplication()
        {
            Application.Current.Shutdown();
        }
    }
}
