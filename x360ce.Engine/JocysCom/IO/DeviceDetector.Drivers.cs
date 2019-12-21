using JocysCom.ClassLibrary.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace JocysCom.ClassLibrary.IO
{
	public partial class DeviceDetector
	{

		public static SP_DRVINFO_DATA[] GetDrivers(Guid? classGuid = null, DIGCF flags = DIGCF.DIGCF_ALLCLASSES, SPDIT driverType = SPDIT.SPDIT_COMPATDRIVER, string deviceId = null, string hardwareId = null)
		{
			var drvInfoList = new List<SP_DRVINFO_DATA>();
			var deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid ?? Guid.Empty, IntPtr.Zero, IntPtr.Zero, flags);
			if (deviceInfoSet.ToInt64() == ERROR_INVALID_HANDLE_VALUE)
				return drvInfoList.ToArray();
			// Loop through device info.
			var deviceInfoData = new SP_DEVINFO_DATA();
			deviceInfoData.Initialize();
			for (int i = 0; NativeMethods.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
			{
				if (!string.IsNullOrEmpty(deviceId))
				{
					var currentDeviceId = GetDeviceId(deviceInfoData.DevInst);
					if (string.Compare(deviceId, currentDeviceId, true) != 0)
						continue;
				}
				if (!string.IsNullOrEmpty(hardwareId))
				{
					var currentHardwareId = GetStringPropertyForDevice(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_HARDWAREID);
					if (string.Compare(hardwareId, currentHardwareId, true) != 0)
						continue;
				}
				var drivers = GetDrivers(deviceInfoSet, ref deviceInfoData, driverType);
				drvInfoList.AddRange(drivers);
			}
			NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
			return drvInfoList.ToArray();
		}

		const int DI_FLAGSEX_INSTALLEDDRIVER = 0x04000000;
		const int DI_FLAGSEX_ALLOWEXCLUDEDDRVS = 0x00000800;

		public static SP_DRVINFO_DATA[] GetDrivers(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, SPDIT driverType = SPDIT.SPDIT_COMPATDRIVER)
		{
			var list = new List<SP_DRVINFO_DATA>();
			var installParams = new SP_DEVINSTALL_PARAMS();
			installParams.Initialize();
			// Retrieve installation parameters for a device information set or a particular device information element.
			if (!NativeMethods.SetupDiGetDeviceInstallParams(deviceInfoSet, ref deviceInfoData, ref installParams))
			{
				var error = new Win32Exception(Marshal.GetLastWin32Error());
				// Return if failed
				return list.ToArray();
			}
			// Set the flags that tell SetupDiBuildDriverInfoList to include just currently installed drivers.
			installParams.FlagsEx |= DI_FLAGSEX_INSTALLEDDRIVER;
			// Set the flags that tell SetupDiBuildDriverInfoList to allow excluded drivers.
			installParams.FlagsEx |= DI_FLAGSEX_ALLOWEXCLUDEDDRVS;
			// Set the flags.
			if (!NativeMethods.SetupDiSetDeviceInstallParams(deviceInfoSet, ref deviceInfoData, ref installParams))
				// Return if failed
				return list.ToArray();
			if (NativeMethods.SetupDiBuildDriverInfoList(deviceInfoSet, ref deviceInfoData, driverType))
			{
				var item = new SP_DRVINFO_DATA();
				item.Initialize();
				for (int i = 0; NativeMethods.SetupDiEnumDriverInfo(deviceInfoSet, ref deviceInfoData, driverType, i, ref item); i++)
				{
					//Console.WriteLine("{0} {1} - {2}", drvInfo.ProviderName, drvInfo.Description, drvInfo.GetVersion());
					list.Add(item);
				}
			}
			return list.ToArray();
		}

	}
}
