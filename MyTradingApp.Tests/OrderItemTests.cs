using MyTradingApp.Core.ViewModels;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrderItemTests
    {
        [Theory]
        [InlineData(100, 1)]
        [InlineData(1000, 5)]
        [InlineData(5000, 10)]
        [InlineData(10000, 10)]
        public void IntervalSetCorrectly(ushort quantity, int interval)
        {
            var item = new OrderItem
            {
                Quantity = quantity
            };

            Assert.Equal(interval, item.QuantityInterval);
        }
    }
}
