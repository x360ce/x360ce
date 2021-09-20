using System;
using System.Globalization;
using System.Windows.Data;

namespace JocysCom.ClassLibrary.Controls
{
	public class ItemFormattingConverter : IMultiValueConverter
	{

		public Func<object[], Type, object, CultureInfo, object> ConvertFunction;
		public Func<object, Type[], object, CultureInfo, object[]> ConvertBackFunction;

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			=> ConvertFunction?.Invoke(values, targetType, parameter, culture);

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			=> ConvertBackFunction?.Invoke(value, targetTypes, parameter, culture);

	}
}
