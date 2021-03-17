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

		public static void LoadAndMonitor(INotifyPropertyChanged source, string sourceProperty, Control control, DependencyProperty controlProperty = null)
		{
			var p = controlProperty ?? GetProperty(control);
			var binding = new System.Windows.Data.Binding(sourceProperty);
			binding.Source = source;
			binding.IsAsync = true;
			control.SetBinding(p, binding);
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
			if (control is Xceed.Wpf.Toolkit.IntegerUpDown)
				return Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty;
			return null;
		}

	}
}
