using System.Diagnostics;

namespace MyTradingApp.Core.Utils
{
    public static class DebuggerHelper
    {
        public static void BreakIfAttached()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
}
