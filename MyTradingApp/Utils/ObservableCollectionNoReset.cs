using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MyTradingApp.Utils
{
    public class ObservableCollectionNoReset<T> : ObservableCollection<T>
    {
        // Some CollectionChanged listeners don't support range actions.
        public bool RangeActionsSupported { get; }

        public ObservableCollectionNoReset(bool rangeActionsSupported = false)
        {
            RangeActionsSupported = rangeActionsSupported;
        }

        protected override void ClearItems()
        {
            if (RangeActionsSupported)
            {
                var removed = new List<T>(this);
                base.ClearItems();
                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
            }
            else
            {
                while (Count > 0)
                {
                    RemoveAt(Count - 1);
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
