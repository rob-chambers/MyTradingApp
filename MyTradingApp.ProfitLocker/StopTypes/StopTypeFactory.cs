namespace MyTradingApp.ProfitLocker.StopTypes
{
    public class StopTypeFactory
    {
        public StopType Create(StopTypeValue stopType)
        {
            return stopType switch
            {
                StopTypeValue.Trailing => new TrailingStop(),
                StopTypeValue.Smart => new SmartStop(),
                StopTypeValue.Floating => new FloatingStop(),
                _ => new TrailingStop(),
            };
        }
    }
}
