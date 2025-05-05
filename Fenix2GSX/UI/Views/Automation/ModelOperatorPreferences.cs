using CFIT.AppFramework.UI.ViewModels;
using System.Collections.Generic;

namespace Fenix2GSX.UI.Views.Automation
{
    public partial class ModelOperatorPreferences(ModelAutomation modelAutomation) : ViewModelCollection<string, string>(modelAutomation.Source.OperatorPreferences, (s) => s, (s) => !string.IsNullOrWhiteSpace(s))
    {
        protected virtual ModelAutomation ModelAutomation { get; } = modelAutomation;
        public override ICollection<string> Source => ModelAutomation.Source.OperatorPreferences;
        public virtual List<string> OperatorList => Source as List<string>;

        public override bool UpdateSource(string oldItem, string newItem)
        {
            try
            {
                int index = OperatorList.IndexOf(oldItem);
                if (IsUpdateAllowed(oldItem, newItem) && index >= 0)
                {
                    OperatorList[index] = newItem;
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
            if (fromIndex < 0 || fromIndex >= OperatorList.Count || toIndex < 0 || toIndex >= OperatorList.Count)
                return;

            string temp = OperatorList[toIndex];
            OperatorList[toIndex] = OperatorList[fromIndex];
            OperatorList[fromIndex] = temp;
            NotifyCollectionChanged();
        }
    }
}
