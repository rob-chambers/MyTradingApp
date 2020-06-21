using MyTradingApp.Messages;
using System;

namespace MyTradingApp
{
    public class ManagedAccountsEventArgs : EventArgs
    {
        public ManagedAccountsEventArgs(ManagedAccountsMessage message)
        {
            Message = message;
        }

        public ManagedAccountsMessage Message { get; }
    }
}