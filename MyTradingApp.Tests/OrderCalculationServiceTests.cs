using MyTradingApp.Models;
using MyTradingApp.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace MyTradingApp.Tests
{
    public class OrderCalculationServiceTests
    {
        [Fact]
        public void StandardDeviationCalulctatedCorrectly()
        {
            var service = new OrderCalculationService();
            var bars = new List<Bar>
            {
                new Bar { Close = 9 },
                new Bar { Close = 2 },
                new Bar { Close = 5 },
                new Bar { Close = 4 },
                new Bar { Close = 12 },
                new Bar { Close = 7 },
                new Bar { Close = 8 },
                new Bar { Close = 11 },
                new Bar { Close = 9 },
                new Bar { Close = 3 },
                new Bar { Close = 7 },
                new Bar { Close = 4 },
                new Bar { Close = 12 },
                new Bar { Close = 5 },
                new Bar { Close = 4 },
                new Bar { Close = 10 },
                new Bar { Close = 9 },
                new Bar { Close = 6 },
                new Bar { Close = 9 },
                new Bar { Close = 4 },
            };
            service.SetHistoricalData("MSFT", bars);
            var sd = Math.Round(service.CalculateStandardDeviation("MSFT"), 3);

            Assert.Equal(2.983, sd);
        }
    }
}
