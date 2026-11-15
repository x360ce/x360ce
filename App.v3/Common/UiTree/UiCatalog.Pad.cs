using System.Collections.Generic;
using x360ce.App.Controls;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

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
			d[nameof(PadControl)] = new Text("Controller page",
				"What one emulated Xbox controller is made of: which device feeds it and how each part is mapped.");
			d[nameof(PadControl) + "." + nameof(PadControl.PadTabControl)] = new Text("Controller pages",
				"The parts of this controller's settings, one page each.");
			d[nameof(PadControl) + "." + nameof(PadControl.GeneralTabPage)] = new Text("General",
				"Which part of the device each button, trigger and stick of the Xbox controller reads.");
			d[nameof(PadControl) + "." + nameof(PadControl.AdvancedTabPage)] = new Text("Advanced",
				"Device type and pass through, combining several devices into one controller, trigger dead zones, and the D-Pad from an axis.");
			d[nameof(PadControl) + "." + nameof(PadControl.DirectInputTabPage)] = new Text("Direct Input",
				"What the device reports about itself, and its live state.");
			d[nameof(PadControl) + "." + nameof(PadControl.LeftThumbTabPage)] = new Text("Left Thumb",
				"Dead zones and sensitivity of the left stick.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightThumbTabPage)] = new Text("Right Thumb",
				"Dead zones and sensitivity of the right stick.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceFeedbackTabPage)] = new Text("Force Feedback",
				"How the game's vibration reaches the device's motors.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToButtonTabPage)] = new Text("Axis to Button",
				"How far an axis must move before a button mapped to it counts as pressed.");

			// Readings of what the emulated controller reports now.
			d[nameof(PadControl) + "." + nameof(PadControl.LeftTriggerTextBox)] = Live("Left trigger now",
				"The value the emulated controller reports for the left trigger, 0 to 255.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightTriggerTextBox)] = Live("Right trigger now",
				"The value the emulated controller reports for the right trigger, 0 to 255.");
			d[nameof(PadControl) + "." + nameof(PadControl.LeftThumbTextBox)] = Live("Left stick now",
				"The position the emulated controller reports for the left stick, as X;Y.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightThumbTextBox)] = Live("Right stick now",
				"The position the emulated controller reports for the right stick, as X;Y.");

			// The number beside each slider, which reads the slider out in its own units.
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadDeadZoneTextBox)] = Live("D-Pad dead zone value", "The dead zone slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadOffsetTextBox)] = Live("D-Pad offset value", "The offset slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.LeftTriggerDeadZoneTextBox)] = Live("Left trigger dead zone value", "The left trigger dead zone slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightTriggerDeadZoneTextBox)] = Live("Right trigger dead zone value", "The right trigger dead zone slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceOverallTextBox)] = Live("Overall strength value", "The overall force feedback strength slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.LeftMotorStrengthTextBox)] = Live("Left motor strength value", "The left motor strength slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightMotorStrengthTextBox)] = Live("Right motor strength value", "The right motor strength slider's setting.");
			d[nameof(PadControl) + "." + nameof(PadControl.LeftMotorPeriodTextBox)] = Live("Left motor period value", "The left motor period slider's setting, in milliseconds.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightMotorPeriodTextBox)] = Live("Right motor period value", "The right motor period slider's setting, in milliseconds.");

			// The buttons along the bottom.
			d[nameof(PadControl) + "." + nameof(PadControl.AutoPresetButton)] = new Text("Auto",
				"Fills this controller from x360ce's controller database, with SDL's mapping where it maps nothing, when either knows the device; otherwise its mapping from what the device offers. After asking.");
			d[nameof(PadControl) + "." + nameof(PadControl.ClearPresetButton)] = new Text("Clear",
				"Empties every mapping of this controller, after asking.");
			d[nameof(PadControl) + "." + nameof(PadControl.ResetPresetButton)] = new Text("Reset",
				"Reloads this controller's settings from x360ce.ini, dropping what was not saved, after asking.");
			d[nameof(PadControl) + "." + nameof(PadControl.SavePresetButton)] = new Text("Save",
				"Writes every setting to x360ce.ini now.");
			d[nameof(PadControl) + "." + nameof(PadControl.GameControllersButton)] = new Text("Game Controllers...",
				"Opens the Windows Game Controllers panel.");
			d[nameof(PadControl) + "." + nameof(PadControl.TestButton)] = new Text("Start Test",
				"Runs both motors at the strengths of the two motor sliders until pressed again.");
			d[nameof(PadControl) + "." + nameof(PadControl.TestTimerNumericUpDown)] = new Text("Test resend interval",
				"Milliseconds between the test strengths being sent again, so a device that stops on its own keeps running.");

			// The shared parts placed on the pages above.
			d[nameof(ThumbUserControl)] = new Text("Stick axis",
				"Dead zone, anti dead zone and sensitivity of one axis of a stick.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.DeadZoneTrackBar)] = new Text("Dead zone",
				"How far the stick must move before the game sees it move, in per cent.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.DeadZoneNumericUpDown)] = new Text("Dead zone",
				"How far the stick must move before the game sees it move, in raw units.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.AntiDeadZoneTrackBar)] = new Text("Anti dead zone",
				"How far the game's own dead zone is skipped, so a small move is not lost, in per cent.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.AntiDeadZoneNumericUpDown)] = new Text("Anti dead zone",
				"How far the game's own dead zone is skipped, so a small move is not lost, in raw units.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.SensitivityTrackBar)] = new Text("Sensitivity",
				"How the stick's movement is curved: slower near the centre, or faster.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.SensitivityNumericUpDown)] = new Text("Sensitivity",
				"How the stick's movement is curved, as a number.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.SensitivityCheckBox)] = new Text("Invert",
				"Turns the sensitivity curve the other way.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.DeadZoneTextBox)] = Live("Dead zone value", "The dead zone slider's setting.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.AntiDeadZoneTextBox)] = Live("Anti dead zone value", "The anti dead zone slider's setting.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.SensitivityTextBox)] = Live("Sensitivity value", "The sensitivity slider's setting.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.PresetMenuStrip)] = new Text("Presets",
				"Common pairs of dead zone and anti dead zone.");
			d[nameof(ThumbUserControl) + "." + nameof(ThumbUserControl.ApplyPresetMenuItem)] = new Text("Apply Preset",
				"Sets the dead zone and anti dead zone to one of the common pairs listed.");

			d[nameof(AxisToButtonUserControl)] = new Text("Axis to button",
				"How far an axis must move before the button mapped to it counts as pressed.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.DeadZoneTrackBar)] = new Text("Dead zone",
				"How far the axis must move before the button counts as pressed, in per cent.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.DeadZoneNumericUpDown)] = new Text("Dead zone",
				"How far the axis must move before the button counts as pressed, in raw units.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.DeadZoneTextBox)] = Live("Dead zone value", "The dead zone slider's setting.");
			d[nameof(AxisToButtonUserControl) + "." + nameof(AxisToButtonUserControl.MappedAxisTextBox)] = Live("Mapped axis",
				"The axis this button is mapped to, from the General page.");
		}

		/// <summary>The Direct Input page: what the device says about itself.</summary>
		static void AddDeviceDetails(Dictionary<string, Text> d)
		{
			d[nameof(DirectInputControl)] = new Text("Device details",
				"What the device reports about itself, and its live state.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceProductNameTextBox)] = new Text("Product", "Name the device gives itself.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceVendorNameTextBox)] = new Text("Vendor", "Maker of the device.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceVidTextBox)] = new Text("Vendor ID", "The maker's USB vendor number.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DevicePidTextBox)] = new Text("Product ID", "The device's USB product number.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceTypeTextBox)] = new Text("Type", "Kind of device Windows reports: gamepad, wheel, joystick.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceProductGuidTextBox)] = new Text("Product GUID", "Identifies the model of device.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceInstanceGuidTextBox)] = new Text("Instance GUID", "Identifies this one device, and names its section in x360ce.ini.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiCapAxesTextBox)] = new Text("Axes", "How many axes the device has.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiCapButtonsTextBox)] = new Text("Buttons", "How many buttons the device has.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiCapDPadsTextBox)] = new Text("D-Pads", "How many D-Pads the device has.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.SlidersTextBox)] = new Text("Sliders", "How many sliders the device has.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.ActuatorsTextBox)] = new Text("Actuators", "Axes that can give force feedback.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiCapFfStateTextBox)] = new Text("Force feedback", "What the device says about its force feedback.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiButtonsTextBox)] = Live("Buttons now", "Buttons of the device held down now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiDPadTextBox)] = Live("D-Pad now", "Where the device's D-Pad points now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiASliderTextBox)] = Live("Acceleration sliders now", "The device's acceleration sliders now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiFSliderTextBox)] = Live("Force sliders now", "The device's force sliders now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiUvSliderTextBox)] = Live("Sliders now", "The device's sliders now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiVSliderTextBox)] = Live("Velocity sliders now", "The device's velocity sliders now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiAxisDataGridView)] = new Text("Axes now", "Each axis of the device and where it is now.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DeviceDetailsTabControl)] = new Text("Device lists", "The device's parts and its force feedback effects.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiObjectsDataGridView)] = new Text("Device objects", "Every button, axis and slider the device reports.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.DiEffectsDataGridView)] = new Text("Force feedback effects", "The force feedback effects the device supports.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.MapToPadComboBox)] = new Text("Map to",
				"Which emulated controller this device feeds.");
			d[nameof(DirectInputControl) + "." + nameof(DirectInputControl.CopyWithHeadersMenuItem)] = new Text("Copy with Headers",
				"Copies the list with its column names.");
		}
	}
}
