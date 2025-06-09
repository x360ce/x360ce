using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Windows;
using System.Windows.Controls;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PadButtonsControl.xaml
	/// </summary>
	public partial class PadItem_AdvancedControl : UserControl
	{
		public PadItem_AdvancedControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Add GamePad typed to ComboBox.
			DeviceSubTypeComboBox.ItemsSource = (DeviceSubType[])Enum.GetValues(typeof(DeviceSubType));
			ControlsHelper.AddWeakHandlerOnWindowClosing(this, (sender, e) => {
				if (e.Cancel)
					return;
				System.Diagnostics.Debug.WriteLine("!!!!!!!!!!!!!!!!!!");
				SetBinding(null);
				DeviceSubTypeComboBox.ItemsSource = null;
			});
		}

		public void SetBinding(PadSetting ps)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(DeviceSubTypeComboBox);
			SettingsManager.UnLoadMonitor(PassThroughCheckBox);
			//SettingsManager.UnLoadMonitor(ForceFeedbackPassThroughCheckBox);
			if (ps == null)
				return;
			// Set binding.
			var enumConverter = new Converters.PadSettingToEnumConverter<DeviceSubType>();
			var boolConverter = new Converters.PadSettingToBoolConverter();
			SettingsManager.LoadAndMonitor(ps, nameof(ps.GamePadType), DeviceSubTypeComboBox, null, enumConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.PassThrough), PassThroughCheckBox, null, boolConverter);
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, EventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
		}

	}
}
