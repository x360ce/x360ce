using System;
using System.Globalization;
using System.Windows.Data;
using x360ce.Engine;
using x360ce.Engine.Maps;

namespace x360ce.App.Converters
{
	public class EnumTypeConverter<T> : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Return value to display.
			return (T)(object)(int)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Return value from cell.
			return (int)(object)(T)value;
		}
	}

	public class MapTypeConverter : EnumTypeConverter<MapType> { }
	public class MapEventTypeConverter : EnumTypeConverter<MapEventType> { }
	public class MapRpmTypeConverter : EnumTypeConverter<MapRpmType> { }

}
