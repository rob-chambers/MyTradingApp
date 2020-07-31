using Serilog.Core;
using Serilog.Events;
using System.Threading;

namespace MyTradingApp.Desktop.Utils
{
    internal class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadID", Thread.CurrentThread.ManagedThreadId.ToString("D4")));
        }
    }
}
