using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		[DllImport("hid.dll", SetLastError = true)]
		public static extern void HidD_GetHidGuid(ref Guid hidGuid);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool HidD_GetSerialNumberString(SafeFileHandle HidDeviceObject, IntPtr Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetProductString(SafeFileHandle HidDeviceObject, IntPtr Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetPhysicalDescriptor(SafeFileHandle HidDeviceObject, IntPtr Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern bool HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, IntPtr Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, ref IntPtr PreparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_FlushQueue(SafeFileHandle HidDeviceObject);

	}
}
