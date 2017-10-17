using SharpDX.XInput;

namespace x360ce.Engine
{
	public class ButtonMap
	{

		public ButtonMap(GamepadButtonFlags flag, string value, string deadZone = null)
		{
			Flag = flag;
			SettingsConverter.TryParseIndexAndType(value, out Index, out Type);
			if (!string.IsNullOrEmpty(deadZone))
				int.TryParse(deadZone, out DeadZone);
			IsAxis = Type == SettingType.Axis || Type == SettingType.IAxis || Type == SettingType.HAxis || Type == SettingType.IHAxis;
			IsSlider = Type == SettingType.Slider || Type == SettingType.ISlider || Type == SettingType.HSlider || Type == SettingType.IHSlider;
			IsHalf = Type == SettingType.HAxis || Type == SettingType.IHAxis || Type == SettingType.HSlider || Type == SettingType.IHSlider;
			IsInverted = Type == SettingType.IAxis || Type == SettingType.IHAxis || Type == SettingType.ISlider || Type == SettingType.IHSlider;
		}

		public bool IsAxis;
		public bool IsSlider;
		public bool IsHalf;
		public bool IsInverted;
		public SettingType Type;
		public int Index;
		public GamepadButtonFlags Flag;
		public int DeadZone;
	}
}
