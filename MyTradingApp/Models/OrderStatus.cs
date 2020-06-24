namespace MyTradingApp.Models
{
    internal enum OrderStatus
    {
        Pending,
        PreSubmitted,
        Submitted,
        Filled,
        Error,
        Cancelled        
    }
}