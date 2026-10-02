using System;
using System.Runtime.InteropServices;
using System.Text;

namespace x360ce.App.DInput
{
	/// <summary>Takes a USB device away and lets it arrive again by cycling the port it is plugged in to, as unplugging and plugging the cable does.</summary>
	/// <remarks>
	/// Windows removes a device politely, asking every program that has it open, and a program that does not let go of it can
	/// refuse. A cable pulled out asks nobody: the device goes, whatever has it open is left holding a handle to nothing, and the
	/// device arrives as a new one when the cable goes back in. Cycling the port is that, done by the hub the device is plugged in
	/// to, so a controller somebody has open can be taken away without asking them to unplug it. Needs Administrator.
	/// </remarks>
	public static class UsbPortCycle
	{
		static readonly Guid HubInterface = new Guid("F18A0E88-C30C-11D0-8815-00A0C906BED8");

		const uint AddressProperty = 0x1D;
		const uint IoctlCyclePort = 0x220444;
		const uint GenericWrite = 0x40000000;
		const uint ShareWrite = 2;
		const uint OpenExisting = 3;

		[StructLayout(LayoutKind.Sequential)]
		struct DeviceInterfaceData
		{
			public int Size;
			public Guid InterfaceClassGuid;
			public int Flags;
			public IntPtr Reserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct DeviceInfoData
		{
			public int Size;
			public Guid ClassGuid;
			public uint DevInst;
			public IntPtr Reserved;
		}

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		static extern int CM_Locate_DevNodeW(out uint devInst, string deviceId, uint flags);

		[DllImport("cfgmgr32.dll")]
		static extern int CM_Get_Parent(out uint parent, uint devInst, uint flags);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		static extern int CM_Get_Device_IDW(uint devInst, StringBuilder buffer, int length, uint flags);

		[DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
		static extern int CM_Get_DevNode_Registry_PropertyW(uint devInst, uint property, out uint dataType, out uint value, ref uint length, uint flags);

		[DllImport("setupapi.dll", SetLastError = true)]
		static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr parent, uint flags);

		[DllImport("setupapi.dll", SetLastError = true)]
		static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr deviceInfo, ref Guid classGuid, uint index, ref DeviceInterfaceData data);

		[DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref DeviceInterfaceData data, IntPtr detail, int size, out int required, ref DeviceInfoData info);

		[DllImport("setupapi.dll")]
		static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool DeviceIoControl(IntPtr handle, uint code, IntPtr input, int inputSize, IntPtr output, int outputSize, out int returned, IntPtr overlapped);

		[DllImport("kernel32.dll")]
		static extern bool CloseHandle(IntPtr handle);

		/// <summary>Cycles the port the device is plugged in to.</summary>
		/// <param name="deviceId">The device plugged straight in to a hub, as a device identifier.</param>
		/// <returns>True when the hub took the request. False when the device is not plugged in to a USB hub port this program can reach.</returns>
		public static bool Cycle(string deviceId)
		{
			uint node;
			if (CM_Locate_DevNodeW(out node, deviceId, 0) != 0)
				return false;
			uint hubNode;
			if (CM_Get_Parent(out hubNode, node, 0) != 0)
				return false;
			var hubId = new StringBuilder(512);
			if (CM_Get_Device_IDW(hubNode, hubId, hubId.Capacity, 0) != 0)
				return false;
			uint type, port, length = 4;
			if (CM_Get_DevNode_Registry_PropertyW(node, AddressProperty, out type, out port, ref length, 0) != 0 || port == 0)
				return false;
			var path = HubPath(hubNode, hubId.ToString());
			return path != null && Cycle(path, (int)port);
		}

		/// <summary>The path the hub with this device identifier is opened by, or null.</summary>
		static string HubPath(uint hubNode, string hubId)
		{
			var guid = HubInterface;
			var set = SetupDiGetClassDevs(ref guid, IntPtr.Zero, IntPtr.Zero, 0x12);
			if (set == new IntPtr(-1))
				return null;
			try
			{
				for (uint i = 0; ; i++)
				{
					var data = new DeviceInterfaceData { Size = Marshal.SizeOf(typeof(DeviceInterfaceData)) };
					if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, i, ref data))
						return null;
					var info = new DeviceInfoData { Size = Marshal.SizeOf(typeof(DeviceInfoData)) };
					int required;
					SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out required, ref info);
					var buffer = Marshal.AllocHGlobal(required);
					try
					{
						Marshal.WriteInt32(buffer, IntPtr.Size == 8 ? 8 : 6);
						if (!SetupDiGetDeviceInterfaceDetail(set, ref data, buffer, required, out required, ref info))
							continue;
						if (info.DevInst != hubNode)
						{
							var other = new StringBuilder(512);
							if (CM_Get_Device_IDW(info.DevInst, other, other.Capacity, 0) != 0
								|| !string.Equals(other.ToString(), hubId, StringComparison.OrdinalIgnoreCase))
								continue;
						}
						return Marshal.PtrToStringUni(new IntPtr(buffer.ToInt64() + 4));
					}
					finally
					{
						Marshal.FreeHGlobal(buffer);
					}
				}
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(set);
			}
		}

		static bool Cycle(string hubPath, int port)
		{
			var hub = CreateFile(hubPath, GenericWrite, ShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
			if (hub == new IntPtr(-1))
				return false;
			try
			{
				var buffer = Marshal.AllocHGlobal(8);
				try
				{
					Marshal.WriteInt32(buffer, 0, port);
					Marshal.WriteInt32(buffer, 4, 0);
					int returned;
					return DeviceIoControl(hub, IoctlCyclePort, buffer, 8, buffer, 8, out returned, IntPtr.Zero);
				}
				finally
				{
					Marshal.FreeHGlobal(buffer);
				}
			}
			finally
			{
				CloseHandle(hub);
			}
		}
	}
}
