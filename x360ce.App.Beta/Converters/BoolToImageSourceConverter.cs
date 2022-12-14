using System;
using System.Globalization;
using System.Windows.Data;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	public class BoolToImageSourceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value
				? Icons_Default.Current[Icons_Default.Icon_square_green]
				: Icons_Default.Current[Icons_Default.Icon_square_grey];
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
