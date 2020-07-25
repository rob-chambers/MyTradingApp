using System;

namespace MyTradingApp.Core.Utils
{
    public interface IDispatcherHelper
    {
        void InvokeOnUiThread(Action action);
    }
}