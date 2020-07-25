using GalaSoft.MvvmLight;
using MyTradingApp.Core.Utils;

namespace MyTradingApp.Core.ViewModels
{
    public abstract class DispatcherViewModel : ObservableObject
    {
        protected DispatcherViewModel(IDispatcherHelper dispatcherHelper)
        {
            DispatcherHelper = dispatcherHelper;
        }

        public IDispatcherHelper DispatcherHelper { get; }
    }
}
