using MyTradingApp.Messages;
using System.Collections.Generic;

namespace MyTradingApp.EventMessages
{
    public class OpenOrdersMessage
    {
        public OpenOrdersMessage(IEnumerable<OpenOrderMessage> orders)
        {
            Orders = orders;
        }

        public IEnumerable<OpenOrderMessage> Orders { get; }
    }
}
