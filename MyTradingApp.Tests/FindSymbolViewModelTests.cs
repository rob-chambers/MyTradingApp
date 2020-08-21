using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class FindSymbolViewModelTests
    {
        private static FindSymbolViewModel GetVm(IFindSymbolService findSymbolService = null)
        {
            var dispatcherHelper = Substitute.For<IDispatcherHelper>();
            dispatcherHelper
                .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                .Do(x =>
                {
                    var action = x.Arg<Action>();
                    action.Invoke();
                });

            var queueProcessor = Substitute.For<IQueueProcessor>();
            findSymbolService ??= Substitute.For<IFindSymbolService>();
            var factory = Substitute.For<INewOrderViewModelFactory>();

            var orderCalculationService = Substitute.For<IOrderCalculationService>();
            var orderManager = Substitute.For<IOrderManager>();

            factory.Create().Returns(new NewOrderViewModel(dispatcherHelper, queueProcessor, findSymbolService, orderCalculationService, orderManager));

            var tradeRepository = Substitute.For<ITradeRepository>();
            var marketDataManager = Substitute.For<IMarketDataManager>();

            var ordersListViewModel = new OrdersListViewModel(dispatcherHelper, queueProcessor, factory, tradeRepository, marketDataManager);

            return new FindSymbolViewModel(dispatcherHelper, queueProcessor, findSymbolService, ordersListViewModel);
        }

        [Fact]
        public void CannotFindUntilAtLeastOneCharacterTyped()
        {
            /*
             * var symbol = order.Symbol.Code;
            if (Orders.Any(x => x != order && x.Symbol.Code == symbol))
            {
                Messenger.Default.Send(new NotificationMessage<NotificationType>(NotificationType.Warning, $"There is already an order for {symbol}."));
                return null;
            }

            order.Symbol.IsFound = false;
            order.Symbol.Name = string.Empty;
             */

            // Arrange
            var vm = GetVm();
            var fired = false;
            vm.Symbol.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Symbol.Code))
                {
                    fired = true;
                }
            };

            Assert.False(vm.FindCommand.CanExecute());

            // Act
            vm.Symbol.Code = "M";

            // Assert
            Assert.True(fired);
            Assert.True(vm.FindCommand.CanExecute());            
        }

        [Fact]
        public async Task FindCommandUpdatesSymbolDetails()
        {
            // Arrange
            const double LatestPrice = 1.23;
            const string CompanyName = "Microsoft";
            const double Tick = 4;

            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    LatestPrice = LatestPrice,
                    Details = new List<ContractDetails>
                    {
                        new ContractDetails { LongName = CompanyName, MinTick = Tick }
                    },
                    PriceHistory = new List<HistoricalDataEventArgs>()
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(vm.Symbol.IsFound);
            Assert.Equal(LatestPrice, vm.Symbol.LatestPrice);
            Assert.Equal(CompanyName, vm.Symbol.Name);
            Assert.Equal(Tick, vm.Symbol.MinTick);
        }

        [Fact]
        public async Task FindCommandDoesNotUpdateDetailsWhenSymbolNotFound()
        {
            // Arrange
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    Details = null
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.False(vm.Symbol.IsFound);
            Assert.Null(vm.Symbol.Name);
        }

        [Fact]
        public void FindCommandButtonCaptionInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.Equal(NewOrderViewModel.FindButtonCaptions.Default, vm.FindCommandCaption);
        }

        [Fact]
        public void IsBusyFlagInitiallyCorrect()
        {
            var vm = GetVm();
            Assert.False(vm.IsBusy);
        }

        [Fact]
        public async Task WhenFindingThenFindCommandDisabledAndButtonCaptionChanged()
        {
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);
            var isBusyEvent = new List<bool>();
            var findCommandCaption = new List<string>();
            var firedCount = 0;

            vm.FindCommand.CanExecuteChanged += (sender, e) => firedCount++;

            vm.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(vm.IsBusy):
                        isBusyEvent.Add(vm.IsBusy);
                        break;

                    case nameof(vm.FindCommandCaption):
                        findCommandCaption.Add(vm.FindCommandCaption);
                        break;
                }
            };

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    Details = null
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(firedCount >= 3);
            Assert.True(isBusyEvent[0]);
            Assert.False(isBusyEvent[1]);
            Assert.Equal(NewOrderViewModel.FindButtonCaptions.Finding, findCommandCaption[0]);
            Assert.Equal(NewOrderViewModel.FindButtonCaptions.Default, findCommandCaption[1]);
        }

        [Fact]
        public async Task WhenSymbolNotFoundThenMessageBoxShown()
        {
            // Arrange
            var fired = false;
            Messenger.Default.Register<NotificationMessage<NotificationType>>(this, msg => fired = true);
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);

            vm.Symbol.Code = "M";
            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    Details = null
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(fired);
        }

        [Fact]
        public async Task FindCommandAddsSymbolToOrdersListWhenFound()
        {
            // Arrange
            const string Symbol = "MSFT";
            const string CompanyName = "Microsoft";
            const double LatestPrice = 10;

            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);

            vm.Symbol.Code = Symbol;
            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(
                new FindCommandResultsModel
                {
                    LatestPrice = LatestPrice,
                    Details = new List<ContractDetails>
                    {
                        new ContractDetails { LongName = CompanyName }
                    },
                    PriceHistory = new List<HistoricalDataEventArgs>()
                }));

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            var order = vm.OrdersListViewModel.Orders.First();
            Assert.Equal(Symbol, order.Symbol.Code);
            Assert.Equal(CompanyName, order.Symbol.Name);
            Assert.Equal(LatestPrice, order.Symbol.LatestPrice);
        }

        [Fact]
        public async Task WhenFindingSymbolForOrderThatAlreadyExistsThenShowNotificationAsync()
        {
            // Arrange
            const string Symbol = "MSFT";

            var fired = false;
            Messenger.Default.Register<NotificationMessage<NotificationType>>(this, msg => fired = true);
            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);
            var model = new FindCommandResultsModel
            {
                Details = new List<ContractDetails>
                    {
                        new ContractDetails { LongName = "Microsoft" }
                    }
            };
            vm.OrdersListViewModel.AddOrder(new Symbol { Code = Symbol }, model);
            vm.Symbol.Code = Symbol;

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            Assert.True(fired);
        }

        [Fact]
        public async Task FindCommandTriesAgainToGetPriceIfFirstAttemptReturnsZero()
        {
            // Arrange
            const string Symbol = "MSFT";
            const string CompanyName = "Microsoft";

            var findSymbolService = Substitute.For<IFindSymbolService>();
            var vm = GetVm(findSymbolService);

            vm.Symbol.Code = Symbol;
            var firstAttempt = true;
            var model = new FindCommandResultsModel
            {
                Details = new List<ContractDetails>
                {
                    new ContractDetails { LongName = CompanyName }
                },
                PriceHistory = new List<HistoricalDataEventArgs>()
            };

            findSymbolService.IssueFindSymbolRequestAsync(Arg.Any<Contract>()).Returns(Task.FromResult(model));
            findSymbolService
                .When(x => x.IssueFindSymbolRequestAsync(Arg.Any<Contract>()))
                .Do(x => 
                {
                    if (firstAttempt)
                    {
                        firstAttempt = false;
                        model.LatestPrice = 0;
                        return;
                    }

                    model.LatestPrice = 10;
                });

            // Act
            await vm.FindCommand.ExecuteAsync();

            // Assert
            var order = vm.OrdersListViewModel.Orders.First();
            Assert.Equal(Symbol, order.Symbol.Code);
            Assert.Equal(CompanyName, order.Symbol.Name);
            Assert.Equal(10, order.Symbol.LatestPrice);
            await findSymbolService.Received(2).IssueFindSymbolRequestAsync(Arg.Any<Contract>());
        }
    }
}
