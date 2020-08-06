using Newtonsoft.Json;
using System;
using System.Text;

namespace MyTradingApp.Core.Utils
{
    public static class ObjectExtensions
    {
        public static string Dump<T>(this T x, string message = null)
        {
            var json = JsonConvert.SerializeObject(x, Formatting.Indented);

            var builder = new StringBuilder();
            if (message != null)
            {
                builder.Append(message);
                builder.Append(" ");
            }
            
            builder.Append("[");
            builder.Append(x.GetType().FullName);
            builder.Append("]");
            builder.Append(Environment.NewLine);
            builder.Append(json);

            return builder.ToString();
        }
    }
}
