using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.Services
{
    internal class TradeRecordingService : ITradeRecordingService
    {
        private readonly ITradeRepository _tradeRepository;
        private IList<Trade> _trades;

        public TradeRecordingService(ITradeRepository tradeRepository)
        {
            _tradeRepository = tradeRepository;
        }

        public async Task LoadTradesAsync()
        {
            Log.Debug("Loading open trades from repository");
            _trades = (await _tradeRepository.GetAllOpenAsync()).ToList();
        }

        public async Task ExitTradeAsync(PositionItem position, OrderStatusChangedMessage message)
        {
            var trade = _trades.SingleOrDefault(x => x.Symbol == message.Symbol);
            if (trade == null || !position.IsOpen)
            {
                Log.Debug("Could not find open trade or position for {0}", message.Symbol);
                return;
            }

            var exit = new Exit { Trade = trade };
            MapMessageDetails(exit, message);

            await _tradeRepository.AddExitAsync(exit);

            if (!AnyRemainingShares(message))
            {
                await CloseOpenTradeAsync(trade, message);
            }
        }

        private void MapMessageDetails(Exit exit, OrderStatusChangedMessage message)
        {
            exit.Quantity = Convert.ToUInt16(message.Message.Filled);
            exit.Price = message.Message.AvgFillPrice;
            exit.TimeStamp = DateTime.UtcNow;
        }

        private bool AnyRemainingShares(OrderStatusChangedMessage message)
        {
            return message.Message.Remaining != 0;
        }

        private async Task CloseOpenTradeAsync(Trade trade, OrderStatusChangedMessage message)
        {
            trade.ExitTimeStamp = DateTime.UtcNow;
            trade.ExitPrice = message.Message.AvgFillPrice;
            trade.ProfitLoss = trade.CalculateProfitLoss();

            await _tradeRepository.UpdateAsync(trade);
        }
    }
}
