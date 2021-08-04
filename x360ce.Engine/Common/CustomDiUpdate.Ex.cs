using SharpDX.DirectInput;

namespace x360ce.Engine
{
	/// <summary>
	///  Custom X360CE direct input update class used for configuration.
	/// </summary>
	public partial class CustomDiUpdate
	{

		public CustomDiUpdate(MouseUpdate update)
		{
			Value = update.Value;
			Index = CustomDiHelper.MouseAxisOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Axis;
				return;
			}
			Type = MapType.Button;
		}

		public CustomDiUpdate(KeyboardUpdate update)
		{
			Value = update.Value;
			Index = (int)update.Key;
			Value = update.IsPressed ? 1 : 0;
			Type = MapType.Button;
		}

		public CustomDiUpdate(JoystickUpdate update)
		{
			Value = update.Value;
			Index = CustomDiHelper.AxisOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Axis;
				return;
			}
			Index = CustomDiHelper.SliderOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Slider;
				return;
			}
			Index = CustomDiHelper.POVOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.POV;
				return;
			}
			Index = CustomDiHelper.ButtonOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Button;
				return;
			}
		}

	}
}
