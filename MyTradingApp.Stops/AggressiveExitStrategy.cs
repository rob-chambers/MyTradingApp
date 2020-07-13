using MyTradingApp.Stops.StopTypes;
using System.Collections.ObjectModel;

namespace MyTradingApp.Stops
{
    public class AggressiveExitStrategy : ExitStrategy
    {
        private const double TrailingPercentage = 15;

        public AggressiveExitStrategy()
        {
            //Exits = new Collection<Exit>
            //    {
            //        new Exit
            //        {
            //            LowerPercentage = null,
            //            UpperPercentage = 15,
            //            Stop = new TrailingStop
            //            {
            //                Percentage = 15
            //            }
            //        },
            //        new Exit
            //        {
            //            LowerPercentage = 15,
            //            UpperPercentage = 20,
            //            Stop = new StandardStop()
            //        },
            //        new Exit
            //        {
            //            LowerPercentage = 20,
            //            UpperPercentage = 40,
            //            Stop = new ClosingStop
            //            {
            //                Lower = new GainStopPair(20, 15),
            //                Upper = new GainStopPair(40, 2)
            //            }
            //        },
            //    };

            Stops = new Collection<Stop>
            {
                new TrailingStop
                {
                    Percentage = TrailingPercentage
                },
                new StandardStop
                {
                    InitiateAtGainPercentage = TrailingPercentage,
                    InitialTrailPercentage = TrailingPercentage
                },
                new ClosingStop
                {
                    InitiateAtGainPercentage = 20,
                    InitialTrailPercentage = TrailingPercentage,
                    ProfitTargetPercentage = 40,
                    //Lower = new GainStopPair(20, 15),
                    //Upper = new GainStopPair(40, 2)
                }
            };
        }
    }
}