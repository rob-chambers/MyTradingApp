using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using System;
using System.Linq;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrdersListViewModelTests
    {
        private static OrdersListViewModel GetVm(ITradeRepository tradeRepository = null)
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

            var findSymbolService = Substitute.For<IFindSymbolService>();
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();

            var factory = new NewOrderViewModelFactory(dispatcherHelper, queueProcessor, findSymbolService, orderCalculationService, orderManager);

            return new OrdersListViewModel(dispatcherHelper, Substitute.For<IQueueProcessor>(), factory, tradeRepository);
        }

        [Fact]
        public void OrdersInitiallyEmpty()
        {
            var vm = GetVm();
            Assert.Empty(vm.Orders);
        }

        [Fact]
        public void ClickingAddAddsOrderToList()
        {
            var vm = GetVm();
            vm.AddCommand.Execute(null);
            Assert.NotEmpty(vm.Orders);
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
            vm.AddCommand.Execute(null);
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
            vm.AddCommand.Execute(null);
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
            vm.AddCommand.Execute(null);
            Assert.True(vm.DeleteAllCommand.CanExecute(null));
        }

        [Fact]
        public void DeletingAllOrdersDeletesAllThatCanBeDeleted()
        {
            // Arrange
            var vm = GetVm();

            vm.AddCommand.Execute(null);
            vm.AddCommand.Execute(null);
            vm.AddCommand.Execute(null);

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
        public void ClickingAddRefreshesDeleteAllCommandStatus()
        {
            // Arrange
            var vm = GetVm();
            var fired = false;
            vm.DeleteAllCommand.CanExecuteChanged += (sender, e) => fired = true;

            // Act
            vm.AddCommand.Execute(null);

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
            vm.AddCommand.Execute(null);
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
    }
}
