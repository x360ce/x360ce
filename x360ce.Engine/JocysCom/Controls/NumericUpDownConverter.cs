using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace JocysCom.ClassLibrary.Controls
{
	/// <summary>
	/// Converter used for control style.
	/// </summary>
	public class NumericUpDownStyleConverter : IValueConverter
	{
		public NumericUpDown Control;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
			return v < 0 ? value : v;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}

	/// <summary>
	/// Converter used for control style.
	/// </summary>
	public class NumericUpDownValueConverter : IValueConverter
	{
		public NumericUpDown Control;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var v = (decimal?)value;
			if (v.HasValue)
				return v.Value.ToString((string)parameter);
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var s = (string)value;
			return NumericUpDownValidationRule.GetValue(s);
		}

	}

	public class NumericUpDownValidationRule : ValidationRule
	{

		public static bool IsValid(string value)
		{
			if (string.IsNullOrEmpty(value))
				return true;
			decimal result;
			return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
		}

		public static decimal? GetValue(string value)
		{
			decimal result;
			if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
				return result;
			return null;
		}

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var s = (string)value;
			if (string.IsNullOrEmpty(s) || GetValue(s).HasValue)
				return new ValidationResult(true, null);
			return new ValidationResult(false, "Please enter a valid value.");
		}
	}

}
