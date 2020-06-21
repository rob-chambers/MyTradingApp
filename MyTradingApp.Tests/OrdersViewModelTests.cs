using MyTradingApp.Models;
using MyTradingApp.Services;
using MyTradingApp.Tests.Orders;
using MyTradingApp.ViewModels;
using NSubstitute;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrdersViewModelTests
    {
        private IContractManager _contractManager;

        private OrdersViewModel GetVm()
        {
            _contractManager = Substitute.For<IContractManager>();
            var marketDataManager = Substitute.For<IMarketDataManager>();
            var historicalDataManager = Substitute.For<IHistoricalDataManager>();
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            return new OrdersViewModel(_contractManager, marketDataManager, historicalDataManager, orderCalculationService);
        }

        [Fact]
        public void ExchangeListPopulatedInitially()
        {
            var vm = GetVm();
            Assert.NotEmpty(vm.ExchangeList);
        }

        [Fact]
        public void DirectionListPopulatedInitially()
        {
            var vm = GetVm();
            Assert.NotEmpty(vm.DirectionList);
        }

        [Fact]
        public void InitiallyCannotFind()
        {
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.False(vm.FindCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CanFindWhenSymbolEntered()
        {
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("MSFT").Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.True(vm.FindCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CanDeleteOrderIfPending()
        {
            var vm = GetVm();
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
            var vm = GetVm();
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
            var vm = GetVm();
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
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("MSFT").Order;
            order.Symbol.Exchange = Exchange.NYSE;
            vm.Orders.Add(order);

            var commandParameter = order;
            vm.FindCommand.Execute(commandParameter);

            // Assert
            _contractManager.Received()
                .RequestFundamentals(Arg.Is<IBApi.Contract>(x => x.Symbol == order.Symbol.Code &&
                x.Exchange == order.Symbol.Exchange.ToString() &&
                x.Currency == "USD" &&
                x.SecType == "STK"), Arg.Is("ReportSnapshot"));
        }

        [Fact]
        public void SymbolGetsCapitalized()
        {
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("msft").Order;

            // Assert
            Assert.Equal("MSFT", order.Symbol.Code);
        }
    }
}
