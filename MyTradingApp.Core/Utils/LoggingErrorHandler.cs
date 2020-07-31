using MyTradingApp.Utils;
using Serilog;
using System;

namespace MyTradingApp.Core.Utils
{
    internal class LoggingErrorHandler : IErrorHandler
    {
        public void HandleError(Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }
}
