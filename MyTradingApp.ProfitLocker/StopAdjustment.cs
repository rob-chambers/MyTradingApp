using MyTradingApp.ProfitLocker.StopTypes;

namespace MyTradingApp.ProfitLocker
{
    public class StopAdjustment
    {
        public bool SubmitOrder { get; set; }

        public string OrderType { get; set; }

        public StopValue StopPrice { get; set; }
    }
}
