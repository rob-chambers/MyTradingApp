using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using NSubstitute;
using System;
using Xunit;

namespace MyTradingApp.Tests
{
    public class DetailsViewModelTests
    {
        private NewOrderViewModel GetOrder()
        {
            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            dispatcherHelper
                .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                .Do(x =>
                {
                    var action = x.Arg<Action>();
                    action.Invoke();
                });

            var queueProcessor = Substitute.For<IQueueProcessor>();
            var findSymbolService = Substitute.For<IFindSymbolService>();           
            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();

            var factory = new NewOrderViewModelFactory(dispatcherHelper, queueProcessor, findSymbolService, orderCalculationService, orderManager);
            return factory.Create();
        }

        [Fact]
        public void WhenOrderSelectionChangedMessageReceivedUpdateSelection()
        {
            // Arrange
            var vm = new DetailsViewModel();
            var order = GetOrder();

            // Act
            Messenger.Default.Send(new OrderSelectionChangedMessage(order));

            // Assert
            Assert.Equal(order, vm.Selection);
        }

        [Fact]
        public void CloseCommandSendsMessage()
        {
            // Arrange
            var vm = new DetailsViewModel();

            // Act
            var fired = false;
            Messenger.Default.Register<DetailsPanelClosedMessage>(this, x => fired = true);
            vm.CloseDetailsCommand.Execute(null);

            // Assert
            Assert.True(fired);
        }
    }
}
