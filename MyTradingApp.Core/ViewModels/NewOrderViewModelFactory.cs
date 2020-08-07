using MyTradingApp.Core.Services;
using MyTradingApp.Core.Utils;

namespace MyTradingApp.Core.ViewModels
{
    public class NewOrderViewModelFactory : INewOrderViewModelFactory
    {
        private readonly IDispatcherHelper _dispatcherHelper;
        private readonly IQueueProcessor _queueProcessor;
        private readonly IFindSymbolService _findSymbolService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IOrderManager _orderManager;

        public NewOrderViewModelFactory(
            IDispatcherHelper dispatcherHelper,
            IQueueProcessor queueProcessor,
            IFindSymbolService findSymbolService,
            IOrderCalculationService orderCalculationService,
            IOrderManager orderManager)
        {
            _dispatcherHelper = dispatcherHelper;
            _queueProcessor = queueProcessor;
            _findSymbolService = findSymbolService;
            _orderCalculationService = orderCalculationService;
            _orderManager = orderManager;
        }

        public NewOrderViewModel Create()
        {
            return new NewOrderViewModel(_dispatcherHelper, _queueProcessor, _findSymbolService, _orderCalculationService, _orderManager);
        }
    }
}
