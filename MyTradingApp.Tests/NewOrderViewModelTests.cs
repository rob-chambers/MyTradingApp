using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Services;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public partial class NewOrderViewModelTests
    {
        private static NewOrderViewModel GetVm()
        {
            var builder = new NewOrderViewModelBuilder();
            return builder.Build();
        }

        [Fact]
        public void DefaultPriceIncrementIsCorrect()
        {
            var vm = GetVm();
            Assert.Equal(0.05, vm.PriceIncrement);
        }

        [Fact]
        public void DefaultDirectionIsBuy()
        {
            var vm = GetVm();
            Assert.Equal(Direction.Buy, vm.Direction);
        }

        [Fact]
        public void DefaultQuantityIsCorrect()
        {
            var vm = GetVm();
            Assert.Equal(1, vm.Quantity);
        }

        [Fact]
        public void OrderIsInitiallyUnlocked()
        {
            var vm = GetVm();
            Assert.False(vm.IsLocked);
        }

        [Theory]
        [InlineData(999, 1)]
        [InlineData(1000, 5)]
        [InlineData(5000, 10)]
        [InlineData(ushort.MaxValue, 10)]
        public void DefaultQuantityIntervalIsCorrect(ushort quantity, int expectedInterval)
        {
            var vm = GetVm();
            vm.Quantity = quantity;
            Assert.Equal(expectedInterval, vm.QuantityInterval);
        }

        [Theory]
        [InlineData(OrderStatus.Filled)]
        public void WhenStatusChangesOrderIsLocked(OrderStatus status)
        {
            var vm = GetVm();
            vm.Status = status;
            Assert.True(vm.IsLocked);
        }

        [Fact]
        public void CannotFindSymbolOnceLocked()
        {
            var vm = GetVm();
            vm.Symbol.Code = "M";
            vm.Status = OrderStatus.Filled;
            Assert.False(vm.FindCommand.CanExecute());
        }

        [Fact]
        public async Task FindCommandSetsLatestPriceOnCalculationServiceWhenSuccessful()
        {
            // Arrange
            const double LatestPrice = 1.23;
            
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var resultsModel = FindCommandResults(LatestPrice);
            await FindCommandTestAsync(orderCalculationService, resultsModel, "M");

            // Assert
            orderCalculationService.Received().SetLatestPrice("M", LatestPrice);
        }

        private static FindCommandResultsModel FindCommandResults(double price = 1)
        {
            return new FindCommandResultsModel
            {
                LatestPrice = price,
                Details = new List<ContractDetails>
                    {
                        new ContractDetails()
                    },
                PriceHistory = new List<HistoricalDataEventArgs>()
            };
        }

        [Fact]
        public async Task FindCommandSetsHasHistoryFlagCorrectlyWhenNoHistory()
        {
            // Arrange
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var resultsModel = FindCommandResults();
            var vm = await FindCommandTestAsync(orderCalculationService, resultsModel);

            // Assert
            Assert.False(vm.HasHistory);
        }

        [Fact]
        public async Task FindCommandSetsHistoryWhenHistoryAvailable()
        {
            // Arrange
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var resultsModel = FindCommandResults();

            var open = 1D; var high = 2D; var low = 0.5D; var close = 1.5D;

            resultsModel.PriceHistory = new List<HistoricalDataEventArgs>
            {
                new HistoricalDataEventArgs(1, DateTime.Now.ToString(NewOrderViewModel.YearMonthDayPattern), open, high, low, close, 10, 1, 1, false)
            };
            var vm = await FindCommandTestAsync(orderCalculationService, resultsModel, "V");

            // Assert
            Assert.True(vm.HasHistory);
            orderCalculationService.SetHistoricalData("V", Arg.Is<BarCollection>(x => 
                x.Single().Key > DateTime.MinValue &&
                x.Single().Value.Open == open &&
                x.Single().Value.High == high &&
                x.Single().Value.Low == low &&
                x.Single().Value.Close == close));
        }

        private static async Task<NewOrderViewModel> FindCommandTestAsync(IOrderCalculationService orderCalculationService, FindCommandResultsModel model, string symbol = "M")
        {
            // Arrange


            //var findSymbolService = Substitute.For<IFindSymbolService>();
            //var builder = new NewOrderViewModelBuilder()
            //                .WithFindSymbolService(findSymbolService)
            //                .WithOrderCalculationService(orderCalculationService);
            //var vm = builder.Build();

            //vm.Symbol.Code = symbol;
            //findSymbolService.IssueFindSymbolRequestAsync(vm).Returns(Task.FromResult(model));

            var vm = GetStandardVm(orderCalculationService, model, symbol);

            // Act
            await vm.FindCommand.ExecuteAsync();

            return vm;
        }

        private static NewOrderViewModel GetStandardVm(
            IOrderCalculationService orderCalculationService, 
            FindCommandResultsModel model, 
            string symbol = "M",
            IOrderManager orderManager = null)
        {
            // Arrange
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var builder = new NewOrderViewModelBuilder()
                            .WithFindSymbolService(findSymbolService)
                            .WithOrderCalculationService(orderCalculationService)
                            .WithOrderManager(orderManager);
            var vm = builder.Build();

            vm.Symbol.Code = symbol;
            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(model));

            return vm;
        }

        [Fact]
        public void CannotSubmitInitially()
        {
            var vm = GetVm();
            Assert.False(vm.SubmitCommand.CanExecute());
        }

        [Fact]
        public async Task WhenOrderSubmittedThenOrderPlacedWithCorrectDetails()
        {
            // Arrange
            const string Symbol = "AMZN";

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol, orderManager);
            vm.Quantity = 100;
            vm.Direction = Direction.Buy;
            vm.EntryPrice = 20;

            // Act
            await vm.FindCommand.ExecuteAsync();
            await vm.SubmitCommand.ExecuteAsync();

            // Assert
            await orderManager.Received().PlaceNewOrderAsync(Arg.Is<Contract>(x => x.Symbol == Symbol &&
                x.SecType == BrokerConstants.Stock &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.LastTradeDateOrContractMonth == string.Empty &&
                x.Strike == 0 &&
                x.Multiplier == string.Empty &&
                x.LocalSymbol == Symbol), 
                    Arg.Is<Order>(x => x.OrderId == 0 &&
                        x.Action == BrokerConstants.Actions.Buy &&
                        x.OrderType == BrokerConstants.OrderTypes.Stop &&
                        x.AuxPrice == vm.EntryPrice &&
                        x.TotalQuantity == vm.Quantity &&
                        x.ModelCode == string.Empty &&
                        x.Tif == BrokerConstants.TimeInForce.Day));
        }

        [Fact]
        public async Task WhenOrderSubmittedThenOrderIdSet()
        {
            // Arrange
            const string Symbol = "AMZN";
            const int OrderId = 123;

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();
            orderManager
                .When(x => x.PlaceNewOrderAsync(Arg.Any<Contract>(), Arg.Any<Order>()))
                .Do(Callback.First(x => x.Arg<Order>().OrderId = OrderId));

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol, orderManager);
            vm.Quantity = 100;
            vm.Direction = Direction.Buy;
            vm.EntryPrice = 20;

            // Act
            await vm.FindCommand.ExecuteAsync();
            await vm.SubmitCommand.ExecuteAsync();

            // Assert
            Assert.Equal(OrderId, vm.Id);
        }

        [Fact]
        public async Task WhenSellOrderSubmittedThenActionIsSell()
        {
            // Arrange
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();
            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), orderManager: orderManager);
            vm.Quantity = 100;
            vm.Direction = Direction.Sell;
            vm.EntryPrice = 20;

            // Act
            await vm.FindCommand.ExecuteAsync();
            await vm.SubmitCommand.ExecuteAsync();

            // Assert
            await orderManager.Received().PlaceNewOrderAsync(
                Arg.Any<Contract>(),
                Arg.Is<Order>(x => x.Action == BrokerConstants.Actions.Sell));
        }

        [Fact]
        public async Task WhenOrderSubmittedPlacedWithAccountId()
        {
            // Arrange
            const string AccountId = "1234";

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();
            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), orderManager: orderManager);
            vm.Quantity = 100;
            vm.EntryPrice = 20;

            // Act
            Messenger.Default.Send(new AccountSummaryCompletedMessage
            {
                AccountId = AccountId
            });
            await vm.FindCommand.ExecuteAsync();
            await vm.SubmitCommand.ExecuteAsync();

            // Assert
            await orderManager.Received().PlaceNewOrderAsync(
                Arg.Any<Contract>(),
                Arg.Is<Order>(x => x.Account == AccountId));
        }

        [Theory]
        [InlineData("V", Direction.Buy)]
        [InlineData("PLUG", Direction.Sell)]
        public async Task QuantityEntryPriceAndStopLossCalculatedWhenOrderCalculationServiceCanCalculate(string symbol, Direction direction)
        {
            // Arrange
            const double EntryPrice = 10.12;
            const double StopLoss = 9.30;
            const ushort Quantity = 1000;

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            orderCalculationService.CanCalculate(symbol).Returns(true);
            orderCalculationService.GetEntryPrice(symbol, direction).Returns(EntryPrice);
            orderCalculationService.CalculateInitialStopLoss(symbol, direction).Returns(StopLoss);
            orderCalculationService.GetCalculatedQuantity(symbol, direction).Returns(Quantity);

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), symbol);
            vm.Direction = direction;

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.Equal(Quantity, vm.Quantity);
            Assert.Equal(EntryPrice, vm.EntryPrice);
            Assert.Equal(StopLoss, vm.InitialStopLossPrice);
        }

        [Fact]
        public async Task CanNotSubmitWhenOrderDetailsNotCalculated()
        {
            // Arrange
            const string Symbol = "AMZN";
            const Direction Direction = Direction.Buy;

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            orderCalculationService.CanCalculate(Symbol).Returns(true);
            orderCalculationService.GetEntryPrice(Symbol, Direction).Returns(0);
            orderCalculationService.CalculateInitialStopLoss(Symbol, Direction).Returns(0);
            orderCalculationService.GetCalculatedQuantity(Symbol, Direction).Returns(Convert.ToUInt16(0));

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol);
            vm.Direction = Direction;

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.False(vm.SubmitCommand.CanExecute());
        }

        [Fact]
        public async Task CanSubmitWhenOrderDetailsCalculated()
        {
            // Arrange
            const string Symbol = "AMZN";
            const Direction Direction = Direction.Buy;

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var fired = false;
            orderCalculationService.CanCalculate(Symbol).Returns(true);
            orderCalculationService.GetEntryPrice(Symbol, Direction).Returns(10);
            orderCalculationService.CalculateInitialStopLoss(Symbol, Direction).Returns(9);
            orderCalculationService.GetCalculatedQuantity(Symbol, Direction).Returns(Convert.ToUInt16(1000));

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol);
            vm.Direction = Direction;
            vm.SubmitCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert            
            Assert.True(fired);
            Assert.True(vm.SubmitCommand.CanExecute());
        }

        [Fact]
        public void ChangingEntryPriceReCalculatesSubmitCommandStatus()
        {
            // Arrange
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var vm = GetStandardVm(orderCalculationService, FindCommandResults());
            var fired = false;
            vm.SubmitCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            vm.EntryPrice = 10;

            // Assert
            Assert.True(fired);
        }

        [Theory]
        [InlineData("V", Direction.Buy)]
        [InlineData("PLUG", Direction.Sell)]
        public async Task QuantityEntryPriceAndStopLossNotCalculatedWhenOrderCalculationServiceCanNotCalculate(string symbol, Direction direction)
        {
            // Arrange
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            orderCalculationService.CanCalculate(symbol).Returns(false);

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), symbol);
            vm.Direction = direction;

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.Equal(1, vm.Quantity);
            Assert.Equal(0, vm.EntryPrice);
            Assert.Equal(0, vm.InitialStopLossPrice);

            orderCalculationService.DidNotReceive().GetEntryPrice(symbol, direction);
            orderCalculationService.DidNotReceive().CalculateInitialStopLoss(symbol, direction);
            orderCalculationService.DidNotReceive().GetCalculatedQuantity(symbol, direction);
        }

        [Fact]
        public void WhenOrderCancelledViaTwsStatusIsUpdatedAndFindAndSubmitDisabled()
        {
            // Arrange
            const string Symbol = "AMZN";

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol);
            var findCommandCanExecuteChangedFired = false;
            var submitCommandCanExecuteChangedFired = false;
            vm.SubmitCommand.CanExecuteChanged += (sender, e) => submitCommandCanExecuteChangedFired = true;
            vm.FindCommand.CanExecuteChanged += (sender, e) => findCommandCanExecuteChangedFired = true;

            // Act
            var msg = new OrderStatusEventArgs(1, BrokerConstants.OrderStatus.Cancelled, 0, 0, 0, 1, 0, 0, 0, null);
            Messenger.Default.Send(new OrderStatusChangedMessage(Symbol, msg), OrderStatusChangedMessage.Tokens.Orders);

            // Assert
            Assert.Equal(OrderStatus.Cancelled, vm.Status);
            Assert.False(vm.SubmitCommand.CanExecute());
            Assert.True(submitCommandCanExecuteChangedFired);

            Assert.False(vm.FindCommand.CanExecute());
            Assert.True(findCommandCanExecuteChangedFired);
            Assert.True(vm.IsLocked);
        }

        [Fact]
        public void WhenOrderCancelledForDifferentOrderStatusIsNotUpdated()
        {
            // Arrange
            const string Symbol = "AMZN";

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol + "different");

            // Act
            var msg = new OrderStatusEventArgs(1, BrokerConstants.OrderStatus.Cancelled, 0, 0, 0, 1, 0, 0, 0, null);
            Messenger.Default.Send(new OrderStatusChangedMessage(Symbol, msg), OrderStatusChangedMessage.Tokens.Orders);

            // Assert
            Assert.Equal(OrderStatus.Pending, vm.Status);
        }

        [Fact]
        public async Task WhenDirectionChangedPriceAndStopLossReCalculated()
        {
            // Arrange
            const string Symbol = "AMZN";

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            orderCalculationService.CanCalculate(Symbol).Returns(true);

            var vm = GetStandardVm(orderCalculationService, FindCommandResults(), Symbol);
            vm.Direction = Direction.Buy;            

            // Act
            await vm.FindCommand.ExecuteAsync();
            vm.Direction = Direction.Sell;

            // Assert
            orderCalculationService.Received().GetEntryPrice(Symbol, Direction.Buy);
            orderCalculationService.Received().CalculateInitialStopLoss(Symbol, Direction.Buy);
            orderCalculationService.Received().GetCalculatedQuantity(Symbol, Direction.Buy);
            orderCalculationService.Received().GetEntryPrice(Symbol, Direction.Sell);
            orderCalculationService.Received().CalculateInitialStopLoss(Symbol, Direction.Sell);
            orderCalculationService.Received().GetCalculatedQuantity(Symbol, Direction.Sell);
        }
    }
}
