using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Persistence;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests.Integration
{
    public class TradeRecordingServiceTests : SqliteInMemoryTest
    {
        private const string Symbol = "MSFT";
        private const ushort DefaultQuantity = 1000;

        [Fact]
        public async Task WhenStopOrderFilledAndPositionClosedExitAddedAndTradeUpdated()
        {
            const double FillPrice = 11;

            using (Context)
            {
                // Arrange
                var service = await InitServiceAsync();
                var position = new PositionItem
                {
                    Symbol = new Symbol { Code = Symbol },
                    Quantity = DefaultQuantity
                };

                // Act
                await service.ExitTradeAsync(position, new OrderStatusChangedMessage(Symbol, GetFilledEvent(DefaultQuantity, FillPrice)));

                // Assert
                var exits = Context.Exits.ToList();
                var exit = exits.Single();

                Assert.Equal(FillPrice, exit.Price);
                Assert.Equal(DefaultQuantity, exit.Quantity);

                var trade = exit.Trade;
                Assert.NotNull(trade.ExitTimeStamp);
                Assert.Equal(FillPrice, trade.ExitPrice);
                Assert.Equal(1, trade.ProfitLoss);
            }
        }

        [Fact]
        public async Task WhenStopOrderFilledAndPositionNotClosedExitAddedButTradeNotUpdated()
        {
            const double FillPrice = 11;

            using (Context)
            {
                // Arrange
                var service = await InitServiceAsync();
                var position = new PositionItem
                {
                    Symbol = new Symbol { Code = Symbol },
                    Quantity = DefaultQuantity
                };

                var filled = DefaultQuantity - 1;

                // Act
                await service.ExitTradeAsync(position, new OrderStatusChangedMessage(Symbol, GetFilledEvent(filled, FillPrice)));

                // Assert
                var exits = Context.Exits.ToList();
                var exit = exits.Single();

                Assert.Equal(FillPrice, exit.Price);
                Assert.Equal(filled, exit.Quantity);

                var trade = exit.Trade;
                Assert.Null(trade.ExitTimeStamp);
                Assert.Null(trade.ExitPrice);
                Assert.Null(trade.ProfitLoss);
            }
        }

        [Fact]
        public async Task WhenFillForSymbolWithNoTradeThenIgnore()
        {
            const double FillPrice = 11;

            using (Context)
            {
                // Arrange
                Context.Trades.Remove(DefaultTrade());
                Context.SaveChanges();
                
                var service = await InitServiceAsync();
                var position = new PositionItem
                {
                    Symbol = new Symbol { Code = Symbol },
                    Quantity = DefaultQuantity
                };

                // Act
                await service.ExitTradeAsync(position, new OrderStatusChangedMessage(Symbol + "A", GetFilledEvent(DefaultQuantity, FillPrice)));

                // Assert
                Assert.Empty(Context.Trades);
                Assert.Empty(Context.Exits);
            }
        }

        [Fact]
        public async Task WhenFillForSymbolWithNoPositionThenIgnore()
        {
            const double FillPrice = 11;

            using (Context)
            {
                // Arrange
                var service = await InitServiceAsync();
                var position = new PositionItem
                {
                    Symbol = new Symbol { Code = Symbol },
                    Quantity = 0
                };

                // Act
                await service.ExitTradeAsync(position, new OrderStatusChangedMessage(Symbol, GetFilledEvent(DefaultQuantity, FillPrice)));

                // Assert
                Assert.NotEmpty(Context.Trades);
                Assert.Empty(Context.Exits);
            }
        }

        private async Task<TradeRecordingService> InitServiceAsync()
        {
            var service = new TradeRecordingService(new TradeRepository(Context));
            await service.LoadTradesAsync();
            return service;
        }

        private static OrderStatusEventArgs GetFilledEvent(double numberFilled, double fillPrice)
        {
            return new OrderStatusEventArgs(1, BrokerConstants.OrderStatus.Filled, numberFilled, DefaultQuantity - numberFilled, fillPrice, 0, 0, fillPrice, 0, null);
        }

        protected override void Seed()
        {
            using (var context = new ApplicationContext(ContextOptions))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.Trades.Add(DefaultTrade());
                context.SaveChanges();
            }
        }

        private static Trade DefaultTrade()
        {
            return new Trade
            {
                Id = 1,
                Direction = Direction.Buy,
                Quantity = DefaultQuantity,
                Symbol = Symbol,
                EntryPrice = 10,
                EntryTimeStamp = DateTime.UtcNow
            };
        }
    }
}
