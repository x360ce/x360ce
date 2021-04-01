using JocysCom.ClassLibrary.Controls;
using SharpDX.XInput;
using System;
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
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Add GamePad typed to ComboBox.
			DeviceSubTypeComboBox.ItemsSource = (DeviceSubType[])Enum.GetValues(typeof(DeviceSubType));
		}

		public void SetBinding(PadSetting o)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(DeviceSubTypeComboBox);
			SettingsManager.UnLoadMonitor(PassThroughCheckBox);
			//SettingsManager.UnLoadMonitor(ForceFeedbackPassThroughCheckBox);
			if (o == null)
				return;
			// Set binding.
			var converter = new Converters.PaddSettingToEnumConverter<DeviceSubType>();
			SettingsManager.LoadAndMonitor(o, nameof(o.GamePadType), DeviceSubTypeComboBox, null, converter);
			SettingsManager.LoadAndMonitor(o, nameof(o.PassThrough), PassThroughCheckBox, null, null);
			//SettingsManager.LoadAndMonitor(o, nameof(o.ForcesPassThrough), ForceFeedbackPassThroughCheckBox, null, null);
		}

	}
}
