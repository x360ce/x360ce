#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#include <Setupapi.h>
#pragma comment(lib,"Setupapi.lib")

#include "InputHook.h"

class HookSA
{
	friend class InputHook;
private:
	static BOOL(WINAPI* TrueSetupDiGetDeviceInstanceIdW)(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize);
	static BOOL WINAPI HookSetupDiGetDeviceInstanceIdW(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize);
};


