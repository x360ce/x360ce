using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

namespace x360ce.Engine
{
	/// <summary>
	///  Custom X360CE direct input update class used for configuration.
	/// </summary>
	public partial class SourceStateUpdate
	{

		public MapType Type;
		public int Index;
		public int Value;

		public SourceStateUpdate(JoystickUpdate update)
		{
			Value = update.Value;
			Index = DirectInputLayout.AxisOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Axis;
				return;
			}
			Index = DirectInputLayout.SliderOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Slider;
				return;
			}
			Index = DirectInputLayout.PovOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.POV;
				return;
			}
			Index = DirectInputLayout.ButtonOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = MapType.Button;
				return;
			}
		}

	}
}
