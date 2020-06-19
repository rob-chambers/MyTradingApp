using System.Linq;
using System.Xml.Linq;

namespace MyTradingApp.Models
{
    public class FundamentalData
    {
        public string CompanyName { get; set; }

        public static FundamentalData Parse(string data)
        {
            var result = new FundamentalData();

            var root = XDocument.Parse(data);
            var companyIds = root.Descendants("CoIDs").FirstOrDefault();
            if (companyIds != null)
            {
                var element = (from el in companyIds.Elements() select el)
                    .Where(e => e.HasAttributes && e.Attribute("Type").Value.Equals("CompanyName"))
                    .FirstOrDefault();

                if (element != null)
                {
                    result.CompanyName = element.Value.ToString();
                }
            }

            return result;
        }
    }
}
