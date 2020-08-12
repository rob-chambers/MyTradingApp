using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.ViewModels;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    public interface ITradeRecordingService
    {
        Task LoadTradesAsync();

        Task ExitTradeAsync(PositionItem position, OrderStatusChangedMessage message);
    }
}