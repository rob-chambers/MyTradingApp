using MyTradingApp.Core.Utils;
using System;
using System.Windows;

namespace MyTradingApp.Desktop.Utils
{
    internal class DispatcherHelper : IDispatcherHelper
    {
        public void InvokeOnUiThread(Action action)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }
    }
}
