using System.Collections.Generic;
using System.Linq;
using x360ce.App.Controls;
using x360ce.Engine;
using static JocysCom.ClassLibrary.Controls.UiTree.UiText;

namespace x360ce.App.UiTree
{
	public static partial class UiCatalog
	{
		/// <summary>The controller page: one of these exists per emulated Xbox controller.</summary>
		static void AddControllerPanel(Dictionary<string, Text> d)
		{
			d[nameof(PadControl) + "." + nameof(PadControl.PadTabControl)] = new Text("Controller pages",
				"Settings for this controller, grouped by the part being mapped.");
			d[nameof(PadControl) + "." + nameof(PadControl.GeneralTabPage)] = new Text("General",
				"Says which control on your device works each part of the Xbox controller.");
			d[nameof(PadControl) + "." + nameof(PadControl.ButtonsTabPage)] = new Text("Buttons",
				"How far a stick or pedal must move before a button mapped to it counts as pressed.");
			d[nameof(PadControl) + "." + nameof(PadControl.AdvancedTabPage)] = new Text("Advanced",
				"Settings only x360ce 3's XInput library reads, from the x360ce.ini file this version does not write. "
				+ "Greyed out, and kept with the settings so x360ce 3's settings in the cloud stay as they are.");
			d[nameof(PadControl) + "." + nameof(PadControl.DeviceGroupBox)] = new Text("Device",
				"What x360ce 3's library told a game about the device, and whether it passed the real controller straight through.");
			// The settings these are bound to describe them.
			d[nameof(PadControl) + "." + nameof(PadControl.PassThroughIndexComboBox)] = new Text("Pass Through ID", null);
			d[nameof(PadControl) + "." + nameof(PadControl.DeviceSubTypeComboBox)] = new Text("Device Sub Type", null);
			d[nameof(PadControl) + "." + nameof(PadControl.CombineGroupBox)] = new Text("Combining",
				"x360ce 3 only: combined this controller into another virtual controller.");
			d[nameof(PadControl) + "." + nameof(PadControl.CombinedIndexComboBox)] = new Text("Combine Into", null);
			d[nameof(PadControl) + "." + nameof(PadControl.DPadTabPage)] = new Text("D-Pad",
				"Axis to D-Pad, which only x360ce 3's XInput library reads. Greyed out, and kept with the settings.");
			d[nameof(PadControl) + "." + nameof(PadControl.TriggersTabPage)] = new Text("Triggers",
				"Shapes what the game receives from the two triggers.");
			d[nameof(PadControl) + "." + nameof(PadControl.LeftThumbTabPage)] = new Text("Left Stick",
				"Shapes what the game receives from the left stick.");
			d[nameof(PadControl) + "." + nameof(PadControl.RightThumbTabPage)] = new Text("Right Stick",
				"Shapes what the game receives from the right stick.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceFeedbackTabPage)] = new Text("Force Feedback",
				"Turns vibration on and sets how strong it is.");
			d[nameof(PadControl) + "." + nameof(PadControl.WheelTabPage)] = new Text("Wheel",
				"The centering spring that holds a wheel at its centre in games that send no steering force, with a chart of its force at every angle, and the wheel's steering range.");
			d[nameof(PadControl) + "." + nameof(PadControl.MouseTabPage)] = new Text("Mouse",
				"How a mouse, or any control that reports movement, moves each stick: by how far it moved, how fast it moves, or where the pointer is, always or while a button is held.");
			d[nameof(PadControl) + "." + nameof(PadControl.MouseNoteLabel)] = new Text("What the Mouse page sets",
				"The mouse's axes are chosen in the stick's boxes on the General page; this page sets how they move the stick.");
			d[nameof(MouseStickUserControl)] = new Text(null,
				"How a mouse moves one stick.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.StickGroupBox)] = new Text(null,
				"How a mouse moves this stick.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.StyleComboBox)] = new Text("Style",
				"Position: the stick follows how far the mouse has moved, and stays there, for steering or flying. Speed: the stick follows how fast the mouse moves, and returns to the centre when it stops, for a camera or aiming. Screen: the stick follows where the pointer is on the screen, as a mouse yoke.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.SensitivityXTrackBar)] = new Text("Sensitivity across",
				"How far the stick moves for a movement across, 1 to 400 percent. 100 is the whole stick for 2,048 counts in Position, for 4,096 counts a second in Speed, and half the screen's height in Screen.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.SensitivityXTextBox)] = Live("Sensitivity across value",
				"The sensitivity across the slider is set to, in percent.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.SensitivityYTrackBar)] = new Text("Sensitivity up and down",
				"How far the stick moves for a movement up or down, 1 to 400 percent, measured as across.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.SensitivityYTextBox)] = Live("Sensitivity up and down value",
				"The sensitivity up and down the slider is set to, in percent.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.SmoothingTrackBar)] = new Text("Smoothing",
				"How long the stick takes to follow a change in Speed and Screen, 0 to 500 milliseconds, which evens out a mouse's uneven reports. Greyed out in Position.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.SmoothingTextBox)] = Live("Smoothing value",
				"The smoothing the slider is set to, in milliseconds.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.ReturnTrackBar)] = new Text("Back to centre",
				"How long the stick takes to go back to the centre, 0 to 2,000 milliseconds. In Position, 0 leaves it where the mouse left it. Greyed out in Screen.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.ReturnTextBox)] = Live("Back to centre value",
				"The time back to the centre the slider is set to, in milliseconds.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.ActiveComboBox)] = new Text("Active",
				"When the mouse moves the stick: always, while the button is held, then back to the centre or staying where it was, or turned on and off by the button.");
			d[nameof(MouseStickUserControl) + "." + nameof(MouseStickUserControl.ButtonComboBox)] = new Text("Button",
				"The button of the same device that turns the mouse on the stick on and off. Greyed out while Active is Always.");
			d[nameof(PadControl) + "." + nameof(PadControl.DirectInputTabPage)] = new Text("Direct Input",
				"What the mapped device reports about itself, and its values as they change. Titled Raw Input for a device read through Raw Input.");
			d[nameof(PadControl) + "." + nameof(PadControl.SdlTabPage)] = new Text("SDL",
				"What SDL's controller database says about the selected device, place by place, beside what this controller's boxes hold, and which copy of the database is in use.");
			d[nameof(SdlUserControl)] = new Text(null,
				"What SDL's controller database says about the selected device.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.VersionLabel)] = Live("SDL database version",
				"Which copy of SDL's controller database is in use: built in, or downloaded and from where, its date and commit, and how many Windows rows it holds; and any rows from a file in the settings folder or from SDL_GAMECONTROLLERCONFIG.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.DeviceLabel)] = Live("SDL's row for the device",
				"Whether SDL's controller database knows the selected device, its name and GUID there, how the pad labels its buttons, and what of the row x360ce has no place for.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.PositionsDataGridView)] = new Text("Places",
				"Each place on the Xbox controller: SDL's name for it, the place in words, its label on this pad, the SDL row's binding, the device's control that binding names in x360ce's words, "
				+ "and what the box holds now, with Same or Differs beside it.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.ApplyButton)] = new Text("Apply SDL Mapping",
				"Fills every mapping box of this controller from SDL's row for the device, and empties those the row leaves out. Dead zones and the other settings stay as they are.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.CopyButton)] = new Text("Copy as SDL",
				"Copies this controller's mapping to the clipboard as a row of SDL's controller database for the device: its buttons, axes and hats, without formulas or other settings.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.UpdateButton)] = new Text("Update SDL Database",
				"Fetches the newest SDL controller database into the settings folder from the address Options > Internet sets, GitHub unless you choose another. The only time x360ce asks the internet for it.");
			d[nameof(SdlUserControl) + "." + nameof(SdlUserControl.StatusLabel)] = Live("SDL update status",
				"How the last update of SDL's controller database went.");

			// General page.
			AddGeneralValues(d);
			d[nameof(PadControl) + "." + nameof(PadControl.MapNameComboBox)] = new Text("Button layout",
				"Names the buttons after the controller in your hands instead of an Xbox one.");
			d[nameof(PadControl) + "." + nameof(PadControl.RemapAllButton)] = new Text("Remap All",
				"Clears the mapping, then asks you to press each control in turn.");

			// Buttons page.
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToButtonGroupBox)] = new Text("Press points",
				"For each button mapped to a stick or pedal, how far it must move to count as pressed.");

			// D-Pad page.
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadGroupBox)] = new Text("Axis to D-Pad",
				"Turns the device's X and Y axes, its main stick, into the four D-Pad directions, as well as whatever else the D-Pad is mapped to: "
				+ "for a pad whose direction buttons report as a stick. The stick's own mappings stay as they are.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadEnabledCheckBox)] = new Text("Enabled",
				"Turns the X and Y axes into D-Pad presses: right and left from X, down and up from Y, two at once on a diagonal.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadDeadZoneTextBox)] = new Text("Dead zone",
				"How far an axis must move from the centre before a direction is pressed. 100 % presses close to the end, where a pad's direction buttons put it even when it falls a little short.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadDeadZoneTrackBar)] = new Text("Dead zone",
				"How far an axis must move from the centre before a direction is pressed. 100 % presses close to the end, where a pad's direction buttons put it even when it falls a little short.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadOffsetTextBox)] = new Text("Centre offset",
				"Moves the point counted as centre towards the right and the bottom, as part of half the travel, for a device that does not rest in the middle.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadOffsetTrackBar)] = new Text("Centre offset",
				"Moves the point counted as centre towards the right and the bottom, as part of half the travel, for a device that does not rest in the middle.");
			d[nameof(PadControl) + "." + nameof(PadControl.AxisToDPadChart)] = new Text("Axis to D-Pad chart",
				"The X and Y axes as a square: the shaded sides are where a direction is pressed, the dot is the device's reading, and a side it presses is lit.");

			// Force feedback page.
			d[nameof(PadControl) + "." + nameof(PadControl.ForceFeedbackGroupBox)] = new Text("Force feedback",
				"Vibration settings shared by both motors.");
			d[nameof(PadControl) + "." + nameof(PadControl.EffectDescriptionLabel)] = Live("Effect description",
				"What the chosen effect type does, or, for a device read through Raw Input whose DirectInput side takes no force feedback, that none reaches it.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSourceGroupBox)] = new Text("Force source",
				"Where the selected controller's force comes from. Shown only while the controller, or its twin read through another source, is on another controller tab too.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSourceLabel)] = Live("Force source",
				"Which controller tab's settings shape the controller's force, the lowest enabled tab it is on, and which controllers' force it plays: "
				+ "on several, the strongest of each motor. Or why no force reaches it. On every other tab the settings that shape the force are greyed out.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceEnableCheckBox)] = new Text("Enable",
				"Passes this controller's vibration from the game to the device. A device on several tabs feels every tab where this switch is also on, the strongest of each motor.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSwapMotorCheckBox)] = new Text("Swap Motors",
				"Sends each motor's vibration to the other one.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceTypeComboBox)] = new Text("Effect type",
				"How vibration is produced: held steady, or repeated as a pulse.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceOverallTextBox)] = new Text("Overall strength",
				"Scales all vibration, so a device that shakes too hard can be calmed.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceOverallTrackBar)] = new Text("Overall strength",
				"Scales all vibration, so a device that shakes too hard can be calmed.");
			d[nameof(PadControl) + "." + nameof(PadControl.MotorPeriodPresetComboBox)] = new Text("Motor periods",
				"Sets both motor periods to the measured motors played this many times slower; 4x suits most wheels. Greyed out under Constant, which has no period.");
			d[nameof(PadControl) + "." + nameof(PadControl.MotorInfoButton)] = new Text("About the motors",
				"Shows what each motor is used for, their measured speeds, and what the multipliers mean.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceDefaultsButton)] = new Text("Defaults",
				"Puts every setting on this page back to its default.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForcePassThroughIndexComboBox)] = new Text("Pass through to",
				"Which XInput place receives the game's vibration when it is passed through.");
			d[nameof(PadControl) + "." + nameof(PadControl.InforTextBox)] = new Text("About force feedback",
				"Explains what the settings on this page do.");
			AddMotor(d, "Left", nameof(PadControl.LeftMotorGroupBox), "Left motor",
				"The big, slow motor, which produces the heavy rumble.");
			AddMotor(d, "Right", "groupBox1", "Right motor",
				"The small, fast motor, which produces the light buzz.");

			// Wheel page.
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringEnableCheckBox)] = new Text("Centering spring",
				"Turns the centering spring on. Off by default, so a wheel's own software can hold the centre without the two fighting.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringCarComboBox)] = new Text("Car",
				"Fills Centre, Peak, Lightening, Damping, Speed and Wheel range with a kind of car's values, and shows the car the values match, or Custom. "
				+ "No standard sets how a steering wheel must feel, so each car names what it is based on: the family car at 100 km/h is worked out "
				+ "from measurements in the ISO 13674-1 weave test; in town and parking are the same car at 40 and 10 km/h; the sports car and the "
				+ "heavy van are estimates at the firm and slow ends of the range measured in that test. Each is a road car, so each sets the Wheel range "
				+ "for road and sports cars. Friction and Strength are left as they are, and the car is not saved.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringFrictionTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringFrictionTrackBar)] = new Text("Friction",
				"The least force that turns this wheel and brings it home, in percent of its force. The spring reaches it a few degrees from the centre "
				+ "and never pushes less beyond, so the wheel comes home from any angle, with every car. 20 % until Auto measures it and adds a tenth. A geared or belt wheel needs much, a G27 about 30 %; "
				+ "a direct drive wheel turns freely and needs little, so lower it by hand when the wheel comes home harder than you like. Kept with the wheel. "
				+ "Greyed out while no wheel is selected.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringStrengthTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringStrengthTrackBar)] = new Text("Strength",
				"The car's steering force at Peak, in percent of the wheel's force, added to Friction. Nought is off.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringCentreTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringCentreTrackBar)] = new Text("Centre",
				"The steering angle in degrees at which the spring reaches half its strength, for the car at 100 km/h. Smaller is firmer on the centre. Widened when the wheel could not follow so steep a spring.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringPeakTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringPeakTrackBar)] = new Text("Peak",
				"The steering angle in degrees of the spring's strongest force, for the car at 100 km/h. Beyond it the force eases off towards the stop. Stop puts the strongest force at the stop.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringLighteningTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringLighteningTrackBar)] = new Text("Lightening",
				"How much of the spring's strongest force is left at the stop, in percent.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringDampingTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringDampingTrackBar)] = new Text("Damping",
				"Resistance to the wheel's speed, the same at every angle, in percent of what the wheel can do. Calms a wheel that swings past the centre.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringSpeedTextBox)] = d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringSpeedTrackBar)] = new Text("Speed",
				"The car's speed in km/h the spring is read at. Slower spreads the force over a wider angle. Below 10 counts as 10. "
				+ "A program that knows the car's speed, such as one reading a game's own data, can later set it live, without saving it.");
			d[nameof(PadControl) + "." + nameof(PadControl.ForceSpringAutoButton)] = new Text("Auto",
				"Measures the wheel's friction from both stops, with hands off the wheel, sets Friction to it and a tenth more, and checks that the spring then brings the wheel home without swinging. Strength is left as it is.");
			d[nameof(PadControl) + "." + nameof(PadControl.WheelRangeComboBox)] = new Text("Wheel range",
				"Degrees lock to lock sent to a Logitech wheel, which powers up at 200, and the range the spring's angles are counted against. "
				+ "Sent while this tab drives the wheel: Enable on the Force Feedback page, or Centering spring. Leave as it is sends nothing, "
				+ "and is for a wheel without the centering spring: ticking the spring picks 900. Any other wheel turns the range its own software sets; "
				+ "the page then says so, and a formula in the steering box, such as =a1*900/270, steers a shorter range.");
			d[nameof(PadControl) + "." + nameof(PadControl.WheelDescriptionLabel)] = Live("Wheel message",
				"What Auto is doing and what it found, why the centering spring does nothing now, or that x360ce cannot set this wheel's range.");

			// The chart on the Wheel page.
			d[nameof(WheelChartUserControl) + "." + nameof(WheelChartUserControl.MainPictureBox)] = new Text("Centering chart",
				"The red curve is the spring's force towards the centre at every angle, from one stop to the other, as it is played: above the middle "
				+ "it pushes right, below it pushes left. The grey band is Friction, forces too weak to turn the wheel: the curve stays above it everywhere "
				+ "but a few degrees round the centre, where it rises gently so the wheel settles there instead of shaking. The crosshair and the dot are "
				+ "the wheel's angle and force now.");
			d[nameof(WheelChartUserControl) + "." + nameof(WheelChartUserControl.AngleValueLabel)] = Live("Wheel angle",
				"The wheel's angle now, in degrees from the centre; negative is to the left.");
			d[nameof(WheelChartUserControl) + "." + nameof(WheelChartUserControl.ForceValueLabel)] = Live("Spring force",
				"The spring's force at that angle, in percent of the wheel's force, and which way it pushes.");

			// Devices and presets.
			d[nameof(PadControl) + "." + nameof(PadControl.MappedDevicesDataGridView)] = new Text("Mapped devices",
				"Devices standing in for this controller. Several can be combined into one.");
			d[nameof(PadControl) + "." + nameof(PadControl.GamesToolStrip)] = new Text("Mapped device actions",
				"Adds, removes and enables the devices that work this controller.");
			d[nameof(PadControl) + "." + nameof(PadControl.AddMapButton)] = new Text("Add...",
				"Chooses another device to work this controller.");
			d[nameof(PadControl) + "." + nameof(PadControl.RemoveMapButton)] = new Text("Remove",
				"Stops the selected device working this controller.");
			d[nameof(PadControl) + "." + nameof(PadControl.AutoMapButton)] = new Text("Auto Map",
				"Not available yet, so greyed out: the program does not map devices by itself. Add maps a device.");
			d[nameof(PadControl) + "." + nameof(PadControl.EnableButton)] = new Text("Enable",
				"Turns this controller on or off for the selected game.");
			d[nameof(PadControl) + "." + nameof(PadControl.GetXInputStatesCheckBox)] = new Text("Show XInput State",
				"Shows on the General page the values read back from XInput - what the game actually receives - instead "
				+ "of the values worked out from your devices. A trigger's or stick's chart always shows the selected device's "
				+ "reading and what its mapping makes of it. The emulated controller works either way.");
			d[nameof(PadControl) + "." + nameof(PadControl.GameControllersButton)] = new Text("Game Controllers...",
				"Opens the Windows game controller panel for the selected device.");
			d[nameof(PadControl) + "." + nameof(PadControl.DxTweakButton)] = new Text("DX Tweak...",
				"Opens DX Tweak, a separate tool for adjusting the device itself.");
			d[nameof(PadControl) + "." + nameof(PadControl.LoadPresetButton)] = new Text("Load Preset...",
				"Replaces this controller's settings with a saved set.");
			d[nameof(PadControl) + "." + nameof(PadControl.AutoPresetButton)] = new Text("Auto Preset",
				"Fills the settings from x360ce's controller database, and the mapping from SDL's where it maps nothing, when either knows the selected device, otherwise from x360ce's own guess. The status bar says which.");
			d[nameof(PadControl) + "." + nameof(PadControl.SavePresetButton)] = new Text("Save Preset...",
				"Writes this controller's settings to a file, to load again or pass on.");
			d[nameof(PadControl) + "." + nameof(PadControl.ClearPresetButton)] = new Text("Clear",
				"Empties every mapping on this controller.");
			d[nameof(PadControl) + "." + nameof(PadControl.ResetPresetButton)] = new Text("Reset",
				"Puts every setting on this controller back to its default. Asks first.");
			d[nameof(PadControl) + "." + nameof(PadControl.CopyPresetButton)] = new Text("Copy Preset",
				"Copies this controller's settings to the clipboard as YAML, the easiest to read.");
			d[nameof(PadControl) + "." + nameof(PadControl.CopyPresetFormatButton)] = new Text("Copy Preset as",
				"Copies this controller's settings in the format you choose: YAML, XML or JSON, or the mapping as a row of SDL's controller database.");
			d[nameof(PadControl) + "." + nameof(PadControl.PastePresetButton)] = new Text("Paste Preset",
				"Applies settings from the clipboard to this controller; YAML, XML, JSON and rows of SDL's controller database are recognised.");
			d[nameof(LoadPresetsForm) + "." + nameof(LoadPresetsForm.OpenFileButton)] = new Text("Open File...",
				"Loads a preset from a file written by Save Preset.");
			d[nameof(LoadPresetsForm) + "." + nameof(LoadPresetsForm.CopyPresetButton)] = new Text("Copy Preset",
				"Copies the selected preset to the clipboard as YAML, to read it before loading.");
			d[nameof(LoadPresetsForm) + "." + nameof(LoadPresetsForm.CopyPresetFormatButton)] = new Text("Copy Preset as",
				"Copies the selected preset in the format you choose: YAML, XML or JSON, or the mapping as a row of SDL's controller database.");
			d[nameof(LoadPresetsForm) + "." + nameof(LoadPresetsForm.SdlTabPage)] = new Text("SDL",
				"The rows of SDL's controller database, the selected device's first. The row chosen is loaded or copied translated for the device.");
			d[nameof(SdlPresetsUserControl)] = new Text(null,
				"The rows of SDL's controller database.");
			d[nameof(SdlPresetsUserControl) + "." + nameof(SdlPresetsUserControl.VersionLabel)] = Live("SDL database version",
				"Which copy of SDL's controller database is in use: built in, or downloaded and from where, its date and commit, and how many Windows rows it holds.");
			d[nameof(SdlPresetsUserControl) + "." + nameof(SdlPresetsUserControl.FilterLabel)] = new Text("Filter", null);
			d[nameof(SdlPresetsUserControl) + "." + nameof(SdlPresetsUserControl.FilterTextBox)] = new Text("Filter",
				"Lists only the rows whose name, vendor and product, or GUID hold this text.");
			d[nameof(SdlPresetsUserControl) + "." + nameof(SdlPresetsUserControl.RowsDataGridView)] = new Text("SDL rows",
				"The rows of SDL's controller database: the selected device's first, marked This device, then the rest by name.");
			d[nameof(SdlPresetsUserControl) + "." + nameof(SdlPresetsUserControl.UpdateButton)] = new Text("Update SDL Database",
				"Fetches the newest SDL controller database into the settings folder from the address Options > Internet sets, GitHub unless you choose another. The only time x360ce asks the internet for it.");
			d[nameof(SdlPresetsUserControl) + "." + nameof(SdlPresetsUserControl.StatusLabel)] = Live("SDL update status",
				"How the last update of SDL's controller database went.");
		}

		/// <summary>The number under each name on the General page, for every part of the controller the page maps.</summary>
		/// <remarks>Named after the part's box, which carries the same words, so a value is found beside its box.</remarks>
		static void AddGeneralValues(Dictionary<string, Text> d)
		{
			var codes = SettingsConverter.TriggerButtonCodes.Concat(SettingsConverter.ShoulderButtonCodes)
				.Concat(SettingsConverter.MenuButtonCodes).Concat(SettingsConverter.MainButtonCodes)
				.Concat(SettingsConverter.DPadCodes).Concat(SettingsConverter.LeftThumbCodes).Concat(SettingsConverter.RightThumbCodes);
			foreach (var code in codes)
				d["PadControl." + code + "ValueLabel"] = Live(ImageInfo.GetName(code) + " value", ValuePurpose(code));
		}

		/// <summary>What the number under a part's name says, as <see cref="ImageInfo.GetValue"/> works it out.</summary>
		static string ValuePurpose(MapCode code)
		{
			switch (code)
			{
				case MapCode.LeftTrigger:
				case MapCode.RightTrigger:
					return "What the game is being given for the trigger right now, from 0 released to 255 pressed all the way.";
				case MapCode.LeftThumbAxisX:
				case MapCode.RightThumbAxisX:
					return "What the game is being given for the stick across right now, from -32768 at the left to 32767 at the right.";
				case MapCode.LeftThumbAxisY:
				case MapCode.RightThumbAxisY:
					return "What the game is being given for the stick up and down right now, from -32768 at the bottom to 32767 at the top.";
				case MapCode.LeftThumbUp:
				case MapCode.LeftThumbLeft:
				case MapCode.LeftThumbRight:
				case MapCode.LeftThumbDown:
				case MapCode.RightThumbUp:
				case MapCode.RightThumbLeft:
				case MapCode.RightThumbRight:
				case MapCode.RightThumbDown:
					return "The stick's position the game is being given while the stick points this way, otherwise 0.";
				case MapCode.DPad:
					return "The D-Pad directions the game is being given, added up: up 1, down 2, left 4, right 8; 0 while none is pressed.";
				default:
					return "1 while the game is being given this button pressed, otherwise 0.";
			}
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
