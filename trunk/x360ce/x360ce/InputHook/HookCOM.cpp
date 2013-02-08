/*  x360ce - XBOX360 Controller Emulator
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
#include "Log.h"
#include "Utilities\Misc.h"

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

typedef HRESULT (WINAPI *tCoCreateInstance)(__in     REFCLSID rclsid,
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

static ConnectServer_t hConnectServer = NULL;
static CreateInstanceEnum_t hCreateInstanceEnum = NULL;
static Next_t hNext = NULL;
static Get_t hGet = NULL;

static CoUninitialize_t oCoUninitialize = NULL;
static tCoCreateInstance oCoCreateInstance = NULL;
static ConnectServer_t oConnectServer = NULL;
static CreateInstanceEnum_t oCreateInstanceEnum = NULL;
static Next_t oNext = NULL;
static Get_t oGet = NULL;

bool bGets = false;

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

    if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

    if(bGets)
    {
        PrintLog(LOG_HOOKCOM,"*Gets*");
        bGets = false;
    }

    if(hr != NO_ERROR) return hr;

    //PrintLog(LOG_HOOKCOM, L"wszName %s pVal->vt %d pType %d",wszName,pVal->vt,&pType);
    //if( pVal->vt == VT_BSTR) PrintLog(LOG_HOOKCOM, L"%s",pVal->bstrVal);

    if( pVal->vt == VT_BSTR && pVal->bstrVal != NULL )
    {
        //PrintLog(L"%s"),pVal->bstrVal);
        DWORD dwPid = 0, dwVid = 0;

        OLECHAR* strVid = wcsstr( pVal->bstrVal, L"VID_" );
        if(!strVid || swscanf_s( strVid, L"VID_%4X", &dwVid ) < 1 )
            return hr;

        OLECHAR* strPid = wcsstr( pVal->bstrVal, L"PID_" );
        if(!strPid || swscanf_s( strPid, L"PID_%4X", &dwPid ) < 1 )
            return hr;

        for(WORD i = 0; i < iHookThis->GetHookCount(); i++)
        {
            iHookDevice &padconf = iHookThis->GetPadConfig(i);
            if(padconf.GetHookState() && padconf.GetProductVIDPID() == (DWORD)MAKELONG(dwVid,dwPid))
            {
                OLECHAR* strUSB = wcsstr( pVal->bstrVal, L"USB\\" );
                OLECHAR tempstr[MAX_PATH];

                DWORD dwHookVid = iHookThis->CheckHook(iHook::HOOK_VIDPID) ? LOWORD(iHookThis->GetFakePIDVID()) : LOWORD(padconf.GetProductVIDPID());
                DWORD dwHookPid = iHookThis->CheckHook(iHook::HOOK_VIDPID) ? HIWORD(iHookThis->GetFakePIDVID()) : HIWORD(padconf.GetProductVIDPID());

                if( strUSB && dwHookVid && dwHookPid)
                {
                    PrintLog(LOG_HOOKCOM,"%s","Device string change:");
                    PrintLog(LOG_HOOKCOM,"%ls",pVal->bstrVal);
                    OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');
                    swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );
                    SysReAllocString(&pVal->bstrVal,tempstr);
                    PrintLog(LOG_HOOKCOM,"%ls",pVal->bstrVal);
                    continue;
                }

                OLECHAR* strHID = wcsstr( pVal->bstrVal, L"HID\\" );

                if( strHID && dwHookVid && dwHookPid )
                {
                    PrintLog(LOG_HOOKCOM,"%s","Device string change:");
                    PrintLog(LOG_HOOKCOM,"%ls",pVal->bstrVal);
                    OLECHAR* p = wcsrchr(pVal->bstrVal,L'\\');
                    swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );
                    SysReAllocString(&pVal->bstrVal,tempstr);
                    PrintLog(LOG_HOOKCOM,"%ls",pVal->bstrVal);
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

    if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

    PrintLog(LOG_HOOKCOM,"*Next %u*",uCount);
    if(uCount) bGets = true;

    if(hr != NO_ERROR) return hr;

    IWbemClassObject* pDevices;

    if(apObjects)
    {
        if(*apObjects)
        {
            pDevices = *apObjects;

            if(pDevices->lpVtbl->Get)
            {
                PrintLog(LOG_HOOKCOM,"Hooking Get");
                hGet = pDevices->lpVtbl->Get;
                MH_CreateHook(hGet,HookGet,reinterpret_cast<void**>(&oGet));
                MH_EnableHook(hGet);
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

    if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

    PrintLog(LOG_HOOKCOM,"*CreateInstanceEnum*");

    if(hr != NO_ERROR) return hr;

    IEnumWbemClassObject* pEnumDevices = NULL;

    if(ppEnum)
    {
        if(*ppEnum)
        {
            pEnumDevices = *ppEnum;

            if(pEnumDevices->lpVtbl->Next)
            {
                PrintLog(LOG_HOOKCOM,"Hooking Next");
                hNext = pEnumDevices->lpVtbl->Next;
                MH_CreateHook(hNext,HookNext,reinterpret_cast<void**>(&oNext));
                MH_EnableHook(hNext);
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

    if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;

    PrintLog(LOG_HOOKCOM,"*ConnectServer*");

    if(hr != NO_ERROR) return hr;

    IWbemServices* pIWbemServices = NULL;

    if(ppNamespace)
    {
        if(*ppNamespace)
        {
            pIWbemServices = *ppNamespace;

            if(pIWbemServices->lpVtbl->CreateInstanceEnum)
            {
                PrintLog(LOG_HOOKCOM,"Hooking CreateInstanceEnum");
                hCreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
                MH_CreateHook(hCreateInstanceEnum,HookCreateInstanceEnum,reinterpret_cast<void**>(&oCreateInstanceEnum));
                MH_EnableHook(hCreateInstanceEnum);
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

    //PrintLog(LOG_CORE,Misc::GUIDtoStringA(riid).c_str());

    if(!iHookThis->CheckHook(iHook::HOOK_COM)) return hr;
    PrintLog(LOG_HOOKCOM,"*CoCreateInstance*");

    //PrintLog(LOG_CORE,"%x",hr);

    //if(hr == CO_E_NOTINITIALIZED) CoInitialize(NULL);

    if(hr != NO_ERROR) return hr;

    if(IsEqualGUID(riid,IID_IDirectInput8A) || IsEqualGUID(riid,IID_IDirectInput8W))
    {
        PrintLog(LOG_IHOOK,"COM wants to create DirectInput8 instance");
        MessageBoxA(NULL,"COM wants to create DirectInput8 instance","Error",MB_ICONERROR);
        //iHookThis->HookDICOM(riid,ppv);
    }

    if(IsEqualGUID(riid,IID_IWbemLocator))
    {
        IWbemLocator* pIWbemLocator = NULL;
        pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

        if(pIWbemLocator)
        {
            if(pIWbemLocator->lpVtbl->ConnectServer)
            {
                PrintLog(LOG_HOOKCOM,"Hooking ConnectServer");
                hConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
                MH_CreateHook(hConnectServer,HookConnectServer,reinterpret_cast<void**>(&oConnectServer));
                MH_EnableHook(hConnectServer);
            }
        }
    }
    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitialize()
{
    if(!iHookThis->CheckHook(iHook::HOOK_COM)) return oCoUninitialize();
    PrintLog(LOG_HOOKCOM,"*CoUninitialize*");

    PrintLog(LOG_HOOKCOM,"Removing HookGet Hook");
    MH_DisableHook(hGet);

    PrintLog(LOG_HOOKCOM,"Removing Next Hook");
    MH_DisableHook(hNext);

    PrintLog(LOG_HOOKCOM,"Removing CreateInstanceEnum Hook");
    MH_DisableHook(hCreateInstanceEnum);

    PrintLog(LOG_HOOKCOM,"Removing ConnectServer Hook");
    MH_DisableHook(hConnectServer);

    oCoUninitialize();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookCOM()
{
    PrintLog(LOG_HOOKCOM,"Hooking COM");
    iHookThis = this;

    MH_CreateHook(CoCreateInstance,HookCoCreateInstance,reinterpret_cast<void**>(&oCoCreateInstance));
    MH_CreateHook(CoUninitialize,HookCoUninitialize,reinterpret_cast<void**>(&oCoUninitialize));

    MH_EnableHook(CoCreateInstance);
    MH_EnableHook(CoUninitialize);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
