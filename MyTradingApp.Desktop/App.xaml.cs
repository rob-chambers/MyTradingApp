using Microsoft.Extensions.Configuration;
using MyTradingApp.Desktop.Utils;
using Serilog;
using System;
using System.Diagnostics;
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
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            base.OnStartup(e);
            InitLogging();
            InitGlobalExceptionHandler();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Log.Debug("Building config");
            Configuration = builder.Build();

            var mainWindow = new MainWindow();
            mainWindow.Show();
            Log.Debug($"Starting up took {sw.ElapsedMilliseconds}ms");
        }

        private static void InitLogging()
        {
            var template = "{Timestamp:HH:mm:ss.fff} [{Level:u3}] ({ThreadID}) {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ThreadIdEnricher())
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File("logfile.txt", rollingInterval: RollingInterval.Month, outputTemplate: template)
                .CreateLogger();

            Log.Information("Starting up");
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
