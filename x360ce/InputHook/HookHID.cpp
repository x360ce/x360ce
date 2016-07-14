#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#include <Setupapi.h>
#pragma comment(lib,"Setupapi.lib")

#include <hidsdi.h>
#pragma comment(lib,"hid.lib")

#include "InputHookManager.h"
#include "InputHook.h"
#include "HookHID.h"

// NOTE: SetupDiGetDeviceInstanceIdW is called inside SetupDiGetDeviceInstanceIdA
BOOL(WINAPI* HookHID::TrueSetupDiGetDeviceInstanceIdW)(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize) = nullptr;

BOOL(WINAPI* HookHID::TrueSetupDiGetDeviceInterfaceDetailW)(
	HDEVINFO DeviceInfoSet,
	PSP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
	PSP_DEVICE_INTERFACE_DETAIL_DATA_W DeviceInterfaceDetailData,
	DWORD DeviceInterfaceDetailDataSize,
	PDWORD RequiredSize,
	PSP_DEVINFO_DATA DeviceInfoData
	) = nullptr;

BOOLEAN(__stdcall*
	HookHID::TrueHidD_GetManufacturerString)(
	HANDLE   HidDeviceObject,
	PVOID Buffer,
	ULONG    BufferLength
	) = nullptr;

BOOLEAN(__stdcall*
	HookHID::TrueHidD_GetProductString)(
	HANDLE   HidDeviceObject,
	PVOID Buffer,
	ULONG    BufferLength
	) = nullptr;

BOOLEAN(__stdcall*
	HookHID::TrueHidD_GetPhysicalDescriptor)(
	HANDLE   HidDeviceObject,
	PVOID Buffer,
	ULONG    BufferLength
	) = nullptr;

BOOLEAN(__stdcall*
	HookHID::TrueHidD_GetAttributes)(
	HANDLE           HidDeviceObject,
	PHIDD_ATTRIBUTES Attributes
	) = nullptr;

BOOL WINAPI HookHID::HookSetupDiGetDeviceInstanceIdW(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD pRequiredSize)
{
	BOOL ret = TrueSetupDiGetDeviceInstanceIdW(DeviceInfoSet, DeviceInfoData, DeviceInstanceId, DeviceInstanceIdSize, pRequiredSize);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_HID)) return ret;
	PrintLog(__FUNCTION__);

	if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
	{
		// we require at last 5 bytes bigger buffer
		if (pRequiredSize) *pRequiredSize = *pRequiredSize + 6;
		return ret;
	}

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
				const wchar_t* strHID = wcsstr(DeviceInstanceId, L"HID\\");

				DWORD dwHookVid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? LOWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
				DWORD dwHookPid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? HIWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

				std::wstring tmpString;
				std::wstring oldDeviceName(DeviceInstanceId);
				if (strUSB || strRoot)
				{
					const wchar_t* p = wcsrchr(DeviceInstanceId, L'\\');
					tmpString = StringFormat(L"USB\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);

					wcscpy_s(DeviceInstanceId, DeviceInstanceIdSize, tmpString.c_str());
					PrintLog(__FUNCTION__": device string change: %ls => %ls", oldDeviceName.c_str(), DeviceInstanceId);
				}

				if (strHID)
				{
					wchar_t* p = wcsrchr(DeviceInstanceId, L'\\');
					tmpString = StringFormat(L"HID\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);

					wcscpy_s(DeviceInstanceId, DeviceInstanceIdSize, tmpString.c_str());
					PrintLog(__FUNCTION__": device string change: %ls => %ls", oldDeviceName.c_str(), DeviceInstanceId);
				}
			}
		}
	}

	return ret;
}

BOOL WINAPI HookHID::HookSetupDiGetDeviceInterfaceDetailW(
	HDEVINFO DeviceInfoSet,
	PSP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
	PSP_DEVICE_INTERFACE_DETAIL_DATA_W DeviceInterfaceDetailData,
	DWORD DeviceInterfaceDetailDataSize,
	PDWORD pRequiredSize,
	PSP_DEVINFO_DATA DeviceInfoData
	)
{
	BOOL ret = TrueSetupDiGetDeviceInterfaceDetailW(DeviceInfoSet, DeviceInterfaceData, DeviceInterfaceDetailData, DeviceInterfaceDetailDataSize, pRequiredSize, DeviceInfoData);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_HID)) return ret;
	PrintLog(__FUNCTION__);

	if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
	{
		// we require at last 5 bytes bigger buffer
		if (pRequiredSize) *pRequiredSize = *pRequiredSize + 6;
		return ret;
	}

	if (DeviceInterfaceDetailData && DeviceInterfaceDetailData->DevicePath)
	{
		/*
		device path look like this:
		\\?\hid#vid_044f&pid_b323#6&1f0400b6&1&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}
		*/

		wchar_t* pCurrentDevicePath = DeviceInterfaceDetailData->DevicePath;

		//PrintLog("%ls", pCurrentDevicePath);

		// We do not need to spoof if IG_ present and PIDVID change is disabled
		if (wcsstr(pCurrentDevicePath, L"ig_"))
		{
			PrintLog("Xinput Device: %ls", pCurrentDevicePath);
			if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID))
				return ret;
		}


		DWORD dwPid = 0, dwVid = 0;

		wchar_t* strVid = wcsstr(pCurrentDevicePath, L"vid_");
		if (!strVid || swscanf_s(strVid, L"vid_%4X", &dwVid) < 1)
			return ret;

		wchar_t* strPid = wcsstr(pCurrentDevicePath, L"pid_");
		if (!strPid || swscanf_s(strPid, L"pid_%4X", &dwPid) < 1)
			return ret;

		const wchar_t* strHID = wcsstr(pCurrentDevicePath, L"\\\\?\\hid");
		if (strHID)
		{
			for (auto padcfg = InputHookManager::Get().GetInputHook().begin(); padcfg != InputHookManager::Get().GetInputHook().end(); ++padcfg)
			{
				if (padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid, dwPid))
				{
					DWORD dwHookVid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? LOWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
					DWORD dwHookPid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? HIWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

					std::wstring oldDeviceName(pCurrentDevicePath);

					const wchar_t* pSuffix = wcsrchr(wcsrchr(pCurrentDevicePath, L'#') - 1, L'#');
					std::wstring tmpString = StringFormat(L"\\\\?\\hid#vid_%04x&pid_%04x&ig_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), pSuffix);
					wcscpy_s(pCurrentDevicePath, DeviceInterfaceDetailDataSize - (offsetof(SP_DEVICE_INTERFACE_DETAIL_DATA_W, DevicePath) + sizeof(WCHAR)), tmpString.c_str());

					PrintLog(__FUNCTION__": device string change: %ls => %ls", oldDeviceName.c_str(), pCurrentDevicePath);
				}
			}
		}
	}

	return ret;
}

BOOLEAN __stdcall HookHID::HookHidD_GetManufacturerString(HANDLE HidDeviceObject, PVOID Buffer, ULONG BufferLength)
{
	BOOLEAN ret = TrueHidD_GetManufacturerString(HidDeviceObject, Buffer, BufferLength);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_HID)) return ret;
	PrintLog(__FUNCTION__);

	if (Buffer) PrintLog("%ls", Buffer);

	return ret;
}

BOOLEAN __stdcall HookHID::HookHidD_GetProductString(HANDLE HidDeviceObject, PVOID Buffer, ULONG BufferLength)
{
	BOOLEAN ret = TrueHidD_GetProductString(HidDeviceObject, Buffer, BufferLength);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_HID)) return ret;
	PrintLog(__FUNCTION__);

	if (Buffer) PrintLog("%ls", Buffer);

	return ret;
}

BOOLEAN __stdcall HookHID::HookHidD_GetPhysicalDescriptor(HANDLE HidDeviceObject, PVOID Buffer, ULONG BufferLength)
{
	BOOLEAN ret = TrueHidD_GetPhysicalDescriptor(HidDeviceObject, Buffer, BufferLength);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_HID)) return ret;
	PrintLog(__FUNCTION__);

	if (Buffer) PrintLog("%ls", Buffer);

	return ret;
}

BOOLEAN __stdcall HookHID::HookHidD_GetAttributes(HANDLE HidDeviceObject, PHIDD_ATTRIBUTES Attributes)
{
	BOOLEAN ret = TrueHidD_GetAttributes(HidDeviceObject, Attributes);
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_HID)) return ret;
	PrintLog(__FUNCTION__);

	if (Attributes)
	{
		//PrintLog("%x %x", Attributes->ProductID, Attributes->VendorID);
		for (auto padcfg = InputHookManager::Get().GetInputHook().begin(); padcfg != InputHookManager::Get().GetInputHook().end(); ++padcfg)
		{
			if (padcfg->GetProductPIDVID() == (DWORD)MAKELONG(Attributes->VendorID, Attributes->ProductID))
			{
				DWORD dwHookVid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? LOWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
				DWORD dwHookPid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? HIWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

				Attributes->ProductID = dwHookPid;
				Attributes->VendorID = dwHookVid;
			}
		}
	}
	return ret;
}

void InputHook::HookHID()
{
	PrintLog("Hooking HID related APIs");

	IH_CreateHook(SetupDiGetDeviceInstanceIdW, HookHID::HookSetupDiGetDeviceInstanceIdW, &HookHID::TrueSetupDiGetDeviceInstanceIdW);
	IH_CreateHook(SetupDiGetDeviceInterfaceDetailW, HookHID::HookSetupDiGetDeviceInterfaceDetailW, &HookHID::TrueSetupDiGetDeviceInterfaceDetailW);
	IH_CreateHook(HidD_GetManufacturerString, HookHID::HookHidD_GetManufacturerString, &HookHID::TrueHidD_GetManufacturerString);
	IH_CreateHook(HidD_GetProductString, HookHID::HookHidD_GetProductString, &HookHID::TrueHidD_GetProductString);
	IH_CreateHook(HidD_GetPhysicalDescriptor, HookHID::HookHidD_GetPhysicalDescriptor, &HookHID::TrueHidD_GetPhysicalDescriptor);
	IH_CreateHook(HidD_GetAttributes, HookHID::HookHidD_GetAttributes, &HookHID::TrueHidD_GetAttributes);
}
