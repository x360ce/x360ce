using System.Windows.Controls;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for DPadControl.xaml
	/// </summary>
	public partial class DPadControl : UserControl
	{
		public DPadControl()
		{
			InitializeComponent();
			deadzoneLink = new TrackBarUpDownTextBoxLink(DeadZoneTrackBar, DeadZoneUpDown, DeadZoneTextBox, 0, 100);
			offsetLink = new TrackBarUpDownTextBoxLink(OffsetTrackBar, OffsetUpDown, OffsetTextBox, 0, 100);
		}

		TrackBarUpDownTextBoxLink deadzoneLink;
		TrackBarUpDownTextBoxLink offsetLink;

		public void SetBinding(PadSetting o)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(EnabledCheckBox);
			SettingsManager.UnLoadMonitor(DeadZoneUpDown);
			SettingsManager.UnLoadMonitor(OffsetUpDown);
			if (o == null)
				return;
			// Set binding.
			var converter = new Converters.PaddSettingToIntegerConverter();
			SettingsManager.LoadAndMonitor(o, nameof(o.AxisToDPadEnabled), EnabledCheckBox);
			SettingsManager.LoadAndMonitor(o, nameof(o.AxisToDPadDeadZone), DeadZoneUpDown, null, converter);
			SettingsManager.LoadAndMonitor(o, nameof(o.AxisToDPadOffset), OffsetUpDown, null, converter);
		}
	}
}
