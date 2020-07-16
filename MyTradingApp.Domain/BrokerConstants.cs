namespace MyTradingApp.Domain
{
    public static class BrokerConstants
    {
        public const int ClientId = 12032108;
        public const string UsCurrency = "USD";
        public const string Stock = "STK";
        public const string Cash = "CASH";

        public static class OrderTypes
        {
            public const string Market = "MKT";
            public const string Limit = "LMT";
            public const string Stop = "STP";
            public const string StopLimit = "STP LMT";
            public const string Trail = "TRAIL";
        }

        public static class Actions
        {
            public const string Buy = "BUY";
            public const string Sell = "SELL";
        }

        public static class TimeInForce
        {
            public const string Day = "DAY";
            public const string GoodTilCancelled = "GTC";
            public const string Opening = "OPG";            
        }

        public static class Routers
        {
            public const string IdealPro = "IDEALPRO";
            public const string Smart = "SMART";
            public const string Island = "ISLAND";
        }

        public static class OrderStatus
        {
            public const string PreSubmitted = "PreSubmitted";
            public const string Submitted = "Submitted";
            public const string Cancelled = "Cancelled";
            public const string Filled = "Filled";
        }
    }
}
