using System.Globalization;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	public class PadSettingToTextValidator: ValidationRule
    {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var v = (string)value;
            if (string.IsNullOrEmpty(v))
                return ValidationResult.ValidResult;
            var iniValue = SettingsConverter.ToIniValue(v);
            if (string.IsNullOrEmpty(iniValue))
            {
                return new ValidationResult(false,
                  $"Please enter correct value.");
            }
            return ValidationResult.ValidResult;
        }
    }
}
