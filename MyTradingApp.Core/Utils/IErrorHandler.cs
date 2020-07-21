using System;

namespace MyTradingApp.Utils
{
    public interface IErrorHandler
    {
        void HandleError(Exception ex);
    }
}
