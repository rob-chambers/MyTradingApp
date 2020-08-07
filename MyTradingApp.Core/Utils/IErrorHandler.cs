using System;

namespace MyTradingApp.Core.Utils
{
    public interface IErrorHandler
    {
        void HandleError(Exception ex);
    }
}
