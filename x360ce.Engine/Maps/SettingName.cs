using System.Collections.Generic;
using System.ComponentModel;
using x360ce.Engine.Data;

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
			public const string Button = ""; // Button must be 'b' and no prefix for auto.
			public const string Axis = "a";
			public const string HAxis = "x";
			public const string Slider = "s";
			public const string HSlider = "h";
			public const string POV = "p";
			public const string POVButton = "d";
		}

		public const string DefaultInternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";

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

		[DefaultValue(""), Description("The locations to scan for games, separated by semicolon (;).")]
		static public string ProgramScanLocations { get { return "ProgramScanLocations"; } }

		[DefaultValue("1"), Description("The configuration file version.")]
		static public string Version { get { return "Version"; } }

		[DefaultValue("0"), Description("Allow multiple controllers to be combined into a virtual controller. 0 = OFF, 1 = ON.")]
		static public string CombineEnabled { get { return "CombineEnabled"; } }

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


		//// [PAD] section.
		//[DefaultValue("Unknown Device"), Description("Device product name.")]
		//static public string ProductName { get { return "ProductName"; } }

		//[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device product GUID.")]
		//static public string ProductGuid { get { return "ProductGuid"; } }

		//[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device instance GUID.")]
		//static public string InstanceGuid { get { return "InstanceGuid"; } }

		[DefaultValue("1"), Description("Device Type. None = 0, Gamepad = 1, Wheel = 2, Stick = 3, FlightStick = 4, DancePad = 5, Guitar = 6, DrumKit = 8.")]
		static public string GamePadType { get { return "ControllerType"; } }

		[DefaultValue("0"), Description("Bypass x360ce and send all input and vibration data directly to the system. This disables combining, mappings, deadzones, etc. for this controller. 0 = OFF, 1 = ON.")]
		static public string PassThrough { get { return "PassThrough"; } }

		[DefaultValue("0"), Description("Bypass x360ce for vibration data only. The controller still participates in mappings, deadzones, etc. 0 = OFF, 1 = ON.")]
		static public string ForcesPassThrough { get { return "ForcesPassThrough"; } }

		//// Default Mapping.
		//[DefaultValue("0"), Description("Index of the PAD which this controller will map to. Auto = 0 or PAD Index 1-4.")]
		//static public string MapToPad { get { return "MapToPad"; } }


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

		[DefaultValue("8192"), Description("Axis to A Button Dead Zone.")]
		static public string ButtonADeadZone { get { return "A DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to B Button Dead Zone.")]
		static public string ButtonBDeadZone { get { return "B DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to X Button Dead Zone.")]
		static public string ButtonXDeadZone { get { return "X DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Y Button Dead Zone.")]
		static public string ButtonYDeadZone { get { return "Y DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Start Button Dead Zone.")]
		static public string ButtonStartDeadZone { get { return "Start DeadZone"; } }
		
		[DefaultValue("8192"), Description("Axis to Guide Button Dead Zone.")]
		static public string ButtonGuideDeadZone { get { return "Guide DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Back Button Dead Zone.")]
		static public string ButtonBackDeadZone { get { return "Back DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Left Bumper Dead Zone.")]
		static public string LeftShoulderDeadZone { get { return "Left Shoulder DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Left Stick Button Dead Zone.")]
		static public string LeftThumbButtonDeadZone { get { return "Left Thumb DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Right Bumper Dead Zone.")]
		static public string RightShoulderDeadZone { get { return "Right Shoulder DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to Right Stick Button Dead Zone.")]
		static public string RightThumbButtonDeadZone { get { return "Right Thumb DeadZone"; } }

		[DefaultValue("8192"), Description("Axis to D-Pad Down Dead Zone.")]
		static public string DPadDownDeadZone { get { return "AxisToDPadDownDeadZone"; } }

		[DefaultValue("8192"), Description("Axis to D-Pad Left Dead Zone.")]
		static public string DPadLeftDeadZone { get { return "AxisToDPadLeftDeadZone"; } }

		[DefaultValue("8192"), Description("Axis to D-Pad Right Dead Zone.")]
		static public string DPadRightDeadZone { get { return "AxisToDPadRightDeadZone"; } }

		[DefaultValue("8192"), Description("Axis to D-Pad Up Dead Zone.")]
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

		//[DefaultValue("0"), Description("Add deadzone to the left trigger. Range is 0 to 255. Default is 0.")]
		//static public string LeftTriggerDeadZone { get { return "Left Trigger DeadZone"; } }

		//[DefaultValue("0"), Description("Decrease in-game deadzone for left trigger. Range is 0 to 255. Default is 0.")]
		//static public string LeftTriggerAntiDeadZone { get { return "Left Trigger AntiDeadZone"; } }

		//[DefaultValue("0"), Description("Increase sensitivity near the bottom of left trigger. Range is -100 to 100. Default is 0.")]
		//static public string LeftTriggerLinear { get { return "Left Trigger Linear"; } }

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

		[DefaultValue("60"), Description("Left motor period. Range is 0 to 500. Default is 60.")]
		static public string LeftMotorPeriod { get { return "LeftMotorPeriod"; } }

		[DefaultValue("100"), Description("Left motor strength. Range is 0 to 100. Default is 100.")]
		static public string LeftMotorStrength { get { return "LeftMotorStrength"; } }

		[DefaultValue("0"), Description("Left motor effect direction. -1, 0, 1.")]
		static public string LeftMotorDirection { get { return "LeftMotorDirection"; } }

		[DefaultValue("120"), Description("Right motor period. Range is 0 to 500. Default is 120.")]
		static public string RightMotorPeriod { get { return "RightMotorPeriod"; } }

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
			return name == nameof(PadSetting.LeftThumbButton)
				|| name == nameof(PadSetting.LeftThumbUp)
				|| name == nameof(PadSetting.LeftThumbRight)
				|| name == nameof(PadSetting.LeftThumbDown)
				|| name == nameof(PadSetting.LeftThumbLeft)
				|| name == nameof(PadSetting.RightThumbButton)
				|| name == nameof(PadSetting.RightThumbUp)
				|| name == nameof(PadSetting.RightThumbRight)
				|| name == nameof(PadSetting.RightThumbDown)
				|| name == nameof(PadSetting.RightThumbLeft);
		}

		public static bool IsDPad(string name)
		{
			return name == nameof(PadSetting.DPad)
				|| name == nameof(PadSetting.DPadDown)
				|| name == nameof(PadSetting.DPadLeft)
				|| name == nameof(PadSetting.DPadRight)
				|| name == nameof(PadSetting.DPadUp);
		}

		public static bool IsThumbAxis(string name)
		{
			return name == nameof(PadSetting.LeftThumbAxisX)
				|| name == nameof(PadSetting.LeftThumbAxisY)
				|| name == nameof(PadSetting.RightThumbAxisX)
				|| name == nameof(PadSetting.RightThumbAxisY);
		}

	}
}
