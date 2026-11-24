using System;

namespace x360ce.Plugins
{
	/// <summary>Real physical hardware identity reported by a plugin device once connected to an actual machine.</summary>
	public sealed class PluginDeviceIdentity
	{
		/// <summary>True once connected to a real physical machine with verified identity; false while unconnected or generic.</summary>
		public bool IsConnected { get; set; }

		/// <summary>Real hardware manufacturer name (for example, "Tunturi", "Wahoo Fitness", "Concept2").</summary>
		public string Manufacturer { get; set; }

		/// <summary>Real hardware product / model name (for example, "Tunturi E60R", "KICKR CORE").</summary>
		public string ProductName { get; set; }

		/// <summary>Hardware vendor ID (Bluetooth SIG or USB VID), or 0 when unavailable.</summary>
		public int VendorId { get; set; }

		/// <summary>Hardware product ID (Bluetooth or USB PID), or 0 when unavailable.</summary>
		public int ProductId { get; set; }

		/// <summary>Deterministic product GUID representing the real physical hardware model for x360ce's online database and TOP Controllers.</summary>
		public Guid ProductGuid { get; set; }
	}

	/// <summary>
	/// Optional interface for a plugin device that connects to real external hardware (such as a Bluetooth Fitness Machine).
	/// While unconnected, the generic plugin device is not uploaded to the cloud; once connected, x360ce updates the device's
	/// <c>ProductGuid</c>, <c>DevVendorId</c>, <c>DevProductId</c>, manufacturer and product name to the real machine's values.
	/// </summary>
	public interface IPluginDeviceIdentity
	{
		/// <summary>The real connected hardware identity, or null while not connected to a physical machine.</summary>
		PluginDeviceIdentity HardwareIdentity { get; }

		/// <summary>
		/// Raised on the UI thread when the user picks a different external device so x360ce unchecks/disconnects the
		/// controller instead of automatically connecting to another person's nearby hardware.
		/// </summary>
		event EventHandler DisconnectRequested;
	}
}
