using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.IO;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace x360ce.App.Converters
{
	public class ConnectionClassToImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert((Guid)value);
		}

		public static ImageSource Convert(Guid value)
		{
			var bm = value == Guid.Empty
				? new Bitmap(32, 32)
				: DeviceDetector.GetClassIcon(value, 32)?.ToBitmap();
			var img = ControlsHelper.GetImageSource(bm);
			return img;

		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

	}

}
