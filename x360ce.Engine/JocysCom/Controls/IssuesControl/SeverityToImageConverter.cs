using System;
using System.Globalization;
using System.Windows.Data;

namespace JocysCom.ClassLibrary.Controls.IssuesControl
{
	public class SeverityToImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is null)
				return Themes.Icons.Current[Themes.Icons.Icon_OK];
			var v = (IssueSeverity)value;
			switch (v)
			{
				case IssueSeverity.None:
					return Themes.Icons.Current[Themes.Icons.Icon_Information];
				case IssueSeverity.Low:
					return Themes.Icons.Current[Themes.Icons.Icon_Information];
				case IssueSeverity.Important:
					return Themes.Icons.Current[Themes.Icons.Icon_Warning];
				case IssueSeverity.Moderate:
					return Themes.Icons.Current[Themes.Icons.Icon_Warning];
				case IssueSeverity.Critical:
					return Themes.Icons.Current[Themes.Icons.Icon_Error];
				default:
					return Themes.Icons.Current[Themes.Icons.Icon_Information];
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
