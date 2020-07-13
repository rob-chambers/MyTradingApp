using MyTradingApp.Stops.StopTypes;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyTradingApp.Stops
{
    public abstract class ExitStrategy
    {
        public Collection<Exit> Exits { get; set; } = new Collection<Exit>();

        public Collection<Stop> Stops { get; set; } = new Collection<Stop>();

        public Exit GetExitForPercentageGain(double value)
        {
            foreach (var exit in Exits)
            {
                if (!exit.LowerPercentage.HasValue && value < exit.UpperPercentage)
                {
                    return exit;
                }
                else if (exit.LowerPercentage.HasValue && value >= exit.LowerPercentage.Value && value <= exit.UpperPercentage)
                {
                    return exit;
                }
            }

            // If we are above the upper percentage of the last exit, we'll fall through to here
            return Exits.Last();
        }

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

            return Stops.First();
        }
    }
}