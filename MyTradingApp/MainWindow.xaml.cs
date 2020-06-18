using MahApps.Metro.Controls;
using MyTradingApp.ViewModels;
using System.ComponentModel;

namespace MyTradingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
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
