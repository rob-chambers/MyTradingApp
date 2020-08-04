﻿using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.Utils;
using MyTradingApp.Domain;
using MyTradingApp.EventMessages;
using MyTradingApp.Repositories;
using MyTradingApp.Utils;
using MyTradingApp.ViewModels;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyTradingApp.Core.ViewModels
{
    public class OrdersListViewModel : DispatcherViewModel
    {
        private readonly INewOrderViewModelFactory _newOrderViewModelFactory;
        private readonly ITradeRepository _tradeRepository;
        private CommandBase _addCommand;
        private CommandBase<NewOrderViewModel> _deleteCommand;
        private CommandBase _deleteAllCommand;        

        public OrdersListViewModel(
            IDispatcherHelper dispatcherHelper, 
            IQueueProcessor queueProcessor,
            INewOrderViewModelFactory newOrderViewModelFactory,
            ITradeRepository tradeRepository)
            : base(dispatcherHelper, queueProcessor)
        {
            _newOrderViewModelFactory = newOrderViewModelFactory;
            _tradeRepository = tradeRepository;
            PopulateDirectionList();
            Messenger.Default.Register<OrderStatusChangedMessage>(this, OrderStatusChangedMessage.Tokens.Orders, OnOrderStatusChangedMessage);
        }

        public ObservableCollection<Direction> DirectionList { get; private set; } = new ObservableCollection<Direction>();

        public ObservableCollectionNoReset<NewOrderViewModel> Orders { get; private set; } = new ObservableCollectionNoReset<NewOrderViewModel>();

        public CommandBase AddCommand
        {
            get
            {
                return _addCommand ?? (_addCommand = new CommandBase(DispatcherHelper, () =>
                {
                    var order = _newOrderViewModelFactory.Create();
                    Orders.Add(order);
                    DispatcherHelper.InvokeOnUiThread(() => DeleteAllCommand.RaiseCanExecuteChanged());
                }));
            }
        }

        public CommandBase<NewOrderViewModel> DeleteCommand
        {
            get
            {
                return _deleteCommand ?? (_deleteCommand = new CommandBase<NewOrderViewModel>(DispatcherHelper,
                    order =>
                    {
                        if (Orders.Contains(order))
                        {
                            Orders.Remove(order);
                            DispatcherHelper.InvokeOnUiThread(() => DeleteAllCommand.RaiseCanExecuteChanged());
                        }
                    },
                    order => CanDelete(order)));
            }
        }

        public CommandBase DeleteAllCommand
        {
            get
            {
                return _deleteAllCommand ?? (_deleteAllCommand = new CommandBase(DispatcherHelper, DeleteAll, () => Orders.Any()));
            }
        }

        private void PopulateDirectionList()
        {
            var values = Enum.GetValues(typeof(Direction));
            foreach (var value in values)
            {
                DirectionList.Add((Direction)value);
            }
        }

        public void AddOrder(Symbol symbol, FindCommandResultsModel results)
        {
            var order = _newOrderViewModelFactory.Create();
            order.ProcessFindCommandResults(symbol, results);
            DispatcherHelper.InvokeOnUiThread(() => Orders.Add(order));
        }

        private async void OnOrderStatusChangedMessage(OrderStatusChangedMessage message)
        {
            // Find corresponding order
            var order = Orders.SingleOrDefault(o => o.Id == message.Message.OrderId);
            if (order == null)
            {
                // Most likely an existing pending order (i.e. one that wasn't submitted via this app while it is currently open)
                return;
            }

            if (order.Status != OrderStatus.Filled)
            {
                return;
            }

            Log.Debug("A new order for {0} was filled", order.Symbol.Code);
            var addTradeTask = AddTradeAsync(order, message.Message.AvgFillPrice);

            await addTradeTask;
            //    var stopOrderTask = SubmitStopOrderAsync(order, message.Message);

            //    await Task.WhenAll(addTradeTask, stopOrderTask).ConfigureAwait(false);

            //    // This order can be removed now that it is dealt with - it will be added as a position
            //    DispatcherHelper.InvokeOnUiThread(() => Orders.Remove(order));

            //    // Pass this message on to the positions vm now that we have a stop order 
            //    Messenger.Default.Send(message, OrderStatusChangedMessage.Tokens.Positions);
            //}
        }

        private Task AddTradeAsync(NewOrderViewModel order, double fillPrice)
        {
            Log.Debug("Recording trade");
            return _tradeRepository.AddTradeAsync(new Trade
            {
                Symbol = order.Symbol.Code,
                Direction = order.Direction,
                EntryPrice = fillPrice,
                EntryTimeStamp = DateTime.UtcNow,
                Quantity = order.Quantity
            });
        }

        private void DeleteAll()
        {
            foreach (var order in Orders.Where(o => CanDelete(o)).ToList())
            {
                Orders.Remove(order);
            }

            DispatcherHelper.InvokeOnUiThread(() => DeleteAllCommand.RaiseCanExecuteChanged());
        }

        private bool CanDelete(NewOrderViewModel order)
        {
            return Orders.Contains(order) && (order?.Status == OrderStatus.Pending || order?.Status == OrderStatus.Cancelled);
        }
    }
}
