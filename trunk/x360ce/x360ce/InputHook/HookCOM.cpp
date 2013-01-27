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

static iHook *iHookThis = NULL;

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
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor);

static tCoUninitialize hCoUninitialize = NULL;
static tCoCreateInstance hCoCreateInstance = NULL;
static tConnectServer hConnectServer = NULL;
static tCreateInstanceEnum hCreateInstanceEnum = NULL;
static tNext hNext = NULL;
static tGet hGet = NULL;

static DWORD dwGets = NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGet(
	IWbemClassObject * This,
	/* [std::string][in] */ LPCWSTR wszName,
	/* [in] */ long lFlags,
	/* [unique][in][out] */ VARIANT *pVal,
	/* [unique][in][out] */ CIMTYPE *pType,
	/* [unique][in][out] */ long *plFlavor)
{
	tGet oGet = (tGet) HooksGetTrampolineAddress(hGet);
	HRESULT hr = oGet(This,wszName,lFlags,pVal,pType,plFlavor);

	if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

	if(dwGets) 
	{
		WriteLog(LOG_HOOKCOM,L"*Gets*");
		dwGets = NULL;
	}

	if(hr != NO_ERROR) return hr;

	//WriteLog(LOG_HOOKCOM, L"wszName %s pVal->vt %d pType %d",wszName,pVal->vt,&pType);
	//if( pVal->vt == VT_BSTR) WriteLog(LOG_HOOKCOM, L"%s",pVal->bstrVal);

	if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL )
	{
		//WriteLog(L"%s"),pVal->bstrVal);
		DWORD dwPid = 0, dwVid = 0;

		OLECHAR* strVid = wcsstr( pVal->bstrVal, L"VID_" );
		if(strVid && swscanf_s( strVid, L"VID_%4X", &dwVid ) != 1 )
			return hr;

		OLECHAR* strPid = wcsstr( pVal->bstrVal, L"PID_" );
		if(strPid && swscanf_s( strPid, L"PID_%4X", &dwPid ) != 1 )
			return hr;

		for(WORD i = 0; i < iHookThis->GetHookCount(); i++)
		{
			iHookPadConfig &padconf = iHookThis->GetPadConfig(i);
			if(padconf.GetHookState() && padconf.GetProductVIDPID() == (DWORD)MAKELONG(dwVid,dwPid))
			{
				OLECHAR* strUSB = wcsstr( pVal->bstrVal, L"USB" );
				OLECHAR tempstr[MAX_PATH];

				DWORD dwHookVid = iHookThis->CheckHook(iHook::HOOK_VIDPID) ? LOWORD(iHookThis->GetFakeVIDPID()) : LOWORD(padconf.GetProductVIDPID());
				DWORD dwHookPid = iHookThis->CheckHook(iHook::HOOK_VIDPID) ? HIWORD(iHookThis->GetFakeVIDPID()) : HIWORD(padconf.GetProductVIDPID());

				if( strUSB && dwHookVid && dwHookPid)
				{	
					WriteLog(LOG_HOOKCOM,L"Original DeviceID = %s",pVal->bstrVal);
					OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');
					swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );
					SysReAllocString(&pVal->bstrVal,tempstr);
					WriteLog(LOG_HOOKCOM,L"Fake DeviceID = %s",pVal->bstrVal);
					break;
				}

				OLECHAR* strHID = wcsstr( pVal->bstrVal, L"HID" );

				if( strHID && dwHookVid && dwHookPid )
				{
					WriteLog(LOG_HOOKCOM,L"Original DeviceID = %s",pVal->bstrVal);
					OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');
					swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );
					SysReAllocString(&pVal->bstrVal,tempstr);
					WriteLog(LOG_HOOKCOM,L"Fake DeviceID = %s",pVal->bstrVal);
					break;
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
	tNext oNext = (tNext) HooksGetTrampolineAddress(hNext);
	HRESULT hr = oNext(This,lTimeout,uCount,apObjects,puReturned);

	if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

	WriteLog(LOG_HOOKCOM,L"*Next %u*",uCount);
	dwGets = uCount;

	if(hr != NO_ERROR) return hr;

	IWbemClassObject* pDevices;

	if(apObjects)
	{
		if(*apObjects)
		{
			pDevices = *apObjects;

			if(!hGet && pDevices->lpVtbl->Get)
			{
				hGet = pDevices->lpVtbl->Get;
				if(HooksSafeTransition(hGet,true))
				{
					WriteLog(LOG_HOOKCOM,L"Hooking Get");
					HooksInsertNewRedirection(hGet,HookGet,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hGet,false);
				}
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
	tCreateInstanceEnum oCreateInstanceEnum = (tCreateInstanceEnum) HooksGetTrampolineAddress(hCreateInstanceEnum);
	HRESULT hr = oCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);

	if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

	WriteLog(LOG_HOOKCOM,L"*CreateInstanceEnum*");

	if(hr != NO_ERROR) return hr;

	IEnumWbemClassObject* pEnumDevices = NULL;

	if(ppEnum)
	{
		if(*ppEnum)
		{
			pEnumDevices = *ppEnum;

			if(!hNext && pEnumDevices->lpVtbl->Next)
			{
				hNext = pEnumDevices->lpVtbl->Next;
				if(HooksSafeTransition(hNext,true))
				{
					WriteLog(LOG_HOOKCOM,L"Hooking Next");
					HooksInsertNewRedirection(hNext,HookNext,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hNext,false);
				}
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
	tConnectServer oConnectServer = (tConnectServer) HooksGetTrampolineAddress(hConnectServer);
	HRESULT hr = oConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

	WriteLog(LOG_HOOKCOM,L"*ConnectServer*");

	if(hr != NO_ERROR) return hr;

	IWbemServices* pIWbemServices = NULL;

	if(ppNamespace)
	{
		if(*ppNamespace)
		{
			pIWbemServices = *ppNamespace;

			if(!hCreateInstanceEnum && pIWbemServices->lpVtbl->CreateInstanceEnum)
			{
				hCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
				if(HooksSafeTransition(hCreateInstanceEnum,true))
				{
					WriteLog(LOG_HOOKCOM,L"Hooking CreateInstanceEnum");
					HooksInsertNewRedirection(hCreateInstanceEnum,HookCreateInstanceEnum,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hCreateInstanceEnum,false);
				}
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
	tCoCreateInstance oCoCreateInstance = (tCoCreateInstance) HooksGetTrampolineAddress(hCoCreateInstance);
	HRESULT hr = oCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;
	WriteLog(LOG_HOOKCOM,L"*CoCreateInstance*");

	if(hr != NO_ERROR) return hr;

	IWbemLocator* pIWbemLocator = NULL;

	if(ppv && IsEqualGUID(riid,IID_IWbemLocator))
	{
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

		if(pIWbemLocator)
		{
			if(!hConnectServer && pIWbemLocator->lpVtbl->ConnectServer)
			{
				hConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
				if(HooksSafeTransition(hConnectServer,true))
				{
					WriteLog(LOG_HOOKCOM,L"Hooking ConnectServer");
					HooksInsertNewRedirection(hConnectServer,HookConnectServer,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hConnectServer,false);
				}
			}
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitialize()
{
	tCoUninitialize oCoUninitialize = (tCoUninitialize) HooksGetTrampolineAddress(hCoUninitialize);
	if(!iHookThis->CheckHook(iHook::HOOK_COM)) return oCoUninitialize();
	WriteLog(LOG_HOOKCOM,L"*CoUninitialize*");

	if(hGet)
	{
		WriteLog(LOG_HOOKCOM,L"Removing HookGet Hook");
		if(HooksSafeTransition(hGet,true))
		{
			HooksRemoveRedirection(hGet,false);
			HooksSafeTransition(hGet,false);
			hGet = NULL;
		}
	}

	if(hNext)
	{
		WriteLog(LOG_HOOKCOM,L"Removing Next Hook");
		if(HooksSafeTransition(hNext,true))
		{
			HooksRemoveRedirection(hNext,false);
			HooksSafeTransition(hNext,false);
			hNext = NULL;
		}
	}

	if(hCreateInstanceEnum)
	{
		WriteLog(LOG_HOOKCOM,L"Removing CreateInstanceEnum Hook");
		if(HooksSafeTransition(hCreateInstanceEnum,true))
		{
			HooksRemoveRedirection(hCreateInstanceEnum,false);
			HooksSafeTransition(hCreateInstanceEnum,false);
			hCreateInstanceEnum = NULL;
		}
	}

	if(hConnectServer)
	{
		WriteLog(LOG_HOOKCOM,L"Removing ConnectServer Hook");
		if(HooksSafeTransition(hConnectServer,true))
		{
			HooksRemoveRedirection(hConnectServer,false);
			HooksSafeTransition(hConnectServer,false);
			hConnectServer = NULL;
		}
	}

	return oCoUninitialize();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookCOM()
{
	if(!CheckHook(iHook::HOOK_COM)) return;
	WriteLog(LOG_HOOKCOM,L"Hooking COM");
	iHookThis = this;

	if(!hCoCreateInstance) 
	{
		hCoCreateInstance = CoCreateInstance;
		if(HooksSafeTransition(hCoCreateInstance,true))
		{
			HooksInsertNewRedirection(hCoCreateInstance,HookCoCreateInstance,TEE_HOOK_NRM_JUMP);
			HooksSafeTransition(hCoCreateInstance,false);
		}
	}
	if(!hCoUninitialize) 
	{
		hCoUninitialize = CoUninitialize;
		if(HooksSafeTransition(hCoUninitialize,true))
		{
			HooksInsertNewRedirection(hCoUninitialize,HookCoUninitialize,TEE_HOOK_NRM_JUMP);
			HooksSafeTransition(hCoUninitialize,false);
		}
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
