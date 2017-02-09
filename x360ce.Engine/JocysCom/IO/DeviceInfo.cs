using System;
namespace JocysCom.ClassLibrary.IO
{
	public class DeviceInfo
	{

		public DeviceInfo(string deviceId, string parentDeviceId, string devicePath, string manufacturer, string description, Guid classGuid, string classDescription, Win32.DeviceNodeStatus status, uint vid, uint pid, uint rev)
		{
			_DeviceId = deviceId ?? "";
			_ParentDeviceId = parentDeviceId ?? "";
			_Description = description ?? "";
			_Manufacturer = manufacturer ?? "";
            _ClassGuid = classGuid;
			_ClassDescription = classDescription ?? "";
			_Status = status;
			_VendorId = vid;
			_ProductId = pid;
			_Revision = rev;
			_DevicePath = devicePath ?? "";
		}

		private string _Manufacturer;
		public string Manufacturer { get { return _Manufacturer; } }

		private uint _VendorId;
		public uint VendorId { get { return _VendorId; } }

		private uint _ProductId;
		public uint ProductId { get { return _ProductId; } }

		private uint _Revision;
		public uint Revision { get { return _Revision; } }

		private string _Description;
		public string Description { get { return _Description; } }

		private string _DeviceId;
		public string DeviceId { get { return _DeviceId; } }

		private string _DevicePath;
		public string DevicePath { get { return _DevicePath; } }

		private string _ParentDeviceId;
		public string ParentDeviceId { get { return _ParentDeviceId; } }

		private Guid _ClassGuid;
		public Guid ClassGuid { get { return _ClassGuid; } }

		private string _ClassDescription;
		public string ClassDescription { get { return _ClassDescription; } }

		private Win32.DeviceNodeStatus _Status;
		public Win32.DeviceNodeStatus Status { get { return _Status; } }

		public bool IsHidden { get { return ((_Status & Win32.DeviceNodeStatus.DN_NO_SHOW_IN_DM) != 0); } }
		public bool IsRemovable { get { return (_Status == 0 && Description.Length > 0) || ((_Status & Win32.DeviceNodeStatus.DN_REMOVABLE) != 0); } }
		public bool IsPresent { get { return (_Status  != 0); } }

	}
}
