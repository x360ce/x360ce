using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JocysCom.ClassLibrary.Controls
{
    public class TabIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as TabItem;
            if (item == null)
                return "";
            var container = ItemsControl.ItemsControlFromItemContainer(item).ItemContainerGenerator;
            var items = container.Items.Cast<TabItem>().Where(x => x.Visibility == Visibility.Visible).ToList();
            var count = items.Count();
            var index = items.IndexOf(item);
            var result = "";
            if (index == 0)
                result += "First";
            if (item.IsSelected)
                result += "Selected";
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
    
}
