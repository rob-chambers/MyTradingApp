﻿using AutoFinance.Broker.InteractiveBrokers.EventArgs;
using GalaSoft.MvvmLight.Messaging;
using IBApi;
using MyTradingApp.Core.EventMessages;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Domain;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MyTradingApp.Tests
{
    public class MainViewModelTests
    {
        private void ConnectionTest(MainViewModelBuilder builder, double netLiquidationValue, double exchangeRate)
        {
            var connectionService = Substitute.For<IConnectionService>();
            connectionService.When(x => x.ConnectAsync())
                .Do(x =>
                {
                    builder.ConnectionService.IsConnected.Returns(true);
                    Messenger.Default.Send(new ConnectionChangedMessage(true));
                });
            builder.WithConnectionService(connectionService);

            var accountManager = Substitute.For<IAccountManager>();
            accountManager.RequestAccountSummaryAsync().Returns(Task.FromResult(new AccountSummary
            {
                NetLiquidation = netLiquidationValue
            }));
            builder.WithAccountManager(accountManager);
        }

        [Fact]
        public async Task WhenConnectionErrorThenShowErrorInTextBox()
        {
            var connectionService = Substitute.For<IConnectionService>();
            var builder = new MainViewModelBuilder()
                .WithConnectionService(connectionService);
            connectionService
                .When(x => x.ConnectAsync())
                .Do(x => Raise.Event<ClientError>(this, new ClientError(1, 1, "Error")));
            var vm = builder.Build();

            await vm.ConnectCommand.ExecuteAsync();
            Assert.False(string.IsNullOrEmpty(vm.ErrorText));
            Assert.Equal("Connect", vm.ConnectButtonCaption);

            vm.ClearCommand.Execute(null);

            Assert.Equal(string.Empty, vm.ErrorText);            
        }

        [Theory]
        [InlineData(100000, 0.5, 1, 1, 500)]
        [InlineData(60000, 0.75, 1, 2, 900)]
        [InlineData(200000, 0.75, 2, 1, 3000)]
        public async Task RiskPerTradeCalculatedOnConnectionCorrectlyAsync(double netLiquidationValue, double exchangeRate, double riskMultiplier, double riskPercentOfAccountSize, double expected)
        {
            // Arrange
            var fired = false;
            var settingsRepository = Substitute.For<ISettingsRepository>();
            settingsRepository.GetAllAsync().Returns(new List<Setting>
                {
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskPercentOfAccountSize,
                        Value = riskPercentOfAccountSize.ToString()
                    },
                    new Setting
                    {
                        Key = SettingsViewModel.SettingsKeys.RiskMultiplier,
                        Value = riskMultiplier.ToString()
                    }
                });

            var riskCalculationService = Substitute.For<IRiskCalculationService>();
            riskCalculationService.RiskPerTrade.Returns(expected);
            var builder = new MainViewModelBuilder()
                .WithSettingsRepository(settingsRepository)
                .WithRiskCalculationService(riskCalculationService);
            ConnectionTest(builder, netLiquidationValue, exchangeRate);
            
            var vm = builder.Build();
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RiskPerTrade))
                {
                    fired = true;
                }
            };

            // Wait for settings to load
            await Task.Delay(25);

            // Act
            await vm.ConnectCommand.ExecuteAsync();

            // Assert
            await riskCalculationService.Received().RequestDataForCalculationAsync();
            riskCalculationService.Received().SetRiskMultiplier(riskMultiplier);            

            Assert.Equal(expected, vm.RiskPerTrade);
            Assert.True(fired);
        }

        [Fact]
        public async Task StatusShownCorrectlyWhenConnected()
        {
            // Arrange
            var builder = new MainViewModelBuilder();            
            ConnectionTest(builder, 10000, 0.5);
            var vm = builder.Build();

            // Act
            await vm.ConnectCommand.ExecuteAsync();

            // Assert
            Assert.Equal("Disconnect", vm.ConnectButtonCaption);
            Assert.True(vm.IsEnabled);
        }

        [Fact]
        public async Task WhenAppClosingThenConnectionClosed()
        {
            // Arrange
            var builder = new MainViewModelBuilder();
            ConnectionTest(builder, 0, 0);
            var vm = builder.Build();
            await vm.ConnectCommand.ExecuteAsync();

            // Act
            vm.AppIsClosing();

            // Assert
            await builder.ConnectionService.Received().DisconnectAsync();
        }

        [Fact]
        public async Task WhenUpdatingSettingThenPersistImmediately()
        {
            // Arrange
            const double Multiplier = 1.234;
            
            var settings = new List<Setting>();
            var settingsRepository = Substitute.For<ISettingsRepository>();
            settingsRepository.GetAllAsync().Returns(settings);
            var builder = new MainViewModelBuilder().WithSettingsRepository(settingsRepository);
            var vm = builder.Build();

            // HACK: Wait here to allow the viewmodel's background task to load the settings initially
            await Task.Delay(100);

            // Act
            vm.RiskMultiplier = Multiplier;

            // Assert
            Assert.Equal(Multiplier, builder.SettingsViewModel.LastRiskMultiplier);
            settingsRepository
                .Received()
                .Update(Arg.Is<Setting>(x => x.Key == SettingsViewModel.SettingsKeys.RiskMultiplier && x.Value == Multiplier.ToString()));
            await settingsRepository.Received().SaveAsync();
        }

        [Fact]
        public async Task StartingAppLoadsSettingsFromRepository()
        {
            // Arrange
            var settingsRepository = Substitute.For<ISettingsRepository>();
            settingsRepository.GetAllAsync().Returns(new List<Setting>());
            var builder = new MainViewModelBuilder()
                .WithSettingsRepository(settingsRepository);
                      
            // Act
            builder.Build();

            // Wait a little while for the secondary task to run
            await Task.Delay(50);

            // Assert
            await settingsRepository.Received().GetAllAsync();
        }

        [Fact]
        public async Task WhenConnectionMadePositionsRequested()
        {
            // Arrange
            var builder = new MainViewModelBuilder();            
            ConnectionTest(builder, 0, 0);
            var accountManager = Substitute.For<IAccountManager>();

            var vm = builder
                .WithAccountManager(accountManager)
                .Build();

            // Act
            await vm.ConnectCommand.ExecuteAsync();

            // Assert
            await accountManager.Received().RequestPositionsAsync();
        }

        [Fact]
        public async Task WhenOrderIsFilledRemoveFromOrdersAndAddToPositions()
        {
            // Arrange
            const int OrderId = 123;
            const string Symbol = "MSFT";

            var builder = new MainViewModelBuilder();
            ConnectionTest(builder, 10000, 1);
            var vm = builder.Build();
            await vm.ConnectCommand.ExecuteAsync();
            vm.OrdersListViewModel.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel
            {
                Details = new List<ContractDetails>(),
                LatestPrice = 10,
                PriceHistory = new List<HistoricalDataEventArgs>()
            });
            var order = vm.OrdersListViewModel.Orders[0];
            order.Id = OrderId;

            // Act            
            Messenger.Default.Send(new OrderStatusChangedMessage(Symbol, new OrderStatusEventArgs(OrderId, BrokerConstants.OrderStatus.Filled, 0, 0, 0, 0, 0, 0, 0, null)), OrderStatusChangedMessage.Tokens.Main);

            // Assert
            Assert.Empty(vm.OrdersListViewModel.Orders);
            //Assert.NotEmpty(vm.PositionsViewModel.Positions);
        }

        [Fact]
        public async Task WhenRiskPerTradeModifiedThenQuantityUpdatedOnAllOrders()
        {
            // Arrange
            const string Symbol = "MSFT";

            var orderCaluclationService = Substitute.For<IOrderCalculationService>();
            orderCaluclationService.CanCalculate(Symbol).Returns(true);

            var builder = new MainViewModelBuilder()
                .WithOrderCalculationService(orderCaluclationService);
            ConnectionTest(builder, 10000, 1);
            var vm = builder.Build();

            await vm.ConnectCommand.ExecuteAsync();
            vm.OrdersListViewModel.AddOrder(new Symbol { Code = Symbol }, new FindCommandResultsModel());
            var order = vm.OrdersListViewModel.Orders[0];
            order.Quantity = 1000;

            // Act
            vm.RiskMultiplier = 2;

            // Assert
            orderCaluclationService.Received(2).GetCalculatedQuantity(Symbol, Direction.Buy);
        }

        [Fact]
        public async Task WhenLoadedThenLoadTrades()
        {
            // Arrange
            var tradeRecordingService = Substitute.For<ITradeRecordingService>();

            var builder = new MainViewModelBuilder()
                .WithTradeRecordingService(tradeRecordingService);

            // Act
            builder.Build();

            // Wait a little while for the secondary task to run
            await Task.Delay(50);

            // Assert
            await tradeRecordingService.Received().LoadTradesAsync();
        }
    }
}
