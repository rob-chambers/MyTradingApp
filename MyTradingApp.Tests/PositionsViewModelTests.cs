﻿using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
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
            return new PositionsViewModel(manager, _accountManager, _positionsManager, _contractManager);
        }

        [Fact]
        public void WhenPositionsReturnedFromRequestThenPositionsAddedToCollection()
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
            vm.GetPositions();

            // Assert
            Assert.Single(vm.Positions);
            var position = vm.Positions[0];
            Assert.Equal(Symbol, position.Symbol.Code);
            Assert.Equal(11, position.AvgPrice);
            Assert.Equal(100, position.Quantity);
        }

        [Fact]
        public void GettingPositionsClearsExistingCollection()
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
            vm.GetPositions();

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

        [Fact]
        public async Task AfterRetrievingPositionsStreamLatestPrice()
        {
            // Arrange
            var manager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(manager);
            var position1 = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = Symbol,
                    Exchange = Exchange.NYSE.ToString()
                },
                Symbol = new Symbol { Code = Symbol },
                Quantity = 100
            };

            var position2 = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = "AMZN",
                    Exchange = Exchange.Nasdaq.ToString()
                },
                Symbol = new Symbol { Code = "AMZN" },
                Quantity = 200
            };

            var closedPosition = new PositionItem
            {
                Contract = new Contract
                {
                    Symbol = "TSLA",
                    Exchange = Exchange.NYSE.ToString()
                },
                Symbol = new Symbol { Code = "TSLA" },
                Quantity = 0
            };

            var positions = new List<PositionItem>
            {
                position1,
                position2,
                closedPosition
            };
            _accountManager.RequestPositionsAsync().Returns(positions);

            // Act
            vm.GetPositions();

            // Assert
            await manager.Received().RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == Symbol && 
                x.Currency == BrokerConstants.UsCurrency &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == Exchange.NYSE.ToString() &&
                x.SecType == BrokerConstants.Stock));
            
            await manager.Received().RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == position2.Symbol.Code &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == BrokerConstants.Routers.Island &&
                x.SecType == BrokerConstants.Stock));

            await manager.DidNotReceive().RequestStreamingPriceAsync(Arg.Is<Contract>(x => x.Symbol == closedPosition.Symbol.Code));
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
        public void RequestingPositionsGetAssociatedOrders()
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
            vm.GetPositions();

            // Assert
            _positionsManager.Received().RequestOpenOrdersAsync();
        }

        [Fact]
        public void RequestingPositionsGetsCompanyNames()
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

            _contractManager.RequestDetailsAsync(Arg.Any<Contract>()).Returns(new List<ContractDetails> { detail }  );

            // Act
            vm.GetPositions();

            // Assert
            _contractManager.Received().RequestDetailsAsync(Arg.Is<Contract>(x => x.Symbol == position.Contract.Symbol &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == position.Contract.Exchange &&
                x.SecType == BrokerConstants.Stock &&
                x.Currency == BrokerConstants.UsCurrency));

            Assert.Equal(CompanyName, position.Symbol.Name);
            Assert.Equal(detail, position.ContractDetails);
        }
    }
}
