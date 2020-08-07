using AutoFinance.Broker.InteractiveBrokers.Controllers;
using Microsoft.Extensions.Configuration;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public class AccountManager : IAccountManager
    {
        public static class AccountSummaryTags
        {
            public const string BuyingPower = "BuyingPower";
            public const string NetLiquidation = "NetLiquidation-S";
        }

        private readonly ITwsObjectFactory _twsObjectFactory;
        private readonly string _accountId;

        public AccountManager(ITwsObjectFactory twsObjectFactory, IConfiguration configuration)
        {
            _twsObjectFactory = twsObjectFactory;
            _accountId = configuration.GetValue<string>("AccountId");
        }

        public async Task<AccountSummaryCompletedMessage> RequestAccountSummaryAsync()
        {
            var details = await _twsObjectFactory.TwsControllerBase.GetAccountDetailsAsync(_accountId);

            var message = new AccountSummaryCompletedMessage();
            if (details.ContainsKey(AccountSummaryTags.NetLiquidation))
            {
                message.NetLiquidation = double.Parse(details[AccountSummaryTags.NetLiquidation]);
            }

            if (details.ContainsKey(AccountSummaryTags.BuyingPower))
            {
                message.BuyingPower = double.Parse(details[AccountSummaryTags.BuyingPower]);
            }

            message.AccountId = _accountId;

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