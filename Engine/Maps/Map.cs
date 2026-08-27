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
			// A formula is compiled once here, where a mapping is read, rather than every time the
			// controller is polled. What follows describes a single control and cannot describe a
			// formula, so the two are kept apart and only one of them is ever set.
			if (MapExpression.IsExpression(value))
			{
				string error;
				int position;
				MapExpression.TryParse(value, out Expression, out error, out position);
				return;
			}
			SettingsConverter.TryParseIniValue(value, out Type, out Index);
			IsButton = SettingsConverter.IsButton(Type);
			IsAxis = SettingsConverter.IsAxis(Type);
			IsSlider = SettingsConverter.IsSlider(Type);
			IsHalf = SettingsConverter.IsHalf(Type);
			IsInverted = SettingsConverter.IsInverted(Type);
		}

		/// <summary>
		/// The formula this row is driven by, or null when it is mapped to a single control.
		/// </summary>
		/// <remarks>
		/// Null also covers a formula that would not compile. A mapping that cannot be read does
		/// nothing, which is the same as one that was never set, and is what stops a mistyped formula
		/// from taking a controller down mid-game.
		/// </remarks>
		public MapExpression Expression;

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
