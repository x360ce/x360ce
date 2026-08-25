using System;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using x360ce.Engine;

namespace x360ce.App
{
	public partial class SettingsManager
	{

		#region Load, Monitor and Sync settings between controls and properties.

		private static void Control_Changed(object sender, EventArgs e)
		{
			// Update property from control.
			Sync((Control)sender, Options);
		}

		private static void Property_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			// Update control from property.
			Sync(Options, e.PropertyName);
		}

		private static object LoadAndSyncLock = new object();
		private static bool IsLoadAndSyncEnabled;

		public static void LoadAndMonitor(Expression<Func<Options, object>> setting, Control control, object dataSource = null)
		{
			var o = Options;
			lock (LoadAndSyncLock)
			{
				// Enable monitoring.
				if (!IsLoadAndSyncEnabled)
					o.PropertyChanged += Property_Changed;
				IsLoadAndSyncEnabled = true;
			}
			// Add control to maps.
			AddMap(setting, control);
			if (dataSource != null)
			{
				// Set ComboBox and attach event last, in order to prevent changing of original value.
				var lc = control as ListControl;
				if (lc != null)
					lc.DataSource = dataSource;
				var lb = control as ListBox;
				if (lb != null)
					lb.DataSource = dataSource;
			}
			// Load settings into control.
			var body = (setting.Body as MemberExpression)
				 ?? (((UnaryExpression)setting.Body).Operand as MemberExpression);
			var propertyName = body.Member.Name;
			// This will triger update of control from the property.
			o.OnPropertyChanged(propertyName);
			// Monitor control changes.
			var chb = control as CheckBox;
			if (chb != null)
				chb.CheckStateChanged += Control_Changed;
			var cbx = control as ComboBox;
			if (cbx != null)
			{
				cbx.TextChanged += Control_Changed;
				cbx.SelectedIndexChanged += Control_Changed;
			}
			var tbx = control as TextBox;
			if (tbx != null)
			{
				tbx.TextChanged += Control_Changed;
			}
		}

		/// <summary>
		/// Set property value from control if different.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		public static void Sync(Control source, object destination)
		{
			var map = Current.SettingsMap.FirstOrDefault(x => x.Control == source);
			if (map == null)
				return;
			var pi = map.Property;
			var oldValue = pi.GetValue(destination, null);
			// Update property from TextBox.
			var textBox = map.Control as TextBox;
			if (textBox != null)
			{
				if (!Equals(oldValue, textBox.Text))
					pi.SetValue(destination, textBox.Text, null);
				return;
			}
			// Update property from CheckBox.
			var checkBox = source as CheckBox;
			if (checkBox != null)
			{
				if (pi.PropertyType == typeof(EnabledState))
				{
					var newValue = EnabledState.None;
					// If CheckBox is in third state then...
					if (checkBox.CheckState != CheckState.Indeterminate)
						newValue = checkBox.Checked ? EnabledState.Enabled : EnabledState.Disabled;
					if (!Equals(oldValue, newValue))
						pi.SetValue(destination, newValue, null);
				}
				else
				{
					if (!Equals(oldValue, checkBox.Checked))
						pi.SetValue(destination, checkBox.Checked, null);
				}
				return;
			}
			// Update property from ComboBox.
			var comboBox = map.Control as ComboBox;
			if (comboBox != null)
			{
				if (pi.PropertyType == typeof(string))
				{
					if (!Equals(oldValue, comboBox.Text))
						pi.SetValue(destination, comboBox.Text, null);
				}
				else
				{
					if (!Equals(oldValue, comboBox.SelectedItem))
						pi.SetValue(destination, comboBox.SelectedItem, null);
				}
				return;
			}
		}

		/// <summary>
		/// Set control value from property if different.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		public static void Sync(object source, string propertyName)
		{
			var map = Current.SettingsMap.FirstOrDefault(x => x.Property.Name == propertyName);
			if (map == null)
				return;
			var propValue = map.Property.GetValue(source, null);
			// Update TextBox from property.
			var textBox = map.Control as TextBox;
			if (textBox != null)
			{
				var value = string.Format("{0}", propValue);
				if (!Equals(value, textBox.Text))
					textBox.Text = value;
				return;
			}
			// Update checkbox from property.
			var checkBox = map.Control as CheckBox;
			if (checkBox != null)
			{
				if (map.Property.PropertyType == typeof(EnabledState))
				{
					var value = (EnabledState)propValue;
					var checkState = CheckState.Indeterminate;
					if (value == EnabledState.Enabled)
						checkState = CheckState.Checked;
					if (value == EnabledState.Disabled)
						checkState = CheckState.Unchecked;
					if (!Equals(checkState, checkBox.CheckState))
						checkBox.CheckState = checkState;
				}
				else
				{
					if (!Equals(propValue, checkBox.Checked))
						checkBox.Checked = (bool)propValue;
				}
				return;
			}
			// Update ComboBox from property.
			var comboBox = map.Control as ComboBox;
			if (comboBox != null)
			{
				if (map.Property.PropertyType == typeof(string))
				{
					var value = string.Format("{0}", propValue);
					if (!Equals(value, comboBox.Text))
						comboBox.Text = value;
				}
				else
				{
					if (!Equals(propValue, comboBox.SelectedItem))
						comboBox.SelectedItem = propValue;
				}
				return;
			}
		}

		#endregion


	}
}
