using System;

namespace MyTradingApp.Stops
{
    public sealed class Bar
    {
        public Bar()
        {
        }

        public Bar(DateTime date, double open, double high, double low, double close)
        {
            Date = date;
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public DateTime Date { get; set; }

        public double Open { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double Close { get; set; }
    }
}