using GalaSoft.MvvmLight.Command;
using System;
using System.Windows.Input;

namespace MyTradingApp.Core.Utils
{
    public class CommandBase : ICommand
    {
        private readonly RelayCommand _command;
        private readonly IDispatcherHelper _dispatcherHelper;

        public event EventHandler CanExecuteChanged;

        public CommandBase(IDispatcherHelper dispatcherHelper, Action execute, bool keepTargetAlive = false)
            : this(dispatcherHelper, execute, null, keepTargetAlive)
        {
        }

        public CommandBase(IDispatcherHelper dispatcherHelper, Action execute, Func<bool> canExecute, bool keepTargetAlive = false)
        {
            _command = new RelayCommand(execute, canExecute, keepTargetAlive);
            _dispatcherHelper = dispatcherHelper;
        }        

        public bool CanExecute(object parameter)
        {
            return _command.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _command.Execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            _dispatcherHelper.InvokeOnUiThread(() =>
            {
                _command.RaiseCanExecuteChanged();
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    public class CommandBase<T> : ICommand
    {
        private readonly RelayCommand<T> _command;
        private readonly IDispatcherHelper _dispatcherHelper;

        public event EventHandler CanExecuteChanged;

        public CommandBase(IDispatcherHelper dispatcherHelper, Action<T> execute, bool keepTargetAlive = false)
            : this(dispatcherHelper, execute, null, keepTargetAlive)
        {
        }

        public CommandBase(IDispatcherHelper dispatcherHelper, Action<T> execute, Func<T, bool> canExecute, bool keepTargetAlive = false)
        {
            _command = new RelayCommand<T>(execute, canExecute, keepTargetAlive);
            _dispatcherHelper = dispatcherHelper;
        }

        public bool CanExecute(object parameter)
        {
            return _command.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _command.Execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            _dispatcherHelper.InvokeOnUiThread(() => 
            {
                _command.RaiseCanExecuteChanged();
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
