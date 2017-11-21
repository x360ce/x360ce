using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.Engine
{
	public class DeviceEffectItem
	{

		public string Name { get; set; }
		public EffectParameterFlags StaticParameters { get; set; }
		public EffectParameterFlags DynamicParameters { get; set; }
	}

}
