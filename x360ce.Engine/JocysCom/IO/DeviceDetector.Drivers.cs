using System;
using JocysCom.ClassLibrary.Win32;
using System.Collections.Generic;

namespace JocysCom.ClassLibrary.IO
{
	public partial class DeviceDetector
	{

		public static SP_DRVINFO_DATA[] GetDrivers(Guid classGuid, DIGCF flags)
		{
			var info = new List<SP_DRVINFO_DATA>();
			var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, flags);
			// Cycle through all devices.
			for (int i = 0; ; i++)
			{
				// Get the device info for this device
				SP_DEVINFO_DATA devInfo = new SP_DEVINFO_DATA();
				devInfo.Initialize();
				// If no more devices found then...
				if (!NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref devInfo))
					break;
				// Build a list of driver info items that we will retrieve below. Exit if failed.
				if (!NativeMethods.SetupDiBuildDriverInfoList(deviceInfoSet, ref devInfo, SPDIT.SPDIT_COMPATDRIVER))
					return null;
				for (int j = 0; ; j++)
				{
					// Get all the info items for this driver 
					SP_DRVINFO_DATA drvInfo = new SP_DRVINFO_DATA();
					drvInfo.Initialize();
					if (!NativeMethods.SetupDiEnumDriverInfo(deviceInfoSet, ref devInfo, SPDIT.SPDIT_COMPATDRIVER, j, ref drvInfo))
						break;
					info.Add(drvInfo);
					//Console.WriteLine("{0} {1} - {2}", drvInfo.ProviderName, drvInfo.Description, drvInfo.GetVersion());
				}
			}
			return info.ToArray();
		}

		public static SP_DRVINFO_DATA[] GetDrivers(string deviceId)
		{
			var info = new SP_DRVINFO_DATA[0];
			var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(Guid.Empty, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_ALLCLASSES);
			if (deviceInfoSet.ToInt64() != ERROR_INVALID_HANDLE_VALUE)
			{
				var deviceInfoData = GetDeviceInfo(deviceInfoSet, deviceId);
				if (deviceInfoData.HasValue)
				{
					var di = deviceInfoData.Value;
					info = GetDrivers(deviceInfoSet, ref di);
				}
			}
			NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			return info;
		}

		public static SP_DRVINFO_DATA[] GetDrivers(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData)
		{
			var info = new List<SP_DRVINFO_DATA>();
			if (NativeMethods.SetupDiBuildDriverInfoList(deviceInfoSet, ref DeviceInfoData, SPDIT.SPDIT_COMPATDRIVER))
			{
				SP_DRVINFO_DATA drvInfo = new SP_DRVINFO_DATA();
				drvInfo.Initialize();
				for (int j = 0; NativeMethods.SetupDiEnumDriverInfo(deviceInfoSet, ref DeviceInfoData, SPDIT.SPDIT_COMPATDRIVER, j, ref drvInfo); j++)
				{
					//Console.WriteLine("{0} {1} - {2}", drvInfo.ProviderName, drvInfo.Description, drvInfo.GetVersion());
					info.Add(drvInfo);
				}
			}
			return info.ToArray();
			//SP_DEVINSTALL_PARAMS InstallParams = new SP_DEVINSTALL_PARAMS();
			//InstallParams.Initialize();
			//if (!NativeMethods.SetupDiGetDeviceInstallParams(DeviceInfoSet, ref DeviceInfoData, ref InstallParams))
			//{
			//	//Error
			//}
			//else
			//{
			//	InstallParams.FlagsEx |= DI_FLAGSEX_INSTALLEDDRIVER;
			//	if (!NativeMethods.SetupDiSetDeviceInstallParams(DeviceInfoSet, ref DeviceInfoData, ref InstallParams))
			//	{
			//		//Errror
			//	}
			//}
			//return info.ToArray();
		}
	}
}
