using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace x360ce.App
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
			public const string Button = "";
			public const string Axis = "a";
			public const string IAxis = "-a";
			public const string HAxis = "x";
			public const string IHAxis = "-x";
			public const string Slider = "s";
			public const string ISlider = "-s";
			public const string HSlider = "h";
			public const string IHSlider = "-h";
			public const string DPad = "p";
			public const string DPadButton = "v";
		}

		public const string DefaultInternetDatabaseUrl = "http://www.x360ce.com/webservices/x360ce.asmx";
        public const string DefaultVersion = "1";
        public const string Mappings = "Mappings";

		// [Options] section.
		[DefaultValue("1"), Description("Use 0 to 1; default 1; beep on init.")]
		public const string UseInitBeep = "UseInitBeep";
		[DefaultValue("0"), Description("0 - Suspend errors; 1 - throw application errors.")]
		public const string DebugMode = "DebugMode";
		[DefaultValue("0"), Description("Use 0 to 1; creates a log file in folder 'x360ce logs'.")]
		public const string Log = "Log";
		[DefaultValue("0"), Description("Use 0 to 1; creates console log window.")]
		public const string Console = "Console";
		[DefaultValue(DefaultInternetDatabaseUrl), Description("Internet settings database URL.")]
		public const string InternetDatabaseUrl = "InternetDatabaseUrl";
		[DefaultValue("0"), Description("Internet features: 0 - Disable; 1 - Enable.")]
		public const string InternetFeatures = "InternetFeatures";
		[DefaultValue("1"), Description("Autoload settings when Internet Settings tab is selected: 0 - Disable; 1 - Enable.")]
		public const string InternetAutoload = "InternetAutoload";
		[DefaultValue("1"), Description("Allow only one copy of Application at a time: 0 - Disable; 1 - Enable.")]
		public const string AllowOnlyOneCopy = "AllowOnlyOneCopy";
        [DefaultValue(""), Description("Game Scan Locations. Separated by semicolon (;).")]
        public const string ProgramScanLocations = "ProgramScanLocations";
        [DefaultValue("1"), Description("Configuration file version.")]
        public const string Version = "Version";

		// [InputHook] section.
		[DefaultValue("1"), Description("WMI API patching, 1 only USB, 2 USB and HID, 0 disable.")]
		public const string HookMode = "HookMode";

		// [Mappings] section.
		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD1.")]
		public const string PAD1 = "PAD1";
		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD1.")]
		public const string PAD2 = "PAD2";
		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD1.")]
		public const string PAD3 = "PAD3";
		[DefaultValue(""), Description("Configuration name of the section which is mapped to PAD1.")]
		public const string PAD4 = "PAD4";

		// [PAD] section.
		[DefaultValue("Unknown Device"), Description("Device product name.")]
		public const string ProductName = "ProductName";
		[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device product GUID.")]
		public const string ProductGuid = "ProductGuid";
		[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device instance GUID.")]
		public const string InstanceGuid = "InstanceGuid";
		[DefaultValue("1"), Description("Device Type. None = 0, Gamepad = 1, Wheel = 2, Stick = 3, FlightStick = 4, DancePad = 5, Guitar = 6, DrumKit = 8.")]
		public const string GamePadType = "ControllerType";
		[DefaultValue("0"), Description("Pass Through mode, calls system's native xinput1_3.dll to support xinput compatible controller together with emulated.")]
		public const string PassThrough = "PassThrough";

        // Default Mapping.
        [DefaultValue("0"), Description("Index of PAD which will be used to map this controller. Auto = 0, PAD Index = 1-4.")]
        public const string MapToPad = "MapToPad";

		// Left Thumb.
		[DefaultValue("0"), Description("Left stick button. Disable = 0.")]
		public const string LeftThumbButton = "Left Thumb";
		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		public const string LeftThumbAxisX = "Left Analog X";
		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		public const string LeftThumbAxisY = "Left Analog Y";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string LeftThumbRight = "Left Analog X+ Button";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string LeftThumbLeft = "Left Analog X- Button";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string LeftThumbUp = "Left Analog Y+ Button";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string LeftThumbDown = "Left Analog Y- Button";
		[DefaultValue("0"), Description("[0;32767]; default 0; add deadzone to left thumb X.")]
		public const string LeftThumbDeadZoneX = "Left Analog X DeadZone";
		[DefaultValue("0"), Description("[0;32767]; default 0; add deadzone to left thumb Y.")]
		public const string LeftThumbDeadZoneY = "Left Analog Y DeadZone";
		[DefaultValue("0"), Description("[0;32767]; default 0; remove in-game deadzone for left thumb X.")]
		public const string LeftThumbAntiDeadZoneX = "Left Analog X AntiDeadZone";
		[DefaultValue("0"), Description("[0;32767]; default 0;  remove in-game deadzone for Left thumb Y.")]
		public const string LeftThumbAntiDeadZoneY = "Left Analog Y AntiDeadZone";
		[DefaultValue("0"), Description("[-100;100]; default 0; Raise this number to increase sensitivity near center")]
		public const string LeftThumbLinearX = "Left Analog X Linear";
		[DefaultValue("0"), Description("[-100;100]; default 0; Raise this number to increase sensitivity near center")]
		public const string LeftThumbLinearY = "Left Analog Y Linear";


		// Right Thumb.
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string RightThumbButton = "Right Thumb";
		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		public const string RightThumbAxisX = "Right Analog X";
		[DefaultValue("0"), Description("Axis index; use - to invert; precede with 's' for a slider eg; s-1; 7 to disable.")]
		public const string RightThumbAxisY = "Right Analog Y";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string RightThumbRight = "Right Analog X+ Button";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string RightThumbLeft = "Right Analog X- Button";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string RightThumbUp = "Right Analog Y+ Button";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string RightThumbDown = "Right Analog Y- Button";
		[DefaultValue("0"), Description("[0;32767]; default 0; add deadzone to right thumb X.")]
		public const string RightThumbDeadZoneX = "Right Analog X DeadZone";
		[DefaultValue("0"), Description("[0;32767]; default 0; add deadzone to right thumb Y.")]
		public const string RightThumbDeadZoneY = "Right Analog Y DeadZone";
		[DefaultValue("0"), Description("[0;32767]; default 0; remove in-game deadzone for right thumb X.")]
		public const string RightThumbAntiDeadZoneX = "Right Analog X AntiDeadZone";
		[DefaultValue("0"), Description("[0;32767]; default 0;  remove in-game deadzone for right thumb Y.")]
		public const string RightThumbAntiDeadZoneY = "Right Analog Y AntiDeadZone";
		[DefaultValue("0"), Description("[-100;100]; default 0; Raise this number to increase sensitivity near center")]
		public const string RightThumbLinearX = "Right Analog X Linear";
		[DefaultValue("0"), Description("[-100;100]; default 0; Raise this number to increase sensitivity near center")]
		public const string RightThumbLinearY = "Right Analog Y Linear";

		// D-Pad.
		[DefaultValue("0"), Description("Disable = 0, POV Index = N.")]
		public const string DPad = "D-pad POV";
		[DefaultValue("UP"), Description("D-Pad up button.")]
		public const string DPadUp = "D-pad Up";
		[DefaultValue("DOWN"), Description("D-Pad down button.")]
		public const string DPadDown = "D-pad Down";
		[DefaultValue("LEFT"), Description("D-Pad left button.")]
		public const string DPadLeft = "D-pad Left";
		[DefaultValue("RIGHT"), Description("D-Pad right button.")]
		public const string DPadRight = "D-pad Right";

		// Axis To D-Pad
		[DefaultValue("0"), Description("Axis to control DPad. Disabled = 0, Enabled = 1.")]
		public const string AxisToDPadEnabled = "AxisToDPad";
		[DefaultValue("256"), Description("Dead zone for axis.")]
		public const string AxisToDPadDeadZone = "AxisToDPadDeadZone";
		[DefaultValue("0"), Description("Axis to D-Pad offset.")]
		public const string AxisToDPadOffset = "AxisToDPadOffset";

		// Button names.
		[DefaultValue("0"), Description("Big button.")]
		public const string ButtonBig = "Big";
		[DefaultValue("0"), Description("Guide button.")]
		public const string ButtonGuide = "GuideButton";
		[DefaultValue("0"), Description("Back button.")]
		public const string ButtonBack = "Back";
		[DefaultValue("0"), Description("Start button.")]
		public const string ButtonStart = "Start";
		[DefaultValue("0"), Description("Button 'A'")]
		public const string ButtonA = "A";
		[DefaultValue("0"), Description("Button 'B'")]
		public const string ButtonB = "B";
		[DefaultValue("0"), Description("Button 'X'")]
		public const string ButtonX = "X";
		[DefaultValue("0"), Description("Button 'Y'")]
		public const string ButtonY = "Y";
		[DefaultValue("0"), Description("Left Shoulder Button . Disable = 0.")]
		public const string LeftShoulder = "Left Shoulder";
		[DefaultValue("0"), Description("Right Shoulder Button. Disable = 0.")]
		public const string RightShoulder = "Right Shoulder";

		// Triggers.
		[DefaultValue("0"), Description("Button id; precede with 'a' for an axis; 's' for a slider; 'x' for a half range axis; 'h' for half slider; use '-' to invert ie. x-2.")]
		public const string LeftTrigger = "Left Trigger";
		[DefaultValue("0"), Description("[0-255] add deadzone to left trigger.")]
		public const string LeftTriggerDeadZone = "TriggerDeadzone";
		[DefaultValue("0"), Description("Button id. [asxh][-][0-128] axis = 'a', slider = 's'; half axis = 'x', half slider = 'h', invert = '-'. Example: 'x-2'.")]
		public const string RightTrigger = "Right Trigger";
		[DefaultValue("0"), Description("[0-255] add deadzone to right trigger.")]
		public const string RightTriggerDeadZone = "RightTriggerDeadZone";

		// Force feedback.
		[DefaultValue("0"), Description("[0,1] Use force feedback. Disabled = 0, Enabled = 1.")]
		public const string ForceEnable = "UseForceFeedback";
		[DefaultValue("0"), Description("[0-2] Force feedback type.")]
		public const string ForceType = "FFBType";
		[DefaultValue("0"), Description("Swap motor. Disabled = 0, Enabled = 1.")]
		public const string ForceSwapMotor = "SwapMotor";
		[DefaultValue("100"), Description("Strenght of force feedback. Use 0 to 100.")]
		public const string ForceOverall = "ForcePercent";
		[DefaultValue("60"), Description("Left motor period. Use 0 to 500.")]
		public const string LeftMotorPeriod = "LeftMotorPeriod";
		[DefaultValue("120"), Description("Right motor period. Use 0 to 500.")]
		public const string RightMotorPeriod = "RightMotorPeriod";

		public static int GetPadIndex(string path)
		{
			var section = path.Split('\\')[0];
			var pads = new List<string>() { SettingName.PAD1, SettingName.PAD2, SettingName.PAD3, SettingName.PAD4 };
			return pads.IndexOf(section);
		}

		public static bool IsButton(string name)
		{
			return name == SettingName.LeftThumbButton
				|| name == SettingName.LeftThumbUp
				|| name == SettingName.LeftThumbRight
				|| name == SettingName.LeftThumbDown
				|| name == SettingName.LeftThumbLeft
				|| name == SettingName.RightThumbButton
				|| name == SettingName.RightThumbUp
				|| name == SettingName.RightThumbRight
				|| name == SettingName.RightThumbDown
				|| name == SettingName.RightThumbLeft;
		}

		public static bool IsDPad(string name)
		{
			return name == SettingName.DPad
				|| name == SettingName.DPadDown
				|| name == SettingName.DPadLeft
				|| name == SettingName.DPadRight
				|| name == SettingName.DPadUp;
		}

		public static bool IsThumbAxis(string name)
		{
			return name == SettingName.LeftThumbAxisX
				|| name == SettingName.LeftThumbAxisY
				|| name == SettingName.RightThumbAxisX
				|| name == SettingName.RightThumbAxisY;
		}

	}
}
