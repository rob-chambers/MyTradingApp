using MyTradingApp.Stops.StopTypes;
using System.Linq;

namespace MyTradingApp.Stops
{
    public abstract class ExitStrategy
    {
        protected ExitStrategy(StopCollection stops)
        {
            Stops = stops;
            Stops.Sort();
        }

        public StopCollection Stops { get; private set; } = new StopCollection();

        public Stop GetStopForPercentageGain(double value)
        {
            for (var index = Stops.Count - 1; index >= 0; index--)
            {
                var stop = Stops[index];

                if (stop.InitiateAtGainPercentage.HasValue && value >= stop.InitiateAtGainPercentage)
                {
                    return stop;
                }
            }

            return Stops.FirstOrDefault();
        }
    }
}