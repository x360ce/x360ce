using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace JocysCom.ClassLibrary.Win32
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	public static partial class NativeMethods
	{

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetupDiGetDriverInfoDetail(
	   IntPtr DeviceInfoSet,
	   ref SP_DEVINFO_DATA DeviceInfoData,
	   ref SP_DRVINFO_DATA DriverInfoData,
	   ref SP_DRVINFO_DETAIL_DATA DriverInfoDetailData,
	   Int32 DriverInfoDetailDataSize,
	   ref Int32 RequiredSize);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetupDiEnumDriverInfo(
				IntPtr DeviceInfoSet,
				ref SP_DEVINFO_DATA DeviceInfoData,
				SPDIT DriverType,
				int MemberIndex,
				ref SP_DRVINFO_DATA DriverInfoData);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetupDiBuildDriverInfoList(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, SPDIT DriverType);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetupDiGetDeviceInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_DEVINSTALL_PARAMS DeviceInstallParams);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetupDiSetDeviceInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, ref SP_DEVINSTALL_PARAMS DeviceInstallParams);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern int SetupDiLoadClassIcon(ref Guid classGuid, out IntPtr hIcon, out int index);

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDeviceInfoSet);

		[DllImport("setupapi.dll", SetLastError = true)]
		internal static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, IntPtr DeviceInstanceId, int DeviceInstanceIdSize, ref int RequiredSize);

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern IntPtr SetupDiGetClassDevs([MarshalAs(UnmanagedType.LPStruct)]System.Guid classGuid, IntPtr enumerator, IntPtr hwndParent, DIGCF flags);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiRemoveDevice(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData);

		[DllImport("Newdev.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool DiUninstallDevice(IntPtr hwndParent, IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, int ulFlags, out bool NeedReboot);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, int memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiGetDeviceInterfaceDetail(
			IntPtr hDevInfo,
			ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
			IntPtr deviceInterfaceDetailData,
			uint deviceInterfaceDetailDataSize,
			ref uint requiredSize,
			 IntPtr deviceInfoData
		);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiGetClassDescription(ref Guid ClassGuid, StringBuilder classDescription, Int32 ClassDescriptionSize, ref UInt32 RequiredSize);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiSetSelectedDevice(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData);

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern CR CM_Get_DevNode_Status(out uint status, out uint probNum, uint devInst, int flags);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto)]
		public static extern CR CM_Get_DevNode_Status_Ex(UInt32 dnDevInst, StringBuilder Buffer, UInt32 BufferLen, UInt32 ulFlags);

		/// <summary>
		/// The SetupDiSetClassInstallParams function sets or clears class install parameters
		/// for a device information set or a particular device information element.
		/// </summary>
		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, SP_PROPCHANGE_PARAMS classInstallParams, UInt32 classInstallParamsSize);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetupDiSetClassInstallParams(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, SP_REMOVEDEVICE_PARAMS classInstallParams, UInt32 classInstallParamsSize);

		/// <summary>
		/// The SetupDiCallClassInstaller function calls the appropriate class installer,
		/// and any registered co-installers, with the specified installation request (DIF code).
		/// </summary>
		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern bool SetupDiCallClassInstaller(UInt32 installFunction, IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData);

		[DllImport("setupapi.dll")]
		public static extern bool SetupDiOpenDeviceInfo(IntPtr deviceInfoSet, string deviceInstanceId, IntPtr hwndParent, UInt32 openFlags, ref SP_DEVINFO_DATA deviceInfoData);

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern bool SetupDiChangeState(IntPtr deviceInfoSet, [In] ref SP_DEVINFO_DATA deviceInfoData);
		/// <summary>
		/// The SetupDiGetDeviceRegistryProperty function retrieves the specified device property.
		/// This handle is typically returned by the SetupDiGetClassDevs or SetupDiGetClassDevsEx function.
		/// </summary>
		/// <param Name="DeviceInfoSet">Handle to the device information set that contains the interface and its underlying device.</param>
		/// <param Name="DeviceInfoData">Pointer to an SP_DEVINFO_DATA structure that defines the device instance.</param>
		/// <param Name="Property">Device property to be retrieved. SEE MSDN</param>
		/// <param Name="PropertyRegDataType">Pointer to a variable that receives the registry data Type. This parameter can be NULL.</param>
		/// <param Name="PropertyBuffer">Pointer to a buffer that receives the requested device property.</param>
		/// <param Name="PropertyBufferSize">Size of the buffer, in bytes.</param>
		/// <param Name="RequiredSize">Pointer to a variable that receives the required buffer size, in bytes. This parameter can be NULL.</param>
		/// <returns>If the function succeeds, the return value is non zero.</returns>
		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetupDiGetDeviceRegistryProperty(
		   IntPtr deviceInfoSet,
		   ref SP_DEVINFO_DATA deviceInfoData,
		   uint property,
		   out uint propertyRegDataType,
		   byte[] propertyBuffer,
		   int propertyBufferSize,
		   out uint requiredSize
		);

		/// <summary>
		/// Retrieves the "device instance ID" for a specified device
		/// </summary>
		[DllImport("setupapi.dll", CharSet = CharSet.Auto)]
		public static extern CR CM_Get_Device_ID(UInt32 dnDevInst, StringBuilder Buffer, int BufferLen, int ulFlags);

		[DllImport("setupapi.dll")]
		public static extern CR CM_Get_Parent(out UInt32 pdnDevInst, UInt32 dnDevInst, int ulFlags);

		/// <summary>
		/// http://msdn.microsoft.com/en-gb/library/windows/hardware/ff538517%28v=vs.85%29.aspx
		/// </summary>
		/// <param name="Status"></param>
		/// <param name="ProblemNumber"></param>
		/// <param name="dnDevInst"></param>
		/// <param name="ulFlags"></param>
		/// <param name="hMachine"></param>
		/// <returns></returns>
		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern CR CM_Get_DevNode_Status_Ex(out UInt32 Status, out UInt32 ProblemNumber, UInt32 dnDevInst, int ulFlags, IntPtr hMachine);

		#region Helper Methods

		public static bool GetDeviceNodeStatus(UInt32 dnDevInst, IntPtr hMachine, out Win32.DeviceNodeStatus status)
		{
			// c:\Program Files\Microsoft SDKs\Windows\v7.1\Include\cfg.h
			uint Status;
			uint ProblemNumber;
			bool success = false;
			// http://msdn.microsoft.com/en-gb/library/windows/hardware/ff538517%28v=vs.85%29.aspx
			var cr = NativeMethods.CM_Get_DevNode_Status_Ex(out Status, out ProblemNumber, dnDevInst, 0, hMachine);
			status = 0;
			if (cr == CR.CR_SUCCESS)
			{
				status = (Win32.DeviceNodeStatus)Status;
				success = true;
			}
			return success;
		}

		#endregion
	}
}
