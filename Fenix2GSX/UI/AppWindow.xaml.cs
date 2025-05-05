using CFIT.AppTools;
using Fenix2GSX.UI.Views.Audio;
using Fenix2GSX.UI.Views.Automation;
using Fenix2GSX.UI.Views.Monitor;
using Fenix2GSX.UI.Views.Profiles;
using Fenix2GSX.UI.Views.Settings;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Fenix2GSX.UI
{
    public interface IView
    {
        public void Start();
        public void Stop();
    }

    public partial class AppWindow : Window
    {
        public static UiIconLoader IconLoader { get; } = new(Assembly.GetExecutingAssembly(), IconLoadSource.Embedded, "Fenix2GSX.UI.Icons.");
        protected virtual Button CurrentButton { get; set; } = null;
        protected virtual IView CurrentView { get; set; } = null;
        protected static SolidColorBrush BrushDefault { get; } = SystemColors.WindowFrameBrush;
        protected static SolidColorBrush BrushHighlight { get; } = SystemColors.HighlightBrush;
        protected static Thickness ThicknessDefault { get; } = new(1);
        protected static Thickness ThicknessHighlight { get; } = new(1.5);

        protected virtual IView ViewMonitor { get; } = new ViewMonitor();
        protected virtual IView ViewAutomation { get; } = new ViewAutomation();
        protected virtual IView ViewProfiles { get; } = new ViewProfiles();
        protected virtual IView ViewAudio { get; } = new ViewAudio();
        protected virtual IView ViewSettings { get; } = new ViewSettings();

        public AppWindow()
        {
            InitializeComponent();
            this.Loaded += OnWindowLoaded;
            this.IsVisibleChanged += OnVisibleChanged;

            ButtonMonitor.Click += (_, _) => SetView(ButtonMonitor, ViewMonitor);
            ButtonAutomation.Click += (_, _) => SetView(ButtonAutomation, ViewAutomation);
            ButtonProfiles.Click += (_, _) => SetView(ButtonProfiles, ViewProfiles);
            ButtonAudio.Click += (_, _) => SetView(ButtonAudio, ViewAudio);
            ButtonSettings.Click += (_, _) => SetView(ButtonSettings, ViewSettings);

            if (Fenix2GSX.Instance.UpdateDetected)
            {
                if (Fenix2GSX.Instance.UpdateIsDev)
                    LabelVersionCheck.Inlines.Add("New Develop Version ");
                else
                    LabelVersionCheck.Inlines.Add("New Stable Version ");
                var run = new Run($"{Fenix2GSX.Instance.UpdateVersion}");

                Hyperlink hyperlink;
                if (Fenix2GSX.Instance.UpdateIsDev)
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/Fragtality/Fenix2GSX/blob/master/Fenix2GSX-Installer-latest.exe")
                    };
                else
                    hyperlink = new Hyperlink(run)
                    {
                        NavigateUri = new Uri("https://github.com/Fragtality/Fenix2GSX/releases/latest")
                    };
                LabelVersionCheck.Inlines.Add(hyperlink);
                LabelVersionCheck.Inlines.Add(" available!");
                this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
                PanelVersion.Visibility = Visibility.Visible;
            }
        }

        protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (CurrentButton == null)
                SetView(ButtonAutomation, ViewAutomation);
        }

        protected virtual void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility != Visibility.Visible)
                CurrentView?.Stop();
            else
                CurrentView?.Start();
        }

        protected virtual void SetView(Button menuButton, IView viewControl)
        {
            CurrentView?.Stop();

            if (CurrentButton != null)
            {
                CurrentButton.IsHitTestVisible = true;
                CurrentButton.BorderBrush = BrushDefault;
                CurrentButton.BorderThickness = ThicknessDefault;
            }

            if (CurrentView != null)
                ViewControl.SizeChanged -= OnViewSizeChanged;

            CurrentButton = menuButton;
            CurrentButton.IsHitTestVisible = false;
            CurrentButton.BorderBrush = BrushHighlight;
            CurrentButton.BorderThickness = ThicknessHighlight;

            ViewControl.Content = viewControl;
            CurrentView = viewControl;
            ViewControl.SizeChanged += OnViewSizeChanged;
            viewControl.Start();
            InvalidateArrange();
            InvalidateMeasure();
            InvalidateVisual();
        }

        protected virtual void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                double height = Math.Max(ViewControl.ActualHeight + 96, 0);
                this.MinHeight = height;
                this.Height = height;
            }
            catch { }
        }
    }
}
