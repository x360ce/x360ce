/*  x360ce - XBOX360 Controler Emulator
*  Copyright (C) 2002-2010 Racer_S
*  Copyright (C) 2010-2011 Robert Krawczyk
*
*  x360ce is free software: you can redistribute it and/or modify it under the terms
*  of the GNU Lesser General Public License as published by the Free Software Found-
*  ation, either version 3 of the License, or (at your option) any later version.
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
#include "Utilities\Log.h"
#include "Utilities\Misc.h"

#define CINTERFACE
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>

#include "InputHook.h"
iHook *iHookWMIU = NULL;

// COM CLSIDs
#pragma comment(lib,"wbemuuid.lib")

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

typedef void (WINAPI *tCoUninitializeW)();

typedef HRESULT (WINAPI *tCoCreateInstanceW)(__in     REFCLSID rclsid,
											 __in_opt LPUNKNOWN pUnkOuter,
											 __in     DWORD dwClsContext,
											 __in     REFIID riid,
											 __deref_out LPVOID FAR* ppv);


typedef HRESULT ( STDMETHODCALLTYPE *tConnectServerW )(
	IWbemLocator * This,
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace);

typedef HRESULT ( STDMETHODCALLTYPE *tCreateInstanceEnumW )(
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

typedef HRESULT ( STDMETHODCALLTYPE *tNextW )(
	IEnumWbemClassObject * This,
	/* [in] */ long lTimeout,
	/* [in] */ ULONG uCount,
	/* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
	/* [out] */ __RPC__out ULONG *puReturned);

typedef HRESULT ( STDMETHODCALLTYPE *tGetW )(
	IWbemClassObject * This,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor);

static tCoUninitializeW hCoUninitializeW = NULL;
static tCoCreateInstanceW hCoCreateInstanceW = NULL;

static tConnectServerW hConnectServerW = NULL;
static tCreateInstanceEnumW hCreateInstanceEnumW = NULL;
static tNextW hNextW = NULL;
static tGetW hGetW = NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetW(
	IWbemClassObject * This,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	HRESULT hr = hGetW(This,wszName,lFlags,pVal,pType,plFlavor);

	if(!iHookWMIU->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookGetW");

	if(FAILED(hr)) return hr;

	//WriteLog(LOG_HOOKWMI, L"wszName %s pVal->vt %d pType %d",wszName,pVal->vt,&pType);
	//if( pVal->vt == VT_BSTR) WriteLog(LOG_HOOKWMI, L"%s",pVal->bstrVal);

	if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL )
	{
		//WriteLog(L"%s"),pVal->bstrVal);
		DWORD dwPid = 0, dwVid = 0;
		WCHAR* strVid = wcsstr( pVal->bstrVal, L"VID_" );

		if(strVid && swscanf_s( strVid, L"VID_%4X", &dwVid ) != 1 )
			return hr;

		WCHAR* strPid = wcsstr( pVal->bstrVal, L"PID_" );

		if(strPid && swscanf_s( strPid, L"PID_%4X", &dwPid ) != 1 )
			return hr;

		for(WORD i = 0; i < iHookWMIU->GetHookCount(); i++)
		{
			iHookPadConfig &padconf = iHookWMIU->GetPadConfig(i);
			if(padconf.GetHookState() && padconf.GetProductVIDPID() == (DWORD)MAKELONG(dwVid,dwPid))
			{
				WCHAR* strUSB = wcsstr( pVal->bstrVal, L"USB" );
				WCHAR tempstr[MAX_PATH];

				if( strUSB )
				{
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);

					DWORD dwHookVid = NULL;
					DWORD dwHookPid = NULL;

					if(iHookWMIU->CheckHook(iHook::HOOK_VIDPID))
					{
						dwHookVid = LOWORD(iHookWMIU->GetFakeVIDPID());
						dwHookPid = HIWORD(iHookWMIU->GetFakeVIDPID());
					}
					else
					{
						dwHookVid = LOWORD(padconf.GetProductVIDPID());
						dwHookPid = HIWORD(padconf.GetProductVIDPID());
					}

					if(dwHookVid && dwHookPid)
					{
						swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d",dwHookVid,dwHookPid,i );
						Hookbstr=SysAllocString(tempstr);
						pVal->bstrVal = Hookbstr;
						WriteLog(LOG_HOOKWMI,L"Fake DeviceID = %s",pVal->bstrVal);
					}
					return hr;
				}

				WCHAR* strHID = wcsstr( pVal->bstrVal, L"HID" );

				if( strHID )
				{
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);

					DWORD dwHookVid = NULL;
					DWORD dwHookPid = NULL;

					if(iHookWMIU->CheckHook(iHook::HOOK_VIDPID))
					{
						dwHookVid = LOWORD(iHookWMIU->GetFakeVIDPID());
						dwHookPid = HIWORD(iHookWMIU->GetFakeVIDPID());
					}
					else
					{
						dwHookVid = LOWORD(padconf.GetProductVIDPID());
						dwHookPid = HIWORD(padconf.GetProductVIDPID());
					}

					if(dwHookVid && dwHookPid)
					{
						swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", dwHookVid, dwHookPid,i );
						Hookbstr=SysAllocString(tempstr);
						pVal->bstrVal = Hookbstr;
						WriteLog(LOG_HOOKWMI,L"Fake DeviceID = %s",pVal->bstrVal);
					}
					return hr;
				}
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookNextW(
	IEnumWbemClassObject * This,
	/* [in] */ long lTimeout,
	/* [in] */ ULONG uCount,
	/* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
	/* [out] */ __RPC__out ULONG *puReturned)
{
	HRESULT hr = hNextW(This,lTimeout,uCount,apObjects,puReturned);

	if(!iHookWMIU->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookNextW");

	if(FAILED(hr)) return hr;

	IWbemClassObject* pDevices;

	if(apObjects)
	{
		if(*apObjects)
		{
			pDevices = *apObjects;

			if(!hGetW)
			{
				WriteLog(LOG_HOOKWMI,L"HookGetW:: Hooking");
				hGetW = pDevices->lpVtbl->Get;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hGetW, HookGetW);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateInstanceEnumW(
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	HRESULT hr = hCreateInstanceEnumW(This,strFilter,lFlags,pCtx,ppEnum);

	if(!iHookWMIU->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW");

	if(FAILED(hr)) return hr;

	IEnumWbemClassObject* pEnumDevices = NULL;

	if(ppEnum)
	{
		if(*ppEnum)
		{
			pEnumDevices = *ppEnum;

			if(!hNextW)
			{
				WriteLog(LOG_HOOKWMI,L"HookNextW:: Hooking");
				hNextW = pEnumDevices->lpVtbl->Next;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hNextW, HookNextW);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookConnectServerW(
	IWbemLocator * This,
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace)

{
	HRESULT hr = hConnectServerW(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(!iHookWMIU->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookConnectServerW");

	if(FAILED(hr)) return hr;

	IWbemServices* pIWbemServices = NULL;

	if(ppNamespace)
	{
		if(*ppNamespace)
		{
			pIWbemServices = *ppNamespace;

			if(!hCreateInstanceEnumW)
			{
				WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW:: Hooking");
				hCreateInstanceEnumW = pIWbemServices->lpVtbl->CreateInstanceEnum;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hCreateInstanceEnumW, HookCreateInstanceEnumW);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookCoCreateInstanceW(__in     REFCLSID rclsid,
									 __in_opt LPUNKNOWN pUnkOuter,
									 __in     DWORD dwClsContext,
									 __in     REFIID riid,
									 __deref_out LPVOID FAR* ppv)
{
	HRESULT hr = hCoCreateInstanceW(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(!iHookWMIU->CheckHook(iHook::HOOK_WMI)) return hr;
	WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceW");

	if(FAILED(hr)) return hr;

	IWbemLocator* pIWbemLocator = NULL;

	if(ppv && IsEqualGUID(riid,IID_IWbemLocator))
	{
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

		if(pIWbemLocator)
		{
			if(!hConnectServerW)
			{
				WriteLog(LOG_HOOKWMI,L"HookConnectServerW:: Hooking");
				hConnectServerW = pIWbemLocator->lpVtbl->ConnectServer;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hConnectServerW, HookConnectServerW);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitializeW()
{
	if(!iHookWMIU->CheckHook(iHook::HOOK_WMI)) return hCoUninitializeW();
	WriteLog(LOG_HOOKWMI,L"HookCoUninitializeW");

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(hGetW)
	{
		WriteLog(LOG_HOOKWMI,L"HookGetW:: Removing Hook");
		DetourDetach(&(PVOID&)hGetW, HookGetW);
	}

	if(hNextW)
	{
		WriteLog(LOG_HOOKWMI,L"HookNextW:: Removing Hook");
		DetourDetach(&(PVOID&)hNextW, HookNextW);
	}

	if(hCreateInstanceEnumW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW:: Removing Hook");
		DetourDetach(&(PVOID&)hCreateInstanceEnumW, HookCreateInstanceEnumW);
	}

	if(hConnectServerW)
	{
		WriteLog(LOG_HOOKWMI,L"HookConnectServerW:: Removing Hook");
		DetourDetach(&(PVOID&)hConnectServerW, HookConnectServerW);
	}

	DetourTransactionCommit();

	return hCoUninitializeW();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookWMI_UNI()
{
	if(!CheckHook(iHook::HOOK_WMI)) return;
	WriteLog(LOG_HOOKWMI,L"HookWMI:: Hooking");
	iHookWMIU = this;

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(!hCoCreateInstanceW)
	{
		hCoCreateInstanceW = CoCreateInstance;
		DetourAttach(&(PVOID&)hCoCreateInstanceW, HookCoCreateInstanceW);
	}

	if(!hCoUninitializeW)
	{
		hCoUninitializeW = CoUninitialize;
		DetourAttach(&(PVOID&)hCoUninitializeW, HookCoUninitializeW);
	}
	DetourTransactionCommit();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void iHook::HookWMI_UNI_Clean()
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(hGetW)
	{
		WriteLog(LOG_HOOKWMI,L"HookGetW:: Removing Hook");
		DetourDetach(&(PVOID&)hGetW, HookGetW);
	}

	if(hNextW)
	{
		WriteLog(LOG_HOOKWMI,L"HookNextW:: Removing Hook");
		DetourDetach(&(PVOID&)hNextW, HookNextW);
	}

	if(hCreateInstanceEnumW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW:: Removing Hook");
		DetourDetach(&(PVOID&)hCreateInstanceEnumW, HookCreateInstanceEnumW);
	}

	if(hConnectServerW)
	{
		WriteLog(LOG_HOOKWMI,L"HookConnectServerW:: Removing Hook");
		DetourDetach(&(PVOID&)hConnectServerW, HookConnectServerW);
	}

	if(hCoCreateInstanceW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceW:: Removing Hook");
		DetourDetach(&(PVOID&)hCoCreateInstanceW, HookCoCreateInstanceW);
	}

	if(hCoUninitializeW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCoUninitializeW:: Removing Hook");
		DetourDetach(&(PVOID&)hCoUninitializeW, HookCoUninitializeW);
	}
	DetourTransactionCommit();
}