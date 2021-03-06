﻿using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
using System.Collections.Generic;
using Xunit;

namespace MyTradingApp.Tests
{
    public class PositionsViewModelTests
    {
        private const string Symbol = "MSFT";

        private PositionsViewModel GetVm(IMarketDataManager marketDataManager = null)
        {
            var manager = marketDataManager ?? Substitute.For<IMarketDataManager>();
            var accountManager = Substitute.For<IAccountManager>();
            var positionsManager = Substitute.For<IPositionManager>();
            var contractManager = Substitute.For<IContractManager>();
            return new PositionsViewModel(manager, accountManager, positionsManager, contractManager);
        }

        [Fact]
        public void WhenMessageReceivedPositionsAddedToCollection()
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
            Messenger.Default.Send(new ExistingPositionsMessage(positions));

            // Assert
            Assert.Single(vm.Positions);
            var position = vm.Positions[0];
            Assert.Equal(Symbol, position.Symbol.Code);
            Assert.Equal(11, position.AvgPrice);
            Assert.Equal(100, position.Quantity);
        }

        [Fact]
        public void NewMessageClearsExistingCollection()
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

            // Act
            Messenger.Default.Send(new ExistingPositionsMessage(new List<PositionItem>()));

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
        public void AfterRetrievingPositionsStreamLatestPrice()
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

            // Act
            Messenger.Default.Send(new ExistingPositionsMessage(new List<PositionItem> { position1, position2, closedPosition }));

            // Assert
            manager.Received().RequestStreamingPrice(Arg.Is<Contract>(x => x.Symbol == Symbol && 
                x.Currency == BrokerConstants.UsCurrency &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == Exchange.NYSE.ToString() &&
                x.SecType == BrokerConstants.Stock), true);
            
            manager.Received().RequestStreamingPrice(Arg.Is<Contract>(x => x.Symbol == position2.Symbol.Code &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.PrimaryExch == BrokerConstants.Routers.Island &&
                x.SecType == BrokerConstants.Stock), true);

            manager.DidNotReceive().RequestStreamingPrice(Arg.Is<Contract>(x => x.Symbol == closedPosition.Symbol.Code), true);
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
    }
}
