using System.Collections.Generic;

namespace x360ce.App.UiTree
{
	public static partial class UiText
	{
		/// <summary>The controls that shape one mapped value, reused on several pages.</summary>
		static void AddMapping(Dictionary<string, Text> d)
		{
			// One axis: a trigger or one direction of a stick. The frame is captioned with whichever
			// control it belongs to, which is meaningless in a description written once for all of
			// them, so it is named for what it holds instead.
			d["AxisMapUserControl.MainGroupBox"] = new Text("Mapped control settings",
				"Dead zone, anti-dead zone and sensitivity for this one control.");
			d["AxisMapUserControl.MainPictureBox"] = new Text("Response curve",
				"Draws what the game receives for every position of the control.");
			d["AxisMapUserControl.DeadZoneTextBox"] = new Text("Dead zone",
				"How far the control must move before the game sees anything. Removes drift at rest.");
			d["AxisMapUserControl.DeadZoneTrackBar"] = new Text("Dead zone",
				"How far the control must move before the game sees anything. Removes drift at rest.");
			d["AxisMapUserControl.DeadZoneNumericUpDown"] = new Text("Dead zone",
				"How far the control must move before the game sees anything. Removes drift at rest.");
			d["AxisMapUserControl.AntiDeadZoneTextBox"] = new Text("Anti-dead zone",
				"Skips past a dead zone the game applies of its own, so small movements are felt.");
			d["AxisMapUserControl.AntiDeadZoneTrackBar"] = new Text("Anti-dead zone",
				"Skips past a dead zone the game applies of its own, so small movements are felt.");
			d["AxisMapUserControl.AntiDeadZoneNumericUpDown"] = new Text("Anti-dead zone",
				"Skips past a dead zone the game applies of its own, so small movements are felt.");
			d["AxisMapUserControl.SensitivityTextBox"] = new Text("Sensitivity",
				"Bends the middle of the travel and leaves both ends where they are.");
			d["AxisMapUserControl.SensitivityTrackBar"] = new Text("Sensitivity",
				"Bends the middle of the travel and leaves both ends where they are.");
			d["AxisMapUserControl.SensitivityNumericUpDown"] = new Text("Sensitivity",
				"Bends the middle of the travel and leaves both ends where they are.");
			d["AxisMapUserControl.SensitivityCheckBox"] = new Text("Invert sensitivity",
				"Bends the middle the other way: more sensitive in the centre instead of less.");
			d["AxisMapUserControl.PresetMenuStrip"] = new Text("Ready-made settings",
				"Common dead zone and anti-dead zone combinations, applied in one click.");
			d["AxisMapUserControl.ApplyPresetMenuItem"] = new Text("Apply a ready-made setting",
				"Fills the three settings above from a common combination.");

			// A button worked by something that is not a button.
			d["AxisToButtonUserControl.MappedAxisTextBox"] = new Text("Mapped control",
				"Which control on your device works this button.");
			d["AxisToButtonUserControl.DeadZoneTextBox"] = new Text("Press point",
				"How far the control must move before this button counts as pressed.");
			d["AxisToButtonUserControl.DeadZoneTrackBar"] = new Text("Press point",
				"How far the control must move before this button counts as pressed.");
			d["AxisToButtonUserControl.DeadZoneNumericUpDown"] = new Text("Press point",
				"How far the control must move before this button counts as pressed.");

			// What the device says about itself.
			d["DirectInputUserControl.DeviceDetailsTabControl"] = new Text("Device detail pages",
				"What the device reports it can do.");
			d["DirectInputUserControl.MapToPadComboBox"] = new Text("Map to",
				"Which of the four emulated controllers this device works.");
			d["DirectInputUserControl.DiObjectsTabPage"] = new Text("Device Objects",
				"Every axis, button and hat the device reports having.");
			d["DirectInputUserControl.DiEffectsDataTabPage"] = new Text("Force Feedback Effects",
				"The vibration effects the device says it can produce.");
		}
	}
}
