namespace MyTradingApp.EventMessages
{
    public class TickPrice
    {
        public TickPrice(string symbol, double price)
        {
            Symbol = symbol;
            Price = price;
        }

        public string Symbol { get; }
        public double Price { get; }
    }
}