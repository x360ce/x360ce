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

#define CINTERFACE			//needed for detours
#define _WIN32_DCOM
#include <wbemidl.h>
#pragma comment(lib, "wbemuuid.lib")
#include <ole2.h>
#include <oleauto.h>
#include <detours.h>
#include "FakeWMI.h"

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT (WINAPI *OriginalCoCreateInstance)(__in     REFCLSID rclsid, 
									  __in_opt LPUNKNOWN pUnkOuter,
									  __in     DWORD dwClsContext, 
									  __in     REFIID riid, 
									  __deref_out LPVOID FAR* ppv) = CoCreateInstance;


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


HRESULT STDMETHODCALLTYPE FakeGet( 
								 IWbemClassObject * This,
								 /* [string][in] */ LPCWSTR wszName,
								 /* [in] */ long lFlags,
								 /* [unique][in][out] */ VARIANT *pVal,
								 /* [unique][in][out] */ CIMTYPE *pType,
								 /* [unique][in][out] */ long *plFlavor)
{
	WriteLog(_T("[FAKEWMI] FakeGet"));
	HRESULT hr;
	hr = OriginalGet(This,wszName,lFlags,pVal,pType,plFlavor);

	//WriteLog(_T("wszName %s pVal->vt %d"),wszName,pVal->vt);

	//BSTR bstrDeviceID = SysAllocString( L"DeviceID" );

	if (wFakeWMI >=1)
	{

		if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL )
		{
			//WriteLog(_T("%s"),pVal->bstrVal); 
			DWORD dwPid = 0, dwVid = 0;
			WCHAR* strVid = _tcsstr( pVal->bstrVal, _T("VID_") );
			if(strVid && _stscanf_s( strVid, _T("VID_%4X"), &dwVid ) != 1 )
				return hr;
			WCHAR* strPid = _tcsstr( pVal->bstrVal, _T("PID_") );
			if(strPid && _stscanf_s( strPid, _T("PID_%4X"), &dwPid ) != 1 )
				return hr;

			for(int i = 0; i < 4; i++)
			{
				if(Gamepad[i].vid != 0 && Gamepad[i].vid == dwVid )
				{
					if(Gamepad[i].pid != 0 && Gamepad[i].pid == dwPid)
					{
						WCHAR* strUSB = _tcsstr( pVal->bstrVal, _T("USB") );
						WCHAR tempstr[MAX_PATHW];
						if( strUSB )
						{
							BSTR fakebstr=NULL;
							WriteLog(_T("[FAKEWMI] Original DeviceID = %s"),pVal->bstrVal);
							_stprintf_s(tempstr,_T("USB\\VID_%04X&PID_%04X&IG_00"), wFakeVID, wFakePID );
							fakebstr=SysAllocString(tempstr);
							pVal->bstrVal = fakebstr;
							SysFreeString(fakebstr);
							WriteLog(_T("[FAKEWMI] Fake DeviceID = %s"),pVal->bstrVal);
							return hr;
						}

						if(wFakeWMI>=2)
						{

							WCHAR* strHID = _tcsstr( pVal->bstrVal, _T("HID") );
							if( strHID )
							{
								BSTR fakebstr=NULL;
								WriteLog(_T("[FAKEWMI] Original DeviceID = %s"),pVal->bstrVal);
								_stprintf_s(tempstr,_T("HID\\VID_%04X&PID_%04X&IG_00"), wFakeVID, wFakePID );
								fakebstr=SysAllocString(tempstr);
								pVal->bstrVal = fakebstr;
								SysFreeString(fakebstr);
								WriteLog(_T("[FAKEWMI] Fake DeviceID = %s"),pVal->bstrVal);
								return hr;
							}
						}

					}

				} 
			}
		}
	}

	return hr;
}

HRESULT STDMETHODCALLTYPE FakeNext( 
								  IEnumWbemClassObject * This,
								  /* [in] */ long lTimeout,
								  /* [in] */ ULONG uCount,
								  /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
								  /* [out] */ __RPC__out ULONG *puReturned)
{
	WriteLog(_T("[FAKEWMI] FakeNext"));
	HRESULT hr;
	IWbemClassObject* pDevices;

	hr = OriginalNext(This,lTimeout,uCount,apObjects,puReturned);

	if(apObjects!=NULL)
	{
		if(*apObjects!=NULL)
		{
			pDevices = *apObjects;
			if(OriginalGet == NULL)
			{

				OriginalGet = pDevices->lpVtbl->Get;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGet, FakeGet);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

HRESULT STDMETHODCALLTYPE FakeCreateInstanceEnum( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter, 
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	WriteLog(_T("[FAKEWIM] FakeCreateInstanceEnum"));
	HRESULT hr;
	IEnumWbemClassObject* pEnumDevices = NULL;

	hr = OriginalCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);

	if(ppEnum != NULL)
	{
		if(*ppEnum != NULL)
		{
			pEnumDevices = *ppEnum;

			if(OriginalNext == NULL) 
			{
				OriginalNext = pEnumDevices->lpVtbl->Next;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalNext, FakeNext);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

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
	WriteLog(_T("[FAKEWMI] FakeConnectServer"));
	HRESULT hr;
	IWbemServices* pIWbemServices = NULL;

	hr = OriginalConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(ppNamespace != NULL)
	{
		if(*ppNamespace != NULL)
		{
			pIWbemServices = *ppNamespace;

			if(OriginalCreateInstanceEnum == NULL) 
			{
				OriginalCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalCreateInstanceEnum, FakeCreateInstanceEnum);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

HRESULT WINAPI FakeCoCreateInstance(__in     REFCLSID rclsid, 
								   __in_opt LPUNKNOWN pUnkOuter,
								   __in     DWORD dwClsContext, 
								   __in     REFIID riid, 
								   __deref_out LPVOID FAR* ppv)
{
	HRESULT hr;
	IWbemLocator* pIWbemLocator = NULL;
	/*
	LPOLESTR str1;
	StringFromIID(rclsid,&str1);
	WriteLog(_T("rclsid: %s"),str1);

	WriteLog(_T("dwClsContext: %d"),dwClsContext);

	LPOLESTR str2;
	StringFromIID(riid,&str2);
	WriteLog(_T("riid: %s"),str2);
	*/
	hr = OriginalCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(ppv != NULL && riid == IID_IWbemLocator) 
	{
		//WriteLog(_T("FakeCoCreateInstance if1 "));

		pIWbemLocator = (IWbemLocator *) *ppv;
		if(pIWbemLocator != NULL) 
		{
			WriteLog(_T("[FakeWMI] FakeCoCreateInstance"));
			if(OriginalConnectServer == NULL) 
			{

				OriginalConnectServer = pIWbemLocator->lpVtbl->ConnectServer;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalConnectServer, FakeConnectServer);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

void FakeWMI(bool state)
{
	WriteLog(_T("[FAKEAPI] FakeWMI(%d)"),state);

	if(state){
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)OriginalCoCreateInstance, FakeCoCreateInstance);
		DetourTransactionCommit();
	}
	else {
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalCoCreateInstance, FakeCoCreateInstance);
		DetourTransactionCommit();
	}

}