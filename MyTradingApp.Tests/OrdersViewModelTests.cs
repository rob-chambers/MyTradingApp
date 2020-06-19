using MyTradingApp.Models;
using MyTradingApp.Services;
using MyTradingApp.Tests.Orders;
using MyTradingApp.ViewModels;
using NSubstitute;
using System.Diagnostics.Contracts;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrdersViewModelTests
    {
        [Fact]
        public void ExchangeListPopulatedInitially()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            Assert.NotEmpty(vm.ExchangeList);
        }

        [Fact]
        public void DirectionListPopulatedInitially()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            Assert.NotEmpty(vm.DirectionList);
        }

        [Fact]
        public void InitiallyCannotFind()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            var builder = new OrderBuilder();
            var order = builder.Default.Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.False(vm.FindCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CanFindWhenSymbolEntered()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("MSFT").Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.True(vm.FindCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CanDeleteOrderIfPending()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            var builder = new OrderBuilder();
            var order = builder.Default.Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.True(vm.DeleteCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CannotDeleteOrderIfNotPending()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            var builder = new OrderBuilder();
            var order = builder.Default.Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.True(vm.DeleteCommand.CanExecute(commandParameter));

            order.Status = OrderStatus.Submitted;
            Assert.False(vm.DeleteCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CannotSubmitOrderUnlessPending()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("MSFT").Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            order.Status = OrderStatus.Submitted;
            Assert.False(vm.SubmitCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void FindCommandReturnsName()
        {
            var contractManager = Substitute.For<IContractManager>();
            var vm = new OrdersViewModel(contractManager);

            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("MSFT").Order;
            order.Symbol.Exchange = Exchange.NYSE;
            vm.Orders.Add(order);

            var commandParameter = order;
            vm.FindCommand.Execute(commandParameter);

            // Assert
            contractManager.Received()
                .RequestFundamentals(Arg.Is<IBApi.Contract>(x => x.Symbol == order.Symbol.Code &&
                x.Exchange == order.Symbol.Exchange.ToString() &&
                x.Currency == "USD" &&
                x.SecType == "STK"), Arg.Is("ReportSnapshot"));
        }
    }
}
