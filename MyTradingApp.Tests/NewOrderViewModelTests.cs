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
        public void DirectionListSetCorrectly()
        {
            var vm = GetVm();
            Assert.Equal(Direction.Buy, vm.DirectionList[0]);
            Assert.Equal(Direction.Sell, vm.DirectionList[1]);
        }

        [Fact]
        public void CannotFindUntilAtLeastOneCharacterTyped()
        {
            /*
             * var symbol = order.Symbol.Code;
            if (Orders.Any(x => x != order && x.Symbol.Code == symbol))
            {
                Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Warning, $"There is already an order for {symbol}."));
                return null;
            }

            order.Symbol.IsFound = false;
            order.Symbol.Name = string.Empty;
             */

            var vm = GetVm();
            Assert.False(vm.FindCommand.CanExecute());
            vm.Symbol.Code = "M";
            Assert.True(vm.FindCommand.CanExecute());
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
        public async Task FindCommandUpdatesSymbolDetails()
        {
            // Arrange
            const double LatestPrice = 1.23;
            const string CompanyName = "Microsoft";
            const double Tick = 4;

            var findSymbolService = Substitute.For<IFindSymbolService>();
            var builder = new NewOrderViewModelBuilder()
                .WithFindSymbolService(findSymbolService);
            var vm = builder.Build();

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(vm).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    LatestPrice = LatestPrice,
                    Details = new List<ContractDetails>
                    {
                        new ContractDetails { LongName = CompanyName, MinTick = Tick }
                    },
                    PriceHistory = new List<HistoricalDataEventArgs>()
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(vm.Symbol.IsFound);
            Assert.Equal(LatestPrice, vm.Symbol.LatestPrice);
            Assert.Equal(CompanyName, vm.Symbol.Name);
            Assert.Equal(Tick, vm.Symbol.MinTick);
        }

        [Fact]
        public async Task FindCommandDoesNotUpdateDetailsWhenSymbolNotFound()
        {
            // Arrange
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var builder = new NewOrderViewModelBuilder()
                .WithFindSymbolService(findSymbolService);
            var vm = builder.Build();

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(vm).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    Details = null
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.False(vm.Symbol.IsFound);
            Assert.Null(vm.Symbol.Name);
        }

        [Fact]
        public void FindCommandButtonCaptionInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.Equal(NewOrderViewModel.FindButtonCaptions.Default, vm.FindCommandCaption);
        }

        [Fact]
        public void IsBusyFlagInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task WhenFindingThenFindCommandDisabledAndButtonCaptionChangedAsync()
        {
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var builder = new NewOrderViewModelBuilder()
                .WithFindSymbolService(findSymbolService);
            var vm = builder.Build();

            var isBusyEvent = new List<bool>();
            var findCommandCaption = new List<string>();

            vm.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(vm.IsBusy):
                        isBusyEvent.Add(vm.IsBusy);
                        break;

                    case nameof(vm.FindCommandCaption):
                        findCommandCaption.Add(vm.FindCommandCaption);
                        break;
                }
            };

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(vm).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    Details = null
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(isBusyEvent[0]);
            Assert.False(isBusyEvent[1]);
            Assert.Equal(NewOrderViewModel.FindButtonCaptions.Finding, findCommandCaption[0]);
            Assert.Equal(NewOrderViewModel.FindButtonCaptions.Default, findCommandCaption[1]);
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
            findSymbolService.IssueFindSymbolRequestAsync(vm).Returns(Task.FromResult(model));

            return vm;
        }

        [Fact]
        public void CannotSubmitInitially()
        {
            var vm = GetVm();
            Assert.False(vm.SubmitCommand.CanExecute());
        }

        [Fact]
        public async Task CanSubmitWhenSymbolAndPriceFound()
        {
            // Arrange
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var fired = false;
            var vm = GetStandardVm(orderCalculationService, FindCommandResults());
            vm.SubmitCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(vm.SubmitCommand.CanExecute());
            Assert.True(fired);
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
