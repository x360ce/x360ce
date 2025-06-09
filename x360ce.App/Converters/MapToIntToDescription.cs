using JocysCom.ClassLibrary.Runtime;
using System;
using System.Globalization;
using System.Windows.Data;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	public class MapToIntToDescription : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Attributes.GetDescription((MapTo)(int)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
