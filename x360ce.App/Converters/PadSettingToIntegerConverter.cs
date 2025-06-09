using System;
using System.Globalization;
using System.Windows.Data;

namespace x360ce.App.Converters
{

	/// <summary>
	///  Convert PAD setting string to integer.
	/// </summary>
	public class PadSettingToNumericConverter<T> : IValueConverter where T : IConvertible
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (string)value == ""
				? default(T)
				: System.Convert.ChangeType(value, typeof(T));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || Equals(value, default(T))
				? ""
				: value.ToString();
		}
	}

}
