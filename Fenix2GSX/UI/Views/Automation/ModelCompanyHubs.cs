using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ModelCompanyHubs(ModelAutomation modelAutomation) : ViewModelCollection<string, string>(modelAutomation.Source.CompanyHubs, (s) => s, (s) => !string.IsNullOrWhiteSpace(s))
    {
        protected virtual ModelAutomation ModelAutomation { get; } = modelAutomation;
        public override ICollection<string> Source => ModelAutomation.Source.CompanyHubs;
        public virtual List<string> HubList => Source as List<string>;

        public override bool UpdateSource(string oldItem, string newItem)
        {
            try
            {
                int index = HubList.IndexOf(oldItem);
                if (IsUpdateAllowed(oldItem, newItem) && index >= 0)
                {
                    HubList[index] = newItem;
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public virtual void MoveItem(int fromIndex, int step)
        {
            int toIndex = fromIndex + step;
            if (fromIndex < 0 || fromIndex >= HubList.Count || toIndex < 0 || toIndex >= HubList.Count)
                return;

            string temp = HubList[toIndex];
            HubList[toIndex] = HubList[fromIndex];
            HubList[fromIndex] = temp;
            NotifyCollectionChanged();
        }
    }
}
