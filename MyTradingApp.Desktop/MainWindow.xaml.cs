using GalaSoft.MvvmLight.Messaging;
using MahApps.Metro.Controls;
using MyTradingApp.ViewModels;
using Serilog;
using System.ComponentModel;
using System.Windows;

namespace MyTradingApp.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            Log.Debug("Loading main window");
            InitializeComponent();
            Closing += OnMainWindowClosing;
            Messenger.Default.Register<NotificationMessage<NotificationType>>(this, HandleNotificationMessage);
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
            if (DataContext != null)
            {
                ((MainViewModel)DataContext).AppIsClosing();
            }
        }
    }
}
