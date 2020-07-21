using MyTradingApp.Domain;

namespace MyTradingApp.Stops
{
    public class Position
    {
        public double EntryPrice { get; set; }

        public Direction Direction { get; set; }

        public ExitStrategy ExitStrategy { get; set; }
    }
}