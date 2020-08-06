using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrdersListViewModelTests
    {
        private static OrdersListViewModel GetVm(
            ITradeRepository tradeRepository = null, 
            IMarketDataManager marketDataManager = null,
            IOrderCalculationService orderCalculationService = null)
        {
            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            dispatcherHelper
                .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                .Do(x => x.Arg<Action>().Invoke());

            var queueProcessor = Substitute.For<IQueueProcessor>();
            if (tradeRepository == null)
            {
                tradeRepository = Substitute.For<ITradeRepository>();
            }

            if (marketDataManager == null)
            {
                marketDataManager = Substitute.For<IMarketDataManager>();
            }

            var findSymbolService = Substitute.For<IFindSymbolService>();

            if (orderCalculationService == null)
            {
                orderCalculationService = Substitute.For<IOrderCalculationService>();
            }
            
            var orderManager = Substitute.For<IOrderManager>();
            var factory = new NewOrderViewModelFactory(dispatcherHelper, queueProcessor, findSymbolService, orderCalculationService, orderManager);

            return new OrdersListViewModel(dispatcherHelper, Substitute.For<IQueueProcessor>(), factory, tradeRepository, marketDataManager);
        }

        [Fact]
        public void OrdersInitiallyEmpty()
        {
            var vm = GetVm();
            Assert.Empty(vm.Orders);
        }

        [Fact]
        public void DirectionListSetCorrectly()
        {
            var vm = GetVm();
            Assert.Equal(Direction.Buy, vm.DirectionList[0]);
            Assert.Equal(Direction.Sell, vm.DirectionList[1]);
        }

        [Fact]
        public void CannotDeleteNullOrder()
        {
            var vm = GetVm();
            Assert.False(vm.DeleteCommand.CanExecute(null));
        }

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Cancelled)]
        public void CanDeletePendingOrder(OrderStatus status)
        {
            var vm = GetVm();
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            var order = vm.Orders[0];
            order.Status = status;
            Assert.True(vm.DeleteCommand.CanExecute(order));
        }

        [Fact]
        public void CanNotDeleteNonAttachedOrder()
        {
            // Arrange
            var vm = GetVm();
            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            var queueProcessor = Substitute.For<IQueueProcessor>();
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();
            var factory = new NewOrderViewModelFactory(dispatcherHelper, queueProcessor, findSymbolService, orderCalculationService, orderManager);
            var order = factory.Create();

            // Assert
            Assert.False(vm.DeleteCommand.CanExecute(order));
        }

        [Fact]
        public void WhenDeleteCommandInvokedThenOrderRemovedFromList()
        {
            // Arrange
            var vm = GetVm();

            // Act
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            var order = vm.Orders[0];
            vm.DeleteCommand.Execute(order);

            // Assert
            Assert.Empty(vm.Orders);
        }

        [Fact]
        public void CannotDeleteAllOrdersUntilAtLeastOneOrderAdded()
        {
            var vm = GetVm();
            Assert.False(vm.DeleteAllCommand.CanExecute(null));
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            Assert.True(vm.DeleteAllCommand.CanExecute(null));
        }

        [Fact]
        public void DeletingAllOrdersDeletesAllThatCanBeDeleted()
        {
            // Arrange
            var vm = GetVm();

            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());

            var filledOrder = vm.Orders[0];
            filledOrder.Status = OrderStatus.Filled;
            var cancelledOrder = vm.Orders[1];
            cancelledOrder.Status = OrderStatus.Cancelled;          

            // Act
            vm.DeleteAllCommand.Execute(null);

            // Assert
            Assert.True(vm.Orders.Single() == filledOrder);
        }

        [Fact]
        public void DeletingAllOrdersRefreshesDeleteAllCommandStatus()
        {
            // Arrange
            var vm = GetVm();
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            var fired = false;
            vm.DeleteAllCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            vm.DeleteAllCommand.Execute(null);

            // Assert
            Assert.True(fired);
        }

        [Fact]
        public void ClickingAddRefreshesDeleteAllCommandStatus()
        {
            // Arrange
            var vm = GetVm();
            var fired = false;
            vm.DeleteAllCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());

            // Assert
            Assert.True(fired);
        }

        [Fact]
        public void WhenOrderFilledTradeAdded()
        {
            // Arrange
            const string Symbol = "AMZN";
            const int OrderId = 1;
            const int Quantity = 99;
            const double FillPrice = 12.03;

            var tradeRepository = Substitute.For<ITradeRepository>();
            var vm = GetVm(tradeRepository);
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            var order = vm.Orders[0];
            order.Status = OrderStatus.Filled;
            order.Symbol.Code = Symbol;
            order.Id = OrderId;
            order.Quantity = Quantity;

            // Act
            var msg = new OrderStatusEventArgs(OrderId, BrokerConstants.OrderStatus.Filled, 0, 0, FillPrice, 1, 0, FillPrice, 0, null);
            Messenger.Default.Send(new OrderStatusChangedMessage(Symbol, msg), OrderStatusChangedMessage.Tokens.Orders);

            // Assert            
            tradeRepository.Received().AddTradeAsync(Arg.Is<Trade>(x => 
                x.Symbol == Symbol &&
                x.Quantity == Quantity &&
                x.Direction == Direction.Buy &&
                x.EntryPrice == FillPrice &&
                !x.ExitPrice.HasValue &&
                !x.ExitTimeStamp.HasValue));
        }

        [Fact]
        public void WhenOrderAddedThenDeleteAllCommandStatusChecked()
        {
            // Arrange
            var vm = GetVm();
            var fired = false;
            vm.DeleteAllCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            vm.AddOrder(new Symbol { Code = "MSFT" }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Assert
            Assert.True(fired);
        }

        [Fact]
        public void StreamingButtonInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.Equal(OrdersListViewModel.StreamingButtonCaptions.StartStreaming, vm.StreamingButtonCaption);
            Assert.False(vm.StartStopStreamingCommand.CanExecute());
        }

        [Fact]
        public void StreamingButtonEnabledWhenAnOrderAdded()
        {
            var vm = GetVm();
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            Assert.True(vm.StartStopStreamingCommand.CanExecute());
        }

        [Fact]
        public void StreamingButtonDisabledWhenAllOrdersRemoved()
        {
            var vm = GetVm();
            vm.AddOrder(new Symbol(), new FindCommandResultsModel());
            vm.DeleteAllCommand.Execute(null);
            Assert.False(vm.StartStopStreamingCommand.CanExecute());
        }

        [Fact]
        public async Task WhenStreamingButtonClickedMarketDataRequestSubmitted()
        {
            const string Symbol = "MSFT";

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(marketDataManager: marketDataManager);
            var fired = false;
            var canExecuteChangedCount = 0;

            vm.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(OrdersListViewModel.StreamingButtonCaption))
                {
                    fired = true;
                }
            };
            vm.StartStopStreamingCommand.CanExecuteChanged += (sender, e) => canExecuteChangedCount++;

            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Act
            await vm.StartStopStreamingCommand.ExecuteAsync();

            // Assert
            await marketDataManager.Received().RequestStreamingPriceAsync(Arg.Is<Contract>(x => 
                x.Symbol == Symbol &&
                x.SecType == BrokerConstants.Stock));

            Assert.True(fired);
            Assert.True(canExecuteChangedCount >= 2);
        }

        [Fact]
        public async Task WhenStreamingStoppedMarketDataRequestIsCancelledAsync()
        {
            const string Symbol = "MSFT";

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(marketDataManager: marketDataManager);
            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Act
            await vm.StartStopStreamingCommand.ExecuteAsync();
            await vm.StartStopStreamingCommand.ExecuteAsync();

            // Assert
            marketDataManager.Received().StopActivePriceStreaming(Arg.Any<IEnumerable<int>>());
        }

        [Fact]
        public async Task IfStartingStreamingThrowsExceptionStatusIsRestored()
        {
            const string Symbol = "MSFT";

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(marketDataManager: marketDataManager);
            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            marketDataManager.RequestStreamingPriceAsync(Arg.Any<Contract>()).Throws(new OutOfMemoryException());

            // Act
            await vm.StartStopStreamingCommand.ExecuteAsync();

            // Assert
            Assert.False(vm.IsStreaming);
        }

        [Fact]
        public async Task WhenStreamingAndBarMessageReceivedThenUpdateOrderDetailsAsync()
        {
            const string Symbol = "MSFT";
            const double EntryPrice = 10;
            const ushort Quantity = 1000;
            const double StopLoss = 9;

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var vm = GetVm(marketDataManager: marketDataManager, orderCalculationService: orderCalculationService);
            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Important - this arrangement must come after adding the order
            orderCalculationService.CanCalculate(Symbol).Returns(true);
            orderCalculationService.GetEntryPrice(Symbol, Direction.Buy).Returns(EntryPrice);
            orderCalculationService.GetCalculatedQuantity(Symbol, Direction.Buy).Returns(Quantity);
            orderCalculationService.CalculateInitialStopLoss(Symbol, Direction.Buy).Returns(StopLoss);

            // Act
            await vm.StartStopStreamingCommand.ExecuteAsync();
            Messenger.Default.Send(new BarPriceMessage(Symbol, new Domain.Bar()));

            // Assert
            var order = vm.Orders[0];
            Assert.Equal(EntryPrice, order.EntryPrice);
            Assert.Equal(Quantity, order.Quantity);
            Assert.Equal(StopLoss, order.InitialStopLossPrice);
        }

        [Fact]
        public void WhenNotStreamingAndBarMessageReceivedThenIgnore()
        {
            const string Symbol = "MSFT";
            const double EntryPrice = 10;
            const ushort Quantity = 1000;

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var vm = GetVm(marketDataManager: marketDataManager, orderCalculationService: orderCalculationService);
            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Important - this arrangement must come after adding the order
            orderCalculationService.CanCalculate(Symbol).Returns(true);
            orderCalculationService.GetEntryPrice(Symbol, Direction.Buy).Returns(EntryPrice);
            orderCalculationService.GetCalculatedQuantity(Symbol, Direction.Buy).Returns(Quantity);

            // Act
            Messenger.Default.Send(new BarPriceMessage(Symbol, new Domain.Bar()));

            // Assert
            var order = vm.Orders[0];
            Assert.Equal(0, order.EntryPrice);
            Assert.Equal(1, order.Quantity);
        }

        [Fact]
        public async Task WhenStreamingAndOrderRemovedCancelStreamingAsync()
        {
            const string Symbol = "MSFT";

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(marketDataManager: marketDataManager);
            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Act
            await vm.StartStopStreamingCommand.ExecuteAsync();
            var order = vm.Orders[0];
            vm.DeleteCommand.Execute(order);

            // Assert
            marketDataManager.Received().StopActivePriceStreaming(Arg.Any<IEnumerable<int>>());
        }

        [Fact]
        public void WhenNotStreamingAndOrderRemovedThenNoRequestToStopStreaming()
        {
            const string Symbol = "MSFT";

            var marketDataManager = Substitute.For<IMarketDataManager>();
            var vm = GetVm(marketDataManager: marketDataManager);
            vm.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                PriceHistory = new List<HistoricalDataEventArgs>()
            });

            // Act
            var order = vm.Orders[0];
            vm.DeleteCommand.Execute(order);

            // Assert
            marketDataManager.DidNotReceive().StopActivePriceStreaming(Arg.Any<IEnumerable<int>>());
        }
    }
}
