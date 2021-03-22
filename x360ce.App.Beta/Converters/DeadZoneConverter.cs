using System;
using System.Globalization;
using System.Windows.Data;

namespace x360ce.App.Converters
{
	public class DeadZoneConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (string)value == ""
				? 0
				: int.Parse((string)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (int)value == 0
				? ""
				: value.ToString();
		}
	}

}
