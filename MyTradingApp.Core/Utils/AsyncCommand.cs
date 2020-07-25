using MyTradingApp.Core.Utils;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MyTradingApp.Utils
{
    public class AsyncCommand : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged;

        private bool _isExecuting;
        private readonly IDispatcherHelper _dispatcherHelper;
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly IErrorHandler _errorHandler;

        public AsyncCommand(
            IDispatcherHelper dispatcherHelper,
            Func<Task> execute,
            Func<bool> canExecute = null,
            IErrorHandler errorHandler = null)
        {
            _dispatcherHelper = dispatcherHelper;
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public bool CanExecute()
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    _isExecuting = true;
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            _dispatcherHelper.InvokeOnUiThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }

        #region Explicit implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync().FireAndForgetSafeAsync(_errorHandler);
        }
        #endregion
    }

    public class AsyncCommand<T> : IAsyncCommand<T>
    {
        public event EventHandler CanExecuteChanged;

        private bool _isExecuting;
        private readonly IDispatcherHelper _dispatcherHelper;
        private readonly Func<T, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private readonly IErrorHandler _errorHandler;

        public AsyncCommand(IDispatcherHelper dispatcherHelper, Func<T, Task> execute, Func<T, bool> canExecute = null, IErrorHandler errorHandler = null)
        {
            _dispatcherHelper = dispatcherHelper;
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public bool CanExecute(T parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public async Task ExecuteAsync(T parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    await _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            _dispatcherHelper.InvokeOnUiThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }

        #region Explicit implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync((T)parameter).FireAndForgetSafeAsync(_errorHandler);
        }
        #endregion
    }
}
