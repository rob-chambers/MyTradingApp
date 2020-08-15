using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
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

        private PositionsViewModel GetVm(
            IMarketDataManager marketDataManager = null,
            IPositionManager positionManager = null,
            IContractManager contractManager = null,
            ITradeRecordingService tradeRecordingService = null)
        {
            var manager = marketDataManager ?? Substitute.For<IMarketDataManager>();
            _accountManager = Substitute.For<IAccountManager>();

            if (positionManager == null)
            {
                positionManager = Substitute.For<IPositionManager>();
            }

            if (contractManager == null)
            {
                contractManager = Substitute.For<IContractManager>();
            }

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

            if (tradeRecordingService == null)
            {
                tradeRecordingService = Substitute.For<ITradeRecordingService>();
            }
            
            return new PositionsViewModel(dispatcherHelper, manager, _accountManager, positionManager, contractManager, queueProcessor, tradeRecordingService);
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
            manager.Received().StopActivePriceStreaming(Arg.Any<IEnumerable<int>>());
        }

        [Fact]
        public async Task RequestingPositionsGetAssociatedOrdersAsync()
        {
            // Arrange
            var positionManager = Substitute.For<IPositionManager>();
            var vm = GetVm(positionManager: positionManager);
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
            await positionManager.Received().RequestOpenOrdersAsync();
        }

        [Fact]
        public async Task RequestingPositionsGetsCompanyNamesAsync()
        {
            // Arrange
            const string CompanyName = "Microsoft";

            var contractManager = Substitute.For<IContractManager>();
            var vm = GetVm(contractManager: contractManager);
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

            contractManager.RequestDetailsAsync(Arg.Any<Contract>()).Returns(new List<ContractDetails> { detail });

            // Act
            await vm.GetPositionsAsync();

            // Assert
            await contractManager.Received().RequestDetailsAsync(Arg.Is<Contract>(x => x.Symbol == position.Contract.Symbol &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == position.Contract.Exchange &&
                x.SecType == BrokerConstants.Stock &&
                x.Currency == BrokerConstants.UsCurrency));

            Assert.Equal(CompanyName, position.Symbol.Name);
            Assert.Equal(detail, position.ContractDetails);
        }

        [Fact]
        public async Task WhenPositionReturnedFromRequestThenPositionAddedToCollection()
        {
            // Arrange
            var item = new PositionItem
            {
                AvgPrice = 11,
                Quantity = 100,
                Symbol = new Symbol
                {
                    Code = Symbol
                }
            };
            var vm = GetPositionForSymbolTest(item);

            // Act
            await vm.GetPositionForSymbolAsync(Symbol);

            // Assert
            Assert.Single(vm.Positions);
            var position = vm.Positions[0];
            Assert.Equal(Symbol, position.Symbol.Code);
            Assert.Equal(11, position.AvgPrice);
            Assert.Equal(100, position.Quantity);
        }

        [Fact]
        public async Task GetPositionForSymbolStreamsPrice()
        {
            // Arrange
            var item = new PositionItem
            {
                Quantity = 1000,
                Symbol = new Symbol { Code = Symbol },
                Contract = new Contract { Symbol = Symbol }
            };

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetPositionForSymbolTest(item, marketDataManager);

            // Act
            await vm.GetPositionForSymbolAsync(Symbol);

            // Assert
            await marketDataManager.Received().RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == Symbol));
        }

        [Fact]
        public async Task ClosedPositionDoesNotStreamPrice()
        {
            // Arrange
            var item = new PositionItem
            {
                Quantity = 0,
                Symbol = new Symbol { Code = Symbol },
                Contract = new Contract { Symbol = Symbol }
            };

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetPositionForSymbolTest(item, marketDataManager);

            // Act
            await vm.GetPositionForSymbolAsync(Symbol);

            // Assert
            await marketDataManager.DidNotReceive().RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == Symbol));
        }

        [Fact]
        public async Task GetPositionForSymbolAssignsContractAndOrderToPosition()
        {
            // Arrange
            var item = new PositionItem
            {
                Quantity = 1000,
                Symbol = new Symbol { Code = Symbol },
                Contract = new Contract { Symbol = Symbol }
            };

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var positionManager = Substitute.For<IPositionManager>();
            var order = new Order
            {
                OrderType = BrokerConstants.OrderTypes.Stop
            };
            var contract = new Contract { Symbol = Symbol };
            var orders = new List<OpenOrderEventArgs>
            {
                new OpenOrderEventArgs(1, contract, order, new OrderState())
            };
            positionManager.RequestOpenOrdersAsync().Returns(orders);

            var vm = GetPositionForSymbolTest(item, marketDataManager, positionManager);

            // Act
            await vm.GetPositionForSymbolAsync(Symbol);

            // Assert
            Assert.Equal(order, vm.Positions[0].Order);
            Assert.Equal(contract, vm.Positions[0].Contract);
        }

        [Fact]
        public async Task GetPositionForSymbolGetsCompanyName()
        {
            // Arrange
            const string CompanyName = "Microsoft";

            var item = new PositionItem
            {
                Quantity = 1000,
                Symbol = new Symbol { Code = Symbol },
                Contract = new Contract { Symbol = Symbol }
            };

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var positionManager = Substitute.For<IPositionManager>();
            var contractManager = Substitute.For<IContractManager>();

            var order = new Order
            {
                OrderType = BrokerConstants.OrderTypes.Stop
            };
            var contract = new Contract { Symbol = Symbol };

            var contractDetails = new ContractDetails()
            {
                LongName = CompanyName,
                Contract = contract,
            };
            contractManager.RequestDetailsAsync(Arg.Is<Contract>(x => x.Symbol == Symbol)).Returns(new List<ContractDetails>
            {
                contractDetails
            });

            var orders = new List<OpenOrderEventArgs>
            {
                new OpenOrderEventArgs(1, contract, order, new OrderState())
            };
            
            positionManager.RequestOpenOrdersAsync().Returns(orders);

            var vm = GetPositionForSymbolTest(item, marketDataManager, positionManager, contractManager);

            // Act
            await vm.GetPositionForSymbolAsync(Symbol);

            // Assert
            Assert.Equal(CompanyName, vm.Positions[0].Symbol.Name);
            Assert.Equal(contractDetails, vm.Positions[0].ContractDetails);
        }

        [Fact]
        public async Task WhenPositionFilledRecordTradeExitAsync()
        {
            // Arrange
            const double FillPrice = 10;
            const ushort Quantity = 1000;

            var tradeRecordingService = Substitute.For<ITradeRecordingService>();
            var position = new PositionItem
            {
                Quantity = Quantity,
                Symbol = new Symbol
                {
                    Code = Symbol
                }
            };
            var vm = GetVm(tradeRecordingService: tradeRecordingService);
            vm.Positions.Add(position);

            // Act
            var msg = new OrderStatusEventArgs(1, BrokerConstants.OrderStatus.Filled, Quantity, 0, FillPrice, 0, 0, FillPrice, 0, null);
            var message = new OrderStatusChangedMessage(Symbol, msg);
            Messenger.Default.Send(message, OrderStatusChangedMessage.Tokens.Positions);

            // Assert
            await tradeRecordingService.Received().ExitTradeAsync(
                Arg.Is<PositionItem>(x => x == position),
                Arg.Is<OrderStatusChangedMessage>(x => x == message));
        }

        [Fact]
        public async Task WhenOrderStatusChangedAndNotFilledDoNotRecordTradeExitAsync()
        {
            // Arrange
            const double FillPrice = 10;
            const ushort Quantity = 1000;

            var tradeRecordingService = Substitute.For<ITradeRecordingService>();
            GetVm(tradeRecordingService: tradeRecordingService);

            // Act
            var msg = new OrderStatusEventArgs(1, BrokerConstants.OrderStatus.Cancelled, Quantity, 0, FillPrice, 0, 0, FillPrice, 0, null);
            var message = new OrderStatusChangedMessage(Symbol, msg);
            Messenger.Default.Send(message, OrderStatusChangedMessage.Tokens.Positions);

            // Assert
            await tradeRecordingService.DidNotReceive().ExitTradeAsync(
                Arg.Any<PositionItem>(),
                Arg.Any<OrderStatusChangedMessage>());
        }

        private PositionsViewModel GetPositionForSymbolTest(
            PositionItem item,
            IMarketDataManager marketDataManager = null,
            IPositionManager positionManager = null,
            IContractManager contractManager = null)
        {
            var vm = GetVm(marketDataManager, positionManager, contractManager);
            var positions = new List<PositionItem>
            {
                item
            };

            // Act
            _accountManager.RequestPositionsAsync().Returns(positions);
            return vm;
        }
    }
}