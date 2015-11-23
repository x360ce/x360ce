using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;

namespace x360ce.App
{
	public class DiDevice
	{
		/// <summary>DInput Device State.</summary>
		public Joystick State;
		/// <summary>DInput Device Info.</summary>
		public DeviceInfo Info;
		/// <summary>DInput Device Instance.</summary>
		public DeviceInstance Instance;
		/// <summary>Previous DInput Device Instance.</summary>
		public DeviceInstance InstanceOld;

		public string VendorName
		{
			get
			{
				var info = Info;
				return info == null ? "" : info.Manufacturer;
			}
		}

		public string ProductName
		{
			get
			{
				var instance = Instance;
				return instance == null ? "" : instance.ProductName;
			}
		}

		public Guid InstanceGuid
		{
			get
			{
				var instance = Instance;
				return instance == null ? Guid.Empty : instance.ProductGuid;
			}
		}

	}
}
