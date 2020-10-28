using SharpDX.XInput;

namespace x360ce.Engine
{

	/// <summary>
	/// Loads string value and creates mapping object.
	/// </summary>
	public class Map
	{

		/// <summary>
		/// Add button mapping.
		/// </summary>
		/// <param name="deadZone">Used when source is range/axis type.</param>
		public Map(MapCode code, string value, GamepadButtonFlags flag, string deadZone)
		{
			Target = TargetType.Button;
			Load(value);
			ButtonFlag = flag;
			int.TryParse(deadZone, out DeadZone);
		}

		/// <summary>
		///  Add trigger and axis mapping: [Left|Right] [Trigger|ThumbAxisX|ThumbAxisY].
		/// </summary>
		public Map(MapCode code, string value, TargetType target, string deadZone, string antiDeadZone, string linear)
		{
			Target = target;
			Load(value);
			int.TryParse(deadZone, out DeadZone);
			int.TryParse(antiDeadZone, out AntiDeadZone);
			int.TryParse(linear, out Linear);
		}


		/// <summary>
		///  Add thumb mapping: [Left|Right] Thumb [Up|Left|Right|Down].
		/// </summary>
		public Map(MapCode code, string value, TargetType target, short axisValue)
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

		MapCode Code;

		// Source Parameters.
		public MapType Type;
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
