using System;
using JocysCom.ClassLibrary.Win32;
using System.Collections.Generic;
using System.ComponentModel;

namespace JocysCom.ClassLibrary.IO
{
    public partial class DeviceDetector
    {

		// Enumerate a list of compatible drivers for the specified device.This driver list type can be specified only if DeviceInfoData is also specified.
		public const int SPDIT_COMPATDRIVER = (0x00000002);
		// Enumerate a class driver list.This driver list type must be specified if DeviceInfoData is not specified.
		public const int SPDIT_CLASSDRIVER = (0x00000001);

        // Computer\HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}

        public static SP_DRVINFO_DATA[] GetDrivers(string deviceId)
        {
            var info = new SP_DRVINFO_DATA[0];
            Guid classGuid = System.Guid.Empty;
            IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF.DIGCF_ALLCLASSES);
            if (deviceInfoSet.ToInt64() != ERROR_INVALID_HANDLE_VALUE)
            {
                var deviceInfoData = GetDeviceInfo(deviceInfoSet, deviceId);
                if (deviceInfoData.HasValue)
                {
                    var di = deviceInfoData.Value;
                    info = GetDrivers(deviceInfoSet, ref di);
                }
                NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
            return info;
        }

        public static SP_DRVINFO_DATA[] GetDrivers(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData)
        {
            var list = new List<DeviceInfo>();
            Guid hidGuid = Guid.Empty;
            var data = new SP_DRVINFO_DATA();
            data.Initialize();
            var info = new List<SP_DRVINFO_DATA>();
            for (int i2 = 0; NativeMethods.SetupDiEnumDriverInfo(DeviceInfoSet, ref DeviceInfoData, SPDIT_CLASSDRIVER, i2, ref data); i2++)
            {
                info.Add(data);
            }
			var ex = new Exception(new Win32Exception().ToString());
			return info.ToArray();
        }
    }
}
