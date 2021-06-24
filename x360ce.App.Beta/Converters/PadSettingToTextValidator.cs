using System.Globalization;
using System.Windows.Controls;
using x360ce.Engine;

namespace x360ce.App.Converters
{
	public class PadSettingToTextValidator: ValidationRule
    {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var iniValue = SettingsConverter.ToIniValue((string)value);
            if (string.IsNullOrEmpty(iniValue))
            {
                return new ValidationResult(false,
                  $"Please enter correct value.");
            }
            return ValidationResult.ValidResult;
        }
    }
}
