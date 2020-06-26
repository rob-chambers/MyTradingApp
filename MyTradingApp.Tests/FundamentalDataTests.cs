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
            Assert.Equal("Interactive Brokers Group, Inc. (IBG, Inc.) is a holding company. The Company is an automated global electronic broker and market maker specializing in routing orders, and executing and processing trades in securities, futures, foreign exchange instruments, bonds and mutual funds on over 120 electronic exchanges and market centers around the world and offering custody, prime brokerage, securities and margin lending services to customers. It operates in two segments: electronic brokerage and market making. It conducts its electronic brokerage business through its Interactive Brokers (IB) subsidiaries. It conducts its market making business through its Timber Hill (TH) subsidiaries. In the United States, it conducts its business from Greenwich, Connecticut and Chicago, Illinois. Outside the United States, it conducts business in Canada, England, Switzerland, Liechtenstein, China (Hong Kong and Shanghai), India, Australia and Japan.", results.CompanyDescription);
        }
    }
}
