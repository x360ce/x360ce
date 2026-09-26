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
		[Description("Windows did not finish building the virtual controller for Controller {0} in time, so it was taken away again. It is tried again shortly.")]
		PlaceNotGiven = 8,
		[Description("XInput {0} is held by another controller, so Controller {0} makes no virtual controller until it is free. Use Auto-Order on the Devices page to move the other controller, or unplug it.")]
		PlaceTaken = 9,
		[Description("Windows put the virtual controller for Controller {0} in another XInput place, where it would push out another controller, so it was taken away again. It is tried again when a controller arrives or leaves.")]
		PlaceWrong = 10,
	}
}
