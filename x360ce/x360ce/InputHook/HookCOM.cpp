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
#include "Logger.h"
#include "Misc.h"

#define CINTERFACE
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

typedef void (WINAPI *CoUninitialize_t)();

typedef HRESULT (WINAPI *CoCreateInstance_t)(__in     REFCLSID rclsid,
        __in_opt LPUNKNOWN pUnkOuter,
        __in     DWORD dwClsContext,
        __in     REFIID riid,
        __deref_out LPVOID FAR* ppv);


typedef HRESULT ( STDMETHODCALLTYPE *ConnectServer_t )(
    IWbemLocator * This,
    /* [in] */ const BSTR strNetworkResource,
    /* [in] */ const BSTR strUser,
    /* [in] */ const BSTR strPassword,
    /* [in] */ const BSTR strLocale,
    /* [in] */ long lSecurityFlags,
    /* [in] */ const BSTR strAuthority,
    /* [in] */ IWbemContext *pCtx,
    /* [out] */ IWbemServices **ppNamespace);

typedef HRESULT ( STDMETHODCALLTYPE *CreateInstanceEnum_t )(
    IWbemServices * This,
    /* [in] */ __RPC__in const BSTR strFilter,
    /* [in] */ long lFlags,
    /* [in] */ __RPC__in_opt IWbemContext *pCtx,
    /* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

typedef HRESULT ( STDMETHODCALLTYPE *Next_t )(
    IEnumWbemClassObject * This,
    /* [in] */ long lTimeout,
    /* [in] */ ULONG uCount,
    /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
    /* [out] */ __RPC__out ULONG *puReturned);

typedef HRESULT ( STDMETHODCALLTYPE *Get_t )(
    IWbemClassObject * This,
    /* [std::string][in] */ LPCWSTR wszName,
    /* [in] */ long lFlags,
    /* [unique][in][out] */ VARIANT *pVal,
    /* [unique][in][out] */ CIMTYPE *pType,
    /* [unique][in][out] */ long *plFlavor);

ConnectServer_t hConnectServer = NULL;
CreateInstanceEnum_t hCreateInstanceEnum = NULL;
Next_t hNext = NULL;
Get_t hGet = NULL;

CoUninitialize_t oCoUninitialize = NULL;
CoCreateInstance_t oCoCreateInstance = NULL;
ConnectServer_t oConnectServer = NULL;
CreateInstanceEnum_t oCreateInstanceEnum = NULL;
Next_t oNext = NULL;
Get_t oGet = NULL;

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
    HRESULT hr = oGet(This,wszName,lFlags,pVal,pType,plFlavor);

    if(!iHookThis->GetState(iHook::HOOK_COM)) return hr;

    PrintLog("*Gets*");

    if(hr != NO_ERROR) return hr;

    //PrintLog( "wszName %ls pVal->vt %d pType %d", wszName, pVal->vt, &pType);
    //if( pVal->vt == VT_BSTR) PrintLog( L"%s",pVal->bstrVal);

    if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL )
    {
        //PrintLog( "  Got device ID '%ls'", pVal->bstrVal);
        DWORD dwPid = 0, dwVid = 0, dummy;

        OLECHAR* strVid = wcsstr( pVal->bstrVal, L"VID_" );
        if(!strVid || swscanf_s( strVid, L"VID_%4X", &dwVid ) < 1 ) {
		// Fallback VID match for OUYA style device IDs
		strVid = wcsstr(pVal->bstrVal, L"VID&");
		if (!strVid || swscanf_s(strVid, L"VID&%4X%4X", &dummy, &dwVid) < 1)
			return hr;
 		}

        OLECHAR* strPid = wcsstr( pVal->bstrVal, L"PID_" );
        if(!strPid || swscanf_s( strPid, L"PID_%4X", &dwPid ) < 1 ) {
		// Fallback PID match for OUYA style device IDs
		strPid = wcsstr(pVal->bstrVal, L"PID&");
		if (!strPid || swscanf_s(strPid, L"PID&%4X", &dwPid) < 1)
			return hr;
 		}

		for(auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
        {
            if(padcfg->GetHookState() && padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid,dwPid))
            {
                const wchar_t* strUSB = wcsstr( pVal->bstrVal, L"USB\\" );
				const wchar_t* strRoot = wcsstr( pVal->bstrVal, L"root\\" );
                OLECHAR tempstr[MAX_PATH];

                DWORD dwHookVid = iHookThis->GetState(iHook::HOOK_PIDVID) ? LOWORD(iHookThis->GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
                DWORD dwHookPid = iHookThis->GetState(iHook::HOOK_PIDVID) ? HIWORD(iHookThis->GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

                if(strUSB || strRoot)
                {
                    PrintLog("%s","Device string change:");
                    PrintLog("%ls",pVal->bstrVal);
                    const wchar_t* p = wcsrchr(pVal->bstrVal,L'\\');
					if(p) swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid, padcfg->GetUserIndex(), p );
					else swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d",dwHookVid,dwHookPid, padcfg->GetUserIndex() );
                    SysReAllocString(&pVal->bstrVal,tempstr);
                    PrintLog("%ls",pVal->bstrVal);
                    continue;
                }

                OLECHAR* strHID = wcsstr( pVal->bstrVal, L"HID\\" );

                if(strHID)
                {
                    PrintLog("%s","Device string change:");
                    PrintLog("%ls",pVal->bstrVal);
                    OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');
                    swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid, padcfg->GetUserIndex(), p );
                    SysReAllocString(&pVal->bstrVal,tempstr);
                    PrintLog("%ls",pVal->bstrVal);
                    continue;
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
    HRESULT hr = oNext(This,lTimeout,uCount,apObjects,puReturned);

    if(!iHookThis->GetState(iHook::HOOK_COM)) return hr;

    PrintLog("*Next %u*",uCount);

    if(hr != NO_ERROR) return hr;

    IWbemClassObject* pDevices;

    if(apObjects)
    {
        if(*apObjects)
        {
            pDevices = *apObjects;

            if(pDevices->lpVtbl->Get)
            {
                hGet = pDevices->lpVtbl->Get;
                IH_CreateHook(hGet, HookGet, &oGet);
                IH_EnableHook(hGet);
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
    HRESULT hr = oCreateInstanceEnum(This,strFilter,lFlags,pCtx,ppEnum);

    if(!iHookThis->GetState(iHook::HOOK_COM)) return hr;

    PrintLog("*CreateInstanceEnum*");

    if(hr != NO_ERROR) return hr;

    IEnumWbemClassObject* pEnumDevices = NULL;

    if(ppEnum)
    {
        if(*ppEnum)
        {
            pEnumDevices = *ppEnum;

            if(pEnumDevices->lpVtbl->Next)
            {
                hNext = pEnumDevices->lpVtbl->Next;
                IH_CreateHook(hNext, HookNext, &oNext);
                IH_EnableHook(hNext);
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
    HRESULT hr = oConnectServer(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

    if(!iHookThis->GetState(iHook::HOOK_COM)) return hr;

    PrintLog("*ConnectServer*");

    if(hr != NO_ERROR) return hr;

    IWbemServices* pIWbemServices = NULL;

    if(ppNamespace)
    {
        if(*ppNamespace)
        {
            pIWbemServices = *ppNamespace;

            if(pIWbemServices->lpVtbl->CreateInstanceEnum)
            {
                hCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
                IH_CreateHook(hCreateInstanceEnum, HookCreateInstanceEnum, &oCreateInstanceEnum);
                IH_EnableHook(hCreateInstanceEnum);
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
    HRESULT hr = oCoCreateInstance(rclsid,pUnkOuter,dwClsContext,riid,ppv);

    //PrintLog(GUIDtoStringA(riid).c_str());

    if(!iHookThis->GetState(iHook::HOOK_COM)) return hr;
    PrintLog("*CoCreateInstance*");

    //PrintLog("%x",hr);

    //if(hr == CO_E_NOTINITIALIZED) CoInitialize(NULL);

    if(hr != NO_ERROR) return hr;

    if(IsEqualCLSID(rclsid,CLSID_DirectInput8))
    {
        PrintLog("COM wants to create DirectInput8 instance");
        //MessageBoxA(NULL,"COM wants to create DirectInput8 instance","x360ce - Error",MB_ICONWARNING);
        //iHookThis->HookDICOM(riid,ppv);
    }

    if(IsEqualIID(riid,IID_IWbemLocator))
    {
        IWbemLocator* pIWbemLocator = NULL;
        pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

        if(pIWbemLocator)
        {
            if(pIWbemLocator->lpVtbl->ConnectServer)
            {
                hConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
                IH_CreateHook(hConnectServer, HookConnectServer, &oConnectServer);
                IH_EnableHook(hConnectServer);
            }
        }
    }
    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitialize()
{
    if(!iHookThis->GetState(iHook::HOOK_COM)) return oCoUninitialize();
    PrintLog("*CoUninitialize*");

	MH_QueueDisableHook(hGet);
	MH_QueueDisableHook(hNext);
	MH_QueueDisableHook(hCreateInstanceEnum);
	MH_QueueDisableHook(hConnectServer);

	MH_ApplyQueued();

    oCoUninitialize();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookCOM()
{
    PrintLog("Hooking COM");
    iHookThis = this;

    IH_CreateHook(CoCreateInstance, HookCoCreateInstance, &oCoCreateInstance);
    IH_CreateHook(CoUninitialize, HookCoUninitialize, &oCoUninitialize);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
