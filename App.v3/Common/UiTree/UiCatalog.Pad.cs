using System.Collections.Generic;
using static x360ce.Engine.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>
		/// A controller page: one exists per emulated Xbox controller. The boxes that map a part of
		/// the Xbox controller are named after their setting, so only the rest is listed here.
		/// </summary>
		static void AddControllerPage(Dictionary<string, Text> d)
		{
			d["PadControl"] = new Text("Controller page",
				"What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.");
			d["PadControl.PadTabControl"] = new Text("Controller pages",
				"The parts of this controller's settings, one page each.");
			d["PadControl.GeneralTabPage"] = new Text("General",
				"Which part of the device each button, trigger and stick of the Xbox controller reads.");
			d["PadControl.AdvancedTabPage"] = new Text("Advanced",
				"Device type and pass through, combining several devices into one controller, trigger dead zones, and the D-Pad from an axis.");
			d["PadControl.DirectInputTabPage"] = new Text("Direct Input",
				"What the device reports about itself, and its live state.");
			d["PadControl.LeftThumbTabPage"] = new Text("Left Thumb",
				"Dead zones and sensitivity of the left stick.");
			d["PadControl.RightThumbTabPage"] = new Text("Right Thumb",
				"Dead zones and sensitivity of the right stick.");
			d["PadControl.ForceFeedbackTabPage"] = new Text("Force Feedback",
				"How the game's vibration reaches the device's motors.");
			d["PadControl.AxisToButtonTabPage"] = new Text("Axis to Button",
				"How far an axis must move before a button mapped to it counts as pressed.");

			// Readings of what the emulated controller reports now.
			d["PadControl.LeftTriggerTextBox"] = Live("Left trigger now",
				"The value the emulated controller reports for the left trigger, 0 to 255.");
			d["PadControl.RightTriggerTextBox"] = Live("Right trigger now",
				"The value the emulated controller reports for the right trigger, 0 to 255.");
			d["PadControl.LeftThumbTextBox"] = Live("Left stick now",
				"The position the emulated controller reports for the left stick, as X;Y.");
			d["PadControl.RightThumbTextBox"] = Live("Right stick now",
				"The position the emulated controller reports for the right stick, as X;Y.");

			// The number beside each slider, which reads the slider out in its own units.
			d["PadControl.AxisToDPadDeadZoneTextBox"] = Live("D-Pad dead zone value", "The dead zone slider's setting.");
			d["PadControl.AxisToDPadOffsetTextBox"] = Live("D-Pad offset value", "The offset slider's setting.");
			d["PadControl.LeftTriggerDeadZoneTextBox"] = Live("Left trigger dead zone value", "The left trigger dead zone slider's setting.");
			d["PadControl.RightTriggerDeadZoneTextBox"] = Live("Right trigger dead zone value", "The right trigger dead zone slider's setting.");
			d["PadControl.ForceOverallTextBox"] = Live("Overall strength value", "The overall force feedback strength slider's setting.");
			d["PadControl.LeftMotorStrengthTextBox"] = Live("Left motor strength value", "The left motor strength slider's setting.");
			d["PadControl.RightMotorStrengthTextBox"] = Live("Right motor strength value", "The right motor strength slider's setting.");
			d["PadControl.LeftMotorPeriodTextBox"] = Live("Left motor period value", "The left motor period slider's setting, in milliseconds.");
			d["PadControl.RightMotorPeriodTextBox"] = Live("Right motor period value", "The right motor period slider's setting, in milliseconds.");

			// The buttons along the bottom.
			d["PadControl.AutoPresetButton"] = new Text("Auto",
				"Fills every mapping of this controller from what the device offers, after asking.");
			d["PadControl.ClearPresetButton"] = new Text("Clear",
				"Empties every mapping of this controller, after asking.");
			d["PadControl.ResetPresetButton"] = new Text("Reset",
				"Reloads this controller's settings from x360ce.ini, dropping what was not saved, after asking.");
			d["PadControl.SavePresetButton"] = new Text("Save",
				"Writes every setting to x360ce.ini now.");
			d["PadControl.GameControllersButton"] = new Text("Game Controllers...",
				"Opens the Windows Game Controllers panel.");
			d["PadControl.TestButton"] = new Text("Start Test",
				"Runs both motors at the strengths of the two motor sliders until pressed again.");
			d["PadControl.TestTimerNumericUpDown"] = new Text("Test resend interval",
				"Milliseconds between the test strengths being sent again, so a device that stops on its own keeps running.");

			// The shared parts placed on the pages above.
			d["ThumbUserControl"] = new Text("Stick axis",
				"Dead zone, anti dead zone and sensitivity of one axis of a stick.");
			d["ThumbUserControl.DeadZoneTrackBar"] = new Text("Dead zone",
				"How far the stick must move before the game sees it move, in per cent.");
			d["ThumbUserControl.DeadZoneNumericUpDown"] = new Text("Dead zone",
				"How far the stick must move before the game sees it move, in raw units.");
			d["ThumbUserControl.AntiDeadZoneTrackBar"] = new Text("Anti dead zone",
				"How far the game's own dead zone is skipped, so a small move is not lost, in per cent.");
			d["ThumbUserControl.AntiDeadZoneNumericUpDown"] = new Text("Anti dead zone",
				"How far the game's own dead zone is skipped, so a small move is not lost, in raw units.");
			d["ThumbUserControl.SensitivityTrackBar"] = new Text("Sensitivity",
				"How the stick's movement is curved: slower near the centre, or faster.");
			d["ThumbUserControl.SensitivityNumericUpDown"] = new Text("Sensitivity",
				"How the stick's movement is curved, as a number.");
			d["ThumbUserControl.SensitivityCheckBox"] = new Text("Invert",
				"Turns the sensitivity curve the other way.");
			d["ThumbUserControl.DeadZoneTextBox"] = Live("Dead zone value", "The dead zone slider's setting.");
			d["ThumbUserControl.AntiDeadZoneTextBox"] = Live("Anti dead zone value", "The anti dead zone slider's setting.");
			d["ThumbUserControl.SensitivityTextBox"] = Live("Sensitivity value", "The sensitivity slider's setting.");
			d["ThumbUserControl.PresetMenuStrip"] = new Text("Presets",
				"Common pairs of dead zone and anti dead zone.");
			d["ThumbUserControl.ApplyPresetMenuItem"] = new Text("Apply Preset",
				"Sets the dead zone and anti dead zone to one of the common pairs listed.");

			d["AxisToButtonUserControl"] = new Text("Axis to button",
				"How far an axis must move before the button mapped to it counts as pressed.");
			d["AxisToButtonUserControl.DeadZoneTrackBar"] = new Text("Dead zone",
				"How far the axis must move before the button counts as pressed, in per cent.");
			d["AxisToButtonUserControl.DeadZoneNumericUpDown"] = new Text("Dead zone",
				"How far the axis must move before the button counts as pressed, in raw units.");
			d["AxisToButtonUserControl.DeadZoneTextBox"] = Live("Dead zone value", "The dead zone slider's setting.");
			d["AxisToButtonUserControl.MappedAxisTextBox"] = Live("Mapped axis",
				"The axis this button is mapped to, from the General page.");
		}

		/// <summary>The Direct Input page: what the device says about itself.</summary>
		static void AddDeviceDetails(Dictionary<string, Text> d)
		{
			d["DirectInputControl"] = new Text("Device details",
				"What the device reports about itself, and its live state.");
			d["DirectInputControl.DeviceProductNameTextBox"] = new Text("Product", "Name the device gives itself.");
			d["DirectInputControl.DeviceVendorNameTextBox"] = new Text("Vendor", "Maker of the device.");
			d["DirectInputControl.DeviceVidTextBox"] = new Text("Vendor ID", "The maker's USB vendor number.");
			d["DirectInputControl.DevicePidTextBox"] = new Text("Product ID", "The device's USB product number.");
			d["DirectInputControl.DeviceTypeTextBox"] = new Text("Type", "Kind of device Windows reports: gamepad, wheel, joystick.");
			d["DirectInputControl.DeviceProductGuidTextBox"] = new Text("Product GUID", "Identifies the model of device.");
			d["DirectInputControl.DeviceInstanceGuidTextBox"] = new Text("Instance GUID", "Identifies this one device, and names its section in x360ce.ini.");
			d["DirectInputControl.DiCapAxesTextBox"] = new Text("Axes", "How many axes the device has.");
			d["DirectInputControl.DiCapButtonsTextBox"] = new Text("Buttons", "How many buttons the device has.");
			d["DirectInputControl.DiCapDPadsTextBox"] = new Text("D-Pads", "How many D-Pads the device has.");
			d["DirectInputControl.SlidersTextBox"] = new Text("Sliders", "How many sliders the device has.");
			d["DirectInputControl.ActuatorsTextBox"] = new Text("Actuators", "Axes that can give force feedback.");
			d["DirectInputControl.DiCapFfStateTextBox"] = new Text("Force feedback", "What the device says about its force feedback.");
			d["DirectInputControl.DiButtonsTextBox"] = Live("Buttons now", "Buttons of the device held down now.");
			d["DirectInputControl.DiDPadTextBox"] = Live("D-Pad now", "Where the device's D-Pad points now.");
			d["DirectInputControl.DiASliderTextBox"] = Live("Acceleration sliders now", "The device's acceleration sliders now.");
			d["DirectInputControl.DiFSliderTextBox"] = Live("Force sliders now", "The device's force sliders now.");
			d["DirectInputControl.DiUvSliderTextBox"] = Live("Sliders now", "The device's sliders now.");
			d["DirectInputControl.DiVSliderTextBox"] = Live("Velocity sliders now", "The device's velocity sliders now.");
			d["DirectInputControl.DiAxisDataGridView"] = new Text("Axes now", "Each axis of the device and where it is now.");
			d["DirectInputControl.DeviceDetailsTabControl"] = new Text("Device lists", "The device's parts and its force feedback effects.");
			d["DirectInputControl.DiObjectsDataGridView"] = new Text("Device objects", "Every button, axis and slider the device reports.");
			d["DirectInputControl.DiEffectsDataGridView"] = new Text("Force feedback effects", "The force feedback effects the device supports.");
			d["DirectInputControl.MapToPadComboBox"] = new Text("Map to",
				"Which emulated controller this device feeds.");
			d["DirectInputControl.CopyWithHeadersMenuItem"] = new Text("Copy with Headers",
				"Copies the list with its column names.");
		}
	}
}
