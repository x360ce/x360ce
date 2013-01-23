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

#define CINTERFACE
#define OLE2ANSI
#define _WIN32_DCOM
#include <wbemidl.h>
#include <ole2.h>
#include <oleauto.h>
#include <dinput.h>

#include "InputHook.h"

static iHook *iHookThis = NULL;

// COM CLSIDs
#pragma comment(lib,"wbemuuid.lib")

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

typedef void (WINAPI *tCoUninitializeA)();

typedef HRESULT (WINAPI *tCoCreateInstanceA)(__in     REFCLSID rclsid,
											 __in_opt LPUNKNOWN pUnkOuter,
											 __in     DWORD dwClsContext,
											 __in     REFIID riid,
											 __deref_out LPVOID FAR* ppv);


typedef HRESULT ( STDMETHODCALLTYPE *tConnectServerA )(
	IWbemLocator * This,
	/* [in] */ const BSTR strNetworkResource,
	/* [in] */ const BSTR strUser,
	/* [in] */ const BSTR strPassword,
	/* [in] */ const BSTR strLocale,
	/* [in] */ long lSecurityFlags,
	/* [in] */ const BSTR strAuthority,
	/* [in] */ IWbemContext *pCtx,
	/* [out] */ IWbemServices **ppNamespace);

typedef HRESULT ( STDMETHODCALLTYPE *tCreateInstanceEnumA )(
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

typedef HRESULT ( STDMETHODCALLTYPE *tNextA )(
	IEnumWbemClassObject * This,
	/* [in] */ long lTimeout,
	/* [in] */ ULONG uCount,
	/* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
	/* [out] */ __RPC__out ULONG *puReturned);

typedef HRESULT ( STDMETHODCALLTYPE *tGetA )(
	IWbemClassObject * This,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor);

static tCoUninitializeA hCoUninitializeA = NULL;
static tCoCreateInstanceA hCoCreateInstanceA = NULL;

static tConnectServerA hConnectServerA = NULL;
static tCreateInstanceEnumA hCreateInstanceEnumA = NULL;
static tNextA hNextA = NULL;
static tGetA hGetA = NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetA(
	IWbemClassObject * This,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	HRESULT hr = hGetA(This,wszName,lFlags,pVal,pType,plFlavor);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookGetA");

	if(FAILED(hr)) return hr;

	//WriteLog(L"wszName %s pVal->vt %d pType %d",wszName,pVal->vt,&pType);
	//if( pVal->vt == VT_BSTR) WriteLog(L"%s",pVal->bstrVal);

	if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL )
	{
		//WriteLog(L"%s"),pVal->bstrVal);
		DWORD dwPid = 0, dwVid = 0;
		char* strVid = strstr( pVal->bstrVal, "VID_" );

		if(strVid && sscanf_s( strVid, "VID_%4X", &dwVid ) != 1 )
			return hr;

		char* strPid = strstr( pVal->bstrVal, "PID_" );

		if(strPid && sscanf_s( strPid, "PID_%4X", &dwPid ) != 1 )
			return hr;

		for(WORD i = 0; i < 4; i++)
		{
			iHookPadConfig &padconf = iHookThis->GetPadConfig(i);
			if(padconf.GetHookState() && padconf.GetProductVIDPID() == (DWORD)MAKELONG(dwVid,dwPid))
			{
				char* strUSB = strstr( pVal->bstrVal, "USB" );
				char tempstr[MAX_PATH];

				if( strUSB )
				{
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);

					DWORD dwHookVid = NULL;
					DWORD dwHookPid = NULL;

					if(iHookThis->CheckHook(iHook::HOOK_VIDPID))
					{
						dwHookVid = LOWORD(iHookThis->GetFakeVIDPID());
						dwHookPid = HIWORD(iHookThis->GetFakeVIDPID());
					}
					else
					{
						dwHookVid = LOWORD(padconf.GetProductVIDPID());
						dwHookPid = HIWORD(padconf.GetProductVIDPID());
					}

					if(dwHookVid && dwHookPid)
					{
						sprintf_s(tempstr,"USB\\VID_%04X&PID_%04X&IG_%02d",dwHookVid,dwHookPid,i );
						Hookbstr=SysAllocString(tempstr);
						pVal->bstrVal = Hookbstr;
						WriteLog(LOG_HOOKWMI,L"Fake DeviceID = %s",pVal->bstrVal);
					}
					continue;
				}

				char* strHID = strstr( pVal->bstrVal, "HID" );

				if( strHID )
				{
					BSTR Hookbstr=NULL;
					WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);

					DWORD dwHookVid = NULL;
					DWORD dwHookPid = NULL;

					if(iHookThis->CheckHook(iHook::HOOK_VIDPID))
					{
						dwHookVid = LOWORD(iHookThis->GetFakeVIDPID());
						dwHookPid = HIWORD(iHookThis->GetFakeVIDPID());
					}
					else
					{
						dwHookVid = LOWORD(padconf.GetProductVIDPID());
						dwHookPid = HIWORD(padconf.GetProductVIDPID());
					}

					if(dwHookVid && dwHookPid)
					{
						sprintf_s(tempstr,"HID\\VID_%04X&PID_%04X&IG_%02d", dwHookVid, dwHookPid,i );
						Hookbstr=SysAllocString(tempstr);
						pVal->bstrVal = Hookbstr;
						WriteLog(LOG_HOOKWMI,L"Fake DeviceID = %s",pVal->bstrVal);
					}
					continue;
				}
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookNextA(
	IEnumWbemClassObject * This,
	/* [in] */ long lTimeout,
	/* [in] */ ULONG uCount,
	/* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
	/* [out] */ __RPC__out ULONG *puReturned)
{
	HRESULT hr = hNextA(This,lTimeout,uCount,apObjects,puReturned);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookNextA");

	if(FAILED(hr)) return hr;

	IWbemClassObject* pDevices;

	if(apObjects)
	{
		if(*apObjects)
		{
			pDevices = *apObjects;

			if(!hGetA)
			{
				WriteLog(LOG_HOOKWMI,L"HookGetA:: Hooking");
				hGetA = pDevices->lpVtbl->Get;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hGetA, HookGetA);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateInstanceEnumA(
	IWbemServices * This,
	/* [in] */ __RPC__in const BSTR strFilter,
	/* [in] */ long lFlags,
	/* [in] */ __RPC__in_opt IWbemContext *pCtx,
	/* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum)
{
	HRESULT hr = hCreateInstanceEnumA(This,strFilter,lFlags,pCtx,ppEnum);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumA");

	if(FAILED(hr)) return hr;

	IEnumWbemClassObject* pEnumDevices = NULL;

	if(ppEnum)
	{
		if(*ppEnum)
		{
			pEnumDevices = *ppEnum;

			if(!hNextA)
			{
				WriteLog(LOG_HOOKWMI,L"HookNextA:: Hooking");
				hNextA = pEnumDevices->lpVtbl->Next;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hNextA, HookNextA);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookConnectServerA(
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
	HRESULT hr = hConnectServerA(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookConnectServerA");

	if(FAILED(hr)) return hr;

	IWbemServices* pIWbemServices = NULL;

	if(ppNamespace)
	{
		if(*ppNamespace)
		{
			pIWbemServices = *ppNamespace;

			if(!hCreateInstanceEnumA)
			{
				WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumA:: Hooking");
				hCreateInstanceEnumA = pIWbemServices->lpVtbl->CreateInstanceEnum;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hCreateInstanceEnumA, HookCreateInstanceEnumA);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookCoCreateInstanceA(__in     REFCLSID rclsid,
									 __in_opt LPUNKNOWN pUnkOuter,
									 __in     DWORD dwClsContext,
									 __in     REFIID riid,
									 __deref_out LPVOID FAR* ppv)
{
	HRESULT hr = hCoCreateInstanceA(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceA");
	//if(FAILED(hr)) return hr;

	IWbemLocator* pIWbemLocator = NULL;

	if(ppv && (riid == IID_IWbemLocator))
	{
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

		if(pIWbemLocator)
		{
			if(!hConnectServerA)
			{
				WriteLog(LOG_HOOKWMI,L"HookConnectServerA:: Hooking");
				hConnectServerA = pIWbemLocator->lpVtbl->ConnectServer;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)hConnectServerA, HookConnectServerA);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitializeA()
{
	if(!iHookThis->CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return hCoUninitializeA();
	WriteLog(LOG_HOOKWMI,L"HookCoUninitializeA");

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(hGetA)
	{
		WriteLog(LOG_HOOKWMI,L"HookGetA:: Removing Hook");
		DetourDetach(&(PVOID&)hGetA, HookGetA);
	}

	if(hNextA)
	{
		WriteLog(LOG_HOOKWMI,L"HookNextA:: Removing Hook");
		DetourDetach(&(PVOID&)hNextA, HookNextA);
	}

	if(hCreateInstanceEnumA)
	{
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumA:: Removing Hook");
		DetourDetach(&(PVOID&)hCreateInstanceEnumA, HookCreateInstanceEnumA);
	}

	if(hConnectServerA)
	{
		WriteLog(LOG_HOOKWMI,L"HookConnectServerA:: Removing Hook");
		DetourDetach(&(PVOID&)hConnectServerA, HookConnectServerA);
	}
	DetourTransactionCommit();

	return hCoUninitializeA();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookWMI_ANSI()
{
	if(!CheckHook(iHook::HOOK_WMI|iHook::HOOK_WMIA)) return;
	WriteLog(LOG_HOOKWMI,L"HookWMI:: Hooking");
	iHookThis = this;

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(!hCoCreateInstanceA)
	{
		hCoCreateInstanceA = CoCreateInstance;
		DetourAttach(&(PVOID&)hCoCreateInstanceA, HookCoCreateInstanceA);
	}

	if(!hCoUninitializeA)
	{
		hCoUninitializeA = CoUninitialize;
		DetourAttach(&(PVOID&)hCoUninitializeA, HookCoUninitializeA);
	}
	DetourTransactionCommit();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void iHook::HookWMI_ANSI_Clean()
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(hGetA)
	{
		WriteLog(LOG_HOOKWMI,L"HookGetA:: Removing Hook");
		DetourDetach(&(PVOID&)hGetA, HookGetA);
	}

	if(hNextA)
	{
		WriteLog(LOG_HOOKWMI,L"HookNextA:: Removing Hook");
		DetourDetach(&(PVOID&)hNextA, HookNextA);
	}

	if(hCreateInstanceEnumA)
	{
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumA:: Removing Hook");
		DetourDetach(&(PVOID&)hCreateInstanceEnumA, HookCreateInstanceEnumA);
	}

	if(hConnectServerA)
	{
		WriteLog(LOG_HOOKWMI,L"HookConnectServerA:: Removing Hook");
		DetourDetach(&(PVOID&)hConnectServerA, HookConnectServerA);
	}

	if(hCoCreateInstanceA)
	{
		WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceA:: Removing Hook");
		DetourDetach(&(PVOID&)hCoCreateInstanceA, HookCoCreateInstanceA);
	}

	if(hCoUninitializeA)
	{
		WriteLog(LOG_HOOKWMI,L"HookCoUninitializeA:: Removing Hook");
		DetourDetach(&(PVOID&)hCoUninitializeA, HookCoUninitializeA);
	}
	DetourTransactionCommit();
}