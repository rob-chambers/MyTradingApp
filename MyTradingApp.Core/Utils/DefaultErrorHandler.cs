using GalaSoft.MvvmLight.Messaging;
using MyTradingApp.Core.ViewModels;
using System;

namespace MyTradingApp.Core.Utils
{
    internal class DefaultErrorHandler : IErrorHandler
    {
        public void HandleError(Exception ex)
        {
            Messenger.Default.Send(new NotificationMessage<NotificationType>(this, NotificationType.Error, $"An unexpected error occurred:\n{ex.Message}"));
        }
    }
}
