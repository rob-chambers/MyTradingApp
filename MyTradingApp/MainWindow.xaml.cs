using MyTradingApp.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace MyTradingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += OnMainWindowClosing; ;
        }

        private void OnMainWindowClosing(object sender, CancelEventArgs e)
        {
            if (e.Cancel) return;

            ((MainViewModel)DataContext).AppIsClosing();
        }
    }
}
