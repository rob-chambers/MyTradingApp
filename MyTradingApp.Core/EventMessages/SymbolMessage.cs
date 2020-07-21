namespace MyTradingApp.EventMessages
{
    public abstract class SymbolMessage
    {
        protected SymbolMessage(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; set; }
    }
}