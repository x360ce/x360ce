using System.Collections.Generic;

namespace x360ce.App.UiTree
{
	public static partial class UiText
	{
		/// <summary>The controller page: one of these exists per emulated Xbox controller.</summary>
		static void AddControllerPanel(Dictionary<string, Text> d)
		{
			d["PadControl.PadTabControl"] = new Text("Controller pages",
				"Settings for this controller, grouped by the part being mapped.");
			d["PadControl.GeneralTabPage"] = new Text("General",
				"Says which control on your device works each part of the Xbox controller.");
			d["PadControl.ButtonsTabPage"] = new Text("Buttons",
				"How far a stick or pedal must move before a button mapped to it counts as pressed.");
			d["PadControl.DPadTabPage"] = new Text("D-Pad",
				"Turns a stick or wheel into the directional pad.");
			d["PadControl.TriggersTabPage"] = new Text("Triggers",
				"Shapes what the game receives from the two triggers.");
			d["PadControl.LeftThumbTabPage"] = new Text("Left Thumb",
				"Shapes what the game receives from the left stick.");
			d["PadControl.RightThumbTabPage"] = new Text("Right Thumb",
				"Shapes what the game receives from the right stick.");
			d["PadControl.ForceFeedbackTabPage"] = new Text("Force Feedback",
				"Turns vibration on and sets how strong it is.");
			d["PadControl.DirectInputTabPage"] = new Text("Direct Input",
				"What the mapped device reports about itself, and its values as they change.");

			// General page.
			d["PadControl.LeftTriggerTextBox"] = new Text("Left trigger value",
				"What the game is being given for the left trigger right now.");
			d["PadControl.RightTriggerTextBox"] = new Text("Right trigger value",
				"What the game is being given for the right trigger right now.");
			d["PadControl.LeftThumbTextBox"] = new Text("Left stick value",
				"Across and up positions the game is being given for the left stick.");
			d["PadControl.RightThumbTextBox"] = new Text("Right stick value",
				"Across and up positions the game is being given for the right stick.");
			d["PadControl.MapNameComboBox"] = new Text("Button layout",
				"Names the buttons after the controller in your hands instead of an Xbox one.");
			d["PadControl.RemapAllButton"] = new Text("Remap All",
				"Clears the mapping, then asks you to press each control in turn.");

			// Buttons page.
			d["PadControl.AxisToButtonGroupBox"] = new Text("Press points",
				"For each button mapped to a stick or pedal, how far it must move to count as pressed.");

			// D-Pad page.
			d["PadControl.AxisToDPadGroupBox"] = new Text("Axis to D-Pad",
				"Turns one axis, a steering wheel among them, into the four pad directions.");
			d["PadControl.AxisToDPadEnabledCheckBox"] = new Text("Enabled",
				"Turns the axis into directional pad presses.");
			d["PadControl.AxisToDPadDeadZoneTextBox"] = new Text("Dead zone",
				"How far the axis must move from the centre before a direction is pressed.");
			d["PadControl.AxisToDPadDeadZoneTrackBar"] = new Text("Dead zone",
				"How far the axis must move from the centre before a direction is pressed.");
			d["PadControl.AxisToDPadOffsetTextBox"] = new Text("Centre offset",
				"Moves the point counted as centre, for a device that does not rest in the middle.");
			d["PadControl.AxisToDPadOffsetTrackBar"] = new Text("Centre offset",
				"Moves the point counted as centre, for a device that does not rest in the middle.");

			// Force feedback page.
			d["PadControl.ForceFeedbackGroupBox"] = new Text("Force feedback",
				"Vibration settings shared by both motors.");
			d["PadControl.ForceEnableCheckBox"] = new Text("Enable",
				"Passes vibration from the game to the device.");
			d["PadControl.ForceSwapMotorCheckBox"] = new Text("Swap Motors",
				"Sends each motor's vibration to the other one.");
			d["PadControl.ForceTypeComboBox"] = new Text("Effect type",
				"How vibration is produced: held steady, or repeated as a pulse.");
			d["PadControl.ForcePassThroughIndexComboBox"] = new Text("Pass through index",
				"Where force feedback is sent when it is passed through.");
			d["PadControl.ForceOverallTextBox"] = new Text("Overall strength",
				"Scales all vibration, so a device that shakes too hard can be calmed.");
			d["PadControl.ForceOverallTrackBar"] = new Text("Overall strength",
				"Scales all vibration, so a device that shakes too hard can be calmed.");
			d["PadControl.ForceSpringStrengthTextBox"] = new Text("Centering spring",
				"Holds a wheel at its centre all the time, for games that only send rumble. Nought is off.");
			d["PadControl.ForceSpringStrengthTrackBar"] = new Text("Centering spring",
				"Holds a wheel at its centre all the time, for games that only send rumble. Nought is off.");
			d["PadControl.InforTextBox"] = new Text("About force feedback",
				"Explains what the settings on this page do.");
			AddMotor(d, "Left", "LeftMotorGroupBox", "Left motor",
				"The big, slow motor, which produces the heavy rumble.");
			AddMotor(d, "Right", "groupBox1", "Right motor",
				"The small, fast motor, which produces the light buzz.");

			// Devices and presets.
			d["PadControl.MappedDevicesDataGridView"] = new Text("Mapped devices",
				"Devices standing in for this controller. Several can be combined into one.");
			d["PadControl.GamesToolStrip"] = new Text("Mapped device actions",
				"Adds, removes and enables the devices that work this controller.");
			d["PadControl.AddMapButton"] = new Text("Add...",
				"Chooses another device to work this controller.");
			d["PadControl.RemoveMapButton"] = new Text("Remove",
				"Stops the selected device working this controller.");
			d["PadControl.AutoMapButton"] = new Text("Auto Map",
				"Lets the program pick a device for this controller by itself when a game starts.");
			d["PadControl.EnableButton"] = new Text("Enable",
				"Turns this controller on or off for the selected game.");
			d["PadControl.GetXInputStatesCheckBox"] = new Text("Show XInput State",
				"Shows the values read back from XInput - what the game actually receives - instead "
				+ "of the values worked out from your device. The emulated controller works either way.");
			d["PadControl.GameControllersButton"] = new Text("Game Controllers...",
				"Opens the Windows game controller panel for the selected device.");
			d["PadControl.DxTweakButton"] = new Text("DX Tweak...",
				"Opens DX Tweak, a separate tool for adjusting the device itself.");
			d["PadControl.LoadPresetButton"] = new Text("Load Preset...",
				"Replaces this controller's settings with a saved set.");
			d["PadControl.AutoPresetButton"] = new Text("Auto Preset",
				"Fills the mapping from a preset that matches the selected device.");
			d["PadControl.SavePresetButton"] = new Text("Save Preset",
				"Stores the current settings as a preset you can load again.");
			d["PadControl.ClearPresetButton"] = new Text("Clear",
				"Empties every mapping on this controller.");
			d["PadControl.ResetPresetButton"] = new Text("Reset",
				"Puts every setting on this controller back to its default. Asks first.");
			d["PadControl.CopyPresetButton"] = new Text("Copy Preset",
				"Copies this controller's settings to the clipboard.");
			d["PadControl.PastePresetButton"] = new Text("Paste Preset",
				"Applies settings from the clipboard to this controller.");
		}

		/// <summary>Both motors carry the same settings, so both are described the same way.</summary>
		static void AddMotor(Dictionary<string, Text> d, string side, string groupField, string name, string purpose)
		{
			var lower = name.ToLower();
			d["PadControl." + groupField] = new Text(name, purpose);
			d["PadControl." + side + "MotorStrengthTextBox"] = new Text(name + " strength",
				"How hard this motor runs when the game asks for vibration.");
			d["PadControl." + side + "MotorStrengthTrackBar"] = new Text(name + " strength",
				"How hard this motor runs when the game asks for vibration.");
			d["PadControl." + side + "MotorPeriodTextBox"] = new Text(name + " period",
				"How long one pulse lasts, when the effect is repeated rather than held.");
			d["PadControl." + side + "MotorPeriodTrackBar"] = new Text(name + " period",
				"How long one pulse lasts, when the effect is repeated rather than held.");
			d["PadControl." + side + "MotorDirectionComboBox"] = new Text(name + " direction",
				"Which way a wheel is pushed by this motor.");
			d["PadControl." + side + "MotorTestTextBox"] = new Text("Test " + lower,
				"Runs this motor at the chosen strength, so you can feel it without a game.");
			d["PadControl." + side + "MotorTestTrackBar"] = new Text("Test " + lower,
				"Runs this motor at the chosen strength, so you can feel it without a game.");
		}
	}
}
