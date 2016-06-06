using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.ComponentModel;
using JocysCom.ClassLibrary.Win32;
using Microsoft.Win32.SafeHandles;

namespace JocysCom.ClassLibrary.IO
{

	[StructLayout(LayoutKind.Sequential)]
	public struct HIDD_ATTRIBUTES
	{
		internal int Size;
		internal ushort VendorID;
		internal ushort ProductID;
		internal ushort VersionNumber;
	}

	public partial class DeviceDetector
	{

		[DllImport("hid.dll", SetLastError = true)]
		internal static extern void HidD_GetHidGuid(out Guid HidGuid);

		[DllImport("hid.dll", SetLastError = true)]
		internal static extern bool HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

		[DllImport("hid.dll", CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetProductString(SafeFileHandle hidDeviceObject, ref byte[] lpReportBuffer, int ReportBufferLength);

		[DllImport("hid.dll", CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetManufacturerString(SafeFileHandle hidDeviceObject, ref byte[] lpReportBuffer, int ReportBufferLength);

		[DllImport("hid.dll", CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetSerialNumberString(SafeFileHandle hidDeviceObject, ref byte[] lpReportBuffer, int reportBufferLength);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, int dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, int hTemplateFile);

		[DllImport("setupapi.dll", SetLastError = true)]
		internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

		[DllImport("setupapi.dll", SetLastError = true)]
		internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

	}
}
