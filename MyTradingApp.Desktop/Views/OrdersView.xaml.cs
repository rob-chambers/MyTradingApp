using MyTradingApp.Core.Utils;
using MyTradingApp.Desktop.Utils;
using MyTradingApp.Utils;
using MyTradingApp.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyTradingApp.Desktop.Views
{
    /// <summary>
    /// Interaction logic for OrdersView.xaml
    /// </summary>
    public partial class OrdersView : UserControl
    {
        public OrdersView()
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
                var vm = (OrdersViewModel)DataContext;

                // Find the ListViewItem the textbox corresponds with
                var item = VisualTreeUtility.FindParentOfType<ListViewItem>(textBox);
                if (item != null)
                {
                    // The command parameter is the binding of the element
                    var orderItem = item.DataContext;

                    /* As this method returns void (it's an event handler), 
                     * there's no reason to introduce async await.
                     * Because the find command is fire and forget, it's more performant
                     * to avoid async / await (which introduces a state machine)
                     * and use our FireAndForgetSafeAsync extension method */
                    vm.FindCommand.ExecuteAsync(orderItem as OrderItem).FireAndForgetSafeAsync(new LoggingErrorHandler());
                }
            }
        }
    }
}
