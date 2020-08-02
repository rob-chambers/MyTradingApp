using MyTradingApp.Core;
using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using MyTradingApp.Services;
using NSubstitute;
using System;

namespace MyTradingApp.Tests
{
    public partial class NewOrderViewModelTests
    {
        public class NewOrderViewModelBuilder
        {
            public IDispatcherHelper DispatcherHelper { get; private set; }

            public IQueueProcessor QueueProcessor { get; private set; }

            public IFindSymbolService FindSymbolService { get; private set; }

            public IOrderCalculationService OrderCalculationService { get; private set; }

            public NewOrderViewModelBuilder WithDispatcherHelper(IDispatcherHelper dispatcherHelper = null)
            {
                DispatcherHelper = dispatcherHelper;
                return this;
            }

            public NewOrderViewModelBuilder WithQueueProcessor(IQueueProcessor queueProcessor = null)
            {
                QueueProcessor = queueProcessor;
                return this;
            }

            public NewOrderViewModelBuilder WithFindSymbolService(IFindSymbolService findSymbolService = null)
            {
                FindSymbolService = findSymbolService;
                return this;
            }

            public NewOrderViewModelBuilder WithOrderCalculationService(IOrderCalculationService orderCalculationService = null)
            {
                OrderCalculationService = orderCalculationService;
                return this;
            }

            public NewOrderViewModel Build()
            {
                var dispatcherHelper = DispatcherHelper ?? Substitute.For<IDispatcherHelper>();
                dispatcherHelper
                    .When(x => x.InvokeOnUiThread(Arg.Any<Action>()))
                    .Do(x => x.Arg<Action>().Invoke());

                return new NewOrderViewModel(
                    dispatcherHelper,
                    QueueProcessor ?? Substitute.For<IQueueProcessor>(),
                    FindSymbolService ?? Substitute.For<IFindSymbolService>(),
                    OrderCalculationService ?? Substitute.For<IOrderCalculationService>());
            }
        }
    }
}
