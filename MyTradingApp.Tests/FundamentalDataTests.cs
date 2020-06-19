using MyTradingApp.Models;
using System.IO;
using Xunit;

namespace MyTradingApp.Tests
{
    public class FundamentalDataTests
    {
        [Fact]
        public void ParsingXmlReturnCorrectValues()
        {
            var xml = File.ReadAllText(@"Resources\fundamentaldata.xml");
            var results = FundamentalData.Parse(xml);
            Assert.Equal("Interactive Brokers Group, Inc.", results.CompanyName);
        }
    }
}
