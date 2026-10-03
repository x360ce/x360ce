#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#define CINTERFACE
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>

// COM CLSIDs
#pragma comment(lib,"wbemuuid.lib")

#include <dinput.h>

#include "InputHookManager.h"
#include "InputHook.h"
#include "HookCOM.h"

void (WINAPI *HookCOM::TrueCoUninitialize)() = nullptr;
HRESULT(WINAPI *HookCOM::TrueCoCreateInstance)(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext, REFIID riid, LPVOID FAR* ppv) = nullptr;
HRESULT(WINAPI *HookCOM::TrueCoCreateInstanceEx)(REFCLSID Clsid, IUnknown * punkOuter, DWORD dwClsCtx, COSERVERINFO * pServerInfo, DWORD dwCount, MULTI_QI * pResults) = nullptr;
HRESULT(WINAPI * HookCOM::TrueCoGetClassObject)(REFCLSID rclsid, DWORD dwClsContext, LPVOID pvReserved, REFIID riid, LPVOID FAR * ppv) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::TrueConnectServer)(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
	const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::TrueCreateInstanceEnum)(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::TrueNext)(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::TrueGet)(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor) = nullptr;

HRESULT(STDMETHODCALLTYPE *HookCOM::ConnectServer)(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
	const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::CreateInstanceEnum)(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::Next)(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned) = nullptr;
HRESULT(STDMETHODCALLTYPE *HookCOM::Get)(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor) = nullptr;

// WARNING: VARIANT *pVal is required to be passed, do not change to pointer to BSTR!
void HookCOM::DeviceStringChange(VARIANT *pVal, InputHookDevice* pInputHookDevice, const wchar_t* pNamespace)
{
	std::wstring oldDeviceName(pVal->bstrVal);
	std::wstring newDeviceName;

	DWORD dwHookVid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? LOWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : LOWORD(pInputHookDevice->GetProductPIDVID());
	DWORD dwHookPid = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) ? HIWORD(InputHookManager::Get().GetInputHook().GetFakePIDVID()) : HIWORD(pInputHookDevice->GetProductPIDVID());

	const wchar_t* p = wcsrchr(pVal->bstrVal, L'\\');
	if (p) newDeviceName = StringFormat(L"%s\\VID_%04X&PID_%04X&IG_%02d%s", pNamespace, dwHookVid, dwHookPid, pInputHookDevice->GetUserIndex(), p);
	else newDeviceName = StringFormat(L"%s\\VID_%04X&PID_%04X&IG_%02d", pNamespace, dwHookVid, dwHookPid, pInputHookDevice->GetUserIndex());

	if (SysReAllocString(&pVal->bstrVal, newDeviceName.c_str()) == TRUE)
	{
		PrintLog("Device string change: %ls => %ls", oldDeviceName.c_str(), pVal->bstrVal);
	}
	else
	{
		PrintLog("Failed to re-alloc string");
	}
}

HRESULT STDMETHODCALLTYPE HookCOM::HookGet(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor)
{
	HRESULT hr = TrueGet(This, wszName, lFlags, pVal, pType, plFlavor);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;

	PrintLog("IWbemClassObject::Get");

	if (hr != NO_ERROR) return hr;

	//PrintLog( "wszName %ls pVal->vt %d pType %d", wszName, pVal->vt, &pType);
	//if( pVal->vt == VT_BSTR) PrintLog( L"%s",pVal->bstrVal);

	if (pVal->vt == VT_BSTR && pVal->bstrVal != NULL)
	{
		//PrintLog( "  Got device ID '%ls'", pVal->bstrVal);
		DWORD dwPid = 0, dwVid = 0, dummy = 0;

		// We do not need to spoof if IG_ present and PIDVID change is disabled
		if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_PIDVID) &&
			wcsstr(pVal->bstrVal, L"IG_"))
			return hr;

		OLECHAR* strVid = wcsstr(pVal->bstrVal, L"VID_");
		if (!strVid || swscanf_s(strVid, L"VID_%4X", &dwVid) < 1)
		{
			// Fallback VID match for OUYA style device IDs
			strVid = wcsstr(pVal->bstrVal, L"VID&");
			if (!strVid || swscanf_s(strVid, L"VID&%4X%4X", &dummy, &dwVid) < 1)
				return hr;
		}

		OLECHAR* strPid = wcsstr(pVal->bstrVal, L"PID_");
		if (!strPid || swscanf_s(strPid, L"PID_%4X", &dwPid) < 1)
		{
			// Fallback PID match for OUYA style device IDs
			strPid = wcsstr(pVal->bstrVal, L"PID&");
			if (!strPid || swscanf_s(strPid, L"PID&%4X", &dwPid) < 1)
				return hr;
		}

		for (auto deviceit = InputHookManager::Get().GetInputHook().begin(); deviceit != InputHookManager::Get().GetInputHook().end(); ++deviceit)
		{
			if (deviceit->GetProductPIDVID() == (u32)MAKELONG(dwVid, dwPid))
			{
				const wchar_t* strUSB = wcsstr(pVal->bstrVal, L"USB\\");
				const wchar_t* strRoot = wcsstr(pVal->bstrVal, L"root\\");
				const wchar_t* strHID = wcsstr(pVal->bstrVal, L"HID\\");

				if (strUSB)
				{
					DeviceStringChange(pVal, &(*deviceit), L"USB");
					continue;
				}
				else if (strRoot)
				{
					DeviceStringChange(pVal, &(*deviceit), L"root");
					continue;
				}
				else if (strHID)
				{
					DeviceStringChange(pVal, &(*deviceit), L"HID");
					continue;
				}

			}
		}
	}

	return hr;
}

HRESULT STDMETHODCALLTYPE HookCOM::HookNext(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned)
{
	HRESULT hr = TrueNext(This, lTimeout, uCount, apObjects, puReturned);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;

	PrintLog("IEnumWbemClassObject::Next %u", uCount);

	if (hr != NO_ERROR) return hr;

	IWbemClassObject* pDevices;

	if (apObjects)
	{
		if (*apObjects)
		{
			pDevices = *apObjects;

			if (pDevices->lpVtbl->Get)
			{
				Get = pDevices->lpVtbl->Get;
				IH_CreateHook(Get, HookGet, &TrueGet);
				IH_EnableHook(Get);
			}
		}
	}

	return hr;
}

HRESULT STDMETHODCALLTYPE HookCOM::HookCreateInstanceEnum(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum)
{
	HRESULT hr = TrueCreateInstanceEnum(This, strFilter, lFlags, pCtx, ppEnum);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;

	PrintLog("IWbemServices::CreateInstanceEnum");

	if (hr != NO_ERROR) return hr;

	IEnumWbemClassObject* pEnumDevices = NULL;

	if (ppEnum)
	{
		if (*ppEnum)
		{
			pEnumDevices = *ppEnum;

			if (pEnumDevices->lpVtbl->Next)
			{
				Next = pEnumDevices->lpVtbl->Next;
				IH_CreateHook(Next, HookNext, &TrueNext);
				IH_EnableHook(Next);
			}
		}
	}

	return hr;
}

HRESULT STDMETHODCALLTYPE HookCOM::HookConnectServer(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
	const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace)

{
	HRESULT hr = TrueConnectServer(This, strNetworkResource, strUser, strPassword, strLocale, lSecurityFlags, strAuthority, pCtx, ppNamespace);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;

	PrintLog("IWbemLocator::ConnectServer");

	if (hr != NO_ERROR) return hr;

	IWbemServices* pIWbemServices = NULL;

	if (ppNamespace)
	{
		if (*ppNamespace)
		{
			pIWbemServices = *ppNamespace;

			if (pIWbemServices->lpVtbl->CreateInstanceEnum)
			{
				CreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
				IH_CreateHook(CreateInstanceEnum, HookCreateInstanceEnum, &TrueCreateInstanceEnum);
				IH_EnableHook(CreateInstanceEnum);
			}
		}
	}

	return hr;
}

HRESULT WINAPI HookCOM::HookCoCreateInstanceEx(REFCLSID Clsid, IUnknown * punkOuter, DWORD dwClsCtx, COSERVERINFO * pServerInfo, DWORD dwCount, MULTI_QI * pResults)
{
	HRESULT hr = TrueCoCreateInstanceEx(Clsid, punkOuter, dwClsCtx, pServerInfo, dwCount, pResults);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;
	PrintLog("CoCreateInstanceEx");

	std::string clsid;
	GUIDtoString(&clsid, Clsid);
	PrintLog("CoCreateInstanceEx %s", clsid.c_str());

	if (!pResults) return hr;

	InputHookManager::Get().GetInputHook().StartTimeoutThread();

	if (IsEqualCLSID(Clsid, CLSID_DirectInput8))
		PrintLog("COM wants to create DirectInput8 instance");

	if (pResults->pIID && IsEqualIID(*pResults->pIID, IID_IWbemLocator) && pResults->pItf)
	{
		IWbemLocator* pIWbemLocator = NULL;
		pIWbemLocator = reinterpret_cast<IWbemLocator*>(pResults->pItf);

		if (pIWbemLocator)
		{
			if (pIWbemLocator->lpVtbl->ConnectServer)
			{
				ConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
				IH_CreateHook(ConnectServer, HookConnectServer, &TrueConnectServer);
				IH_EnableHook(ConnectServer);
			}
		}
	}
	return hr;
}

HRESULT WINAPI HookCOM::HookCoCreateInstance(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext, REFIID riid, LPVOID FAR* ppv)
{
	HRESULT hr = TrueCoCreateInstance(rclsid, pUnkOuter, dwClsContext, riid, ppv);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;

	std::string clsid;
	std::string iid;
	GUIDtoString(&clsid, rclsid);
	GUIDtoString(&iid, riid);
	PrintLog("CoCreateInstance %s => %s", clsid.c_str(), iid.c_str());

	if (hr != NO_ERROR) return hr;

	InputHookManager::Get().GetInputHook().StartTimeoutThread();

	if (IsEqualCLSID(rclsid, CLSID_DirectInput8))
		PrintLog("COM wants to create DirectInput8 instance");

	if (IsEqualIID(riid, IID_IWbemLocator))
	{
		IWbemLocator* pIWbemLocator = NULL;
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

		if (pIWbemLocator)
		{
			if (pIWbemLocator->lpVtbl->ConnectServer)
			{
				ConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
				IH_CreateHook(ConnectServer, HookConnectServer, &TrueConnectServer);
				IH_EnableHook(ConnectServer);
			}
		}
	}
	return hr;
}

HRESULT WINAPI HookCOM::HookCoGetClassObject(REFCLSID rclsid, DWORD dwClsContext, LPVOID pvReserved, REFIID riid, LPVOID FAR * ppv)
{
	HRESULT hr = TrueCoGetClassObject(rclsid, dwClsContext, pvReserved, riid, ppv);

	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return hr;

	std::string clsid;
	std::string iid;
	GUIDtoString(&clsid, rclsid);
	GUIDtoString(&iid, riid);
	PrintLog("CoGetClassObject %s => %s", clsid.c_str(), iid.c_str());

	if (hr != NO_ERROR) return hr;

	InputHookManager::Get().GetInputHook().StartTimeoutThread();

	if (IsEqualCLSID(rclsid, CLSID_DirectInput8))
		PrintLog("COM wants to create DirectInput8 instance");

	if (IsEqualIID(riid, IID_IWbemLocator))
	{
		IWbemLocator* pIWbemLocator = NULL;
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

		if (pIWbemLocator)
		{
			if (pIWbemLocator->lpVtbl->ConnectServer)
			{
				ConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
				IH_CreateHook(ConnectServer, HookConnectServer, &TrueConnectServer);
				IH_EnableHook(ConnectServer);
			}
		}
	}
	return hr;
}

void WINAPI HookCOM::HookCoUninitialize()
{
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_COM)) return TrueCoUninitialize();
	PrintLog("CoUninitialize");

	if (TrueGet) MH_QueueDisableHook(Get);
	if (TrueNext) MH_QueueDisableHook(Next);
	if (TrueCreateInstanceEnum) MH_QueueDisableHook(CreateInstanceEnum);
	if (TrueConnectServer) MH_QueueDisableHook(ConnectServer);

	MH_ApplyQueued();

	TrueCoUninitialize();
}

void InputHook::HookCOM()
{
	PrintLog("Hooking COM");

	IH_CreateHook(CoCreateInstance, HookCOM::HookCoCreateInstance, &HookCOM::TrueCoCreateInstance);
	IH_CreateHook(CoCreateInstanceEx, HookCOM::HookCoCreateInstanceEx, &HookCOM::TrueCoCreateInstanceEx);
	IH_CreateHook(CoGetClassObject, HookCOM::HookCoGetClassObject, &HookCOM::TrueCoGetClassObject);
	IH_CreateHook(CoUninitialize, HookCOM::HookCoUninitialize, &HookCOM::TrueCoUninitialize);
}

