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
		// Convert string number to enumeration.
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = value == null || (string)value == ""
				? 0
				: Enum.Parse(typeof(T), (string)value);
			return v;
		}

		/// <summary>
		/// Convert enumeration to integer string.
		/// </summary>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return "";
			var i = ((IConvertible)value).ToInt32(null);
			return i == 0
				? ""
				: i.ToString();

		}
	}
}
