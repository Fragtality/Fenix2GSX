using System.Windows.Controls;

namespace Fenix2GSX.UI.Views.Monitor
{
    public partial class ViewMonitor : UserControl, IView
    {
        protected virtual ModelMonitor ViewModel { get; }

        public ViewMonitor()
        {
            InitializeComponent();
            ViewModel = new(AppService.Instance);
            this.DataContext = ViewModel;
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
