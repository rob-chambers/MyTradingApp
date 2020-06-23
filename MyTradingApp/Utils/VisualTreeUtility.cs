using System.Windows;
using System.Windows.Media;

namespace MyTradingApp.Utils
{
    internal static class VisualTreeUtility
    {
        public static T FindParentOfType<T>(this DependencyObject child) 
            where T : DependencyObject
        {
            var parentDepObj = child;
            do
            {
                parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
                if (parentDepObj is T parent) return parent;
            }
            while (parentDepObj != null);
            return null;
        }
    }
}
