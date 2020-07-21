using Microsoft.Extensions.DependencyInjection;

namespace MyTradingApp.Desktop
{
    public static class ServiceProviderFactory
    {
        public static ServiceProvider ServiceProvider { get; internal set; }
    }
}
