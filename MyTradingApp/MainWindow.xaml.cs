using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using MyTradingApp.Models;
using MyTradingApp.ViewModels;
using Serilog;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace MyTradingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitLogging();
            InitializeComponent();
            Closing += OnMainWindowClosing;
            Messenger.Default.Register<NotificationMessage<NotificationType>>(this, HandleNotificationMessage);
            InitGlobalExceptionHandler();
        }

        private void InitLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File("logfile.txt", rollingInterval: RollingInterval.Month)
                .CreateLogger();

            Log.Debug("Starting up");            
        }

        private void InitGlobalExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            Application.Current.DispatcherUnhandledException += (s, e) => LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
        }

        private void LogUnhandledException(Exception exception, string source)
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

        private void HamburgerMenuControl_OnItemClick(object sender, ItemClickEventArgs e)
        {
            HamburgerMenuControl.Content = e.ClickedItem;
            HamburgerMenuControl.IsPaneOpen = false;
        }

        private void HandleNotificationMessage(NotificationMessage<NotificationType> message)
        {
            MessageBox.Show(message.Notification, Title, MessageBoxButton.OK, NotificationTypeToImage(message.Content));
        }

        private static MessageBoxImage NotificationTypeToImage(NotificationType notificationType)
        {
            switch (notificationType)
            {
                case NotificationType.Info:
                    return MessageBoxImage.Information;

                case NotificationType.Warning:
                    return MessageBoxImage.Warning;

                case NotificationType.Error:
                    return MessageBoxImage.Error;
            }

            return MessageBoxImage.Information;
        }

        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            if (e.Cancel) return;

            ((MainViewModel)DataContext).AppIsClosing();

            Log.Debug("Shutting down");
        }
    }
}
