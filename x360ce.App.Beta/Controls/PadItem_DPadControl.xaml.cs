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
			var intConverter = new Converters.PadSettingToNumericConverter<decimal>();
			var boolConverter = new Converters.PadSettingToBoolConverter();
			SettingsManager.LoadAndMonitor(ps, nameof(ps.AxisToDPadEnabled), EnabledCheckBox, null, boolConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.AxisToDPadDeadZone), DeadZoneUpDown, null, intConverter);
			SettingsManager.LoadAndMonitor(ps, nameof(ps.AxisToDPadOffset), OffsetUpDown, null, intConverter);
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowLoad(this))
				return;
		}

		private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (!ControlsHelper.AllowUnload(this))
				return;
			// Moved to MainBodyControl_Unloaded().
		}

		public void ParentWindow_Unloaded()
		{
			// TODO: Lines below must be executed onbmly when main window close.
			deadzoneLink.Dispose();
			offsetLink.Dispose();
			SetBinding(null);
		}

	}
}
