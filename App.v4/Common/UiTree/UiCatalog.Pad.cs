using System.Collections.Generic;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
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
			d["PadControl.AdvancedTabPage"] = new Text("Advanced",
				"Settings only x360ce 3's XInput library reads, from the x360ce.ini file this version does not write. "
				+ "Greyed out, and kept with the settings so x360ce 3's settings in the cloud stay as they are.");
			d["PadControl.DeviceGroupBox"] = new Text("Device",
				"What x360ce 3's library told a game about the device, and whether it passed the real controller straight through.");
			// The settings these are bound to describe them.
			d["PadControl.PassThroughIndexComboBox"] = new Text("Pass Through ID", null);
			d["PadControl.DeviceSubTypeComboBox"] = new Text("Device Sub Type", null);
			d["PadControl.CombineGroupBox"] = new Text("Combining",
				"x360ce 3 only: combined this controller into another virtual controller.");
			d["PadControl.CombinedIndexComboBox"] = new Text("Combine Into", null);
			d["PadControl.DPadTabPage"] = new Text("D-Pad",
				"Axis to D-Pad, which only x360ce 3's XInput library reads. Greyed out, and kept with the settings.");
			d["PadControl.TriggersTabPage"] = new Text("Triggers",
				"Shapes what the game receives from the two triggers.");
			d["PadControl.LeftThumbTabPage"] = new Text("Left Thumb",
				"Shapes what the game receives from the left stick.");
			d["PadControl.RightThumbTabPage"] = new Text("Right Thumb",
				"Shapes what the game receives from the right stick.");
			d["PadControl.ForceFeedbackTabPage"] = new Text("Force Feedback",
				"Turns vibration on and sets how strong it is.");
			d["PadControl.WheelTabPage"] = new Text("Wheel",
				"The centering spring that holds a wheel at its centre in games that send no steering force, with a chart of its force at every angle, and the wheel's steering range.");
			d["PadControl.DirectInputTabPage"] = new Text("Direct Input",
				"What the mapped device reports about itself, and its values as they change. Titled Raw Input for a device read through Raw Input.");

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
				"Turns the device's X and Y axes, its main stick, into the four D-Pad directions, as well as whatever else the D-Pad is mapped to: "
				+ "for a pad whose direction buttons report as a stick. The stick's own mappings stay as they are.");
			d["PadControl.AxisToDPadEnabledCheckBox"] = new Text("Enabled",
				"Turns the X and Y axes into D-Pad presses: right and left from X, down and up from Y, two at once on a diagonal.");
			d["PadControl.AxisToDPadDeadZoneTextBox"] = new Text("Dead zone",
				"How far an axis must move from the centre before a direction is pressed. 100 % presses close to the end, where a pad's direction buttons put it even when it falls a little short.");
			d["PadControl.AxisToDPadDeadZoneTrackBar"] = new Text("Dead zone",
				"How far an axis must move from the centre before a direction is pressed. 100 % presses close to the end, where a pad's direction buttons put it even when it falls a little short.");
			d["PadControl.AxisToDPadOffsetTextBox"] = new Text("Centre offset",
				"Moves the point counted as centre towards the right and the bottom, as part of half the travel, for a device that does not rest in the middle.");
			d["PadControl.AxisToDPadOffsetTrackBar"] = new Text("Centre offset",
				"Moves the point counted as centre towards the right and the bottom, as part of half the travel, for a device that does not rest in the middle.");
			d["PadControl.AxisToDPadChart"] = new Text("Axis to D-Pad chart",
				"The X and Y axes as a square: the shaded sides are where a direction is pressed, the dot is the device's reading, and a side it presses is lit.");

			// Force feedback page.
			d["PadControl.ForceFeedbackGroupBox"] = new Text("Force feedback",
				"Vibration settings shared by both motors.");
			d["PadControl.EffectDescriptionLabel"] = Live("Effect description",
				"What the chosen effect type does, or, for a device read through Raw Input whose DirectInput side takes no force feedback, that none reaches it.");
			d["PadControl.ForceSourceGroupBox"] = new Text("Force source",
				"Where the selected controller's force comes from. Shown only while the controller, or its twin read through another source, is on another controller tab too.");
			d["PadControl.ForceSourceLabel"] = Live("Force source",
				"Which controller tab's settings shape the controller's force, the lowest enabled tab it is on, and which controllers' force it plays: "
				+ "on several, the strongest of each motor. Or why no force reaches it. On every other tab the settings that shape the force are greyed out.");
			d["PadControl.ForceEnableCheckBox"] = new Text("Enable",
				"Passes this controller's vibration from the game to the device. A device on several tabs feels every tab where this switch is also on, the strongest of each motor.");
			d["PadControl.ForceSwapMotorCheckBox"] = new Text("Swap Motors",
				"Sends each motor's vibration to the other one.");
			d["PadControl.ForceTypeComboBox"] = new Text("Effect type",
				"How vibration is produced: held steady, or repeated as a pulse.");
			d["PadControl.ForceOverallTextBox"] = new Text("Overall strength",
				"Scales all vibration, so a device that shakes too hard can be calmed.");
			d["PadControl.ForceOverallTrackBar"] = new Text("Overall strength",
				"Scales all vibration, so a device that shakes too hard can be calmed.");
			d["PadControl.MotorPeriodPresetComboBox"] = new Text("Motor periods",
				"Sets both motor periods to the measured motors played this many times slower; 4x suits most wheels. Greyed out under Constant, which has no period.");
			d["PadControl.MotorInfoButton"] = new Text("About the motors",
				"Shows what each motor is used for, their measured speeds, and what the multipliers mean.");
			d["PadControl.ForceDefaultsButton"] = new Text("Defaults",
				"Puts every setting on this page back to its default.");
			d["PadControl.ForcePassThroughIndexComboBox"] = new Text("Pass through to",
				"Which XInput place receives the game's vibration when it is passed through.");
			d["PadControl.InforTextBox"] = new Text("About force feedback",
				"Explains what the settings on this page do.");
			AddMotor(d, "Left", "LeftMotorGroupBox", "Left motor",
				"The big, slow motor, which produces the heavy rumble.");
			AddMotor(d, "Right", "groupBox1", "Right motor",
				"The small, fast motor, which produces the light buzz.");

			// Wheel page.
			d["PadControl.ForceSpringEnableCheckBox"] = new Text("Centering spring",
				"Turns the centering spring on. Off by default, so a wheel's own software can hold the centre without the two fighting.");
			d["PadControl.ForceSpringCarComboBox"] = new Text("Car",
				"Fills Centre, Peak, Lightening, Damping, Speed and Wheel range with a kind of car's values, and shows the car the values match, or Custom. "
				+ "No standard sets how a steering wheel must feel, so each car names what it is based on: the family car at 100 km/h is worked out "
				+ "from measurements in the ISO 13674-1 weave test; in town and parking are the same car at 40 and 10 km/h; the sports car and the "
				+ "heavy van are estimates at the firm and slow ends of the range measured in that test. Each is a road car, so each sets the Wheel range "
				+ "for road and sports cars. Friction and Strength are left as they are, and the car is not saved.");
			d["PadControl.ForceSpringFrictionTextBox"] = d["PadControl.ForceSpringFrictionTrackBar"] = new Text("Friction",
				"The least force that turns this wheel and brings it home, in percent of its force. The spring reaches it a few degrees from the centre "
				+ "and never pushes less beyond, so the wheel comes home from any angle, with every car. 20 % until Auto measures it and adds a tenth. A geared or belt wheel needs much, a G27 about 30 %; "
				+ "a direct drive wheel turns freely and needs little, so lower it by hand when the wheel comes home harder than you like. Kept with the wheel. "
				+ "Greyed out while no wheel is selected.");
			d["PadControl.ForceSpringStrengthTextBox"] = d["PadControl.ForceSpringStrengthTrackBar"] = new Text("Strength",
				"The car's steering force at Peak, in percent of the wheel's force, added to Friction. Nought is off.");
			d["PadControl.ForceSpringCentreTextBox"] = d["PadControl.ForceSpringCentreTrackBar"] = new Text("Centre",
				"The steering angle in degrees at which the spring reaches half its strength, for the car at 100 km/h. Smaller is firmer on the centre. Widened when the wheel could not follow so steep a spring.");
			d["PadControl.ForceSpringPeakTextBox"] = d["PadControl.ForceSpringPeakTrackBar"] = new Text("Peak",
				"The steering angle in degrees of the spring's strongest force, for the car at 100 km/h. Beyond it the force eases off towards the stop. Stop puts the strongest force at the stop.");
			d["PadControl.ForceSpringLighteningTextBox"] = d["PadControl.ForceSpringLighteningTrackBar"] = new Text("Lightening",
				"How much of the spring's strongest force is left at the stop, in percent.");
			d["PadControl.ForceSpringDampingTextBox"] = d["PadControl.ForceSpringDampingTrackBar"] = new Text("Damping",
				"Resistance to the wheel's speed, the same at every angle, in percent of what the wheel can do. Calms a wheel that swings past the centre.");
			d["PadControl.ForceSpringSpeedTextBox"] = d["PadControl.ForceSpringSpeedTrackBar"] = new Text("Speed",
				"The car's speed in km/h the spring is read at. Slower spreads the force over a wider angle. Below 10 counts as 10. "
				+ "A program that knows the car's speed, such as one reading a game's own data, can later set it live, without saving it.");
			d["PadControl.ForceSpringAutoButton"] = new Text("Auto",
				"Measures the wheel's friction from both stops, with hands off the wheel, sets Friction to it and a tenth more, and checks that the spring then brings the wheel home without swinging. Strength is left as it is.");
			d["PadControl.WheelRangeComboBox"] = new Text("Wheel range",
				"Degrees lock to lock sent to a Logitech wheel, which powers up at 200, and the range the spring's angles are counted against. "
				+ "Sent while this tab drives the wheel: Enable on the Force Feedback page, or Centering spring. Leave as it is sends nothing, "
				+ "and is for a wheel without the centering spring: ticking the spring picks 900. Any other wheel turns the range its own software sets; "
				+ "the page then says so, and a formula in the steering box, such as =a1*900/270, steers a shorter range.");
			d["PadControl.WheelDescriptionLabel"] = Live("Wheel message",
				"What Auto is doing and what it found, why the centering spring does nothing now, or that x360ce cannot set this wheel's range.");

			// The chart on the Wheel page.
			d["WheelChartUserControl.MainPictureBox"] = new Text("Centering chart",
				"The red curve is the spring's force towards the centre at every angle, from one stop to the other, as it is played: above the middle "
				+ "it pushes right, below it pushes left. The grey band is Friction, forces too weak to turn the wheel: the curve stays above it everywhere "
				+ "but a few degrees round the centre, where it rises gently so the wheel settles there instead of shaking. The crosshair and the dot are "
				+ "the wheel's angle and force now.");
			d["WheelChartUserControl.AngleValueLabel"] = Live("Wheel angle",
				"The wheel's angle now, in degrees from the centre; negative is to the left.");
			d["WheelChartUserControl.ForceValueLabel"] = Live("Spring force",
				"The spring's force at that angle, in percent of the wheel's force, and which way it pushes.");

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
				"Not available yet, so greyed out: the program does not map devices by itself. Add maps a device.");
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
			d["PadControl.SavePresetButton"] = new Text("Save Preset...",
				"Writes this controller's settings to a file, to load again or pass on.");
			d["PadControl.ClearPresetButton"] = new Text("Clear",
				"Empties every mapping on this controller.");
			d["PadControl.ResetPresetButton"] = new Text("Reset",
				"Puts every setting on this controller back to its default. Asks first.");
			d["PadControl.CopyPresetButton"] = new Text("Copy Preset",
				"Copies this controller's settings to the clipboard as YAML, the easiest to read.");
			d["PadControl.CopyPresetFormatButton"] = new Text("Copy Preset as",
				"Copies this controller's settings in the format you choose: YAML, XML or JSON.");
			d["PadControl.PastePresetButton"] = new Text("Paste Preset",
				"Applies settings from the clipboard to this controller; YAML, XML and JSON are recognised.");
			d["LoadPresetsForm.OpenFileButton"] = new Text("Open File...",
				"Loads a preset from a file written by Save Preset.");
			d["LoadPresetsForm.CopyPresetButton"] = new Text("Copy Preset",
				"Copies the selected preset to the clipboard as YAML, to read it before loading.");
			d["LoadPresetsForm.CopyPresetFormatButton"] = new Text("Copy Preset as",
				"Copies the selected preset in the format you choose: YAML, XML or JSON.");
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
				"How long one pulse of the Sine or Sawtooth effect lasts. Greyed out under Constant, which has no period.");
			d["PadControl." + side + "MotorPeriodTrackBar"] = new Text(name + " period",
				"How long one pulse of the Sine or Sawtooth effect lasts. Greyed out under Constant, which has no period.");
			d["PadControl." + side + "MotorDirectionComboBox"] = new Text(name + " direction",
				"Which way a wheel is pushed by this motor.");
			d["PadControl." + side + "MotorTestTextBox"] = new Text("Test " + lower,
				"Runs this motor at the chosen strength, so you can feel it without a game.");
			d["PadControl." + side + "MotorTestTrackBar"] = new Text("Test " + lower,
				"Runs this motor at the chosen strength, so you can feel it without a game.");
		}
	}
}
