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

	[StructLayout(LayoutKind.Sequential)]
	public struct HIDP_CAPS
	{
		public short Usage;
		public short UsagePage;
		public short InputReportByteLength;
		public short OutputReportByteLength;
		public short FeatureReportByteLength;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
		public short[] Reserved;
		public short NumberLinkCollectionNodes;
		public short NumberInputButtonCaps;
		public short NumberInputValueCaps;
		public short NumberInputDataIndices;
		public short NumberOutputButtonCaps;
		public short NumberOutputValueCaps;
		public short NumberOutputDataIndices;
		public short NumberFeatureButtonCaps;
		public short NumberFeatureValueCaps;
		public short NumberFeatureDataIndices;

	}

	public partial class DeviceDetector
	{

		[DllImport("hid.dll", SetLastError = true)]
		public static extern void HidD_GetHidGuid(ref Guid hidGuid);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool HidD_GetSerialNumberString(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetProductString(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetPhysicalDescriptor(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder Buffer, uint BufferLength);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, ref IntPtr PreparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern int HidP_GetCaps(IntPtr preparsedData, ref HIDP_CAPS capabilities);

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_FlushQueue(SafeFileHandle HidDeviceObject);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern SafeFileHandle CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile
		);

		[DllImport("setupapi.dll", SetLastError = true)]
		internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

			}
}
