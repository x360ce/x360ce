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
	public partial class CustomDiUpdate
	{

		public SettingType Type;
		public int Index;
		public int Value;

		public CustomDiUpdate(JoystickUpdate update)
		{
			Value = update.Value;
			Index = CustomDiHelper.AxisOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = SettingType.Axis;
				return;
			}
			Index = CustomDiHelper.SliderOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = SettingType.Slider;
				return;
			}
			Index = CustomDiHelper.PovOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = SettingType.POV;
				return;
			}
			Index = CustomDiHelper.ButtonOffsets.IndexOf(update.Offset);
			if (Index > -1)
			{
				Type = SettingType.Button;
				return;
			}
		}

	}
}
