using System.ComponentModel;

namespace x360ce.App
{

	/// <summary>
	/// region Load, Monitor and Sync settings between controls and properties.
	/// </summary>
	public partial class SettingsManager
	{

		public static void LoadAndMonitor(INotifyPropertyChanged source, string sourceProperty, System.Windows.Controls.Control control, System.Windows.DependencyProperty controlProperty = null)
		{
			if (controlProperty == null)
			{
				if (control is System.Windows.Controls.TextBox)
					controlProperty = System.Windows.Controls.TextBox.TextProperty;
				if (control is System.Windows.Controls.CheckBox)
					controlProperty = System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;
				if (control is System.Windows.Controls.ComboBox || control is System.Windows.Controls.ListBox)
					controlProperty = System.Windows.Controls.Primitives.Selector.SelectedValueProperty;
				if (control is Xceed.Wpf.Toolkit.IntegerUpDown)
					controlProperty = Xceed.Wpf.Toolkit.IntegerUpDown.ValueProperty;
			}
			var binding = new System.Windows.Data.Binding(sourceProperty);
			binding.Source = source;
			binding.IsAsync = true;
			control.SetBinding(controlProperty, binding);
		}


	}
}
