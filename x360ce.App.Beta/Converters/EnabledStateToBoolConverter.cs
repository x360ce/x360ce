using System;
using System.Globalization;
using System.Windows.Data;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	public class EnabledStateToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = (EnabledState)value;
			if (v == EnabledState.None)
				return null;
			return v == EnabledState.Enabled;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = (bool?)value;
			if (v == null)
				return EnabledState.None;
			return v.Value ? EnabledState.Enabled : EnabledState.Disabled;
		}
	}
}
