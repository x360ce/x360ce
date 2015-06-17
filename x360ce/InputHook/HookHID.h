#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#include <Setupapi.h>
#include <hidsdi.h>

#include "InputHook.h"

class HookHID
{
	friend class InputHook;
private:
	static BOOL(WINAPI* TrueSetupDiGetDeviceInstanceIdW)(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize);
	static BOOL WINAPI HookSetupDiGetDeviceInstanceIdW(HDEVINFO DeviceInfoSet, PSP_DEVINFO_DATA DeviceInfoData, PWSTR DeviceInstanceId, DWORD DeviceInstanceIdSize, PDWORD RequiredSize);

	static BOOL(WINAPI* TrueSetupDiGetDeviceInterfaceDetailW)(
		HDEVINFO DeviceInfoSet,
		PSP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
		PSP_DEVICE_INTERFACE_DETAIL_DATA_W DeviceInterfaceDetailData,
		DWORD DeviceInterfaceDetailDataSize,
		PDWORD RequiredSize,
		PSP_DEVINFO_DATA DeviceInfoData
		);
	static BOOL WINAPI HookSetupDiGetDeviceInterfaceDetailW(
		HDEVINFO DeviceInfoSet,
		PSP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
		PSP_DEVICE_INTERFACE_DETAIL_DATA_W DeviceInterfaceDetailData,
		DWORD DeviceInterfaceDetailDataSize,
		PDWORD RequiredSize,
		PSP_DEVINFO_DATA DeviceInfoData
		);

	static BOOLEAN(__stdcall*
		TrueHidD_GetManufacturerString)(
		HANDLE   HidDeviceObject,
		PVOID Buffer,
		ULONG    BufferLength
		);
	static BOOLEAN __stdcall
		HookHidD_GetManufacturerString(
		HANDLE   HidDeviceObject,
		PVOID Buffer,
		ULONG    BufferLength
		);

	static BOOLEAN(__stdcall*
		TrueHidD_GetProductString)(
		HANDLE   HidDeviceObject,
		PVOID Buffer,
		ULONG    BufferLength
		);
	static BOOLEAN __stdcall
		HookHidD_GetProductString(
		HANDLE   HidDeviceObject,
		PVOID Buffer,
		ULONG    BufferLength
		);

	static BOOLEAN(__stdcall*
		TrueHidD_GetPhysicalDescriptor)(
		HANDLE   HidDeviceObject,
		PVOID Buffer,
		ULONG    BufferLength
		);
	static BOOLEAN __stdcall
		HookHidD_GetPhysicalDescriptor(
		HANDLE   HidDeviceObject,
		PVOID Buffer,
		ULONG    BufferLength
		);

	static BOOLEAN(__stdcall*
		TrueHidD_GetAttributes)(
		HANDLE           HidDeviceObject,
		PHIDD_ATTRIBUTES Attributes
		);
	static BOOLEAN __stdcall
		HookHidD_GetAttributes(
		HANDLE           HidDeviceObject,
		PHIDD_ATTRIBUTES Attributes
		);
};


