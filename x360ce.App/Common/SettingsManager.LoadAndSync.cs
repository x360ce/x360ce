using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace x360ce.App
{

	/// <summary>
	/// region Load, Monitor and Sync settings between controls and properties.
	/// </summary>
	public partial class SettingsManager
	{

		public static void LoadAndMonitor(INotifyPropertyChanged source, string sourceProperty, Control control, DependencyProperty controlProperty = null, IValueConverter converter = null, BindingMode mode = BindingMode.Default)
		{
			var p = controlProperty ?? GetProperty(control);
			var binding = new System.Windows.Data.Binding(sourceProperty);
			binding.Source = source;
			binding.IsAsync = true;
			binding.Converter = converter;
			binding.Mode = mode;
			// Set UpdateSourceTrigger to PropertyChanged, otherwise control to object update won't trigger.
			binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
			if (converter is Converters.PaddSettingToText)
			{
				var padValidator = new Converters.PadSettingToTextValidator();
				binding.ValidationRules.Add(padValidator);
				control.SetBinding(p, binding);
			}
			else if (converter != null)
			{
				binding.Mode = BindingMode.OneWayToSource;
				var value = source.GetType().GetProperty(sourceProperty).GetValue(source);
				var v = converter.Convert(value, null, null, null);
				control.SetBinding(p, binding);
				control.SetValue(p, v);
			}
			else
			{
				control.SetBinding(p, binding);
			}
		}

		public static void UnLoadMonitor(Control control, DependencyProperty controlProperty = null)
		{
			var p = controlProperty ?? GetProperty(control);
			BindingOperations.ClearBinding(control, p);
		}

		static DependencyProperty GetProperty(Control control)
		{
			if (control is TextBox)
				return TextBox.TextProperty;
			if (control is CheckBox)
				return System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;
			if (control is ComboBox || control is ListBox)
				return System.Windows.Controls.Primitives.Selector.SelectedValueProperty;
			if (control is JocysCom.ClassLibrary.Controls.NumericUpDown)
				return JocysCom.ClassLibrary.Controls.NumericUpDown.ValueProperty;
			return null;
		}

	}
}
