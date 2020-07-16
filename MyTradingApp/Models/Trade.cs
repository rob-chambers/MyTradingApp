using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyTradingApp.Models
{
    public class Trade
    {
        public Trade()
        {
            Stops = new HashSet<StopLoss>();
        }

        public int Id { get; set; }

        [Column(TypeName = "varchar")]
        [MaxLength(6)]
        public string Symbol { get; set; }

        public DateTime EntryTimeStamp { get; set; }

        public double EntryPrice { get; set; }

        public Direction Direction { get; set; }

        public ushort Quantity { get; set; }

        public DateTime? ExitTimeStamp { get; set; }

        public double? ExitPrice { get; set; }

        public double? ProfitLoss { get; set; }

        public virtual ICollection<StopLoss> Stops { get; set; }
    }
}
