using System;
using System.Globalization;
using System.Windows.Data;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	public class ChecksumToIdConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return EngineHelper.GetID((Guid)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
