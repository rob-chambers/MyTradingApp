using MyTradingApp.Stops;
using System;

namespace MyTradingApp.Models
{
    public class StopLoss
    {
        public int Id { get; set; }

        public int TradeId { get; set; }

        public virtual Trade Trade { get; set; }

        public DateTime TimeStamp { get; set; }

        public double Price { get; set; }

        public double StopPrice { get; set; }

        public StopType StopType { get; set; }
    }
}
