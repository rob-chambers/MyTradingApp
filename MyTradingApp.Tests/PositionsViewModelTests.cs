using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class PositionsViewModelTests
    {
        private const string Symbol = "MSFT";

        private IAccountManager _accountManager;
        private IPositionManager _positionsManager;
        private IContractManager _contractManager;

        private PositionsViewModel GetVm(IMarketDataManager marketDataManager = null)
        {
            var manager = marketDataManager ?? Substitute.For<IMarketDataManager>();
            _accountManager = Substitute.For<IAccountManager>();
            _positionsManager = Substitute.For<IPositionManager>();
            _contractManager = Substitute.For<IContractManager>();
            var queueProcessor = Substitute.For<IQueueProcessor>();
            queueProcessor
                .When(x => x.Enqueue(Arg.Any<Action>()))
                .Do(x =>
                {
                    var action = x.Arg<Action>();
                    action.Invoke();
                });

            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            dispatcherHelper
                .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                .Do(x =>
                {
                    var action = x.Arg<Action>();
                    action.Invoke();
                });

            return new PositionsViewModel(dispatcherHelper, manager, _accountManager, _positionsManager, _contractManager, queueProcessor);
        }

        [Fact]
        public async Task WhenPositionsReturnedFromRequestThenPositionsAddedToCollection()
        {
            // Arrange
            var vm = GetVm();
            var positions = new List<PositionItem>
            {
                new PositionItem
                {
                    AvgPrice = 11,
                    Quantity = 100,
                    Symbol = new Symbol
                    {
                        Code = Symbol
                    }
                }
            };

            // Act
            _accountManager.RequestPositionsAsync().Returns(positions);
            await vm.GetPositionsAsync();

            // Assert
            Assert.Single(vm.Positions);
            var position = vm.Positions[0];
            Assert.Equal(Symbol, position.Symbol.Code);
            Assert.Equal(11, position.AvgPrice);
            Assert.Equal(100, position.Quantity);
        }

        [Fact]
        public async Task GettingPositionsClearsExistingCollectionAsync()
        {
            // Arrange
            var vm = GetVm();
            var position = new PositionItem
            {
                AvgPrice = 11,
                Quantity = 100,
                Symbol = new Symbol
                {
                    Code = Symbol
                }
            };

            vm.Positions.Add(position);
            _accountManager.RequestPositionsAsync().Returns(new List<PositionItem>());

            // Act
            await vm.GetPositionsAsync();

            // Assert
            Assert.Empty(vm.Positions);
        }

        [Fact]
        public void TickMessageUpdatesLatestPrice()
        {
            const double Price = 10.11;

            var vm = GetVm();
            var position = new PositionItem { Symbol = new Symbol { Code = Symbol }, Quantity = 100 };
            vm.Positions.Add(position);

            // Act
            Messenger.Default.Send(new BarPriceMessage(Symbol, new Domain.Bar
            {
                Close = Price
            }));

            // Assert
            Assert.Equal(Price, position.Symbol.LatestPrice);
        }

        [Fact]
        public void TickMessageForLongPositionUpdatesProfitLossCorrectly()
        {
            const double Price = 11;
            const double EntryPrice = 10;

            var vm = GetVm();
            var position = new PositionItem 
            { 
                Symbol = new Symbol { Code = Symbol }, 
                Quantity = 100,
                AvgPrice = EntryPrice 
            };
            vm.Positions.Add(position);

            // Act
            Messenger.Default.Send(new BarPriceMessage(Symbol, new Domain.Bar
            {
                Close = Price
            }));

            // Assert
            Assert.Equal(100, position.ProfitLoss);
            Assert.Equal(10, position.PercentageGainLoss);
        }

        [Fact]
        public void TickMessageForShortPositionUpdatesProfitLossCorrectly()
        {
            const double Price = 9;
            const double EntryPrice = 10;

            var vm = GetVm();
            var position = new PositionItem
            {
                Symbol = new Symbol { Code = Symbol },
                Quantity = -100,
                AvgPrice = EntryPrice
            };
            vm.Positions.Add(position);

            // Act
            Messenger.Default.Send(new BarPriceMessage(Symbol, new Domain.Bar
            {
                Close = Price
            }));

            // Assert
            Assert.Equal(100, position.ProfitLoss);
            Assert.Equal(10, position.PercentageGainLoss);
        }

        [Theory]
        [InlineData(Symbol, "NYSE", "NYSE")]
        [InlineData("AMZN", "Nasdaq", BrokerConstants.Routers.Island)]
        [InlineData("NKLA", BrokerConstants.Routers.Smart, null)]
        [InlineData("ABCD", null, null)]
        public async Task AfterRetrievingPositionsStreamLatestPrice(string symbol, string exchange, string expectedPrimaryExchange)
        {
            // Arrange
            var manager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(manager);
            var position = GetPositionItem(symbol, exchange);
            var closedPosition = GetPositionItem("TSLA", Exchange.NYSE.ToString(), 0);

            var positions = new List<PositionItem>
            {
                position,
                closedPosition
            };
            _accountManager.RequestPositionsAsync().Returns(positions);

            // Act
            await vm.GetPositionsAsync();

            // Assert
            Received.InOrder(async () => await manager.RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == symbol &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == expectedPrimaryExchange &&
                x.SecType == BrokerConstants.Stock)));

            await manager.DidNotReceive().RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == closedPosition.Symbol.Code));
        }

        private static PositionItem GetPositionItem(string symbol, string exchange, int quantity = 1)
        {
            return new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = symbol,
                    Exchange = exchange
                },
                Symbol = new Symbol { Code = symbol },
                Quantity = quantity
            };
        }

        [Fact]
        public void BeforeDisconnectingStopStreaming()
        {
            // Arrange
            const string ClosedSymbol = "AMZN";

            var manager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(manager);
            var openPosition = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = Symbol,
                },
                Symbol = new Symbol { Code = Symbol },
                Quantity = 100
            };

            var closedPosition = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = ClosedSymbol,
                },
                Symbol = new Symbol { Code = ClosedSymbol },
            };

            vm.Positions.Add(openPosition);
            vm.Positions.Add(closedPosition);

            // Act
            Messenger.Default.Send(new ConnectionChangingMessage(false));

            // Assert
            manager.Received().StopPriceStreaming(Symbol);
            manager.DidNotReceive().StopPriceStreaming(ClosedSymbol);
        }

        [Fact]
        public async Task RequestingPositionsGetAssociatedOrdersAsync()
        {
            // Arrange
            var vm = GetVm();
            var position = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = Symbol,
                    Exchange = Exchange.NYSE.ToString()
                },
                Symbol = new Symbol { Code = Symbol },
                Quantity = 100
            };

            var positions = new List<PositionItem> { position };
            _accountManager.RequestPositionsAsync().Returns(positions);

            // Act
            await vm.GetPositionsAsync();

            // Assert
            await _positionsManager.Received().RequestOpenOrdersAsync();
        }

        [Fact]
        public async Task RequestingPositionsGetsCompanyNamesAsync()
        {
            // Arrange
            const string CompanyName = "Microsoft";

            var vm = GetVm();
            var position = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = Symbol,
                    Exchange = Exchange.NYSE.ToString()
                },
                Symbol = new Symbol { Code = Symbol },
                Quantity = 100
            };

            var positions = new List<PositionItem> { position };
            _accountManager.RequestPositionsAsync().Returns(positions);

            var detail = new ContractDetails
            {
                Contract = position.Contract,
                LongName = CompanyName
            };

            _contractManager.RequestDetailsAsync(Arg.Any<Contract>()).Returns(new List<ContractDetails> { detail });

            // Act
            await vm.GetPositionsAsync();

            // Assert
            await _contractManager.Received().RequestDetailsAsync(Arg.Is<Contract>(x => x.Symbol == position.Contract.Symbol &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == position.Contract.Exchange &&
                x.SecType == BrokerConstants.Stock &&
                x.Currency == BrokerConstants.UsCurrency));

            Assert.Equal(CompanyName, position.Symbol.Name);
            Assert.Equal(detail, position.ContractDetails);
        }
    }
}
