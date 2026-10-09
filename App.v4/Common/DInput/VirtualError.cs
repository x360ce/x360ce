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
		[Description("Windows did not finish building the virtual controller for Controller {0} in time, so it was taken away again. It is tried again when a controller arrives or leaves.")]
		PlaceNotGiven = 8,
		[Description("XInput {0} is held by {1}, so Controller {0} makes no virtual controller until it is free. Use Auto-Order on the Devices page to move it, or unplug it.")]
		PlaceTaken = 9,
		[Description("Windows put the virtual controller for Controller {0} in another XInput place, where it would push out another controller, so it was taken away again. It is tried again when a controller arrives or leaves.")]
		PlaceWrong = 10,
		[Description("XInput did not answer, so the virtual controller for Controller {0} was not made, or was taken away again. It is tried again when XInput answers, or, if it was taken away, when a controller arrives or leaves.")]
		NotAnswering = 11,
		[Description("The driver would not remove a virtual controller made for Controller {0}, so no other is made for Controller {0} while the driver holds it. Repair the driver on the Issues tab, or restart x360ce.")]
		RemovalRefused = 12,
	}
}
