// ditool.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <sstream>
#include <iostream>
#include <iomanip>

#include "Utils.h"
#include "Logger.h"

#include <setupapi.h>
#include <hidsdi.h>


void Run()
{
	GUID hidGUID;
	//Get the HID GUID value - used as mask to get list of devices
	HidD_GetHidGuid(&hidGUID);

	HDEVINFO                         hDevInfo;
	SP_DEVICE_INTERFACE_DATA         DevIntfData;
	PSP_DEVICE_INTERFACE_DETAIL_DATA DevIntfDetailData;
	SP_DEVINFO_DATA                  DevData;

	DWORD dwSize, dwType, dwMemberIdx;
	HKEY hKey;
	BYTE lpData[1024];

	// We will try to get device information set for all USB devices that have a
	// device interface and are currently present on the system (plugged in).
	hDevInfo = SetupDiGetClassDevs(
		&hidGUID, NULL, 0, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

	if (hDevInfo != INVALID_HANDLE_VALUE)
	{
		// Prepare to enumerate all device interfaces for the device information
		// set that we retrieved with SetupDiGetClassDevs(..)
		DevIntfData.cbSize = sizeof(SP_DEVICE_INTERFACE_DATA);
		dwMemberIdx = 0;

		// Next, we will keep calling this SetupDiEnumDeviceInterfaces(..) until this
		// function causes GetLastError() to return  ERROR_NO_MORE_ITEMS. With each
		// call the dwMemberIdx value needs to be incremented to retrieve the next
		// device interface information.

		SetupDiEnumDeviceInterfaces(hDevInfo, NULL, &hidGUID,
			dwMemberIdx, &DevIntfData);

		while (GetLastError() != ERROR_NO_MORE_ITEMS)
		{
			// As a last step we will need to get some more details for each
			// of device interface information we are able to retrieve. This
			// device interface detail gives us the information we need to identify
			// the device (VID/PID), and decide if it's useful to us. It will also
			// provide a DEVINFO_DATA structure which we can use to know the serial
			// port name for a virtual com port.

			DevData.cbSize = sizeof(DevData);

			// Get the required buffer size. Call SetupDiGetDeviceInterfaceDetail with
			// a NULL DevIntfDetailData pointer, a DevIntfDetailDataSize
			// of zero, and a valid RequiredSize variable. In response to such a call,
			// this function returns the required buffer size at dwSize.

			SetupDiGetDeviceInterfaceDetail(
				hDevInfo, &DevIntfData, NULL, 0, &dwSize, NULL);

			// Allocate memory for the DeviceInterfaceDetail struct. Don't forget to
			// deallocate it later!
			DevIntfDetailData = (PSP_DEVICE_INTERFACE_DETAIL_DATA)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, dwSize);
			DevIntfDetailData->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);

			if (SetupDiGetDeviceInterfaceDetail(hDevInfo, &DevIntfData,
				DevIntfDetailData, dwSize, &dwSize, &DevData))
			{
				PrintLog("%s", DevIntfDetailData->DevicePath);

				// Finally we can start checking if we've found a useable device,
				// by inspecting the DevIntfDetailData->DevicePath variable.
				// The DevicePath looks something like this:
				//
				// \\?\usb#vid_04d8&pid_0033#5&19f2438f&0&2#{a5dcbf10-6530-11d2-901f-00c04fb951ed}
				//
				// The VID for Velleman Projects is always 10cf. The PID is variable
				// for each device:
				//
				//    -------------------
				//    | Device   | PID  |
				//    -------------------
				//    | K8090    | 8090 |
				//    | VMB1USB  | 0b1b |
				//    -------------------
				//
				// As you can see it contains the VID/PID for the device, so we can check
				// for the right VID/PID with string handling routines.

				if (NULL != _tcsstr((TCHAR*)DevIntfDetailData->DevicePath, _T("vid_10cf&pid_8090")))
				{
					// To find out the serial port for our K8090 device,
					// we'll need to check the registry:

					hKey = SetupDiOpenDevRegKey(
						hDevInfo,
						&DevData,
						DICS_FLAG_GLOBAL,
						0,
						DIREG_DEV,
						KEY_READ
						);

					dwType = REG_SZ;
					dwSize = sizeof(lpData);
					RegQueryValueEx(hKey, _T("PortName"), NULL, &dwType, lpData, &dwSize);
					RegCloseKey(hKey);

					// Eureka!
					PrintLog("Found a device on port '%s'\n", lpData);
				}
			}

			HeapFree(GetProcessHeap(), 0, DevIntfDetailData);

			// Continue looping
			SetupDiEnumDeviceInterfaces(
				hDevInfo, NULL, &hidGUID, ++dwMemberIdx, &DevIntfData);
		}

		SetupDiDestroyDeviceInfoList(hDevInfo);
	}
}

int _tmain(int argc, _TCHAR* argv[])
{
	LogFile("ditool.log");
	LogConsole("ditool");

	Run();

	_getch();
	return 0;
}

