using System;
using System.Globalization;
using System.Windows.Data;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	/// <summary>
	///  Convert PAD setting string to enumeration.
	/// </summary>
	public class PaddSettingToText : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return SettingsConverter.FromIniValue((string)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return SettingsConverter.ToIniValue((string)value);
		}
	}
}
