using System.Linq;
using System.Xml.Linq;

namespace MyTradingApp.Domain
{
    public class FundamentalData
    {
        // Hide constructor - can only instantiate via Parse method
        private FundamentalData(string companyName, string companyDescription)
        {
            CompanyName = companyName;
            CompanyDescription = companyDescription;
        }

        public string CompanyName { get; }

        public string CompanyDescription { get; set; }

        public static FundamentalData Parse(string data)
        {
            var root = XDocument.Parse(data);
            var companyName = GetCompanyName(root);
            var companyDescription = GetCompanyDescription(root);

            return new FundamentalData(companyName, companyDescription);
        }

        private static string GetCompanyName(XDocument root)
        {
            var companyName = string.Empty;
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

            return companyName;
        }

        private static string GetCompanyDescription(XDocument root)
        {
            var description = string.Empty;
            var companyIds = root.Descendants("TextInfo").FirstOrDefault();
            if (companyIds != null)
            {
                var element = (from el in companyIds.Elements() select el)
                    .Where(e => e.HasAttributes && e.Attribute("Type").Value.Equals("Business Summary"))
                    .FirstOrDefault();

                if (element != null)
                {
                    description = element.Value.ToString();
                }
            }

            return description;
        }
    }
}