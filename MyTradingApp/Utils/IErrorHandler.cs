using System;

namespace MyTradingApp.Utils
{
    internal interface IErrorHandler
    {
        void HandleError(Exception ex);
    }
}
