/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#include "stdafx.h"
#include "globals.h"
#include "Log.h"
#include "Misc.h"

#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>

#include <dinput.h>

#include "InputHook.h"

#include "HookCOM\IWbemLocator.h"
#include "HookCOM\IWbemClassObject.h"

static iHook *iHookThis = NULL;

// COM CLSIDs
#pragma comment(lib,"wbemuuid.lib")

typedef void (WINAPI *CoUninitialize_t)();

typedef HRESULT(WINAPI *CoCreateInstance_t)(__in     REFCLSID rclsid,
	__in_opt LPUNKNOWN pUnkOuter,
	__in     DWORD dwClsContext,
	__in     REFIID riid,
	__deref_out LPVOID FAR* ppv);

CoUninitialize_t oCoUninitialize = NULL;
CoCreateInstance_t oCoCreateInstance = NULL;


HRESULT HookGet(
	HRESULT hr,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	if (!iHookThis->GetState(iHook::HOOK_COM)) return hr;

	PrintLog(LOG_HOOKCOM, "*Gets*");

	if (hr != NO_ERROR) return hr;

	//PrintLog(LOG_HOOKCOM, "wszName %ls pVal->vt %d pType %d", wszName, pVal->vt, &pType);
	//if( pVal->vt == VT_BSTR) PrintLog(LOG_HOOKCOM, L"%s",pVal->bstrVal);

	if (pVal->vt == VT_BSTR && pVal->bstrVal != NULL)
	{
		//PrintLog(LOG_HOOKCOM, "  Got device ID '%ls'", pVal->bstrVal);
		DWORD dwPid = 0, dwVid = 0, dummy;

		OLECHAR* strVid = wcsstr(pVal->bstrVal, L"VID_");
		if (!strVid || swscanf_s(strVid, L"VID_%4X", &dwVid) < 1) {
			// Fallback VID match for OUYA style device IDs
			strVid = wcsstr(pVal->bstrVal, L"VID&");
			if (!strVid || swscanf_s(strVid, L"VID&%4X%4X", &dummy, &dwVid) < 1)
				return hr;
		}

		OLECHAR* strPid = wcsstr(pVal->bstrVal, L"PID_");
		if (!strPid || swscanf_s(strPid, L"PID_%4X", &dwPid) < 1) {
			// Fallback PID match for OUYA style device IDs
			strPid = wcsstr(pVal->bstrVal, L"PID&");
			if (!strPid || swscanf_s(strPid, L"PID&%4X", &dwPid) < 1)
				return hr;
		}

		for (auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
		{
			if (padcfg->GetHookState() && padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid, dwPid))
			{
				const wchar_t* strUSB = wcsstr(pVal->bstrVal, L"USB\\");
				const wchar_t* strRoot = wcsstr(pVal->bstrVal, L"root\\");
				OLECHAR tempstr[MAX_PATH];

				DWORD dwHookVid = iHookThis->GetState(iHook::HOOK_PIDVID) ? LOWORD(iHookThis->GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
				DWORD dwHookPid = iHookThis->GetState(iHook::HOOK_PIDVID) ? HIWORD(iHookThis->GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

				if (strUSB || strRoot)
				{
					PrintLog(LOG_HOOKCOM, "%s", "Device string change:");
					PrintLog(LOG_HOOKCOM, "%ls", pVal->bstrVal);
					const wchar_t* p = wcsrchr(pVal->bstrVal, L'\\');
					if (p) swprintf_s(tempstr, L"USB\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);
					else swprintf_s(tempstr, L"USB\\VID_%04X&PID_%04X&IG_%02d", dwHookVid, dwHookPid, padcfg->GetUserIndex());
					SysReAllocString(&pVal->bstrVal, tempstr);
					PrintLog(LOG_HOOKCOM, "%ls", pVal->bstrVal);
					continue;
				}

				OLECHAR* strHID = wcsstr(pVal->bstrVal, L"HID\\");

				if (strHID)
				{
					PrintLog(LOG_HOOKCOM, "%s", "Device string change:");
					PrintLog(LOG_HOOKCOM, "%ls", pVal->bstrVal);
					OLECHAR* p = wcsrchr(pVal->bstrVal, L'\\');
					swprintf_s(tempstr, L"HID\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);
					SysReAllocString(&pVal->bstrVal, tempstr);
					PrintLog(LOG_HOOKCOM, "%ls", pVal->bstrVal);
					continue;
				}
			}
		}
	}

	return hr;
}

HRESULT WINAPI HookCoCreateInstance(__in     REFCLSID rclsid,
	__in_opt LPUNKNOWN pUnkOuter,
	__in     DWORD dwClsContext,
	__in     REFIID riid,
	__deref_out LPVOID FAR* ppv)
{
	HRESULT hr = oCoCreateInstance(rclsid, pUnkOuter, dwClsContext, riid, ppv);

	if (!iHookThis->GetState(iHook::HOOK_COM)) return hr;
	PrintLog(LOG_HOOKCOM, "*CoCreateInstance*");

	if (hr != NO_ERROR) return hr;

	if (IsEqualCLSID(rclsid, CLSID_DirectInput8))
		PrintLog(LOG_IHOOK, "COM wants to create DirectInput8 instance");

	if (IsEqualIID(riid, IID_IWbemLocator))
	{
		IWbemLocator* pIWbemLocator = static_cast<IWbemLocator*>(*ppv);
		new hkIWbemLocator(&pIWbemLocator);
	}

	return hr;
}

void iHook::HookCOM()
{
	PrintLog(LOG_HOOKCOM, "Hooking COM");
	iHookThis = this;
	if (MH_CreateHook(CoCreateInstance, HookCoCreateInstance, reinterpret_cast<void**>(&oCoCreateInstance)) == MH_OK)
		PrintLog(LOG_HOOKCOM, "Hooking CoCreateInstance");
}

