namespace MyTradingApp.Stops
{
    public class Position
    {
        public double EntryPrice { get; set; }

        public TradeDirection Direction { get; set; }

        public ExitStrategy ExitStrategy { get; set; }
    }
}