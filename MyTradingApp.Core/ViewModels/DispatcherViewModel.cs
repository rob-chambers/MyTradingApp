using GalaSoft.MvvmLight;
using MyTradingApp.Core.Utils;
using System;

namespace MyTradingApp.Core.ViewModels
{
    public abstract class DispatcherViewModel : ObservableObject
    {
        protected DispatcherViewModel(IDispatcherHelper dispatcherHelper)
        {
            DispatcherHelper = dispatcherHelper;
        }

        protected DispatcherViewModel(IDispatcherHelper dispatcherHelper, IQueueProcessor queueProcessor)
            : this(dispatcherHelper)
        {
            QueueProcessor = queueProcessor;
        }

        public IDispatcherHelper DispatcherHelper { get; }

        public IQueueProcessor QueueProcessor { get; }

        public void DispatchOnUi(Action action)
        {
            DispatcherHelper.InvokeOnUiThread(action);
        }
    }
}
