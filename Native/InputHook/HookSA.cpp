#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#include <Setupapi.h>
#pragma comment(lib,"Setupapi.lib")

#include "InputHookManager.h"
#include "InputHook.h"
#include "HookSA.h"

// NOTE: SetupDiGetDeviceInstanceIdW is called inside SetupDiGetDeviceInstanceIdA
BOOL(WINAPI* HookSA::TrueSetupDiGetDeviceInstanceIdW)(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize) = nullptr;

BOOL WINAPI HookSA::HookSetupDiGetDeviceInstanceIdW(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize)
{
	BOOL ret = TrueSetupDiGetDeviceInstanceIdW(DeviceInfoSet, DeviceInfoData, DeviceInstanceId, DeviceInstanceIdSize, RequiredSize);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_SA)) return ret;
	PrintLog("SetupDiGetDeviceInstanceId");

	if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) return ret;

	InputHookManager::Get().GetInputHook().StartTimeoutThread();

	if (DeviceInstanceId && ret)
	{
		DWORD dwPid = 0, dwVid = 0;

		wchar_t* strVid = wcsstr(DeviceInstanceId, L"VID_");
		if (!strVid || swscanf_s(strVid, L"VID_%4X", &dwVid) < 1)
			return ret;

		wchar_t* strPid = wcsstr(DeviceInstanceId, L"PID_");
		if (!strPid || swscanf_s(strPid, L"PID_%4X", &dwPid) < 1)
			return ret;

		for (auto padcfg = InputHookManager::Get().GetInputHook().begin(); padcfg != InputHookManager::Get().GetInputHook().end(); ++padcfg)
		{
			if (padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid, dwPid))
			{
				const wchar_t* strUSB = wcsstr(DeviceInstanceId, L"USB\\");
				const wchar_t* strRoot = wcsstr(DeviceInstanceId, L"root\\");

				DWORD dwHookVid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? LOWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
				DWORD dwHookPid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? HIWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

				std::wstring tmpString;
				if (strUSB || strRoot)
				{
					const wchar_t* p = wcsrchr(DeviceInstanceId, L'\\');
					if (p) tmpString = StringFormat(L"USB\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);
					else tmpString = StringFormat(L"USB\\VID_%04X&PID_%04X&IG_%02d", dwHookVid, dwHookPid, padcfg->GetUserIndex());

					if (DeviceInstanceIdSize < tmpString.size())
					{
						SetLastError(ERROR_INSUFFICIENT_BUFFER);
						if (RequiredSize) *RequiredSize = (DWORD)tmpString.size() + 1;
						//return FALSE; //NOTE: return FALSE here breaks Beat Hazard
						continue;
					}
					else if (DeviceInstanceIdSize > tmpString.size())
					{
						PrintLog("Device string change:", DeviceInstanceId);
						PrintLog("%ls", DeviceInstanceId);
						wcscpy_s(DeviceInstanceId, DeviceInstanceIdSize, tmpString.c_str());
						if (RequiredSize) *RequiredSize = (DWORD)tmpString.size() + 1;
						PrintLog("%ls", DeviceInstanceId);
						continue;
					}
				}

				wchar_t* strHID = wcsstr(DeviceInstanceId, L"HID\\");

				if (strHID)
				{
					wchar_t* p = wcsrchr(DeviceInstanceId, L'\\');
					tmpString = StringFormat(L"HID\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);

					if (DeviceInstanceIdSize < tmpString.size())
					{
						SetLastError(ERROR_INSUFFICIENT_BUFFER);
						if (RequiredSize) *RequiredSize = (DWORD)tmpString.size() + 1;
						//return FALSE; //NOTE: return FALSE here breaks Beat Hazard
						continue;
					}
					else if (DeviceInstanceIdSize > tmpString.size())
					{
						PrintLog("Device string change:", DeviceInstanceId);
						PrintLog("%ls", DeviceInstanceId);
						wcscpy_s(DeviceInstanceId, DeviceInstanceIdSize, tmpString.c_str());
						if (RequiredSize) *RequiredSize = (DWORD)tmpString.size() + 1;
						PrintLog("%ls", DeviceInstanceId);
						continue;
					}
				}
			}
		}
	}

	return ret;
}

void InputHook::HookSA()
{
	PrintLog("Hooking SetupApi");

	IH_CreateHook(SetupDiGetDeviceInstanceIdW, HookSA::HookSetupDiGetDeviceInstanceIdW, &HookSA::TrueSetupDiGetDeviceInstanceIdW);
}
