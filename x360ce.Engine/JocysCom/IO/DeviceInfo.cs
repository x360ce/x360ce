using System;
namespace JocysCom.ClassLibrary.IO
{
	public class DeviceInfo
	{
		public DeviceInfo()
		{
		}

		public DeviceInfo(string deviceId, string parentDeviceId, string devicePath, string manufacturer, string description, Guid classGuid, string classDescription, Win32.DeviceNodeStatus status, uint vid, uint pid, uint rev)
		{
			DeviceId = deviceId ?? "";
			ParentDeviceId = parentDeviceId ?? "";
			Description = description ?? "";
			Manufacturer = manufacturer ?? "";
			ClassGuid = classGuid;
			ClassDescription = classDescription ?? "";
			Status = status;
			VendorId = vid;
			ProductId = pid;
			Revision = rev;
			DevicePath = devicePath ?? "";
		}

		public string Manufacturer { get; set; }

		public uint VendorId { get; set; }

		public uint ProductId { get; set; }

		public uint Revision { get; set; }
		public string Description { get; set; }

		public string DeviceId { get; set; }

		public string DevicePath { get; set; }

		public string ParentDeviceId { get; set; }

		public Guid ClassGuid { get; set; }

		public string ClassDescription { get; set; }

		public Win32.DeviceNodeStatus Status { get; set; }

		public bool IsHidden { get { return ((Status & Win32.DeviceNodeStatus.DN_NO_SHOW_IN_DM) != 0); } }
		public bool IsRemovable { get { return (Status == 0 && Description.Length > 0) || ((Status & Win32.DeviceNodeStatus.DN_REMOVABLE) != 0); } }
		public bool IsPresent { get { return (Status != 0); } }

	}
}
