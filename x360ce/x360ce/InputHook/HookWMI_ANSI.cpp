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
#include "externals.h"
#include "InputHook.h"
#include "Utilities\Log.h"

#define OLE2ANSI
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
    /* [string][in] */ LPCWSTR wszName,
    /* [in] */ long lFlags,
    /* [unique][in][out] */ VARIANT *pVal,
    /* [unique][in][out] */ CIMTYPE *pType,
    /* [unique][in][out] */ long *plFlavor);

MologieDetours::Detour<tCoUninitializeA>* hCoUninitializeA = NULL;
MologieDetours::Detour<tCoCreateInstanceA>* hCoCreateInstanceA = NULL;
MologieDetours::Detour<tConnectServerA>* hConnectServerA = NULL;
MologieDetours::Detour<tCreateInstanceEnumA>* hCreateInstanceEnumA = NULL;
MologieDetours::Detour<tNextA>* hNextA = NULL;
MologieDetours::Detour<tGetA>* hGetA = NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetA(
    IWbemClassObject * This,
    /* [string][in] */ LPCWSTR wszName,
    /* [in] */ long lFlags,
    /* [unique][in][out] */ VARIANT *pVal,
    /* [unique][in][out] */ CIMTYPE *pType,
    /* [unique][in][out] */ long *plFlavor)
{
    HRESULT hr = hGetA->GetOriginalFunction()(This,wszName,lFlags,pVal,pType,plFlavor);

    if(!InputHookConfig.bEnabled) return hr;

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
            if(GamepadConfig[i].bEnabled && GamepadConfig[i].dwVID == dwVid && GamepadConfig[i].dwPID == dwPid)
            {
                char* strUSB = strstr( pVal->bstrVal, "USB" );
                char tempstr[MAX_PATH];

                if( strUSB )
                {
                    BSTR Hookbstr=NULL;
                    WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);

                    if(InputHookConfig.dwHookMode >= HOOK_COMPAT) sprintf_s(tempstr,"USB\\VID_%04X&PID_%04X&IG_%02d", InputHookConfig.dwHookVID, InputHookConfig.dwHookPID,i );
                    else sprintf_s(tempstr,"USB\\VID_%04X&PID_%04X&IG_%02d", GamepadConfig[i].dwVID , GamepadConfig[i].dwPID,i );

                    Hookbstr=SysAllocString(tempstr);
                    pVal->bstrVal = Hookbstr;
                    WriteLog(LOG_HOOKWMI,L"Hook DeviceID = %s",pVal->bstrVal);
                    return hr;
                }

                char* strHID = strstr( pVal->bstrVal, "HID" );

                if( strHID )
                {
                    BSTR Hookbstr=NULL;
                    WriteLog(LOG_HOOKWMI,L"Original DeviceID = %s",pVal->bstrVal);

                    if(InputHookConfig.dwHookMode >= HOOK_COMPAT) sprintf_s(tempstr,"HID\\VID_%04X&PID_%04X&IG_%02d", InputHookConfig.dwHookVID, InputHookConfig.dwHookPID,i );
                    else sprintf_s(tempstr,"HID\\VID_%04X&PID_%04X&IG_%02d", GamepadConfig[i].dwVID , GamepadConfig[i].dwPID,i );

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
HRESULT STDMETHODCALLTYPE HookNextA(
    IEnumWbemClassObject * This,
    /* [in] */ long lTimeout,
    /* [in] */ ULONG uCount,
    /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
    /* [out] */ __RPC__out ULONG *puReturned)
{
    HRESULT hr = hNextA->GetOriginalFunction()(This,lTimeout,uCount,apObjects,puReturned);

    if(!InputHookConfig.bEnabled) return hr;

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
                hGetA = new MologieDetours::Detour<tGetA>(pDevices->lpVtbl->Get, HookGetA);
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
    HRESULT hr = hCreateInstanceEnumA->GetOriginalFunction()(This,strFilter,lFlags,pCtx,ppEnum);

    if(!InputHookConfig.bEnabled) return hr;

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
                hNextA = new MologieDetours::Detour<tNextA>(pEnumDevices->lpVtbl->Next, HookNextA);
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
    HRESULT hr = hConnectServerA->GetOriginalFunction()(This,strNetworkResource,strUser,strPassword,strLocale,lSecurityFlags,strAuthority,pCtx,ppNamespace);

    if(!InputHookConfig.bEnabled) return hr;

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
                hCreateInstanceEnumA = new MologieDetours::Detour<tCreateInstanceEnumA>(pIWbemServices->lpVtbl->CreateInstanceEnum, HookCreateInstanceEnumA);
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
    HRESULT hr = hCoCreateInstanceA->GetOriginalFunction()(rclsid,pUnkOuter,dwClsContext,riid,ppv);

    if(!InputHookConfig.bEnabled) return hr;

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
                hConnectServerA = new MologieDetours::Detour<tConnectServerA>(pIWbemLocator->lpVtbl->ConnectServer, HookConnectServerA);
            }
        }
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void WINAPI HookCoUninitializeA()
{
    if(!InputHookConfig.bEnabled) return hCoUninitializeA->GetOriginalFunction()();

    WriteLog(LOG_HOOKWMI,L"HookCoUninitializeA");

    if(hGetA)
    {
        WriteLog(LOG_HOOKWMI,L"HookGetA:: Removing Hook");
        SAFE_DELETE(hGetA);
    }

    if(hNextA)
    {
        WriteLog(LOG_HOOKWMI,L"HookNextA:: Removing Hook");
        SAFE_DELETE(hNextA);
    }

    if(hCreateInstanceEnumA)
    {
        WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumA:: Removing Hook");
        SAFE_DELETE(hCreateInstanceEnumA);;
    }

    if(hConnectServerA)
    {
        WriteLog(LOG_HOOKWMI,L"HookConnectServerA:: Removing Hook");
        SAFE_DELETE(hConnectServerA);
    }

    return hCoUninitializeA->GetOriginalFunction()();
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookWMI_ANSI()
{
    if(!hCoCreateInstanceA)
    {
        hCoCreateInstanceA = new MologieDetours::Detour<tCoCreateInstanceA>(CoCreateInstance, HookCoCreateInstanceA);
    }

    if(!hCoUninitializeA)
    {
        hCoUninitializeA = new MologieDetours::Detour<tCoUninitializeA>(CoUninitialize, HookCoUninitializeA);
    }
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void HookWMI_ANSI_Clean()
{
    if(hGetA)
    {
        WriteLog(LOG_HOOKWMI,L"HookGetA:: Removing Hook");
        SAFE_DELETE(hGetA);
    }

    if(hNextA)
    {
        WriteLog(LOG_HOOKWMI,L"HookNextA:: Removing Hook");
        SAFE_DELETE(hNextA);
    }

    if(hCreateInstanceEnumA)
    {
        WriteLog(LOG_HOOKWMI,L"HookCreateInstanceEnumA:: Removing Hook");
        SAFE_DELETE(hCreateInstanceEnumA);;
    }

    if(hConnectServerA)
    {
        WriteLog(LOG_HOOKWMI,L"HookConnectServerA:: Removing Hook");
        SAFE_DELETE(hConnectServerA);
    }

    if(hCoCreateInstanceA)
    {
        WriteLog(LOG_HOOKWMI,L"HookCoCreateInstanceA:: Removing Hook");
        SAFE_DELETE(hCoCreateInstanceA);
    }

    if(hCoUninitializeA)
    {
        WriteLog(LOG_HOOKWMI,L"HookCoUninitializeA:: Removing Hook");
        SAFE_DELETE(hCoUninitializeA);
    }
}