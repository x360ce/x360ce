using JocysCom.ClassLibrary.IO;
using SharpDX.DirectInput;
using System;
using x360ce.Engine;

namespace x360ce.App
{
	public class DiDevice
	{
		/// <summary>DInput Device State.</summary>
		public Joystick Device;
		/// <summary>DInput Device Info.</summary>
		public DeviceInfo Info;
		/// <summary>DInput Device Instance.</summary>
		public DeviceInstance Instance;
		/// <summary>Previous DInput Device Instance.</summary>
		public DeviceInstance InstanceOld;

		public string InstanceId
		{
			get
			{
				return EngineHelper.GetID(InstanceGuid);
			}
		}

		public string VendorName
		{
			get
			{
				var o = Info;
				return o == null ? "" : o.Manufacturer;
			}
		}

		public string ProductName
		{
			get
			{
				var o = Instance;
				return o == null ? "" : o.ProductName;
			}
		}

		public Guid InstanceGuid
		{
			get
			{
				var o = Instance;
				return o == null ? Guid.Empty : o.InstanceGuid;
			}
		}

		public string DeviceId
		{
			get
			{
				var o = Info;
				return o == null ? "" : o.DeviceId;
			}
		}


	}
}
