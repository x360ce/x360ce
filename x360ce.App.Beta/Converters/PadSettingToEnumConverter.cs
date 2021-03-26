using System;
using System.Globalization;
using System.Windows.Data;

namespace x360ce.App.Converters
{
	/// <summary>
	///  Convert PAD setting string to enumeration.
	/// </summary>
	public class PaddSettingToEnumConverter<T> : IValueConverter where T : System.Enum
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = value == null || (string)value == ""
				? 0
				: Enum.Parse(typeof(T), (string)value);
			return v;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (int)value == 0
				? ""
				: ((int)value).ToString();
		}
	}
}
