using System.Windows;
using System.Windows.Controls;

namespace MyTradingApp.Desktop.Views
{
    public partial class PositionsView : UserControl
    {
        public PositionsView()
        {
            InitializeComponent();
        }

        public bool ShowClosedPositions
        {
            get { return (bool)GetValue(ShowClosedPositionsProperty); }
            set { SetValue(ShowClosedPositionsProperty, value); }
        }

        public static readonly DependencyProperty ShowClosedPositionsProperty =
            DependencyProperty.Register("ShowClosedPositions", typeof(bool), typeof(PositionsView), new PropertyMetadata(true));
    }
}
