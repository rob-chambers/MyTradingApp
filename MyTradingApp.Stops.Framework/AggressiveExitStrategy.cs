using MyTradingApp.Stops.StopTypes;

namespace MyTradingApp.Stops
{
    public class AggressiveExitStrategy : ExitStrategy
    {
        private const double TrailingPercentage = 10;

        public AggressiveExitStrategy() : base(new StopCollection
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
                    InitiateAtGainPercentage = TrailingPercentage + 3,
                    InitialTrailPercentage = TrailingPercentage,
                    ProfitTargetPercentage = 28
                }
            })
        {
        }
    }
}