using System;

namespace MyTradingApp.Domain
{
    public class Exit
    {
        public int Id { get; set; }

        public int TradeId { get; set; }

        public virtual Trade Trade { get; set; }

        public DateTime TimeStamp { get; set; }

        public double Price { get; set; }

        public ushort Quantity { get; set; }
    }
}
