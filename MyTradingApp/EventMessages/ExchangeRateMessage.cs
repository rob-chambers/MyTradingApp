namespace MyTradingApp.EventMessages
{
    public class ExchangeRateMessage
    {
        public ExchangeRateMessage(double price)
        {
            Price = price;
        }

        public double Price { get; }
    }
}