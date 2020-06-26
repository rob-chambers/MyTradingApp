using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.EventMessages;
using MyTradingApp.Models;
using MyTradingApp.Services;
using MyTradingApp.Tests.Orders;
using MyTradingApp.ViewModels;
using NSubstitute;
using System.IO;
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
            var orderManager = Substitute.For<IOrderManager>();
            return new OrdersViewModel(_contractManager, marketDataManager, historicalDataManager, orderCalculationService, orderManager);
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

        [Theory]
        [InlineData(OrderStatus.Pending)]
        [InlineData(OrderStatus.Cancelled)]
        public void CanDeleteOrderIfPendingOrCancelled(OrderStatus status)
        {
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.Order;
            order.Status = status;
            vm.Orders.Add(order);

            var commandParameter = order;
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
                x.Exchange == "SMART" &&
                x.Currency == "USD" &&
                x.SecType == "STK" &&
                x.PrimaryExch == order.Symbol.Exchange.ToString()), Arg.Is("ReportSnapshot"));
        }

        [Fact]
        public void SymbolGetsCapitalized()
        {
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol("msft").Order;

            // Assert
            Assert.Equal("MSFT", order.Symbol.Code);
        }

        [Fact]
        public void StreamingInitiallyDisabled()
        {
            var vm = GetVm();
            Assert.False(vm.StartStopStreamingCommand.CanExecute(null));
        }

        [Fact]
        public void CanOnlyStartStreamingWhenAtLeastOneOrder()
        {
            // Arrange
            const string Symbol = "MSFT";
            var fired = false;
            var vm = GetVm();
            vm.StartStopStreamingCommand.CanExecuteChanged += (s, e) => fired = true; ;

            var xml = File.ReadAllText(@"Resources\fundamentaldata.xml");
            var fundamentalData = FundamentalData.Parse(xml);
            _contractManager
                .When(x => x.RequestFundamentals(Arg.Any<Contract>(), Arg.Any<string>()))
                .Do(x => Messenger.Default.Send(new FundamentalDataMessage(Symbol, fundamentalData)));

            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol(Symbol).Order;
            order.Symbol.Exchange = Exchange.NYSE;
            vm.Orders.Add(order);

            var commandParameter = order;

            // Act
            vm.FindCommand.Execute(commandParameter);

            // Assert
            Assert.True(order.Symbol.IsFound);
            Assert.True(vm.StartStopStreamingCommand.CanExecute(null));
            Assert.True(fired);
        }

        private void StartStopStreamingCommand_CanExecuteChanged(object sender, System.EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
