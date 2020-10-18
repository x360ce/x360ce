using SharpDX.XInput;
using System.Linq;

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


		/// <summary>
		///  Add range/axis type mapping.
		/// </summary>
		public Map(string value, TargetType target, short axisValue)
		{
			Target = target;
			Load(value);
			AxisValue = axisValue;
		}

		void Load(string value)
		{
			SettingsConverter.TryParseIniValue(value, out Type, out Index);
			IsButton = SettingsConverter.IsButton(Type);
			IsAxis = SettingsConverter.IsAxis(Type);
			IsSlider = SettingsConverter.IsSlider(Type);
			IsHalf = SettingsConverter.IsHalf(Type);
			IsInverted = SettingsConverter.IsInverted(Type);
		}

		// Source Parameters.
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
		public short? AxisValue;

		// Used for Thumbs and Triggers.
		public int AntiDeadZone;
		public int Linear;

	}
}
