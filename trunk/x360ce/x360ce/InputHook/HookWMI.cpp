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
#include "InputHook.h"
#include "Utilities\Log.h"

#define CINTERFACE
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>
#include <dinput.h>

// COM CLSIDs
#pragma comment(lib,"wbemuuid.lib")

// EasyHook handles
TRACED_HOOK_HANDLE		hHookGet = NULL;
TRACED_HOOK_HANDLE		hHookNext = NULL;
TRACED_HOOK_HANDLE		hHookCreateInstanceEnum = NULL;
TRACED_HOOK_HANDLE		hHookConnectServer = NULL;
TRACED_HOOK_HANDLE		hHookCoCreateInstance = NULL;
TRACED_HOOK_HANDLE		hHookCoUninitialize = NULL;


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void (WINAPI *OriginalCoUninitialize)() = NULL;
HRESULT (WINAPI *OriginalCoCreateInstance)(__in     REFCLSID rclsid, 
									  __in_opt LPUNKNOWN pUnkOuter,
									  __in     DWORD dwClsContext, 
									  __in     REFIID riid, 
									  __deref_out LPVOID FAR* ppv) = NULL;


HRESULT ( STDMETHODCALLTYPE *OriginalConnectServer )( 
	IWbemLocator * This,
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace) = NULL;

HRESULT ( STDMETHODCALLTYPE *OriginalCreateInstanceEnum )( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = NULL;

HRESULT ( STDMETHODCALLTYPE *OriginalNext )( 
									   IEnumWbemClassObject * This,
									   /* [in] */ long lTimeout,
									   /* [in] */ ULONG uCount,
									   /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
									   /* [out] */ __RPC__out ULONG *puReturned) = NULL;

HRESULT ( STDMETHODCALLTYPE *OriginalGet )( 
									  IWbemClassObject * This,
									  /* [string][in] */ LPCWSTR wszName,
									  /* [in] */ long lFlags,
									  /* [unique][in][out] */ VARIANT *pVal,
									  /* [unique][in][out] */ CIMTYPE *pType,
									  /* [unique][in][out] */ long *plFlavor) = NULL;
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGet( 
	IWbemClassObject * This,
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	if(!InputHook_Config()->bEnabled) return OriginalGet(This,wszName,lFlags,pVal,pType,plFlavor);

	WriteLog(LOG_HOOKWMI,L"HookGet");
	HRESULT hr = OriginalGet(This,wszName,lFlags,pVal,pType,plFlavor);
	if(FAILED(hr)) return hr;

	//WriteLog(L"wszName %s pVal->vt %d pType %d",wszName,pVal->vt,&pType);
	//if( pVal->vt == VT_BSTR) WriteLog(L"%s",pVal->bstrVal);

	if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL ) {
		//WriteLog(L"%s"),pVal->bstrVal); 
		DWORD dwPid = 0, dwVid = 0;
		WCHAR* strVid = wcsstr( pVal->bstrVal, L"VID_" );
		if(strVid && swscanf_s( strVid, L"VID_%4X", &dwVid ) != 1 )
			return hr;
		WCHAR* strPid = wcsstr( pVal->bstrVal, L"PID_" );
		if(strPid && swscanf_s( strPid, L"PID_%4X", &dwPid ) != 1 )
			return hr;

		for(WORD i = 0; i < 4; i++) {
			if(InputHook_GamepadConfig(i)->bEnabled && InputHook_GamepadConfig(i)->dwVID == dwVid && InputHook_GamepadConfig(i)->dwPID == dwPid) {
				WCHAR* strUSB = wcsstr( pVal->bstrVal, L"USB" );
				WCHAR tempstr[MAX_PATH];
				if( strUSB ) {
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);
					if(InputHook_Config()->dwHookMode >= HOOK_COMPAT) swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d", InputHook_Config()->dwHookVID, InputHook_Config()->dwHookPID,i ); 
					else swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d", InputHook_GamepadConfig(i)->dwVID , InputHook_GamepadConfig(i)->dwPID,i );
					Hookbstr=SysAllocString(tempstr);
					pVal->bstrVal = Hookbstr;
					WriteLog(LOG_HOOKWMI,L"Hook DeviceID = %s",pVal->bstrVal);
					return hr;
				}
				WCHAR* strHID = wcsstr( pVal->bstrVal, L"HID" );
				if( strHID ) {
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);
					if(InputHook_Config()->dwHookMode >= HOOK_COMPAT) swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", InputHook_Config()->dwHookVID, InputHook_Config()->dwHookPID,i );
					else swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", InputHook_GamepadConfig(i)->dwVID , InputHook_GamepadConfig(i)->dwPID,i );	 
					Hookbstr=SysAllocString(tempstr);
					pVal->bstrVal = Hookbstr;
					WriteLog(LOG_HOOKWMI,L"Hook DeviceID = %s",pVal->bstrVal);
					return hr;
				}
			}
		} 
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookNext( 
								  IEnumWbemClassObject * This,
								  /* [in] */ long lTimeout,
								  /* [in] */ ULONG uCount,
								  /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
								  /* [out] */ __RPC__out ULONG *puReturned)
{
	if(!InputHook_Config()->bEnabled) return OriginalNext(This,lTimeout,uCount,apObjects,puReturned);
	WriteLog(LOG_HOOKWMI,L"HookNext");
	HRESULT hr;
	IWbemClassObject* pDevices;

	hr = OriginalNext(This,lTimeout,uCount,apObjects,puReturned);
	if(FAILED(hr)) return hr;

	if(apObjects) {
		if(*apObjects) {
			pDevices = *apObjects;
			if(!OriginalGet) {
				OriginalGet = pDevices->lpVtbl->Get;
				hHookGet = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalGet,HookGet,static_cast<PVOID>(NULL),hHookGet);
				LhSetExclusiveACL(ACLEntries, 0, hHookGet);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateInstanceEnum( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter, 
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	if(!InputHook_Config()->bEnabled) return OriginalCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);
	WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnum");
	HRESULT hr;
	IEnumWbemClassObject* pEnumDevices = NULL;

	hr = OriginalCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);
	if(FAILED(hr)) return hr;

	if(ppEnum) {
		if(*ppEnum) {
			pEnumDevices = *ppEnum;

			if(!OriginalNext) {
				OriginalNext = pEnumDevices->lpVtbl->Next;
				hHookNext = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalNext,HookNext,static_cast<PVOID>(NULL),hHookNext);
				LhSetExclusiveACL(ACLEntries, 0, hHookNext);

			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookConnectServer( 
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
	if(!InputHook_Config()->bEnabled) return OriginalConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);
	WriteLog(LOG_HOOKWMI,L"HookConnectServer");
	HRESULT hr;
	IWbemServices* pIWbemServices = NULL;

	hr = OriginalConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);
	if(FAILED(hr)) return hr;

	if(ppNamespace) {
		if(*ppNamespace) {
			pIWbemServices = *ppNamespace;

			if(!OriginalCreateInstanceEnum) {
				OriginalCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
				hHookCreateInstanceEnum = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalCreateInstanceEnum,HookCreateInstanceEnum,static_cast<PVOID>(NULL),hHookCreateInstanceEnum);
				LhSetExclusiveACL(ACLEntries, 0, hHookCreateInstanceEnum);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookCoCreateInstance(__in     REFCLSID rclsid, 
								   __in_opt LPUNKNOWN pUnkOuter,
								   __in     DWORD dwClsContext, 
								   __in     REFIID riid, 
								   __deref_out LPVOID FAR* ppv)
{
	if(!InputHook_Config()->bEnabled) return OriginalCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);
	HRESULT hr;
	IWbemLocator* pIWbemLocator = NULL;

	hr = OriginalCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);
	if(FAILED(hr)) return hr;

	if(ppv && (riid == IID_IWbemLocator)) {
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);
		if(pIWbemLocator) {
			WriteLog(LOG_HOOKWMI,L"HookCoCreateInstance");
			if(!OriginalConnectServer) {
				OriginalConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
				hHookConnectServer = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalConnectServer,HookConnectServer,static_cast<PVOID>(NULL),hHookConnectServer);
				LhSetExclusiveACL(ACLEntries, 0, hHookConnectServer);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookCoUninitialize()
{
	WriteLog(LOG_HOOKWMI,L"HookCoUninitialize");

	if(OriginalGet) {
		WriteLog(LOG_HOOKWMI,L"HookGet:: Removing Hook");

		LhUninstallHook(hHookGet);
		LhWaitForPendingRemovals();
		SAFE_DELETE(hHookGet);
		OriginalGet = NULL;
	}

	if(OriginalNext) {
		WriteLog(LOG_HOOKWMI,L"HookNext:: Removing Hook");

		LhUninstallHook(hHookNext);
		LhWaitForPendingRemovals();
		SAFE_DELETE(hHookNext);
		OriginalNext=NULL;
	}

	if(OriginalCreateInstanceEnum) {
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnum:: Removing Hook");

		LhUninstallHook(hHookCreateInstanceEnum);
		LhWaitForPendingRemovals();
		SAFE_DELETE(hHookCreateInstanceEnum);
		OriginalCreateInstanceEnum=NULL;
	}

	if(OriginalConnectServer) {
		WriteLog(LOG_HOOKWMI,L"HookConnectServer:: Removing Hook");

		LhUninstallHook(hHookConnectServer);
		LhWaitForPendingRemovals();
		SAFE_DELETE(hHookConnectServer);
		OriginalConnectServer=NULL;
	}
	OriginalCoUninitialize();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookWMI()
{
	if(!OriginalCoCreateInstance) {
		OriginalCoCreateInstance = CoCreateInstance;
		hHookCoCreateInstance = new HOOK_TRACE_INFO();
		WriteLog(LOG_IHOOK,L"HookCoCreateInstance:: Hooking");

		LhInstallHook(OriginalCoCreateInstance,HookCoCreateInstance,static_cast<PVOID>(NULL),hHookCoCreateInstance);
		LhSetExclusiveACL(ACLEntries, 0, hHookCoCreateInstance);

	}
	if(!OriginalCoUninitialize) {
		OriginalCoUninitialize = CoUninitialize;
		hHookCoUninitialize = new HOOK_TRACE_INFO();
		WriteLog(LOG_IHOOK,L"HookCoUninitialize:: Hooking");

		LhInstallHook(OriginalCoUninitialize,HookCoUninitialize,static_cast<PVOID>(NULL),hHookCoUninitialize);
		LhSetExclusiveACL(ACLEntries, 0, hHookCoUninitialize);

	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void HookWMIClean()
{
	SAFE_DELETE(hHookGet);
	SAFE_DELETE(hHookNext);
	SAFE_DELETE(hHookCreateInstanceEnum);
	SAFE_DELETE(hHookConnectServer);
	SAFE_DELETE(hHookCoCreateInstance);
	SAFE_DELETE(hHookCoUninitialize);
}