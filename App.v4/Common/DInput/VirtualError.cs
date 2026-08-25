using System.ComponentModel;

namespace x360ce.App
{
	public enum VirtualError
	{
		None = 0,
		[Description("Virtual Device {0} is already owned by this feeder.")]
		Owned = 1,
		[Description("Virtual Device {0} is owned by another feeder.")]
		Busy = 2,
		[Description("Virtual Device {0} is free.")]
		Free = 3,
		[Description("Virtual Device {0} is not installed or disabled.")]
		Missing = 4,
		[Description("Virtual Device {0} general error.")]
		Other = 5,
		[Description("Virtual Device {0} invalid index.")]
		Index = 6,
	}
}
