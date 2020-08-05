using MyTradingApp.Core.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MyTradingApp.Utils
{
    public class ObservableCollectionNoReset<T> : ObservableCollection<T>
    {
        private readonly IDispatcherHelper _dispatcherHelper;

        // Some CollectionChanged listeners don't support range actions.
        public bool RangeActionsSupported { get; }

        public ObservableCollectionNoReset(bool rangeActionsSupported = false, IDispatcherHelper dispatcherHelper = null)
        {
            RangeActionsSupported = rangeActionsSupported;
            _dispatcherHelper = dispatcherHelper;
        }

        protected override void ClearItems()
        {
            if (RangeActionsSupported)
            {
                var removed = new List<T>(this);
                Action action = () =>
                {
                    base.ClearItems();
                    base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
                };

                if (_dispatcherHelper != null)
                {
                    _dispatcherHelper.InvokeOnUiThread(() => action.Invoke());
                    return;
                }

                action.Invoke();
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
                if (_dispatcherHelper != null)
                {
                    _dispatcherHelper.InvokeOnUiThread(() => base.OnCollectionChanged(e));
                    return;
                }

                base.OnCollectionChanged(e);
            }
        }
    }
}
