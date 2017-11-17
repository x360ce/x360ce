using SharpDX.XInput;

namespace x360ce.Engine
{
	public class Map
	{

		/// <summary>
		/// Add binary/button type mapping.
		/// </summary>
		/// <param name="deadZone">Used when source is range/axis type.</param>
		public Map(string value, GamepadButtonFlags flag, string deadZone)
		{
			Target = TargetType.Button;
			Load(value);
			ButtonFlag = flag;
			int.TryParse(deadZone, out DeadZone);
		}

		/// <summary>
		///  Add range/axis type mapping.
		/// </summary>
		public Map(string value, TargetType target, string deadZone, string antiDeadZone, string linear)
		{
			Target = target;
			Load(value);
			int.TryParse(deadZone, out DeadZone);
			int.TryParse(antiDeadZone, out AntiDeadZone);
			int.TryParse(linear, out Linear);
		}

		void Load(string value)
		{
			SettingsConverter.TryParseIniValue(value, out Type, out Index);
			IsButton = Type == SettingType.Button || Type == SettingType.IButton;
			IsAxis = Type == SettingType.Axis || Type == SettingType.IAxis || Type == SettingType.HAxis || Type == SettingType.IHAxis;
			IsSlider = Type == SettingType.Slider || Type == SettingType.ISlider || Type == SettingType.HSlider || Type == SettingType.IHSlider;
			IsHalf = Type == SettingType.HAxis || Type == SettingType.IHAxis || Type == SettingType.HSlider || Type == SettingType.IHSlider;
			IsInverted = Type == SettingType.IAxis || Type == SettingType.IHAxis || Type == SettingType.ISlider || Type == SettingType.IHSlider;
		}

		// Source Paramaters.
		public SettingType Type;
		public int Index;

		public bool IsButton;
		public bool IsAxis;
		public bool IsSlider;
		public bool IsHalf;
		public bool IsInverted;

		public TargetType Target;

		// Used for Buttons.
		public GamepadButtonFlags ButtonFlag;

		// Used for Buttons (AxisToButton DeadZone), Thumbs and Triggers.
		public int DeadZone;

		// Used for Thumbs and Triggers.
		public int AntiDeadZone;
		public int Linear;

	}
}
