using MyTradingApp.Core.Utils;
using MyTradingApp.Core.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyTradingApp.Desktop.Views
{
    /// <summary>
    /// Interaction logic for FindSymbolView.xaml
    /// </summary>
    public partial class FindSymbolView : UserControl
    {
        public FindSymbolView()
        {
            InitializeComponent();
        }

        private void OnSymbolTextBoxKeyUp(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox textBox) || textBox.Text.Length == 0)
            {
                return;
            }

            if (e.Key == Key.Enter)
            {
                // Execute Find Command
                var vm = textBox.DataContext as FindSymbolViewModel;

                /* As this method returns void (it's an event handler), 
                    * there's no reason to introduce async await.
                    * Because the find command is fire and forget, it's more performant
                    * to avoid async / await (which introduces a state machine)
                    * and use our FireAndForgetSafeAsync extension method */
                vm.FindCommand.ExecuteAsync().FireAndForgetSafeAsync(new LoggingErrorHandler());
            }
        }
    }
}
