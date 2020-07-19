//using GalaSoft.MvvmLight.Messaging;
//using MyTradingApp.EventMessages;
//using MyTradingApp.Stops;
//using MyTradingApp.ViewModels;
//using System;
//using System.Collections.Generic;

//namespace MyTradingApp.Services
//{
//    internal class PositionsStopService
//    {
//        private Dictionary<string, StopManager> _managers = new Dictionary<string, StopManager>();

//        public PositionsStopService()
//        {
//            Messenger.Default.Register<TickPrice>(this, HandleTickPriceMessage);
//        }

//        public void Manage(PositionItem position)
//        {
//            _managers.Add(position.Symbol.Code, new StopManager
//            {
//                Position = new Position
//                {
//                    Direction = position.Quantity > 0
//                        ? Domain.Direction.Buy
//                        : Domain.Direction.Sell,
//                    EntryPrice = position.AvgPrice
//                }
//            });
//        }

//        private void HandleTickPriceMessage(TickPrice message)
//        {
//            var symbol = message.Symbol;
//            if (!_managers.ContainsKey(symbol))
//            {
//                return;
//            }

//            var manager = _managers[symbol];
//            //manager.AddLatestBar(message.Price)
//        }
//    }
//}
