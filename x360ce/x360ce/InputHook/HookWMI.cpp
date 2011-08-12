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

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

typedef void (WINAPI *tCoUninitialize)();

typedef HRESULT (WINAPI *tCoCreateInstance)(__in     REFCLSID rclsid, 
									  __in_opt LPUNKNOWN pUnkOuter,
									  __in     DWORD dwClsContext, 
									  __in     REFIID riid, 
									  __deref_out LPVOID FAR* ppv);


typedef HRESULT ( STDMETHODCALLTYPE *tConnectServer )( 
	IWbemLocator * This,
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace);

typedef HRESULT ( STDMETHODCALLTYPE *tCreateInstanceEnum )( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

typedef HRESULT ( STDMETHODCALLTYPE *tNext )( 
									   IEnumWbemClassObject * This,
									   /* [in] */ long lTimeout,
									   /* [in] */ ULONG uCount,
									   /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
									   /* [out] */ __RPC__out ULONG *puReturned);

typedef HRESULT ( STDMETHODCALLTYPE *tGet )( 
									  IWbemClassObject * This,
									  /* [string][in] */ LPCWSTR wszName,
									  /* [in] */ long lFlags,
									  /* [unique][in][out] */ VARIANT *pVal,
									  /* [unique][in][out] */ CIMTYPE *pType,
									  /* [unique][in][out] */ long *plFlavor);

MologieDetours::Detour<tCoUninitialize>* hCoUninitialize = NULL;
MologieDetours::Detour<tCoCreateInstance>* hCoCreateInstance = NULL;
MologieDetours::Detour<tConnectServer>* hConnectServer = NULL;
MologieDetours::Detour<tCreateInstanceEnum>* hCreateInstanceEnum = NULL;
MologieDetours::Detour<tNext>* hNext = NULL;
MologieDetours::Detour<tGet>* hGet = NULL;

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
	HRESULT hr = hGet->GetOriginalFunction()(This,wszName,lFlags,pVal,pType,plFlavor);
	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKWMI,L"HookGet");

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
			if(GamepadConfig[i].bEnabled && GamepadConfig[i].dwVID == dwVid && GamepadConfig[i].dwPID == dwPid) {
				WCHAR* strUSB = wcsstr( pVal->bstrVal, L"USB" );
				WCHAR tempstr[MAX_PATH];
				if( strUSB ) {
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);
					if(InputHookConfig.dwHookMode >= HOOK_COMPAT) swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d", InputHookConfig.dwHookVID, InputHookConfig.dwHookPID,i ); 
					else swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d", GamepadConfig[i].dwVID , GamepadConfig[i].dwPID,i );
					Hookbstr=SysAllocString(tempstr);
					pVal->bstrVal = Hookbstr;
					WriteLog(LOG_HOOKWMI,L"Hook DeviceID = %s",pVal->bstrVal);
					return hr;
				}
				WCHAR* strHID = wcsstr( pVal->bstrVal, L"HID" );
				if( strHID ) {
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);
					if(InputHookConfig.dwHookMode >= HOOK_COMPAT) swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", InputHookConfig.dwHookVID, InputHookConfig.dwHookPID,i );
					else swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", GamepadConfig[i].dwVID , GamepadConfig[i].dwPID,i );	 
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
	HRESULT hr = hNext->GetOriginalFunction()(This,lTimeout,uCount,apObjects,puReturned);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKWMI,L"HookNext");

	if(FAILED(hr)) return hr;

	IWbemClassObject* pDevices;

	if(apObjects) {
		if(*apObjects) {
			pDevices = *apObjects;
			if(!hGet) {
				WriteLog(LOG_HOOKWMI,L"HookGet:: Hooking");
				hGet = new MologieDetours::Detour<tGet>(pDevices->lpVtbl->Get, HookGet);
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
	HRESULT hr = hCreateInstanceEnum->GetOriginalFunction()(This,strFilter,lFlags,pCtx,ppEnum);
	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnum");

	if(FAILED(hr)) return hr;
	IEnumWbemClassObject* pEnumDevices = NULL;

	if(ppEnum) {
		if(*ppEnum) {
			pEnumDevices = *ppEnum;

			if(!hNext) {
				WriteLog(LOG_HOOKWMI,L"HookNext:: Hooking");
				hNext = new MologieDetours::Detour<tNext>(pEnumDevices->lpVtbl->Next, HookNext);
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
	HRESULT hr = hConnectServer->GetOriginalFunction()(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKWMI,L"HookConnectServer");
	if(FAILED(hr)) return hr;

	IWbemServices* pIWbemServices = NULL;

	if(ppNamespace) {
		if(*ppNamespace) {
			pIWbemServices = *ppNamespace;

			if(!hCreateInstanceEnum) {
				WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnum:: Hooking");
				hCreateInstanceEnum = new MologieDetours::Detour<tCreateInstanceEnum>(pIWbemServices->lpVtbl->CreateInstanceEnum, HookCreateInstanceEnum);
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
	HRESULT hr = hCoCreateInstance->GetOriginalFunction()(rclsid,pUnkOuter,dwClsContext,riid,ppv);
	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKWMI,L"HookCoCreateInstance");
	if(FAILED(hr)) return hr;

	IWbemLocator* pIWbemLocator = NULL;

	if(ppv && (riid == IID_IWbemLocator)) {
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);
		if(pIWbemLocator) {
			if(!hConnectServer) {
				WriteLog(LOG_HOOKWMI,L"HookConnectServer:: Hooking");
				hConnectServer = new MologieDetours::Detour<tConnectServer>(pIWbemLocator->lpVtbl->ConnectServer, HookConnectServer);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitialize()
{
	if(!InputHookConfig.bEnabled) return hCoUninitialize->GetOriginalFunction()();
	WriteLog(LOG_HOOKWMI,L"HookCoUninitialize");

	if(hGet) {
		WriteLog(LOG_HOOKWMI,L"HookGet:: Removing Hook");
		SAFE_DELETE(hGet);
	}

	if(hNext) {
		WriteLog(LOG_HOOKWMI,L"HookNext:: Removing Hook");
		SAFE_DELETE(hNext);
	}

	if(hCreateInstanceEnum) {
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnum:: Removing Hook");
		SAFE_DELETE(hCreateInstanceEnum);;
	}

	if(hConnectServer) {
		WriteLog(LOG_HOOKWMI,L"HookConnectServer:: Removing Hook");
		SAFE_DELETE(hConnectServer);
	}

	return hCoUninitialize->GetOriginalFunction()();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookWMI()
{
	if(!hCoCreateInstance) {
		hCoCreateInstance = new MologieDetours::Detour<tCoCreateInstance>(CoCreateInstance, HookCoCreateInstance);
	}
	if(!hCoUninitialize) {
		hCoUninitialize = new MologieDetours::Detour<tCoUninitialize>(CoUninitialize, HookCoUninitialize);
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void HookWMIClean()
{
	if(hGet) {
		WriteLog(LOG_HOOKWMI,L"HookGet:: Removing Hook");
		SAFE_DELETE(hGet);
	}

	if(hNext) {
		WriteLog(LOG_HOOKWMI,L"HookNext:: Removing Hook");
		SAFE_DELETE(hNext);
	}

	if(hCreateInstanceEnum) {
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnum:: Removing Hook");
		SAFE_DELETE(hCreateInstanceEnum);;
	}

	if(hConnectServer) {
		WriteLog(LOG_HOOKWMI,L"HookConnectServer:: Removing Hook");
		SAFE_DELETE(hConnectServer);
	}

	if(hCoCreateInstance) {
		WriteLog(LOG_HOOKWMI,L"HookCoCreateInstance:: Removing Hook");
		SAFE_DELETE(hCoCreateInstance);
	}

	if(hCoUninitialize) {
		WriteLog(LOG_HOOKWMI,L"HookCoUninitialize:: Removing Hook");
		SAFE_DELETE(hCoUninitialize);
	}
}