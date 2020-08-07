using AutoFinance.Broker.InteractiveBrokers.Constants;
using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private IHistoricalDataManager _historicalDataManager;
        private IDispatcherHelper _dispatcherHelper;
        private List<ContractDetails> _contractDetails = new List<ContractDetails>();

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

        public IHistoricalDataManager HistoricalDataManager
        {
            get
            {                
                return _historicalDataManager ?? (_historicalDataManager = Substitute.For<IHistoricalDataManager>());
            }
        }

        public IDispatcherHelper DispatcherHelper
        {
            get
            {
                if (_dispatcherHelper == null)
                {
                    _dispatcherHelper = Substitute.For<IDispatcherHelper>();
                    _dispatcherHelper.When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                        .Do(x => 
                        {
                            var action = x.Arg<Action>();
                            action.Invoke();
                        });
                }

                return _dispatcherHelper;
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

        public OrdersViewModelBuilder ReturnsContractDetails(List<ContractDetails> details)
        {
            _contractDetails = details;
            return this;
        }
    }
}
