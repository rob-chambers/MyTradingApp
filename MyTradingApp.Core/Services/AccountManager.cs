using AutoFinance.Broker.InteractiveBrokers.Controllers;
using Microsoft.Extensions.Configuration;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
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
            _accountId = configuration.GetValue<string>(Settings.AccountId);
        }

        public async Task<AccountSummary> RequestAccountSummaryAsync()
        {
            var details = await _twsObjectFactory.TwsControllerBase.GetAccountDetailsAsync(_accountId);

            var summary = new AccountSummary();
            if (details.ContainsKey(AccountSummaryTags.NetLiquidation))
            {
                summary.NetLiquidation = double.Parse(details[AccountSummaryTags.NetLiquidation]);
            }

            if (details.ContainsKey(AccountSummaryTags.BuyingPower))
            {
                summary.BuyingPower = double.Parse(details[AccountSummaryTags.BuyingPower]);
            }

            summary.AccountId = _accountId;

            return summary;
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