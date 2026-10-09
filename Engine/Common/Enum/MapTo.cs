using System.ComponentModel;

namespace x360ce.Engine
{
	public enum MapTo : int
	{
		[Description("Disabled")]
		Disabled = -1,
		[Description("None")]
		None = 0,
		[Description("Controller 1")]
		Controller1 = 1,
		[Description("Controller 2")]
		Controller2 = 2,
		[Description("Controller 3")]
		Controller3 = 3,
		[Description("Controller 4")]
		Controller4 = 4,
	}
}
