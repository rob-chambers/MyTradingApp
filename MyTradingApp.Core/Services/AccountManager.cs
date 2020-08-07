using AutoFinance.Broker.InteractiveBrokers.Controllers;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class AccountManager : IAccountManager
    {
        // TODO: Move this to appsettings
        private const string AccountId = "DU1070034";

        public static class AccountSummaryTags
        {
            public const string BuyingPower = "BuyingPower";
            public const string NetLiquidation = "NetLiquidation-S";
        }

        private readonly ITwsObjectFactory _twsObjectFactory;

        public AccountManager(ITwsObjectFactory twsObjectFactory)
        {
            _twsObjectFactory = twsObjectFactory;
        }

        public async Task<AccountSummaryCompletedMessage> RequestAccountSummaryAsync()
        {
            var details = await _twsObjectFactory.TwsControllerBase.GetAccountDetailsAsync(AccountId);

            var message = new AccountSummaryCompletedMessage();
            if (details.ContainsKey(AccountSummaryTags.NetLiquidation))
            {
                message.NetLiquidation = double.Parse(details[AccountSummaryTags.NetLiquidation]);
            }

            if (details.ContainsKey(AccountSummaryTags.BuyingPower))
            {
                message.BuyingPower = double.Parse(details[AccountSummaryTags.BuyingPower]);
            }

            message.AccountId = AccountId;

            return message;
        }

        public async Task<IEnumerable<PositionItem>> RequestPositionsAsync()
        {
            var positions = await _twsObjectFactory.TwsControllerBase.RequestPositions();
            var items = new List<PositionItem>();
            foreach (var item in positions)
            {
                items.Add(new PositionItem
                {
                    Contract = item.Contract,
                    AvgPrice = item.AverageCost,
                    Quantity = item.Position,
                    Symbol = new Symbol
                    {
                        Code = item.Contract.Symbol
                    }
                });
            }

            return items;
        }
    }
}