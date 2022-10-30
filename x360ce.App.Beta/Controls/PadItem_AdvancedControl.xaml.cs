using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
using System.Windows.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for PadButtonsControl.xaml
	/// </summary>
	public partial class PadItem_AdvancedControl : UserControl, ILoadUnload
	{
		public PadItem_AdvancedControl()
		{
			// This line will create leak...
			InitHelper.InitTimer(this, InitializeComponent);
			//InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Add GamePad typed to ComboBox.
			DeviceSubTypeComboBox.ItemsSource = (DeviceSubType[])Enum.GetValues(typeof(DeviceSubType));
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
			if (ControlsHelper.AllowLoad(this))
				Load();
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ControlsHelper.AllowUnload(this))
				Unload();
		}

		public void Load()
		{
		}

		public void Unload()
		{
			SetBinding(null);
			DeviceSubTypeComboBox.ItemsSource = null;
		}

	}
}
