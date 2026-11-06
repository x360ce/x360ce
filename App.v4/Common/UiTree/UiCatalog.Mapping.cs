using System.Collections.Generic;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The controls that shape one mapped value, reused on several pages.</summary>
		static void AddMapping(Dictionary<string, Text> d)
		{
			// One axis: a trigger or one direction of a stick. The frame is captioned with whichever
			// control it belongs to, which is meaningless in a description written once for all of
			// them, so it is named for what it holds instead.
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.MainGroupBox)] = new Text("Mapped control settings",
				"Dead zone, anti-dead zone and sensitivity for this one control.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.MainPictureBox)] = new Text("Response curve",
				"Draws what the game receives for every position of the control.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.DeadZoneTextBox)] = new Text("Dead zone",
				"How far the control must move before the game sees anything. Removes drift at rest.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.DeadZoneTrackBar)] = new Text("Dead zone",
				"How far the control must move before the game sees anything. Removes drift at rest.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.DeadZoneNumericUpDown)] = new Text("Dead zone",
				"How far the control must move before the game sees anything. Removes drift at rest.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.AntiDeadZoneTextBox)] = new Text("Anti-dead zone",
				"Skips past a dead zone the game applies of its own, so small movements are felt.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.AntiDeadZoneTrackBar)] = new Text("Anti-dead zone",
				"Skips past a dead zone the game applies of its own, so small movements are felt.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.AntiDeadZoneNumericUpDown)] = new Text("Anti-dead zone",
				"Skips past a dead zone the game applies of its own, so small movements are felt.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.SensitivityTextBox)] = new Text("Sensitivity",
				"Bends the middle of the travel and leaves both ends where they are.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.SensitivityTrackBar)] = new Text("Sensitivity",
				"Bends the middle of the travel and leaves both ends where they are.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.SensitivityNumericUpDown)] = new Text("Sensitivity",
				"Bends the middle of the travel and leaves both ends where they are.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.SensitivityCheckBox)] = new Text("Invert sensitivity",
				"Bends the middle the other way: more sensitive in the centre instead of less.");
			d[nameof(AxisMapUserControl) + "." + nameof(AxisMapUserControl.PresetComboBox)] = new Text("Preset",
				"Fills the dead zone, anti-dead zone and sensitivity from a common combination as soon as one is picked; " +
				"\"Clear\" sets all three to nought. The list then shows \"Apply Preset\" again.");

			// A button worked by something that is not a button.
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.MappedAxisTextBox)] = new Text("Mapped control",
				"Which control on your device works this button.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.DeadZoneTextBox)] = new Text("Press point",
				"How far the control must move before this button counts as pressed.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.DeadZoneTrackBar)] = new Text("Press point",
				"How far the control must move before this button counts as pressed.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.DeadZoneNumericUpDown)] = new Text("Press point",
				"How far the control must move before this button counts as pressed.");

			// What the device says about itself.
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DeviceDetailsTabControl)] = new Text("Device detail pages",
				"What the device reports it can do.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiObjectsTabPage)] = new Text("Device Objects",
				"Every axis, button and hat the device reports having.");
			d[nameof(DirectInputUserControl) + "." + nameof(DirectInputUserControl.DiEffectsDataTabPage)] = new Text("Force Feedback Effects",
				"The vibration effects the device says it can produce.");
		}
	}
}
