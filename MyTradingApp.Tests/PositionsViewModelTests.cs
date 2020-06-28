using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.EventMessages;
using MyTradingApp.ViewModels;
using System.Collections.Generic;
using Xunit;

namespace MyTradingApp.Tests
{
    public class PositionsViewModelTests
    {
        [Fact]
        public void WhenMessageReceivedPostionsAddedToCollection()
        {
            // Arrange
            var vm = new PositionsViewModel();
            var positions = new List<PositionItem>
            {
                new PositionItem
                {
                    AvgPrice = 11,
                    Quantity = 100,
                    Symbol = new Models.Symbol
                    {
                        Code = "MSFT"
                    }
                }
            };

            // Act
            Messenger.Default.Send(new ExistingPositionsMessage(positions));

            // Assert
            Assert.Single(vm.Positions);
            var position = vm.Positions[0];
            Assert.Equal("MSFT", position.Symbol.Code);
            Assert.Equal(11, position.AvgPrice);
            Assert.Equal(100, position.Quantity);
        }

        [Fact]
        public void NewMessageClearsExistingCollection()
        {
            // Arrange
            var vm = new PositionsViewModel();
            var position = new PositionItem
            {
                AvgPrice = 11,
                Quantity = 100,
                Symbol = new Models.Symbol
                {
                    Code = "MSFT"
                }
            };

            vm.Positions.Add(position);

            // Act
            Messenger.Default.Send(new ExistingPositionsMessage(new List<PositionItem>()));

            // Assert
            Assert.Empty(vm.Positions);
        }
    }
}
