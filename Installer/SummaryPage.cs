using CFIT.AppTools;
using CFIT.Installer.UI.Behavior;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Installer
{
    public class SummaryPage : PageSummary
    {
        protected override void SetFooter()
        {
            base.SetFooter();
            TextBlock label = new TextBlock()
            {
                MaxWidth = 364,
                FontWeight = FontWeights.DemiBold,
                TextWrapping = TextWrapping.Wrap,
            };
            label.Inlines.Add("Be sure to check out the ");
            Hyperlink hyperlink = new Hyperlink(new Run("README"))
            {
                NavigateUri = new Uri("https://github.com/Fragtality/Fenix2GSX/blob/master/README.md")
            };
            label.Inlines.Add(hyperlink);
            label.Inlines.Add(" to gain a basic Understanding and find Answers to the most common Questions ;)");

            this.PanelFooter.Children.Add(label);
            Window.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(Nav.RequestNavigateHandler));
        }
    }
}
