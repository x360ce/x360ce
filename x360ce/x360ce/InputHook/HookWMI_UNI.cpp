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

tCoUninitializeW hCoUninitializeW = NULL;
tCoCreateInstanceW hCoCreateInstanceW = NULL;

tConnectServerW hConnectServerW = NULL;
tCreateInstanceEnumW hCreateInstanceEnumW = NULL;
tNextW hNextW = NULL;
tGetW hGetW = NULL;

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
	tGetW oGetW = (tGetW) HooksGetTrampolineAddress(hGetW);
	HRESULT hr = oGetW(This,wszName,lFlags,pVal,pType,plFlavor);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookGetW");

	if(FAILED(hr)) return hr;

	//WriteLog(LOG_HOOKWMI, L"wszName %s pVal->vt %d pType %d",wszName,pVal->vt,&pType);
	//if( pVal->vt == VT_BSTR) WriteLog(LOG_HOOKWMI, L"%s",pVal->bstrVal);

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

				if( strUSB )
				{
					
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
						static VARIANT v;
						OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');

						swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );
						BSTR Hookbstr = SysAllocString(tempstr);
						SysFreeString(pVal->bstrVal);

						v = *pVal;
						v.bstrVal = Hookbstr;
						pVal=&v;
						WriteLog(LOG_HOOKWMI,L"Fake DeviceID = %s",pVal->bstrVal);
					}
					break;
				}

				OLECHAR* strHID = wcsstr( pVal->bstrVal, L"HID" );

				if( strHID )
				{
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
						static VARIANT v;
						OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');

						swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid,i, p);
						BSTR Hookbstr = SysAllocString(tempstr);
						SysFreeString(pVal->bstrVal);

						v = *pVal;
						v.bstrVal = Hookbstr;
						pVal=&v;
						WriteLog(LOG_HOOKWMI,L"Fake DeviceID = %s",pVal->bstrVal);
					}
					break;
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
	tNextW oNextW = (tNextW) HooksGetTrampolineAddress(hNextW);
	HRESULT hr = oNextW(This,lTimeout,uCount,apObjects,puReturned);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookNextW");

	if(FAILED(hr)) return hr;
	if(hr != WBEM_S_NO_ERROR) return hr;

	IWbemClassObject* pDevices;

	if(apObjects)
	{
		if(*apObjects)
		{
			pDevices = *apObjects;

			if(!hGetW && pDevices->lpVtbl->Get)
			{
				hGetW = pDevices->lpVtbl->Get;
				if(HooksSafeTransition(hGetW,true))
				{
					WriteLog(LOG_HOOKWMI,L"HookGetW:: Hooking");
					HooksInsertNewRedirection(hGetW,HookGetW,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hGetW,false);
				}
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
	tCreateInstanceEnumW oCreateInstanceEnumW = (tCreateInstanceEnumW) HooksGetTrampolineAddress(hCreateInstanceEnumW);
	HRESULT hr = oCreateInstanceEnumW(This,strFilter,lFlags,pCtx,ppEnum);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW");

	if(FAILED(hr)) return hr;

	IEnumWbemClassObject* pEnumDevices = NULL;

	if(ppEnum)
	{
		if(*ppEnum)
		{
			pEnumDevices = *ppEnum;

			if(!hNextW && pEnumDevices->lpVtbl->Next)
			{
				hNextW = pEnumDevices->lpVtbl->Next;
				if(HooksSafeTransition(hNextW,true))
				{
					WriteLog(LOG_HOOKWMI,L"HookNextW:: Hooking");
					HooksInsertNewRedirection(hNextW,HookNextW,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hNextW,false);
				}
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
	tConnectServerW oConnectServerW = (tConnectServerW) HooksGetTrampolineAddress(hConnectServerW);
	HRESULT hr = oConnectServerW(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI)) return hr;

	WriteLog(LOG_HOOKWMI,L"HookConnectServerW");

	if(FAILED(hr)) return hr;

	IWbemServices* pIWbemServices = NULL;

	if(ppNamespace)
	{
		if(*ppNamespace)
		{
			pIWbemServices = *ppNamespace;

			if(!hCreateInstanceEnumW && pIWbemServices->lpVtbl->CreateInstanceEnum)
			{
				hCreateInstanceEnumW = pIWbemServices->lpVtbl->CreateInstanceEnum;
				if(HooksSafeTransition(hCreateInstanceEnumW,true))
				{
					WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW:: Hooking");
					HooksInsertNewRedirection(hCreateInstanceEnumW,HookCreateInstanceEnumW,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hCreateInstanceEnumW,false);
				}
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
	tCoCreateInstanceW oCoCreateInstanceW = (tCoCreateInstanceW) HooksGetTrampolineAddress(hCoCreateInstanceW);
	HRESULT hr = oCoCreateInstanceW(rclsid,pUnkOuter,dwClsContext,riid,ppv);

	if(!iHookThis->CheckHook(iHook::HOOK_WMI)) return hr;
	WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceW");

	if(FAILED(hr)) return hr;

	IWbemLocator* pIWbemLocator = NULL;

	if(ppv && IsEqualGUID(riid,IID_IWbemLocator))
	{
		pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

		if(pIWbemLocator)
		{
			if(!hConnectServerW && pIWbemLocator->lpVtbl->ConnectServer)
			{
				hConnectServerW = pIWbemLocator->lpVtbl->ConnectServer;
				if(HooksSafeTransition(hConnectServerW,true))
				{
					WriteLog(LOG_HOOKWMI,L"HookConnectServerW:: Hooking");
					HooksInsertNewRedirection(hConnectServerW,HookConnectServerW,TEE_HOOK_NRM_JUMP);
					HooksSafeTransition(hConnectServerW,false);
				}
			}
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitializeW()
{
	tCoUninitializeW oCoUninitializeW = (tCoUninitializeW) HooksGetTrampolineAddress(hCoUninitializeW);
	if(!iHookThis->CheckHook(iHook::HOOK_WMI)) return oCoUninitializeW();
	WriteLog(LOG_HOOKWMI,L"HookCoUninitializeW");

	if(hGetW)
	{
		WriteLog(LOG_HOOKWMI,L"HookGetW:: Removing Hook");
		if(HooksSafeTransition(hGetW,true))
		{
			HooksRemoveRedirection(hGetW,false);
			HooksSafeTransition(hGetW,false);
			hGetW = NULL;
		}
	}

	if(hNextW)
	{
		WriteLog(LOG_HOOKWMI,L"HookNextW:: Removing Hook");
		if(HooksSafeTransition(hNextW,true))
		{
			HooksRemoveRedirection(hNextW,false);
			HooksSafeTransition(hNextW,false);
			hNextW = NULL;
		}
	}

	if(hCreateInstanceEnumW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW:: Removing Hook");
		if(HooksSafeTransition(hCreateInstanceEnumW,true))
		{
			HooksRemoveRedirection(hCreateInstanceEnumW,false);
			HooksSafeTransition(hCreateInstanceEnumW,false);
			hCreateInstanceEnumW = NULL;
		}
	}

	if(hConnectServerW)
	{
		WriteLog(LOG_HOOKWMI,L"HookConnectServerW:: Removing Hook");
		if(HooksSafeTransition(hConnectServerW,true))
		{
			HooksRemoveRedirection(hConnectServerW,false);
			HooksSafeTransition(hConnectServerW,false);
			hConnectServerW = NULL;
		}
	}

	return oCoUninitializeW();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookWMI_UNI()
{
	if(!CheckHook(iHook::HOOK_WMI)) return;
	WriteLog(LOG_HOOKWMI,L"HookWMI:: Hooking");
	iHookThis = this;

	if(!hCoCreateInstanceW) 
	{
		hCoCreateInstanceW = CoCreateInstance;
		if(HooksSafeTransition(hCoCreateInstanceW,true))
		{
			HooksInsertNewRedirection(hCoCreateInstanceW,HookCoCreateInstanceW,TEE_HOOK_NRM_JUMP);
			HooksSafeTransition(hCoCreateInstanceW,false);
		}
	}
	if(!hCoUninitializeW) 
	{
		hCoUninitializeW = CoUninitialize;
		if(HooksSafeTransition(hCoUninitializeW,true))
		{
			HooksInsertNewRedirection(hCoUninitializeW,HookCoUninitializeW,TEE_HOOK_NRM_JUMP);
			HooksSafeTransition(hCoUninitializeW,false);
		}
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void iHook::HookWMI_UNI_Clean()
{
	WriteLog(LOG_HOOKWMI,L"HookWMIU Clean");

	if(hGetW)
	{
		WriteLog(LOG_HOOKWMI,L"HookGetW:: Removing Hook");
		if(HooksSafeTransition(hGetW,true))
		{
			HooksRemoveRedirection(hGetW,true);
			HooksSafeTransition(hGetW,false);
			hGetW = NULL;
		}
	}

	if(hNextW)
	{
		WriteLog(LOG_HOOKWMI,L"HookNextW:: Removing Hook");
		if(HooksSafeTransition(hNextW,true))
		{
			HooksRemoveRedirection(hNextW,true);
			HooksSafeTransition(hNextW,false);
			hNextW = NULL;
		}
	}

	if(hCreateInstanceEnumW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumW:: Removing Hook");
		if(HooksSafeTransition(hCreateInstanceEnumW,true))
		{
			HooksRemoveRedirection(hCreateInstanceEnumW,true);
			HooksSafeTransition(hCreateInstanceEnumW,false);
			hCreateInstanceEnumW = NULL;
		}
	}

	if(hConnectServerW)
	{
		WriteLog(LOG_HOOKWMI,L"HookConnectServerW:: Removing Hook");
		if(HooksSafeTransition(hConnectServerW,true))
		{
			HooksRemoveRedirection(hConnectServerW,true);
			HooksSafeTransition(hConnectServerW,false);
			hConnectServerW = NULL;
		}
	}


	if(hCoCreateInstanceW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceW:: Removing Hook");
		if(HooksSafeTransition(hCoCreateInstanceW,true))
		{
			HooksRemoveRedirection(hCoCreateInstanceW,true);
			HooksSafeTransition(hCoCreateInstanceW,false);
			hCoCreateInstanceW = NULL;
		}
	}

	if(hCoUninitializeW)
	{
		WriteLog(LOG_HOOKWMI,L"HookCoUninitializeW:: Removing Hook");
		if(HooksSafeTransition(hCoUninitializeW,true))
		{
			HooksRemoveRedirection(hCoUninitializeW,true);
			HooksSafeTransition(hCoUninitializeW,false);
			hCoUninitializeW = NULL;
		}
	}
}