namespace MyTradingApp.EventMessages
{
    public class TickPrice : SymbolMessage
    {
        public TickPrice(string symbol, int type, double price)
            : base(symbol)
        {
            Symbol = symbol;
            Type = type;
            Price = price;
        }

        public int Type { get; }

        public double Price { get; }
    }
}