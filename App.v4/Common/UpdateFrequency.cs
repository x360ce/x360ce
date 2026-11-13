using System.ComponentModel;
using System.Runtime.Serialization;

namespace x360ce.App
{
	/// <summary>How often the engine runs a pass. Each value is the time between two passes, in microseconds.</summary>
	/// <remarks>The settings store the name, not the number; the Polling Rate list shows the description.</remarks>
	public enum UpdateFrequency: int
	{
		[Description("4000 Hz (0.25 ms)")]
		us250_4000Hz = 250,
		[Description("1000 Hz (1 ms)")]
		ms1_1000Hz = 1000,
		[Description("500 Hz (2 ms)")]
		ms2_500Hz = 2000,
		//ms3_333Hz = 3000,
		[Description("250 Hz (4 ms)")]
		ms4_250Hz = 4000,
		//ms5_200Hz = 5000,
		//ms6_166Hz = 6000,
		//ms7_142Hz = 7000,
		[Description("125 Hz (8 ms)")]
		ms8_125Hz = 8000,
	}
}
