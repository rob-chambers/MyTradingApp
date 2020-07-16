using MyTradingApp.Stops.StopTypes;

namespace MyTradingApp.Stops
{
    public class AggressiveExitStrategy : ExitStrategy
    {
        private const double TrailingPercentage = 20;

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
                    InitiateAtGainPercentage = TrailingPercentage + 5,
                    InitialTrailPercentage = TrailingPercentage,
                    ProfitTargetPercentage = 50
                }
            })
        {
        }
    }
}