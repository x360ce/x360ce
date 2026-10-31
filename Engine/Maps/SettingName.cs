using System.Collections.Generic;
using System.ComponentModel;

namespace x360ce.Engine
{
	/// <summary>
	/// Map between .NET and INI file. This makes refactoring easier.
	/// </summary>
	/// <remarks>
	/// Adopted names:
	/// "Internal Code Name" = "Public Name"
	/// Big = "Guide"
	/// Thumb = "Stick"
	/// Shoulder = "Bumper"
	/// </remarks>
	public struct SettingName
	{

		/// <summary>
		/// Button type prefix.
		/// </summary>
		public struct SType
		{
			public const string None = null;
			/// <summary>A button in a button field, where a number without a letter is a button.</summary>
			public const string Button = "";
			/// <summary>A button in a field whose bare number names another kind of control, such as a stick axis.</summary>
			public const string ButtonMark = "b";
			public const string Axis = "a";
			public const string HAxis = "x";
			public const string Slider = "s";
			public const string HSlider = "h";
			public const string POV = "p";
			public const string POVButton = "d";
		}

		/// <summary>How far an axis must move to press a button it drives: a quarter of the way.</summary>
		public const string DefaultButtonDeadZone = "8192";

		public const string DefaultInternetDatabaseUrl = "https://www.x360ce.com/webservices/x360ce.asmx";

		/// <summary>The web service of the Web project, as Visual Studio runs it on this computer.</summary>
		public const string LocalInternetDatabaseUrl = "https://localhost:44360/webservices/x360ce.asmx";

		/// <summary>The address to use for a saved one: the program's own servers over HTTPS, anything else as it is.</summary>
		/// <remarks>
		/// Settings can hold the plain HTTP addresses of the program's own servers. Those are read as the
		/// HTTPS addresses offered in their place, and an address somebody typed for a server of their own
		/// is kept as they wrote it.
		/// </remarks>
		public static string WithHttps(string url)
		{
			if (string.Equals(url, "http://www.x360ce.com/webservices/x360ce.asmx", System.StringComparison.OrdinalIgnoreCase))
				return DefaultInternetDatabaseUrl;
			if (string.Equals(url, "http://localhost:20360/webservices/x360ce.asmx", System.StringComparison.OrdinalIgnoreCase))
				return LocalInternetDatabaseUrl;
			return url;
		}
		static public string DefaultVersion { get { return "2"; } }

		// [Options] section.
		[DefaultValue("1"), Description("Beep when initialized. 0 = OFF, 1 = ON.")]
		static public string UseInitBeep { get { return "UseInitBeep"; } }

		[DefaultValue("0"), Description("Throw or suspend errors. 0 = Suspend, 1 = Throw.")]
		static public string DebugMode { get { return "DebugMode"; } }

		[DefaultValue("0"), Description("Create a log file in the folder 'x360ce logs'. 0 = OFF, 1 = ON.")]
		static public string Log { get { return "Log"; } }

		[DefaultValue("0"), Description("Display the console window. 0 = OFF, 1 = ON.")]
		static public string Console { get { return "Console"; } }

		[DefaultValue(DefaultInternetDatabaseUrl), Description("Internet settings database URL.")]
		static public string InternetDatabaseUrl { get { return "InternetDatabaseUrl"; } }

		[DefaultValue("0"), Description("Enable the use of Internet features like the settings database. 0 = OFF, 1 = ON.")]
		static public string InternetFeatures { get { return "InternetFeatures"; } }

		[DefaultValue("1"), Description("Auto load settings from Online Database. 0 = OFF, 1 = ON.")]
		static public string InternetAutoLoad { get { return "InternetAutoLoad"; } }

		[DefaultValue("1"), Description("Auto save settings to Online Database. 0 = OFF, 1 = ON.")]
		static public string InternetAutoSave { get { return "InternetAutoSave"; } }

		[DefaultValue("1"), Description("Allow only one instance of the application to run at a time. 0 = Allow multiple instances, 1 = Allow only one instance.")]
		static public string AllowOnlyOneCopy { get { return "AllowOnlyOneCopy"; } }

		[DefaultValue("System"), Description("Light or dark colours. System follows the Windows setting for apps.")]
		static public string Theme { get { return "Theme"; } }

		[DefaultValue(""), Description("The locations to scan for games, separated by semicolon (;).")]
		static public string ProgramScanLocations { get { return "ProgramScanLocations"; } }

		[DefaultValue("1"), Description("The configuration file version.")]
		static public string Version { get { return "Version"; } }

		[DefaultValue("0"), Description("Allow multiple controllers to be combined into a virtual controller. 0 = OFF, 1 = ON.")]
		static public string CombineEnabled { get { return "CombineEnabled"; } }

		// [InputHook] section.
		[DefaultValue("1"), Description("WMI API patching. 0 = Disabled, 1 = USB Only, 2 = USB and HID.")]
		static public string HookMode { get { return "HookMode"; } }

		[DefaultValue("0x28E"), Description("FakePID. Works in conjunction with HOOKPIDVID.")]
		static public string FakePID { get { return "FakePID"; } }

		[DefaultValue("0x45E"), Description("FakeVID. Works in conjunction with HOOKPIDVID.")]
		static public string FakeVID { get { return "FakeVID"; } }


		// [Mappings] section.
		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD1.")]
		static public string PAD1 { get { return "PAD1"; } }

		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD2.")]
		static public string PAD2 { get { return "PAD2"; } }

		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD3.")]
		static public string PAD3 { get { return "PAD3"; } }

		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD4.")]
		static public string PAD4 { get { return "PAD4"; } }


		// [PAD] section.
		[DefaultValue("Unknown Device"), Description("Device product name.")]
		static public string ProductName { get { return "ProductName"; } }

		[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device product GUID.")]
		static public string ProductGuid { get { return "ProductGuid"; } }

		[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device instance GUID.")]
		static public string InstanceGuid { get { return "InstanceGuid"; } }

		[DefaultValue("1"), Description("Device Type. None = 0, Gamepad = 1, Wheel = 2, Stick = 3, FlightStick = 4, DancePad = 5, Guitar = 6, DrumKit = 8.")]
		static public string GamePadType { get { return "ControllerType"; } }

		[DefaultValue("0"), Description("Bypass x360ce and send all input and vibration data directly to the system. This disables combining, mappings, deadzones, etc. for this controller. 0 = OFF, 1 = ON.")]
		static public string PassThrough { get { return "PassThrough"; } }

		[DefaultValue("0"), Description("Bypass x360ce for vibration data only. The controller still participates in mappings, deadzones, etc. 0 = OFF, 1 = ON.")]
		static public string ForcesPassThrough { get { return "ForcesPassThrough"; } }

		[DefaultValue("0"), Description("Send the force feedback a game asks for on to a real XInput controller, which an emulated one cannot feel. 0 = OFF, 1 = ON.")]
		static public string ForcePassThrough { get { return "ForcePassThrough"; } }

		[DefaultValue("0"), Description("Which XInput place the force feedback is sent to. 0 = work it out, 1 to 4 = that XInput place.")]
		static public string ForcePassThroughIndex { get { return "ForcePassThroughIndex"; } }

		[DefaultValue("0"), Description("The player that controller data will be passed through to or vibration will be read from. 0 = Player 1, 1 = Player 2, 2 = Player 3, 3 = Player 4.")]
		static public string PassThroughIndex { get { return "PassThroughIndex"; } }


		// Combining
		[DefaultValue("0"), Description("Allow this controller to be combined into another virtual controller. 0 = OFF, 1 = ON.")]
		static public string Combined { get { return "Combined"; } }

		[DefaultValue("0"), Description("The player that virtual controller will be mapped to. 0 = Player 1, 1 = Player 2, 2 = Player 3, 3 = Player 4.")]
		static public string CombinedIndex { get { return "CombinedIndex"; } }


		// Default Mapping.
		[DefaultValue("0"), Description("Index of the PAD which this controller will map to. Auto = 0 or PAD Index 1-4.")]
		static public string MapToPad { get { return "MapToPad"; } }


		// Left Thumb.
		[DefaultValue("0"), Description("Left stick button. Disable = 0.")]
		static public string LeftThumbButton { get { return "Left Thumb"; } }

		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		static public string LeftThumbAxisX { get { return "Left Analog X"; } }

		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		static public string LeftThumbAxisY { get { return "Left Analog Y"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string LeftThumbRight { get { return "Left Analog X+ Button"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string LeftThumbLeft { get { return "Left Analog X- Button"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string LeftThumbUp { get { return "Left Analog Y+ Button"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string LeftThumbDown { get { return "Left Analog Y- Button"; } }

		[DefaultValue("0"), Description("Add deadzone to left thumb X. Range is 0 to 32767. Default is 0.")]
		static public string LeftThumbDeadZoneX { get { return "Left Analog X DeadZone"; } }

		[DefaultValue("0"), Description("Add deadzone to left thumb Y. Range is 0 to 32767. Default is 0.")]
		static public string LeftThumbDeadZoneY { get { return "Left Analog Y DeadZone"; } }

		[DefaultValue("0"), Description("Decrease in-game deadzone for left thumb X. Range is 0 to 32767. Default is 0.")]
		static public string LeftThumbAntiDeadZoneX { get { return "Left Analog X AntiDeadZone"; } }

		[DefaultValue("0"), Description("Decrease in-game deadzone for left thumb Y. Range is 0 to 32767. Default is 0.")]
		static public string LeftThumbAntiDeadZoneY { get { return "Left Analog Y AntiDeadZone"; } }

		[DefaultValue("0"), Description("Increase sensitivity near the center of left thumb X. Range is -100 to 100. Default is 0.")]
		static public string LeftThumbLinearX { get { return "Left Analog X Linear"; } }

		[DefaultValue("0"), Description("Increase sensitivity near the center of left thumb Y. Range is -100 to 100. Default is 0.")]
		static public string LeftThumbLinearY { get { return "Left Analog Y Linear"; } }


		// Right Thumb.
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string RightThumbButton { get { return "Right Thumb"; } }

		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		static public string RightThumbAxisX { get { return "Right Analog X"; } }

		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		static public string RightThumbAxisY { get { return "Right Analog Y"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string RightThumbRight { get { return "Right Analog X+ Button"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string RightThumbLeft { get { return "Right Analog X- Button"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string RightThumbUp { get { return "Right Analog Y+ Button"; } }

		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		static public string RightThumbDown { get { return "Right Analog Y- Button"; } }

		[DefaultValue("0"), Description("Add deadzone to right thumb X. Range is 0 to 32767. Default is 0.")]
		static public string RightThumbDeadZoneX { get { return "Right Analog X DeadZone"; } }

		[DefaultValue("0"), Description("Add deadzone to right thumb Y. Range is 0 to 32767. Default is 0.")]
		static public string RightThumbDeadZoneY { get { return "Right Analog Y DeadZone"; } }

		[DefaultValue("0"), Description("Decrease in-game deadzone for right thumb X. Range is 0 to 32767. Default is 0.")]
		static public string RightThumbAntiDeadZoneX { get { return "Right Analog X AntiDeadZone"; } }

		[DefaultValue("0"), Description("Decrease in-game deadzone for right thumb Y. Range is 0 to 32767. Default is 0.")]
		static public string RightThumbAntiDeadZoneY { get { return "Right Analog Y AntiDeadZone"; } }

		[DefaultValue("0"), Description("Increase sensitivity near the center of right thumb X. Range is -100 to 100. Default is 0.")]
		static public string RightThumbLinearX { get { return "Right Analog X Linear"; } }

		[DefaultValue("0"), Description("Increase sensitivity near the center of right thumb Y. Range is -100 to 100. Default is 0.")]
		static public string RightThumbLinearY { get { return "Right Analog Y Linear"; } }


		// D-Pad.
		[DefaultValue("0"), Description("Disable = 0, POV Index = N.")]
		static public string DPad { get { return "D-pad POV"; } }

		[DefaultValue("UP"), Description("D-Pad up button.")]
		static public string DPadUp { get { return "D-pad Up"; } }

		[DefaultValue("DOWN"), Description("D-Pad down button.")]
		static public string DPadDown { get { return "D-pad Down"; } }

		[DefaultValue("LEFT"), Description("D-Pad left button.")]
		static public string DPadLeft { get { return "D-pad Left"; } }

		[DefaultValue("RIGHT"), Description("D-Pad right button.")]
		static public string DPadRight { get { return "D-pad Right"; } }

		#region Axis To Button / D-Pad

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to A Button Dead Zone.")]
		static public string ButtonADeadZone { get { return "A DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to B Button Dead Zone.")]
		static public string ButtonBDeadZone { get { return "B DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to X Button Dead Zone.")]
		static public string ButtonXDeadZone { get { return "X DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Y Button Dead Zone.")]
		static public string ButtonYDeadZone { get { return "Y DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Start Button Dead Zone.")]
		static public string ButtonStartDeadZone { get { return "Start DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Back Button Dead Zone.")]
		static public string ButtonBackDeadZone { get { return "Back DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Left Bumper Dead Zone.")]
		static public string LeftShoulderDeadZone { get { return "Left Shoulder DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Left Stick Button Dead Zone.")]
		static public string LeftThumbButtonDeadZone { get { return "Left Thumb DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Right Bumper Dead Zone.")]
		static public string RightShoulderDeadZone { get { return "Right Shoulder DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to Right Stick Button Dead Zone.")]
		static public string RightThumbButtonDeadZone { get { return "Right Thumb DeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to D-Pad Down Dead Zone.")]
		static public string DPadDownDeadZone { get { return "AxisToDPadDownDeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to D-Pad Left Dead Zone.")]
		static public string DPadLeftDeadZone { get { return "AxisToDPadLeftDeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to D-Pad Right Dead Zone.")]
		static public string DPadRightDeadZone { get { return "AxisToDPadRightDeadZone"; } }

		[DefaultValue(DefaultButtonDeadZone), Description("Axis to D-Pad Up Dead Zone.")]
		static public string DPadUpDeadZone { get { return "AxisToDPadUpDeadZone"; } }

		[DefaultValue("0"), Description("Axis to control DPad. Disabled = 0, Enabled = 1.")]
		static public string AxisToDPadEnabled { get { return "AxisToDPad"; } }

		[DefaultValue("256"), Description("Dead zone for axis.")]
		static public string AxisToDPadDeadZone { get { return "AxisToDPadDeadZone"; } }

		[DefaultValue("0"), Description("Axis to D-Pad offset.")]
		static public string AxisToDPadOffset { get { return "AxisToDPadOffset"; } }

		#endregion

		// Button names.

		[DefaultValue("0"), Description("Guide button.")]
		static public string ButtonGuide { get { return "GuideButton"; } }

		[DefaultValue("0"), Description("Back button.")]
		static public string ButtonBack { get { return "Back"; } }

		[DefaultValue("0"), Description("Start button.")]
		static public string ButtonStart { get { return "Start"; } }

		[DefaultValue("0"), Description("Button 'A'")]
		static public string ButtonA { get { return "A"; } }

		[DefaultValue("0"), Description("Button 'B'")]
		static public string ButtonB { get { return "B"; } }

		[DefaultValue("0"), Description("Button 'X'")]
		static public string ButtonX { get { return "X"; } }

		[DefaultValue("0"), Description("Button 'Y'")]
		static public string ButtonY { get { return "Y"; } }

		[DefaultValue("0"), Description("Left Shoulder Button . Disable = 0.")]
		static public string LeftShoulder { get { return "Left Shoulder"; } }

		[DefaultValue("0"), Description("Right Shoulder Button. Disable = 0.")]
		static public string RightShoulder { get { return "Right Shoulder"; } }


		// Triggers.
		[DefaultValue("0"), Description("Button id; precede with 'a' for an axis; 's' for a slider; 'x' for a half range axis; 'h' for half slider; use '-' to invert ie. x-2.")]
		static public string LeftTrigger { get { return "Left Trigger"; } }

		[DefaultValue("0"), Description("Add deadzone to the left trigger. Range is 0 to 255. Default is 0.")]
		static public string LeftTriggerDeadZone { get { return "Left Trigger DeadZone"; } }

		[DefaultValue("0"), Description("Decrease in-game deadzone for left trigger. Range is 0 to 255. Default is 0.")]
		static public string LeftTriggerAntiDeadZone { get { return "Left Trigger AntiDeadZone"; } }

		[DefaultValue("0"), Description("Increase sensitivity near the bottom of left trigger. Range is -100 to 100. Default is 0.")]
		static public string LeftTriggerLinear { get { return "Left Trigger Linear"; } }

		[DefaultValue("0"), Description("Button id. [asxh][-][0-128] axis = 'a', slider = 's'; half axis = 'x', half slider = 'h', invert = '-'. Example: 'x-2'.")]
		static public string RightTrigger { get { return "Right Trigger"; } }

		[DefaultValue("0"), Description("Add deadzone to the right trigger. Range is 0 to 255. Default is 0.")]
		static public string RightTriggerDeadZone { get { return "Right Trigger DeadZone"; } }

		[DefaultValue("0"), Description("Decrease in-game deadzone for right trigger. Range is 0 to 255. Default is 0.")]
		static public string RightTriggerAntiDeadZone { get { return "Right Trigger AntiDeadZone"; } }

		[DefaultValue("0"), Description("Increase sensitivity near the bottom of right trigger. Range is -100 to 100. Default is 0.")]
		static public string RightTriggerLinear { get { return "Right Trigger Linear"; } }

		// Force feedback.
		[DefaultValue("0"), Description("Use Force Feedback. 0 = OFF, 1 = ON.")]
		static public string ForceEnable { get { return "UseForceFeedback"; } }

		[DefaultValue("0"), Description("Force Feedback type. 0 = Constant, 1 = Periodic Sine, 2 = Periodic Sawtooth")]
		static public string ForceType { get { return "FFBType"; } }

		[DefaultValue("0"), Description("Swap motor. 0 = OFF, 1 = ON.")]
		static public string ForceSwapMotor { get { return "SwapMotor"; } }

		[DefaultValue("100"), Description("Strength of force feedback. Range is 0 to 100. Default is 100.")]
		static public string ForceOverall { get { return "ForcePercent"; } }

		[DefaultValue("0"), Description("Use the centering spring. 0 = OFF, 1 = ON. Off by default, so a wheel's own software can hold the centre instead.")]
		static public string ForceSpringEnable { get { return "SpringEnable"; } }

		[DefaultValue("30"), Description("Strength of the car's steering, which the always-on centering spring adds on top of the wheel's friction. Range is 0 to 100; nought turns the spring off. Default is 30; the spring is still off until enabled.")]
		static public string ForceSpringStrength { get { return "SpringStrength"; } }

		[DefaultValue(DefaultForceSpringCentre), Description("The steering angle in degrees at which the centering spring reaches half its strength, for the car at 100 km/h. Range is 1 to 450. Default is 12, a family car.")]
		static public string ForceSpringCentre { get { return "SpringCentre"; } }
		/// <summary>The family car at 100 km/h, measured in the ISO 13674-1 weave test.</summary>
		public const string DefaultForceSpringCentre = "12";

		[DefaultValue(DefaultForceSpringPeak), Description("The steering angle in degrees of the centering spring's strongest force, for the car at 100 km/h. Beyond it the force eases off towards the stop. 0 puts the peak at the stop. Range is 0 to 450. Default is 60, a family car.")]
		static public string ForceSpringPeak { get { return "SpringPeak"; } }
		/// <summary>The family car at 100 km/h: the tyres' grip peaks at about 3 degrees of slip.</summary>
		public const string DefaultForceSpringPeak = "60";

		[DefaultValue(DefaultForceSpringLightening), Description("How much of the centering spring's strongest force is left at the stop, in percent. Range is 0 to 100. Default is 40, a family car.")]
		static public string ForceSpringLightening { get { return "SpringLightening"; } }
		/// <summary>The family car: the share of its centring that does not come from the tyres.</summary>
		public const string DefaultForceSpringLightening = "40";

		[DefaultValue(DefaultForceSpringDamping), Description("The centering spring's resistance to the wheel's speed, the same at every angle, in percent of what the device can do. Range is 0 to 100. Default is 5.")]
		static public string ForceSpringDamping { get { return "SpringDamping"; } }
		/// <summary>The light damping of power steering.</summary>
		public const string DefaultForceSpringDamping = "5";

		[DefaultValue(DefaultForceSpringSpeed), Description("The car's speed in km/h that the centering spring's curve is read at. At a lower speed the force builds over a wider angle. Below 10 counts as 10. A program that knows the car's speed can later set it live, without saving it. Range is 0 to 250. Default is 100.")]
		static public string ForceSpringSpeed { get { return "SpringSpeed"; } }
		/// <summary>The speed of the ISO 13674-1 weave test, which Centre, Peak and Lightening describe.</summary>
		public const string DefaultForceSpringSpeed = "100";

		[DefaultValue("0"), Description("Steering range in degrees sent to a Logitech wheel, which powers up at 200. Range is 40 to 900. Default is 0 (leave the wheel as it is).")]
		static public string WheelRange { get { return "WheelRange"; } }

		#region Mouse to stick

		// How a mouse, or any control that reports how far it moved rather than where it is, moves each stick. Every one
		// of these is stored empty at its default, which is how a mouse moved a stick before they existed, so presets
		// saved before keep their checksums and act as they did.

		/// <summary>Position: the stick follows how far the mouse has moved, and stays there.</summary>
		public const string DefaultMouseStyle = "0";
		/// <summary>The whole stick for 2,048 counts in Position, 4,096 counts a second in Speed, half the screen in Screen.</summary>
		public const string DefaultMouseSensitivity = "100";
		/// <summary>Evens out a mouse's reports, which arrive in bursts, without a lag a player feels.</summary>
		public const string DefaultMouseSmoothing = "30";
		/// <summary>Position stays where the mouse left it; Speed returns as fast as it smooths.</summary>
		public const string DefaultMouseReturn = "0";
		/// <summary>Always.</summary>
		public const string DefaultMouseActive = "0";
		/// <summary>None.</summary>
		public const string DefaultMouseButton = "0";

		const string MouseStyleText = " 0 = Position: the stick follows how far the mouse has moved, and stays there. 1 = Speed: the stick follows how fast the mouse moves, and returns to the centre when it stops. 2 = Screen: the stick follows where the pointer is on the screen. Default is 0.";
		const string MouseSensitivityText = " in percent. In Position, 100 moves it from the centre to the end in 2,048 counts; in Speed, 100 reaches the end at 4,096 counts a second; in Screen, 100 reaches it half the screen's height from the centre. Range is 1 to 400. Default is 100.";
		const string MouseSmoothingText = " takes to follow a change in Speed and Screen, in milliseconds, which evens out a mouse's uneven reports. Range is 0 to 500. Default is 30.";
		const string MouseReturnText = " takes to go back to the centre, in milliseconds. In Position, 0 leaves it where the mouse left it; in Speed, 0 returns it as fast as it smooths. Range is 0 to 2000. Default is 0.";
		const string MouseActiveText = " 0 = Always. 1 = While the button is held; let go, the stick returns to the centre. 2 = While the button is held; let go, the stick stays where it was. 3 = The button turns it on and off; off, the stick is at the centre. Default is 0.";
		const string MouseButtonText = ", counted from 1: the button of the same device. 0 = none. Default is 0.";

		[DefaultValue(DefaultMouseStyle), Description("How a mouse moves the left stick." + MouseStyleText)]
		static public string MouseLeftThumbStyle { get { return "MouseLeftThumbStyle"; } }

		[DefaultValue(DefaultMouseSensitivity), Description("How far the left stick moves for a movement across," + MouseSensitivityText)]
		static public string MouseLeftThumbSensitivityX { get { return "MouseLeftThumbSensitivityX"; } }

		[DefaultValue(DefaultMouseSensitivity), Description("How far the left stick moves for a movement up or down," + MouseSensitivityText)]
		static public string MouseLeftThumbSensitivityY { get { return "MouseLeftThumbSensitivityY"; } }

		[DefaultValue(DefaultMouseSmoothing), Description("How long the left stick" + MouseSmoothingText)]
		static public string MouseLeftThumbSmoothing { get { return "MouseLeftThumbSmoothing"; } }

		[DefaultValue(DefaultMouseReturn), Description("How long the left stick" + MouseReturnText)]
		static public string MouseLeftThumbReturn { get { return "MouseLeftThumbReturn"; } }

		[DefaultValue(DefaultMouseActive), Description("When the mouse moves the left stick." + MouseActiveText)]
		static public string MouseLeftThumbActive { get { return "MouseLeftThumbActive"; } }

		[DefaultValue(DefaultMouseButton), Description("The button that turns the mouse on the left stick on and off" + MouseButtonText)]
		static public string MouseLeftThumbButton { get { return "MouseLeftThumbButton"; } }

		[DefaultValue(DefaultMouseStyle), Description("How a mouse moves the right stick." + MouseStyleText)]
		static public string MouseRightThumbStyle { get { return "MouseRightThumbStyle"; } }

		[DefaultValue(DefaultMouseSensitivity), Description("How far the right stick moves for a movement across," + MouseSensitivityText)]
		static public string MouseRightThumbSensitivityX { get { return "MouseRightThumbSensitivityX"; } }

		[DefaultValue(DefaultMouseSensitivity), Description("How far the right stick moves for a movement up or down," + MouseSensitivityText)]
		static public string MouseRightThumbSensitivityY { get { return "MouseRightThumbSensitivityY"; } }

		[DefaultValue(DefaultMouseSmoothing), Description("How long the right stick" + MouseSmoothingText)]
		static public string MouseRightThumbSmoothing { get { return "MouseRightThumbSmoothing"; } }

		[DefaultValue(DefaultMouseReturn), Description("How long the right stick" + MouseReturnText)]
		static public string MouseRightThumbReturn { get { return "MouseRightThumbReturn"; } }

		[DefaultValue(DefaultMouseActive), Description("When the mouse moves the right stick." + MouseActiveText)]
		static public string MouseRightThumbActive { get { return "MouseRightThumbActive"; } }

		[DefaultValue(DefaultMouseButton), Description("The button that turns the mouse on the right stick on and off" + MouseButtonText)]
		static public string MouseRightThumbButton { get { return "MouseRightThumbButton"; } }

		/// <summary>True for the mouse settings chosen from a list: the style, when it is active, and its button.</summary>
		public static bool IsMouseChoice(string key)
		{
			return key != null && key.StartsWith("Mouse") && (key.EndsWith("Style") || key.EndsWith("Active") || key.EndsWith("Button"));
		}

		#endregion

		[DefaultValue(DefaultLeftMotorPeriod), Description("Left motor period at full drive, in milliseconds; it stretches as the drive falls, as the motor slows. The left motor is the low-frequency one. Range is 0 to 400. Default is 160, the measured motor played 4 times slower for a wheel.")]
		static public string LeftMotorPeriod { get { return "LeftMotorPeriod"; } }
		/// <summary>MotorModel.PeriodAtFullMs(MotorModel.DefaultMultiplier, true), spelt out because an attribute needs a constant.</summary>
		public const string DefaultLeftMotorPeriod = "160";

		[DefaultValue("100"), Description("Left motor strength. Range is 0 to 100. Default is 100.")]
		static public string LeftMotorStrength { get { return "LeftMotorStrength"; } }

		[DefaultValue("0"), Description("Left motor effect direction. -1, 0, 1.")]
		static public string LeftMotorDirection { get { return "LeftMotorDirection"; } }

		[DefaultValue(DefaultRightMotorPeriod), Description("Right motor period at full drive, in milliseconds; it stretches as the drive falls, as the motor slows. The right motor is the high-frequency one. Range is 0 to 400. Default is 64, the measured motor played 4 times slower for a wheel.")]
		static public string RightMotorPeriod { get { return "RightMotorPeriod"; } }
		/// <summary>MotorModel.PeriodAtFullMs(MotorModel.DefaultMultiplier, false), spelt out because an attribute needs a constant.</summary>
		public const string DefaultRightMotorPeriod = "64";

		[DefaultValue("100"), Description("Right motor strength. Range is 0 to 100. Default is 100.")]
		static public string RightMotorStrength { get { return "RightMotorStrength"; } }

		[DefaultValue("0"), Description("Right motor effect direction. -1, 0, 1.")]
		static public string RightMotorDirection { get { return "RightMotorDirection"; } }

		public static int GetPadIndex(string path)
		{
			var section = path.Split('\\')[0];
			var pads = new List<string>() { PAD1, PAD2, PAD3, PAD4 };
			return pads.IndexOf(section);
		}

		public static bool IsButton(string name)
		{
			return name == LeftThumbButton
				|| name == LeftThumbUp
				|| name == LeftThumbRight
				|| name == LeftThumbDown
				|| name == LeftThumbLeft
				|| name == RightThumbButton
				|| name == RightThumbUp
				|| name == RightThumbRight
				|| name == RightThumbDown
				|| name == RightThumbLeft;
		}

		public static bool IsDPad(string name)
		{
			return name == DPad
				|| name == DPadDown
				|| name == DPadLeft
				|| name == DPadRight
				|| name == DPadUp;
		}

		public static bool IsThumbAxis(string name)
		{
			return name == LeftThumbAxisX
				|| name == LeftThumbAxisY
				|| name == RightThumbAxisX
				|| name == RightThumbAxisY;
		}

	}
}
