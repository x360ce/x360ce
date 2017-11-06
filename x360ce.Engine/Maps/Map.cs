using SharpDX.XInput;

namespace x360ce.Engine
{
	public class Map
	{

		/// <summary>
		/// Load button parametes.
		/// </summary>
		public Map(string value, GamepadButtonFlags flag, string deadZone = null)
		{
			Load(value);

			ButtonFlag = flag;
			if (!string.IsNullOrEmpty(deadZone))
				int.TryParse(deadZone, out DeadZone);
			Target = TargetType.Button;
		}

		void Load(string value)
		{
			// Source parameters.
			SettingsConverter.TryParseIniValue(value, out Type, out Index);
			IsButton = Type == SettingType.Button || Type == SettingType.IButton;
			IsAxis = Type == SettingType.Axis || Type == SettingType.IAxis || Type == SettingType.HAxis || Type == SettingType.IHAxis;
			IsSlider = Type == SettingType.Slider || Type == SettingType.ISlider || Type == SettingType.HSlider || Type == SettingType.IHSlider;
			IsHalf = Type == SettingType.HAxis || Type == SettingType.IHAxis || Type == SettingType.HSlider || Type == SettingType.IHSlider;
			IsInverted = Type == SettingType.IAxis || Type == SettingType.IHAxis || Type == SettingType.ISlider || Type == SettingType.IHSlider;
			Target = TargetType.Button;
		}

		//int Min;
		//int Max;

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
		// Used for AxisToButton DeadZone
		public int DeadZone;

	}
}
