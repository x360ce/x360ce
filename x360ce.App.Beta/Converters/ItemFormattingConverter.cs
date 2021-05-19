using System;
using System.Globalization;
using System.Windows.Data;

namespace x360ce.App.Converters
{
	public class ItemFormattingConverter : IValueConverter
	{

		public Func<object, Type, object, CultureInfo, object> ConvertFunction;
		public Func<object, Type, object, CultureInfo, object> ConvertBackFunction;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> ConvertFunction?.Invoke(value, targetType, parameter, culture);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> ConvertBackFunction?.Invoke(value, targetType, parameter, culture);

	}
}
