using System;
namespace JocysCom.ClassLibrary.IO
{
	/// <summary>
	/// Represents the metadata and state of a hardware device, bridging Windows API status flags
	/// and device registry properties for enumeration and UI operations.
	/// </summary>
	public class DeviceInfo
	{
		public DeviceInfo()
		{
			// Initialize string properties to empty to avoid null references when enumerating device metadata.
			Manufacturer = "";
			Description = "";
			FriendlyName = "";
			DeviceId = "";
			HardwareIds = "";
			DevicePath = "";
			ParentDeviceId = "";
			ParentHardwareId = "";
			ClassDescription = "";
		}

		public string Manufacturer { get; set; }

		/// <summary>USB Vendor ID (VID) parsed from the hardware ID string.</summary>
		public uint VendorId { get; set; }

		/// <summary>USB Product ID (PID) parsed from the hardware ID string.</summary>
		public uint ProductId { get; set; }

		/// <summary>Device revision number (REV) parsed from the hardware ID string.</summary>
		public uint Revision { get; set; }

		public string Description { get; set; }

		public string FriendlyName { get; set; }

		public string DeviceId { get; set; }

		public string HardwareIds { get; set; }

		public uint DeviceHandle { get; set; }

		/// <summary>Device Human Interface Device (HID) interface path.</summary>
		public string DevicePath { get; set; }

		/// <summary>Instance ID of the parent device in the device tree.</summary>
		public string ParentDeviceId { get; set; }

		/// <summary>Hardware ID string of the parent device.</summary>
		public string ParentHardwareId { get; set; }

		/// <summary>GUID of the device setup class.</summary>
		public Guid ClassGuid { get; set; }

		public string ClassDescription { get; set; }

		/// <summary>Raw status flags returned by CM_Get_DevNode_Status, representing device state.</summary>
		public Win32.DeviceNodeStatus Status { get; set; }

		/// <summary>True if the DN_NO_SHOW_IN_DM flag is set, indicating the device should be hidden in Device Manager.</summary>
		public bool IsHidden { get { return ((Status & Win32.DeviceNodeStatus.DN_NO_SHOW_IN_DM) != 0); } }

		/// <summary>True if the device is removable: either has no error status and a description, or has the DN_REMOVABLE flag.</summary>
		public bool IsRemovable { get { return (Status == 0 && Description.Length > 0) || ((Status & Win32.DeviceNodeStatus.DN_REMOVABLE) != 0); } }

		/// <summary>True if the device is currently present (Status ≠ 0).</summary>
		public bool IsPresent { get { return (Status != 0); } }

	}
}
