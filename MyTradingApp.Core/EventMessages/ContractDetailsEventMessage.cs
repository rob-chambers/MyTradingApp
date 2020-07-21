using IBApi;

namespace MyTradingApp.EventMessages
{
    public class ContractDetailsEventMessage
    {
        public ContractDetailsEventMessage(ContractDetails details)
        {
            Details = details;
        }

        public ContractDetails Details { get; }
    }
}
