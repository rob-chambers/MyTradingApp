using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyTradingApp.Desktop.Views
{
    /// <summary>
    /// Interaction logic for PositionSizerView.xaml
    /// </summary>
    public partial class PositionSizerView : UserControl
    {
        public PositionSizerView()
        {
            InitializeComponent();

            // Select the text in a TextBox when it receives focus.
            EventManager.RegisterClassHandler(typeof(NumericUpDown), PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(NumericUpDown), GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(NumericUpDown), MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));
        }

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            // Find the NumericUpDown
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is NumericUpDown))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent != null)
            {
                var textBox = (NumericUpDown)parent;
                if (!textBox.IsKeyboardFocusWithin)
                {
                    // If the text box is not yet focused, give it the focus and
                    // stop further processing of this click event.
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}