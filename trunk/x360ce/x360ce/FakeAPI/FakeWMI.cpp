/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 ToCA Edit
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
#include "FakeAPI.h"
#include "Utils.h"

#define CINTERFACE
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>
#include <dinput.h>

#pragma comment(lib,"wbemuuid.lib")

//EasyHook
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
HRESULT STDMETHODCALLTYPE FakeGet( 
	IWbemClassObject * This,
	/* [string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalGet(This,wszName,lFlags,pVal,pType,plFlavor);

	WriteLog(L"[FAKEWMI] FakeGet");
	HRESULT hr = OriginalGet(This,wszName,lFlags,pVal,pType,plFlavor);

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
			if(FakeAPI_GamepadConfig(i)->bEnabled && FakeAPI_GamepadConfig(i)->dwVID == dwVid && FakeAPI_GamepadConfig(i)->dwPID == dwPid) {
				WCHAR* strUSB = wcsstr( pVal->bstrVal, L"USB" );
				WCHAR tempstr[MAX_PATH];
				if( strUSB ) {
					BSTR fakebstr=NULL;
					WriteLog(L"[FAKEWMI] Original DeviceID = %s",pVal->bstrVal);
					if(FakeAPI_Config()->dwFakeMode >= 2) swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d", FakeAPI_Config()->dwFakeVID, FakeAPI_Config()->dwFakePID,i ); 
					else swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d", FakeAPI_GamepadConfig(i)->dwVID , FakeAPI_GamepadConfig(i)->dwPID,i );
					fakebstr=SysAllocString(tempstr);
					pVal->bstrVal = fakebstr;
					WriteLog(L"[FAKEWMI] Fake DeviceID = %s",pVal->bstrVal);
					return hr;
				}
				WCHAR* strHID = wcsstr( pVal->bstrVal, L"HID" );
				if( strHID ) {
					BSTR fakebstr=NULL;
					WriteLog(L"[FAKEWMI] Original DeviceID = %s",pVal->bstrVal);
					if(FakeAPI_Config()->dwFakeMode >= 2) swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", FakeAPI_Config()->dwFakeVID, FakeAPI_Config()->dwFakePID,i );
					else swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d", FakeAPI_GamepadConfig(i)->dwVID , FakeAPI_GamepadConfig(i)->dwPID,i );	 
					fakebstr=SysAllocString(tempstr);
					pVal->bstrVal = fakebstr;
					WriteLog(L"[FAKEWMI] Fake DeviceID = %s",pVal->bstrVal);
					return hr;
				}
			}
		} 
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeNext( 
								  IEnumWbemClassObject * This,
								  /* [in] */ long lTimeout,
								  /* [in] */ ULONG uCount,
								  /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
								  /* [out] */ __RPC__out ULONG *puReturned)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalNext(This,lTimeout,uCount,apObjects,puReturned);
	WriteLog(L"[FAKEWMI] FakeNext");
	HRESULT hr;
	IWbemClassObject* pDevices;

	hr = OriginalNext(This,lTimeout,uCount,apObjects,puReturned);

	if(apObjects) {
		if(*apObjects) {
			pDevices = *apObjects;
			if(!OriginalGet) {
				OriginalGet = pDevices->lpVtbl->Get;
				hHookGet = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalGet,FakeGet,(PVOID)NULL,hHookGet);
				LhSetInclusiveACL(ACLEntries, 1, hHookGet);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeCreateInstanceEnum( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter, 
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);
	WriteLog(L"[FAKEWMI] FakeCreateInstanceEnum");
	HRESULT hr;
	IEnumWbemClassObject* pEnumDevices = NULL;

	hr = OriginalCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);


	if(ppEnum) {
		if(*ppEnum) {
			pEnumDevices = *ppEnum;

			if(!OriginalNext) {
				OriginalNext = pEnumDevices->lpVtbl->Next;
				hHookNext = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalNext,FakeNext,(PVOID)NULL,hHookNext);
				LhSetInclusiveACL(ACLEntries, 1, hHookNext);

			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeConnectServer( 
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
	if(!FakeAPI_Config()->bEnabled) return OriginalConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);
	WriteLog(L"[FAKEWMI] FakeConnectServer");
	HRESULT hr;
	IWbemServices* pIWbemServices = NULL;

	hr = OriginalConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(ppNamespace) {
		if(*ppNamespace) {
			pIWbemServices = *ppNamespace;

			if(!OriginalCreateInstanceEnum) {
				OriginalCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
				hHookCreateInstanceEnum = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalCreateInstanceEnum,FakeCreateInstanceEnum,(PVOID)NULL,hHookCreateInstanceEnum);
				LhSetInclusiveACL(ACLEntries, 1, hHookCreateInstanceEnum);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI FakeCoCreateInstance(__in     REFCLSID rclsid, 
								   __in_opt LPUNKNOWN pUnkOuter,
								   __in     DWORD dwClsContext, 
								   __in     REFIID riid, 
								   __deref_out LPVOID FAR* ppv)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);
	HRESULT hr;
	IWbemLocator* pIWbemLocator = NULL;

	/*
	LPOLESTR str1;
	StringFromIID(rclsid,&str1);
	WriteLog(L"rclsid: %s"),str1);

	WriteLog(L"dwClsContext: %d"),dwClsContext);

	LPOLESTR str2;
	StringFromIID(riid,&str2);
	WriteLog(L"riid: %s"),str2);
	*/

	hr = OriginalCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(ppv && (riid == IID_IWbemLocator)) {
		//WriteLog(L"FakeCoCreateInstance if1 "));

		pIWbemLocator = (IWbemLocator *) *ppv;
		if(pIWbemLocator) {
			WriteLog(L"[FakeWMI] FakeCoCreateInstance");
			if(!OriginalConnectServer) {
				OriginalConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
				hHookConnectServer = new HOOK_TRACE_INFO();

				LhInstallHook(OriginalConnectServer,FakeConnectServer,(PVOID)NULL,hHookConnectServer);
				LhSetInclusiveACL(ACLEntries, 1, hHookConnectServer);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FakeCoUninitialize()
{
	WriteLog(L"[FAKEAPI] FakeCoUninitialize");

	if(OriginalGet) {
		WriteLog(L"[FAKEWMI] FakeGet:: Detaching");

		if(hHookGet) LhUninstallHook(hHookGet);
		SAFE_DELETE(hHookGet);

		OriginalGet = NULL;
	}

	if(OriginalNext) {
		WriteLog(L"[FAKEWMI] FakeNext:: Detaching");

		if(hHookNext) LhUninstallHook(hHookNext);
		SAFE_DELETE(hHookNext);

		OriginalNext=NULL;
	}

	if(OriginalCreateInstanceEnum) {
		WriteLog(L"[FAKEWMI] FakeCreateInstanceEnum:: Detaching");

		if(hHookCreateInstanceEnum) LhUninstallHook(hHookCreateInstanceEnum);
		SAFE_DELETE(hHookCreateInstanceEnum);

		OriginalCreateInstanceEnum=NULL;
	}

	if(OriginalConnectServer) {
		WriteLog(L"[FAKEWMI] FakeConnectServer:: Detaching");

		if(hHookConnectServer) LhUninstallHook(hHookConnectServer);
		SAFE_DELETE(hHookConnectServer);

		OriginalConnectServer=NULL;
	}
	OriginalCoUninitialize();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FakeWMI()
{
	if(!OriginalCoCreateInstance) {
		OriginalCoCreateInstance = CoCreateInstance;
		hHookCoCreateInstance = new HOOK_TRACE_INFO();
		WriteLog(L"[FAKEAPI] FakeCoCreateInstance:: Attaching");

		LhInstallHook(OriginalCoCreateInstance,FakeCoCreateInstance,(PVOID)NULL,hHookCoCreateInstance);
		LhSetInclusiveACL(ACLEntries, 1, hHookCoCreateInstance);

	}
	if(!OriginalCoUninitialize) {
		OriginalCoUninitialize = CoUninitialize;
		hHookCoUninitialize = new HOOK_TRACE_INFO();
		WriteLog(L"[FAKEAPI] FakeCoUninitialize:: Attaching");

		LhInstallHook(OriginalCoUninitialize,FakeCoUninitialize,(PVOID)NULL,hHookCoUninitialize);
		LhSetInclusiveACL(ACLEntries, 1, hHookCoUninitialize);

	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void FakeWMIClean()
{
	SAFE_DELETE(hHookGet);
	SAFE_DELETE(hHookNext);
	SAFE_DELETE(hHookCreateInstanceEnum);
	SAFE_DELETE(hHookConnectServer);
	SAFE_DELETE(hHookCoCreateInstance);
	SAFE_DELETE(hHookCoUninitialize);
}