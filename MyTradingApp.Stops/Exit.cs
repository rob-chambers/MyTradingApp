using MyTradingApp.Stops.StopTypes;

namespace MyTradingApp.Stops
{
    public class Exit
    {
        public double? LowerPercentage { get; set; }

        public double UpperPercentage { get; set; }

        public Stop Stop { get; set; }
    }
}