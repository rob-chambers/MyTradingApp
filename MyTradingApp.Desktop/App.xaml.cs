using IBApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyTradingApp.Core.Repositories;
using MyTradingApp.Persistence;
using MyTradingApp.Repositories;
using MyTradingApp.Services;
using MyTradingApp.ViewModels;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace MyTradingApp.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitLogging();
            InitGlobalExceptionHandler();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Log.Debug("Building config");
            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProviderFactory.ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProviderFactory.ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void InitLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File("logfile.txt", rollingInterval: RollingInterval.Month)
                .CreateLogger();

            Log.Information("Starting up");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            Log.Debug("Configuring services");
            services.AddTransient(typeof(MainWindow));

            var connectionString = ConfigurationExtensions.GetConnectionString(Configuration, "DefaultConnection");
            services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connectionString));

            services.AddScoped<EReaderSignal, EReaderMonitorSignal>();
            services.AddScoped<EReaderSignal, EReaderMonitorSignal>();
            services.AddScoped<MainViewModel>();
            services.AddScoped<OrdersViewModel>();
            services.AddScoped<PositionsViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<StatusBarViewModel>();
            services.AddScoped<DetailsViewModel>();
            services.AddScoped<IBClient>();
            services.AddScoped<IAccountManager, AccountManager>();
            services.AddScoped<IConnectionService, ConnectionService>();
            services.AddScoped<IOrderManager, OrderManager>();
            services.AddScoped<IContractManager, ContractManager>();
            services.AddScoped<IMarketDataManager, MarketDataManager>();
            services.AddScoped<IHistoricalDataManager, HistoricalDataManager>();
            services.AddScoped<IPositionManager, PositionManager>();
            services.AddScoped<IOrderCalculationService, OrderCalculationService>();
            services.AddScoped<IExchangeRateService, ExchangeRateService>();
            services.AddScoped<ITradeRepository, TradeRepository>();
            services.AddScoped<ISettingsRepository, SettingsRepository>();
            services.AddScoped<IApplicationContext, ApplicationContext>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Log.Information("Shutting down");
        }

        private void InitGlobalExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            Current.DispatcherUnhandledException += (s, e) => LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
        }

        private static void LogUnhandledException(Exception exception, string source)
        {
            var message = $"Unhandled exception ({source})";
            try
            {
                var assemblyName = Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception in {nameof(LogUnhandledException)}");
            }
            finally
            {
                Log.Fatal(exception, message);
            }
        }
    }
}
