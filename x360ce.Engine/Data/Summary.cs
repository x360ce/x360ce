using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine.Data
{
	public partial class Summary
	{
		public Summary()
		{
			// Weight = 100 * Items mapped / Maximum items to map.
			// Maximum buttons = Math.Min(XInput Buttons/Axis/Sliders count, DInput Buttons/Axis/Sliders count)
		}

	}
}
