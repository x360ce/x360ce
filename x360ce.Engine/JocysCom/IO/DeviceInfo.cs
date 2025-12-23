using System;
namespace JocysCom.ClassLibrary.IO
{
	public class DeviceInfo
	{
		public DeviceInfo()
		{
			// Set all string values to empty for entities.
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

		public uint VendorId { get; set; }

		public uint ProductId { get; set; }

		public uint Revision { get; set; }

		public string Description { get; set; }

		public string FriendlyName { get; set; }

		public string DeviceId { get; set; }

		public string HardwareIds { get; set; }

		public uint DeviceHandle { get; set; }

		/// <summary>Device HID Interface Path.</summary>
		public string DevicePath { get; set; }

		public string ParentDeviceId { get; set; }

		public string ParentHardwareId { get; set; }

		public Guid ClassGuid { get; set; }

		public string ClassDescription { get; set; }

		public Win32.DeviceNodeStatus Status { get; set; }

		public bool IsHidden { get { return ((Status & Win32.DeviceNodeStatus.DN_NO_SHOW_IN_DM) != 0); } }
		public bool IsRemovable { get { return (Status == 0 && Description.Length > 0) || ((Status & Win32.DeviceNodeStatus.DN_REMOVABLE) != 0); } }
		public bool IsPresent { get { return (Status != 0); } }

	}
}
