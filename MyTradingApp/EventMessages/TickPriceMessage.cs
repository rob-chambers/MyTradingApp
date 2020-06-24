namespace MyTradingApp.EventMessages
{
    public class TickPrice : SymbolMessage
    {
        public TickPrice(string symbol, double price)
            : base(symbol)
        {
            Symbol = symbol;
            Price = price;
        }

        public double Price { get; }
    }
}