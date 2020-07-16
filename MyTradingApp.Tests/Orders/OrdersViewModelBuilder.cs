using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace MyTradingApp.Tests.Orders
{
    internal class OrdersViewModelBuilder
    {
        private readonly List<Tuple<OrderItem, bool>> _orderItems = new List<Tuple<OrderItem, bool>>();
        private IOrderCalculationService _orderCalculationService;
        private IContractManager _contractManager;
        private IMarketDataManager _marketDataManager;
        private IOrderManager _orderManager;
        private AccountSummaryCompletedMessage _accountSummaryCompletedMessage;
        private ITradeRepository _tradeRepository;

        public IOrderCalculationService OrderCalculationService
        {
            get
            {
                return _orderCalculationService ?? (_orderCalculationService = Substitute.For<IOrderCalculationService>());
            }
        }

        public IContractManager ContractManager
        {
            get
            {
                return _contractManager ?? (_contractManager = Substitute.For<IContractManager>());
            }
        }

        public IMarketDataManager MarketDataManager
        {
            get
            {
                return _marketDataManager ?? (_marketDataManager = Substitute.For<IMarketDataManager>());
            }
        }

        public IOrderManager OrderManager
        {
            get
            {
                return _orderManager ?? (_orderManager = Substitute.For<IOrderManager>());
            }
        }

        public ITradeRepository TradeRepository
        {
            get
            {
                return _tradeRepository ?? (_tradeRepository = Substitute.For<ITradeRepository>());
            }
        }

        public OrdersViewModelBuilder AddSingleOrder(string symbol, bool found)
        {
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol(symbol).Order;
            order.Symbol.Exchange = Exchange.NYSE;
            _orderItems.Add(new Tuple<OrderItem, bool>(order, found));

            return this;
        }

        public OrdersViewModelBuilder CompleteAccountSummary(AccountSummaryCompletedMessage message)
        {
            _accountSummaryCompletedMessage = message;
            return this;
        }

        public OrdersViewModel Build()
        {
            var historicalDataManager = Substitute.For<IHistoricalDataManager>();            
            var vm = new OrdersViewModel(ContractManager, MarketDataManager, historicalDataManager, OrderCalculationService, OrderManager, TradeRepository);

            foreach (var item in _orderItems)
            {
                vm.Orders.Add(item.Item1);
                if (item.Item2)
                {
                    // Requesting this item to be found
                    item.Item1.Symbol.IsFound = true;
                }
            }

            if (_accountSummaryCompletedMessage != null)
            {
                Messenger.Default.Send(_accountSummaryCompletedMessage);
            }

            return vm;
        }
    }
}
