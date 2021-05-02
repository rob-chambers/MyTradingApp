using MyTradingApp.ProfitLocker.StopTypes;
using Xunit;

namespace MyTradingApp.ProfitLocker.Tests
{
    public class StopTypeFactoryTests
    {
        [Fact]
        public void FloatingStop()
        {
            var factory = new StopTypeFactory();
            var floatingStop = factory.Create(StopTypeValue.Floating);
            Assert.IsType<FloatingStop>(floatingStop);
        }

        [Fact]
        public void TrailingStop()
        {
            var factory = new StopTypeFactory();
            var trailingStop = factory.Create(StopTypeValue.Trailing);
            Assert.IsType<TrailingStop>(trailingStop);
        }

        [Fact]
        public void SmartStop()
        {
            var factory = new StopTypeFactory();
            var smartStop = factory.Create(StopTypeValue.Smart);
            Assert.IsType<SmartStop>(smartStop);
        }

        [Fact]
        public void WhenUnknownUseTrailingStop()
        {
            var factory = new StopTypeFactory();
            var stop = factory.Create((StopTypeValue)10);
            Assert.IsType<TrailingStop>(stop);
        }
    }
}
