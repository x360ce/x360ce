using System;
using System.Globalization;
using System.Windows.Data;

namespace x360ce.App.Converters
{
	/// <summary>
	///  Convert PAD setting string to boolean.
	/// </summary>
	public class PadSettingToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = (string)value;
			return string.IsNullOrEmpty(v)
				? (bool?)null
				: v == "1";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = (bool?)value;
			return v == null
				? ""
				: v.Value ? "1" : "0";
		}
	}
}
