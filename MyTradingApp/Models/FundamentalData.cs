using System.Linq;
using System.Xml.Linq;

namespace MyTradingApp.Models
{
    public class FundamentalData
    {
        // Hide constructor - can only instantiate via Parse method
        private FundamentalData(string companyName)
        {
            CompanyName = companyName;
        }

        public string CompanyName { get; }

        public static FundamentalData Parse(string data)
        {
            var companyName = string.Empty;
            var root = XDocument.Parse(data);
            var companyIds = root.Descendants("CoIDs").FirstOrDefault();
            if (companyIds != null)
            {
                var element = (from el in companyIds.Elements() select el)
                    .Where(e => e.HasAttributes && e.Attribute("Type").Value.Equals("CompanyName"))
                    .FirstOrDefault();

                if (element != null)
                {
                    companyName = element.Value.ToString();
                }
            }

            return new FundamentalData(companyName);
        }
    }
}
