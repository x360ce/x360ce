using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using x360ce.Engine.Data;

namespace x360ce.App.Controls
{
	/// <summary>
	/// Interaction logic for DPadControl.xaml
	/// </summary>
	public partial class PadItem_DPadControl : UserControl
	{
		public PadItem_DPadControl()
		{
			InitHelper.InitTimer(this, InitializeComponent);
			deadzoneLink = new TrackBarUpDownTextBoxLink(DeadZoneTrackBar, DeadZoneUpDown, DeadZoneTextBox, 0, 100);
			offsetLink = new TrackBarUpDownTextBoxLink(OffsetTrackBar, OffsetUpDown, OffsetTextBox, 0, 100);
		}

		TrackBarUpDownTextBoxLink deadzoneLink;
		TrackBarUpDownTextBoxLink offsetLink;

		public void SetBinding(PadSetting ps)
		{
			// Unbind first.
			SettingsManager.UnLoadMonitor(EnabledCheckBox);
			SettingsManager.UnLoadMonitor(DeadZoneUpDown);
			SettingsManager.UnLoadMonitor(OffsetUpDown);
			if (ps == null)
				return;
			// Set binding.
			var intConverter = new Converters.PadSettingToIntegerConverter();
			var boolConverter = new Converters.PadSettingToBoolConverter();
			SettingsManager.LoadAndMonitor(ps, nameof(ps.AxisToDPadEnabled), EnabledCheckBox, null, boolConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.AxisToDPadDeadZone), DeadZoneUpDown, null, intConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.AxisToDPadOffset), OffsetUpDown, null, intConverter);
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			deadzoneLink.Dispose();
			offsetLink.Dispose();
			SetBinding(null);
		}
	}
}
