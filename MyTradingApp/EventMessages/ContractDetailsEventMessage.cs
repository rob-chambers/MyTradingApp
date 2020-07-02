using IBApi;

namespace MyTradingApp.EventMessages
{
    internal class ContractDetailsEventMessage
    {
        public ContractDetailsEventMessage(ContractDetails details)
        {
            Details = details;
        }

        public ContractDetails Details { get; }
    }
}
