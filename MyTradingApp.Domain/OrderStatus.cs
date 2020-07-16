namespace MyTradingApp.Domain
{
    public enum OrderStatus
    {
        Pending,
        PreSubmitted,
        Submitted,
        Filled,
        Error,
        Cancelled        
    }
}