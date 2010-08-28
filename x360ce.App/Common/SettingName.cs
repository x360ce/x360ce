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

		// [Options] section.
		[DefaultValue("1"), Description("Use 0 to 1; default 1; beep on init.")]
		public const string UseInitBeep = "UseInitBeep";
		[DefaultValue("0"), Description("0 - Suspend errors; 1 - throw application errors.")]
		public const string DebugMode = "DebugMode";
		[DefaultValue("0"), Description("use 0 to 1; creates a log file in folder 'x360ce logs'.")]
		public const string Log = "Log";

		// [FakeAPI] section.
		[DefaultValue("1"), Description("WMI API patching, 1 only USB, 2 USB and HID, 0 disable.")]
		public const string FakeMode = "FakeMODE";
		[DefaultValue("1"), Description("Don't change PID/VID in FakeWMI spoofing, so this add only IG_00 string.")]
		public const string FakeWmiNoPidVid = "FakeWMI_NOPIDVID";

		// [PAD] section.
		[DefaultValue("Unknown Device"), Description("Device product name.")]
		public const string ProductName = "ProductName";
		[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device product GUID.")]
        public const string ProductGuid = "ProductGuid";
		[DefaultValue("00000000-0000-0000-0000-000000000000"), Description("Device instance GUID.")]
        public const string InstanceGuid = "InstanceGuid";
		[DefaultValue("1"), Description("Device Type. None = 0, Gamepad = 1, Wheel = 2, Stick = 3, FlightStick = 4, DancePad = 5, Guitar = 6, DrumKit = 8.")]
		public const string GamePadType = "ControllerType";
		[DefaultValue("0"), Description("Native mode, calls system xinput1_3.dll to support xinput compatible controller together with emulated.")]
		public const string NativeMode = "Native";

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
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string LeftShoulder = "Left Shoulder";
		[DefaultValue("0"), Description("Button Id. Disable = 0.")]
		public const string RightShoulder = "Right Shoulder";

		// Triggers.
		[DefaultValue("0"), Description("Button id; precede with 'a' for an axis; 's' for a slider; 'x' for a half range axis; 'h' for half slider; use '-' to invert ie. x-2.")]
		public const string LeftTrigger = "Left Trigger";
		[DefaultValue("0"), Description("[0-255]; default 0; add deadzone to trigger.")]
		public const string LeftTriggerDeadZoneX = "Left Analog X DeadZone";
		[DefaultValue("0"), Description("[0-255]; default 0; add deadzone to trigger.")]
		public const string LeftTriggerDeadZoneY = "Left Analog Y DeadZone";
		[DefaultValue("0"), Description("Button id. [asxh][-][0-128] axis = 'a', slider = 's'; half axis = 'x', half slider = 'h', invert = '-'. Example: 'x-2'.")]
		public const string RightTrigger = "Right Trigger";
		[DefaultValue("0"), Description("[0-255] add deadzone to trigger.")]
		public const string RightTriggerDeadZoneX = "Right Analog X DeadZone";
		[DefaultValue("0"), Description("[0-255] add deadzone to trigger.")]
		public const string RightTriggerDeadZoneY = "Right Analog Y DeadZone";

		// Force feedback.
		[DefaultValue("0"), Description("[0,1] Use force feedback. Disabled = 0, Enabled = 1.")]
		public const string ForceEnable = "UseForceFeedback";
		[DefaultValue("0"), Description("Swap motor. Disabled = 0, Enabled = 1.")]
		public const string ForceSwapMotor = "SwapMotor";
		[DefaultValue("100"), Description("Strenght of force feedback. Use 0 to 100.")]
		public const string ForceOverall = "ForcePercent";
		[DefaultValue("60"), Description("Left motor period. Use 0 to 500.")]
		public const string LeftMotorPeriod = "LeftMotorPeriod";
		[DefaultValue("120"), Description("Right motor period. Use 0 to 500.")]
		public const string RightMotorPeriod = "RightMotorPeriod";



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

		public static int _maxNameLength;
		public static int MaxNameLength
		{
			get
			{
				if (_maxNameLength > 0) return _maxNameLength;
				var o = new SettingName();
				FieldInfo[] items = o.GetType().GetFields(BindingFlags.Static | BindingFlags.Public);
				int max = 0;
				for (int i = 0; i < items.Length; i++)
				{
					if (!items[i].IsLiteral) continue;
					max = Math.Max(max, ((string)items[i].GetValue(o)).Length);
				}
				_maxNameLength = max;
				return _maxNameLength;
			}
		}

		static FieldInfo GetFieldInfo(string key)
		{
			var o = new SettingName();
			FieldInfo[] items = o.GetType().GetFields(BindingFlags.Static | BindingFlags.Public);
			for (int i = 0; i < items.Length; i++)
			{
				if (!items[i].IsLiteral) continue;
				var value = (string)items[i].GetValue(o);
				if (value == key) return items[i];
			}
			return null;
		}

		/// <summary>
		/// Get setting description.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string GetDescription(string key)
		{
			var comment = string.Empty;
			var info = GetFieldInfo(key);
			if (info == null) return string.Empty;
			DescriptionAttribute a = (DescriptionAttribute)info.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
			return a.Description;
		}

		/// <summary>
		/// Get setting default value.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static string GetDefaultValue(string key)
		{
			var comment = string.Empty;
			var info = GetFieldInfo(key);
			if (info == null) return string.Empty;
			DefaultValueAttribute a = (DefaultValueAttribute)info.GetCustomAttributes(typeof(DefaultValueAttribute), false)[0];
			return (string)a.Value;
		}

	}
}
