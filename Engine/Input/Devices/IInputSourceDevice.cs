using System;

namespace x360ce.Engine
{
	/// <summary>
	/// A device as every input source names it, so the device list, the twin search and the pages treat a DirectInput
	/// device and a Raw Input one alike. Each source's device adds what only it has: DirectInput's force feedback, Raw
	/// Input's report layout.
	/// </summary>
	public interface IInputSourceDevice
	{
		/// <summary>The API the device is read through.</summary>
		InputSourceType Source { get; }

		/// <summary>The same for the device on every connection and every start.</summary>
		Guid InstanceGuid { get; }

		/// <summary>The product GUID DirectInput gives the device: {PPPPVVVV-0000-0000-0000-504944564944} for a HID device.</summary>
		Guid ProductGuid { get; }

		/// <summary>The device interface path, which both sources report for the same HID device, so it is what makes two of them twins; empty for a device that is not HID.</summary>
		string InterfacePath { get; }

		/// <summary>The name the device reports, or null when it reports none.</summary>
		string ProductName { get; }

		int VendorId { get; }

		int ProductId { get; }
	}
}
