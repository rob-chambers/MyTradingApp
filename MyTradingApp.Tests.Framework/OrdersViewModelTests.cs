﻿using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Messages;
using MyTradingApp.Models;
using MyTradingApp.Tests.Orders;
using MyTradingApp.ViewModels;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrdersViewModelTests
    {
        private const string DefaultSymbol = "MSFT";

        private OrdersViewModel GetVm()
        {
            return new OrdersViewModelBuilder().Build();
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
            var order = builder.Default.SetSymbol(DefaultSymbol).Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            Assert.True(vm.FindCommand.CanExecute(commandParameter));
        }

        [Theory]
        [InlineData(OrderStatus.PreSubmitted)]
        [InlineData(OrderStatus.Submitted)]
        [InlineData(OrderStatus.Filled)]
        [InlineData(OrderStatus.Cancelled)]
        public void OrderLockedOnceSubmitted(OrderStatus status)
        {
            // Arrange
            const int OrderId = 9876;

            var findCommandCanExecuteChangedFired = false;
            var submitCommandCanExecuteChangedFired = false;
            var deleteCommandCanExecuteChangedFired = false;

            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol(DefaultSymbol).Order;
            vm.FindCommand.CanExecuteChanged += (s, e) => findCommandCanExecuteChangedFired = true; ;
            vm.SubmitCommand.CanExecuteChanged += (s, e) => submitCommandCanExecuteChangedFired = true;
            vm.DeleteCommand.CanExecuteChanged += (s, e) => deleteCommandCanExecuteChangedFired = true;
            vm.Orders.Add(order);
            order.Id = OrderId;

            // Act
            var message = new OrderStatusMessage(OrderId, status.ToString(), 0, 0, 0, 0, 0, 0, 0, null, 0);
            Messenger.Default.Send(new OrderStatusChangedMessage(order.Symbol.Code, message));

            // Assert
            Assert.True(findCommandCanExecuteChangedFired);
            Assert.True(order.IsLocked);
            Assert.False(vm.SubmitCommand.CanExecute(order));
            Assert.True(submitCommandCanExecuteChangedFired);
            Assert.True(deleteCommandCanExecuteChangedFired);
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

        [Theory]
        [InlineData(OrderStatus.Submitted)]
        [InlineData(OrderStatus.PreSubmitted)]
        [InlineData(OrderStatus.Filled)]
        public void CannotDeleteOrderIfNotPending(OrderStatus status)
        {
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.Order;
            vm.Orders.Add(order);
            order.Status = status;
            Assert.False(vm.DeleteCommand.CanExecute(order));
        }

        [Theory]
        [InlineData(OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Error)]
        [InlineData(OrderStatus.Filled)]
        [InlineData(OrderStatus.PreSubmitted)]
        [InlineData(OrderStatus.Submitted)]
        public void CannotSubmitOrderUnlessPending(OrderStatus status)
        {
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol(DefaultSymbol).Order;
            vm.Orders.Add(order);

            var commandParameter = order;
            order.Status = status;
            Assert.False(vm.SubmitCommand.CanExecute(commandParameter));
        }

        [Fact]
        public void CannotSubmitOrderUntilFound()
        {
            var vm = GetVm();
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol(DefaultSymbol).Order;
            vm.Orders.Add(order);

            Assert.False(vm.SubmitCommand.CanExecute(order));

            order.Symbol.IsFound = true;
            Assert.True(vm.SubmitCommand.CanExecute(order));
        }

        [Fact]
        public void FindCommandRequestsFundamentalData()
        {
            var builder = new OrdersViewModelBuilder();
            var vm = builder
                .AddSingleOrder(DefaultSymbol, false)
                .Build();

            var order = vm.Orders[0];
            vm.FindCommand.Execute(order);

            // Assert
            builder.ContractManager.Received()
                .RequestFundamentals(Arg.Is<Contract>(x => x.Symbol == order.Symbol.Code &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.SecType == BrokerConstants.Stock &&
                x.PrimaryExch == order.Symbol.Exchange.ToString()), Arg.Is("ReportSnapshot"));
        }

        [Fact]
        public void NasdaqOrdersRoutedThroughIsland()
        {
            // Arrange
            var builder = new OrdersViewModelBuilder();
            var vm = builder
                .AddSingleOrder(DefaultSymbol, false)
                .Build();
            var order = vm.Orders[0];
            order.Symbol.Exchange = Exchange.Nasdaq;

            // Act
            vm.FindCommand.Execute(order);

            // Assert
            builder.ContractManager.Received()
                .RequestFundamentals(Arg.Is<Contract>(x => x.Symbol == order.Symbol.Code &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.SecType == BrokerConstants.Stock &&
                x.PrimaryExch == BrokerConstants.Routers.Island), Arg.Is("ReportSnapshot"));
        }

        [Fact]
        public void SymbolGetsCapitalized()
        {
            var builder = new OrderBuilder();
            var order = builder.Default.SetSymbol(DefaultSymbol).Order;
            Assert.Equal(DefaultSymbol, order.Symbol.Code);
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
            var fired = false;
            var builder = new OrdersViewModelBuilder();
            var vm = builder
                .AddSingleOrder(DefaultSymbol, false)
                .Build();
            var order = vm.Orders[0];

            vm.StartStopStreamingCommand.CanExecuteChanged += (s, e) => fired = true; ;
            builder.MarketDataManager
                .When(x => x.RequestLatestPrice(Arg.Any<Contract>()))
                .Do(x => Messenger.Default.Send(new TickPrice(DefaultSymbol, TickType.LAST, 0)));

            // Act
            vm.FindCommand.Execute(order);

            // Assert
            Assert.True(order.Symbol.IsFound);
            Assert.True(vm.StartStopStreamingCommand.CanExecute(null));
            Assert.True(fired);
        }

        [Fact]
        public void StreamingRequestsMarketDataForSymbol()
        {
            // Arrange           
            var builder = new OrdersViewModelBuilder();
            var vm = builder
                .AddSingleOrder(DefaultSymbol, true)
                .Build();

            // Act
            vm.StartStopStreamingCommand.Execute(null);

            // Assert
            builder.MarketDataManager.Received().RequestStreamingPrice(Arg.Is<Contract>(x => 
                x.Symbol == DefaultSymbol &&
                x.Currency == BrokerConstants.UsCurrency &&
                x.Exchange == BrokerConstants.Routers.Smart &&
                x.SecType == BrokerConstants.Stock));
        }

        [Fact]
        public void StoppingStreamingStopsMarketDataStreaming()
        {
            // Arrange           
            var builder = new OrdersViewModelBuilder();
            var vm = builder
                .AddSingleOrder(DefaultSymbol, true)
                .Build();            
            vm.StartStopStreamingCommand.Execute(null);

            // Act
            vm.StartStopStreamingCommand.Execute(null);

            // Assert
            builder.MarketDataManager.Received().StopActivePriceStreaming();
        }

        [Fact]
        public void WhenAlreadyStreamingAndOrderDeletedStopStreamingSymbol()
        { 
            // Arrange
            var builder = new OrdersViewModelBuilder();
            var vm = builder
                .AddSingleOrder(DefaultSymbol, true)
                .Build();

            var symbols = new List<string>();

            Messenger.Default.Register<OrderRemovedMessage>(this, msg => symbols.Add(msg.Order.Symbol.Code));

            vm.StartStopStreamingCommand.Execute(null);

            // Act
            vm.DeleteCommand.Execute(vm.Orders.First());

            // Assert
            builder.MarketDataManager.Received().StopPriceStreaming(DefaultSymbol);
            Assert.Single(symbols);
            Assert.Equal(DefaultSymbol, symbols.First());
        }

        [Fact]
        public void AddCommandAddsOrder()
        {
            // Arrange
            var builder = new OrdersViewModelBuilder();
            var vm = builder.Build();

            // Act
            vm.AddCommand.Execute(null);

            // Assert
            var order = vm.Orders.Single();
            Assert.Equal(Direction.Buy, order.Direction);
            Assert.Equal(0, order.EntryPrice);
            Assert.Equal(0, order.InitialStopLossPrice);
            Assert.Equal(0, order.Id);
            Assert.Equal(0.05, order.PriceIncrement);
            Assert.Equal(0, order.Quantity);
            Assert.Equal(1, order.QuantityInterval);
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Null(order.Symbol.Code);
            Assert.Null(order.Symbol.Name);
            Assert.Null(order.Symbol.CompanyDescription);
            Assert.False(order.Symbol.IsFound);
        }

        [Fact]
        public void WhenDirectionChangedThenOtherValuesRecalculated()
        {
            // Arrange            
            const double Stop = 12;
            const double Entry = 10;
            const ushort Quantity = 100;

            var builder = new OrdersViewModelBuilder();
            builder.OrderCalculationService.CanCalculate(DefaultSymbol).Returns(true);
            builder.OrderCalculationService.CalculateInitialStopLoss(DefaultSymbol, Direction.Sell).Returns(Stop);
            builder.OrderCalculationService.GetEntryPrice(DefaultSymbol, Direction.Sell).Returns(Entry);
            builder.OrderCalculationService.GetCalculatedQuantity(DefaultSymbol, Direction.Sell).Returns(Quantity);
            var vm = builder.Build();

            // Act
            vm.AddCommand.Execute(null);
            var order = vm.Orders.Single();
            order.Symbol.Code = DefaultSymbol;
            order.Direction = Direction.Sell;

            // Assert
            Assert.Equal(Entry, order.EntryPrice);
            Assert.Equal(Stop, order.InitialStopLossPrice);
            Assert.Equal(Quantity, order.Quantity);
        }

        [Fact]
        public void SubmittingOrderSendsToBroker()
        {
            // Arrange            
            const int OrderId = 123;
            const Exchange Exchange = Exchange.Amex;
            const string AccountId = "U12345678";
            const double Entry = 10;
            const double Stop = 9.14;
            const ushort Qty = 123;

            var builder = new OrdersViewModelBuilder();
            builder.OrderManager
                .When(x => x.PlaceNewOrder(Arg.Any<Contract>(), Arg.Any<Order>()))
                .Do(x => x.Arg<Order>().OrderId = OrderId);

            var vm = builder
                .AddSingleOrder(DefaultSymbol, true)
                .CompleteAccountSummary(new AccountSummaryCompletedMessage
                {
                    AccountId = AccountId
                })
                .Build();

            var order = vm.Orders[0];
            order.Symbol.Exchange = Exchange;
            order.EntryPrice = Entry;
            order.Quantity = Qty;
            order.InitialStopLossPrice = Stop;

            // Act
            vm.SubmitCommand.Execute(vm.Orders[0]);

            // Assert primary order
            builder.OrderManager.Received()
                .PlaceNewOrder(Arg.Is<Contract>(x => x.Symbol == DefaultSymbol &&
                    x.SecType == BrokerConstants.Stock &&
                    x.LocalSymbol == DefaultSymbol &&
                    x.Currency == BrokerConstants.UsCurrency &&
                    x.Exchange == BrokerConstants.Routers.Smart &&
                    x.PrimaryExch == Exchange.ToString()
                ), Arg.Is<Order>(x => x.Action == BrokerConstants.Actions.Buy &&
                    x.OrderType == BrokerConstants.OrderTypes.Stop &&
                    x.Tif == BrokerConstants.TimeInForce.Day &&
                    x.AuxPrice == Entry &&
                    x.TotalQuantity == Qty && 
                    x.Account == AccountId));

            //// Assert stop order
            //builder.OrderManager.Received()
            //    .PlaceNewOrder(Arg.Is<Contract>(x => x.Symbol == DefaultSymbol &&
            //        x.SecType == BrokerConstants.Stock &&
            //        x.Currency == BrokerConstants.UsCurrency &&
            //        x.Exchange == BrokerConstants.Routers.Smart &&
            //        x.PrimaryExch == Exchange.ToString()
            //    ), Arg.Is<Order>(x => x.ParentId == OrderId &&
            //        x.Action == BrokerConstants.Actions.Sell &&
            //        x.OrderType == BrokerConstants.OrderTypes.Stop &&
            //        x.Tif == BrokerConstants.TimeInForce.GoodTilCancelled &&
            //        x.AuxPrice == Stop &&
            //        x.TotalQuantity == Qty &&
            //        x.Account == AccountId &&
            //        x.Transmit));

            Assert.Equal(OrderId, vm.Orders[0].Id);
        }

        [Fact]
        public void StreamingButtonCaptionInitiallyCorrect()
        {
            var builder = new OrdersViewModelBuilder();
            var vm = builder.Build();
            Assert.Equal("Start Streaming", vm.StreamingButtonCaption);
        }

        [Fact]
        public void StreamingButtonCaptionChangesCorrectly()
        {
            var builder = new OrdersViewModelBuilder();
            var vm = builder.Build();
            vm.IsStreaming = true;
            Assert.Equal("Stop Streaming", vm.StreamingButtonCaption);

            vm.IsStreaming = false;
            Assert.Equal("Start Streaming", vm.StreamingButtonCaption);
        }

        [Fact]
        public void LatestTickPriceUpdatesOrderDetails()
        {
            // Arrange
            const double LatestPrice = 10;
            const double EntryPrice = 10.12;
            const double StopPrice = 9;
            const ushort Quantity = 567;

            var builder = new OrdersViewModelBuilder();
            var calculationService = builder.OrderCalculationService;
            calculationService.CanCalculate(DefaultSymbol).Returns(true);
            calculationService.CalculateInitialStopLoss(DefaultSymbol, Direction.Buy).Returns(StopPrice);
            calculationService.GetCalculatedQuantity(DefaultSymbol, Direction.Buy).Returns(Quantity);
            calculationService.GetEntryPrice(DefaultSymbol, Direction.Buy).Returns(EntryPrice);

            var vm = builder
                .AddSingleOrder(DefaultSymbol, true)
                .Build();

            // Act
            Messenger.Default.Send(new TickPrice(DefaultSymbol, TickType.LAST, LatestPrice));

            // Assert
            var order = vm.Orders[0];
            Assert.Equal(LatestPrice, order.Symbol.LatestPrice);

            var service = builder.OrderCalculationService.Received();
            service.SetLatestPrice(DefaultSymbol, LatestPrice);

            Assert.Equal(Quantity, order.Quantity);
            Assert.Equal(StopPrice, order.InitialStopLossPrice);
            Assert.Equal(EntryPrice, order.EntryPrice);
        }

        [Fact]
        public void DeleteAllRemovesAllPendingOrCancelledOrders()
        {
            // Arrange
            var builder = new OrdersViewModelBuilder();
            var vm = builder.Build();
            vm.AddCommand.Execute(null);
            vm.AddCommand.Execute(null);
            vm.AddCommand.Execute(null);
            vm.Orders[1].Status = OrderStatus.Submitted;
            vm.Orders[2].Status = OrderStatus.Cancelled;

            // Act
            vm.DeleteAllCommand.Execute(null);

            // Assert
            var order = vm.Orders.Single();
            Assert.Equal(OrderStatus.Submitted, order.Status);
        }

        [Fact]
        public void TradeRecordedToDatabaseWhenFilled()
        {
            // Arrange
            const double FillPrice = 10.03;
            const ushort Quantity = 123;

            var builder = new OrdersViewModelBuilder().AddSingleOrder(DefaultSymbol, true);
            var vm = builder.Build();
            vm.Orders[0].Quantity = Quantity;

            // Act
            var message = new OrderStatusMessage(0, OrderStatus.Filled.ToString(), 0, 0, FillPrice, 0, 0, 0, 0, null, 0);
            Messenger.Default.Send(new OrderStatusChangedMessage(DefaultSymbol, message));

            // Assert
            builder.TradeRepository.Received().AddTrade(Arg.Is<Trade>(x =>
                x.Direction == Direction.Buy &&
                x.Quantity == Quantity &&
                x.EntryPrice == FillPrice &&
                x.ExitPrice == null &&
                x.ExitTimeStamp == null &&
                x.ProfitLoss == null));
        }
    }
}
