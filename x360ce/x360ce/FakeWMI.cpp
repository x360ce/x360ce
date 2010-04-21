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

#define CINTERFACE			//needed for detours
#define _WIN32_DCOM
#include <wbemidl.h>
#pragma comment(lib, "wbemuuid.lib")
#include <ole2.h>
#include <oleauto.h>
#include <detours.h>
#include "Utils.h"
#include "DirectInput.h"

#define _CRT_SECURE_NO_DEPRECATE
#define _WIN32_DCOM

HRESULT ( STDMETHODCALLTYPE *OldGet )( 
									  IWbemClassObject * This,
									  /* [string][in] */ LPCWSTR wszName,
									  /* [in] */ long lFlags,
									  /* [unique][in][out] */ VARIANT *pVal,
									  /* [unique][in][out] */ CIMTYPE *pType,
									  /* [unique][in][out] */ long *plFlavor) = NULL;

HRESULT STDMETHODCALLTYPE NewGet( 
								 IWbemClassObject * This,
								 /* [string][in] */ LPCWSTR wszName,
								 /* [in] */ long lFlags,
								 /* [unique][in][out] */ VARIANT *pVal,
								 /* [unique][in][out] */ CIMTYPE *pType,
								 /* [unique][in][out] */ long *plFlavor)
{
	WriteLog(_T("NewGet"));
	HRESULT hr;
	hr = OldGet(This,wszName,lFlags,pVal,pType,plFlavor);

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
							WriteLog(_T("[FAKEWMI] Old DeviceID = %s"),pVal->bstrVal);
							_stprintf_s(tempstr,_T("USB\\VID_%04X&PID_%04X&IG_00"), wFakeVID, wFakePID );
							fakebstr=SysAllocString(tempstr);
							pVal->bstrVal = fakebstr;
							SysFreeString(fakebstr);
							WriteLog(_T("[FAKEWMI] New DeviceID = %s"),pVal->bstrVal);
							return hr;
						}

						if(wFakeWMI>=2)
						{

							WCHAR* strHID = _tcsstr( pVal->bstrVal, _T("HID") );
							if( strHID )
							{
								BSTR fakebstr=NULL;
								WriteLog(_T("[FAKEWMI] Old DeviceID = %s"),pVal->bstrVal);
								_stprintf_s(tempstr,_T("HID\\VID_%04X&PID_%04X&IG_00"), wFakeVID, wFakePID );
								fakebstr=SysAllocString(tempstr);
								pVal->bstrVal = fakebstr;
								SysFreeString(fakebstr);
								WriteLog(_T("[FAKEWMI] New DeviceID = %s"),pVal->bstrVal);
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

HRESULT ( STDMETHODCALLTYPE *OldNext )( 
									   IEnumWbemClassObject * This,
									   /* [in] */ long lTimeout,
									   /* [in] */ ULONG uCount,
									   /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
									   /* [out] */ __RPC__out ULONG *puReturned) = NULL;

HRESULT STDMETHODCALLTYPE NewNext( 
								  IEnumWbemClassObject * This,
								  /* [in] */ long lTimeout,
								  /* [in] */ ULONG uCount,
								  /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
								  /* [out] */ __RPC__out ULONG *puReturned)
{
	WriteLog(_T("NewNext"));
	HRESULT hr;
	IWbemClassObject* pDevices;

	hr = OldNext(This,lTimeout,uCount,apObjects,puReturned);

	if(apObjects!=NULL)
	{
		if(*apObjects!=NULL)
		{
			pDevices = *apObjects;
			if(OldGet == NULL)
			{

				OldGet = pDevices->lpVtbl->Get;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldGet, NewGet);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

HRESULT ( STDMETHODCALLTYPE *OldCreateInstanceEnum )( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum) = NULL;

HRESULT STDMETHODCALLTYPE NewCreateInstanceEnum( 
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter, 
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	WriteLog(_T("NewCreateInstanceEnum"));
	HRESULT hr;
	IEnumWbemClassObject* pEnumDevices = NULL;

	hr = OldCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);

	if(ppEnum != NULL)
	{
		if(*ppEnum != NULL)
		{
			pEnumDevices = *ppEnum;

			if(OldNext == NULL) 
			{
				OldNext = pEnumDevices->lpVtbl->Next;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldNext, NewNext);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

HRESULT ( STDMETHODCALLTYPE *OldConnectServer )( 
	IWbemLocator * This,
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace) = NULL;

HRESULT STDMETHODCALLTYPE NewConnectServer( 
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
	WriteLog(_T("NewConnectServer"));
	HRESULT hr;
	IWbemServices* pIWbemServices = NULL;

	hr = OldConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(ppNamespace != NULL)
	{
		if(*ppNamespace != NULL)
		{
			pIWbemServices = *ppNamespace;

			if(OldCreateInstanceEnum == NULL) 
			{
				OldCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldCreateInstanceEnum, NewCreateInstanceEnum);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

HRESULT (WINAPI *OldCoCreateInstance)(__in     REFCLSID rclsid, 
									  __in_opt LPUNKNOWN pUnkOuter,
									  __in     DWORD dwClsContext, 
									  __in     REFIID riid, 
									  __deref_out LPVOID FAR* ppv) = CoCreateInstance;

HRESULT WINAPI NewCoCreateInstance(__in     REFCLSID rclsid, 
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
	hr = OldCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(ppv != NULL && riid == IID_IWbemLocator) 
	{
		//WriteLog(_T("NewCoCreateInstance if1 "));

		pIWbemLocator = (IWbemLocator *) *ppv;
		if(pIWbemLocator != NULL) 
		{
			//WriteLog(_T("NewCoCreateInstance if2"));
			if(OldConnectServer == NULL) 
			{

				OldConnectServer = pIWbemLocator->lpVtbl->ConnectServer;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldConnectServer, NewConnectServer);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

void FakeWMI()
{
	WriteLog(_T("[FAKEAPI] FakeWMI"));

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)OldCoCreateInstance, NewCoCreateInstance);
	DetourTransactionCommit();

}