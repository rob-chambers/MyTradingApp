using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using Xunit;

namespace MyTradingApp.Tests
{
    public class DetailsViewModelTests
    {
        [Fact]
        public void WhenOrderSelectionChangedMessageReceivedUpdateSelection()
        {
            // Arrange
            var vm = new DetailsViewModel();
            var order = new OrderItem();

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
