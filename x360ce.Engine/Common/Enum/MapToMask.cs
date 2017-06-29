using System;
using System.ComponentModel;

namespace x360ce.Engine
{
	[Flags]
	public enum MapToMask
	{
		[Description("None")]
		None = 0,
		[Description("Controller 1")]
		Controller1 = 1,
		[Description("Controller 2")]
		Controller2 = 2,
		[Description("Controller 3")]
		Controller3 = 4,
		[Description("Controller 4")]
		Controller4 = 8,
	}
}
