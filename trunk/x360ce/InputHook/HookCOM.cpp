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

// COM CLSIDs
#pragma comment(lib,"wbemuuid.lib")

#include <dinput.h>

#include "InputHook.h"

namespace HookCOM
{
    static InputHook *s_InputHook = nullptr;

    typedef void (WINAPI *CoUninitialize_t)();

    typedef HRESULT(WINAPI *CoCreateInstance_t)(__in     REFCLSID rclsid,
        __in_opt LPUNKNOWN pUnkOuter,
        __in     DWORD dwClsContext,
        __in     REFIID riid,
        __deref_out LPVOID FAR* ppv);

    typedef HRESULT(STDMETHODCALLTYPE *ConnectServer_t)(
        IWbemLocator * This,
        /* [in] */ const BSTR strNetworkResource,
        /* [in] */ const BSTR strUser,
        /* [in] */ const BSTR strPassword,
        /* [in] */ const BSTR strLocale,
        /* [in] */ long lSecurityFlags,
        /* [in] */ const BSTR strAuthority,
        /* [in] */ IWbemContext *pCtx,
        /* [out] */ IWbemServices **ppNamespace);

    typedef HRESULT(STDMETHODCALLTYPE *CreateInstanceEnum_t)(
        IWbemServices * This,
        /* [in] */ __RPC__in const BSTR strFilter,
        /* [in] */ long lFlags,
        /* [in] */ __RPC__in_opt IWbemContext *pCtx,
        /* [out] */ __RPC__deref_out_opt IEnumWbemClassObject **ppEnum);

    typedef HRESULT(STDMETHODCALLTYPE *Next_t)(
        IEnumWbemClassObject * This,
        /* [in] */ long lTimeout,
        /* [in] */ ULONG uCount,
        /* [length_is][size_is][out] */ __RPC__out_ecount_part(uCount, *puReturned) IWbemClassObject **apObjects,
        /* [out] */ __RPC__out ULONG *puReturned);

    typedef HRESULT(STDMETHODCALLTYPE *Get_t)(
        IWbemClassObject * This,
        /* [std::string][in] */ LPCWSTR wszName,
        /* [in] */ long lFlags,
        /* [unique][in][out] */ VARIANT *pVal,
        /* [unique][in][out] */ CIMTYPE *pType,
        /* [unique][in][out] */ long *plFlavor);

    static ConnectServer_t ConnectServer = nullptr;
    static CreateInstanceEnum_t CreateInstanceEnum = nullptr;
    static Next_t Next = nullptr;
    static Get_t Get = nullptr;

    static CoCreateInstance_t TrueCoCreateInstance = nullptr;
    static CoUninitialize_t TrueCoUninitialize = nullptr;
    static ConnectServer_t TrueConnectServer = nullptr;
    static CreateInstanceEnum_t TrueCreateInstanceEnum = nullptr;
    static Next_t TrueNext = nullptr;
    static Get_t TrueGet = nullptr;

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
        HRESULT hr = TrueGet(This, wszName, lFlags, pVal, pType, plFlavor);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;

        PrintLog("IWbemClassObject::Get");

        if (hr != NO_ERROR) return hr;

        //PrintLog( "wszName %ls pVal->vt %d pType %d", wszName, pVal->vt, &pType);
        //if( pVal->vt == VT_BSTR) PrintLog( L"%s",pVal->bstrVal);

        if (pVal->vt == VT_BSTR && pVal->bstrVal != NULL)
        {
            if (wcsstr(pVal->bstrVal, L"IG_") && !s_InputHook->GetState(InputHook::HOOK_PIDVID))
            {
                PrintLog("Xinput device skipped");
                return hr;
            }

            //PrintLog( "  Got device ID '%ls'", pVal->bstrVal);
            DWORD dwPid = 0, dwVid = 0, dummy;

            OLECHAR* strVid = wcsstr(pVal->bstrVal, L"VID_");
            if (!strVid || swscanf_s(strVid, L"VID_%4X", &dwVid) < 1) {
                // Fallback VID match for OUYA style device IDs
                strVid = wcsstr(pVal->bstrVal, L"VID&");
                if (!strVid || swscanf_s(strVid, L"VID&%4X%4X", &dummy, &dwVid) < 1)
                    return hr;
            }

            OLECHAR* strPid = wcsstr(pVal->bstrVal, L"PID_");
            if (!strPid || swscanf_s(strPid, L"PID_%4X", &dwPid) < 1) {
                // Fallback PID match for OUYA style device IDs
                strPid = wcsstr(pVal->bstrVal, L"PID&");
                if (!strPid || swscanf_s(strPid, L"PID&%4X", &dwPid) < 1)
                    return hr;
            }

            for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
            {
                if (padcfg->GetHookState() && padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid, dwPid))
                {
                    const wchar_t* strUSB = wcsstr(pVal->bstrVal, L"USB\\");
                    const wchar_t* strRoot = wcsstr(pVal->bstrVal, L"root\\");
                    OLECHAR tempstr[MAX_PATH];

                    DWORD dwHookVid = s_InputHook->GetState(InputHook::HOOK_PIDVID) ? LOWORD(s_InputHook->GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
                    DWORD dwHookPid = s_InputHook->GetState(InputHook::HOOK_PIDVID) ? HIWORD(s_InputHook->GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

                    if (strUSB || strRoot)
                    {
                        PrintLog("%s", "Device string change:");
                        PrintLog("%ls", pVal->bstrVal);
                        const wchar_t* p = wcsrchr(pVal->bstrVal, L'\\');
                        if (p) swprintf_s(tempstr, L"USB\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);
                        else swprintf_s(tempstr, L"USB\\VID_%04X&PID_%04X&IG_%02d", dwHookVid, dwHookPid, padcfg->GetUserIndex());
                        SysReAllocString(&pVal->bstrVal, tempstr);
                        PrintLog("%ls", pVal->bstrVal);
                        continue;
                    }

                    OLECHAR* strHID = wcsstr(pVal->bstrVal, L"HID\\");

                    if (strHID)
                    {
                        PrintLog("%s", "Device string change:");
                        PrintLog("%ls", pVal->bstrVal);
                        OLECHAR* p = wcsrchr(pVal->bstrVal, L'\\');
                        swprintf_s(tempstr, L"HID\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);
                        SysReAllocString(&pVal->bstrVal, tempstr);
                        PrintLog("%ls", pVal->bstrVal);
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
        HRESULT hr = TrueNext(This, lTimeout, uCount, apObjects, puReturned);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;

        PrintLog("IEnumWbemClassObject::Next %u", uCount);

        if (hr != NO_ERROR) return hr;

        IWbemClassObject* pDevices;

        if (apObjects)
        {
            if (*apObjects)
            {
                pDevices = *apObjects;

                if (pDevices->lpVtbl->Get)
                {
                    Get = pDevices->lpVtbl->Get;
                    IH_CreateHook(Get, HookGet, &TrueGet);
                    IH_EnableHook(Get);
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
        HRESULT hr = TrueCreateInstanceEnum(This, strFilter, lFlags, pCtx, ppEnum);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;

        PrintLog("IWbemServices::CreateInstanceEnum");

        if (hr != NO_ERROR) return hr;

        IEnumWbemClassObject* pEnumDevices = NULL;

        if (ppEnum)
        {
            if (*ppEnum)
            {
                pEnumDevices = *ppEnum;

                if (pEnumDevices->lpVtbl->Next)
                {
                    Next = pEnumDevices->lpVtbl->Next;
                    IH_CreateHook(Next, HookNext, &TrueNext);
                    IH_EnableHook(Next);
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
        HRESULT hr = TrueConnectServer(This, strNetworkResource, strUser, strPassword, strLocale, lSecurityFlags, strAuthority, pCtx, ppNamespace);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;

        PrintLog("IWbemLocator::ConnectServer");

        if (hr != NO_ERROR) return hr;

        IWbemServices* pIWbemServices = NULL;

        if (ppNamespace)
        {
            if (*ppNamespace)
            {
                pIWbemServices = *ppNamespace;

                if (pIWbemServices->lpVtbl->CreateInstanceEnum)
                {
                    CreateInstanceEnum = pIWbemServices->lpVtbl->CreateInstanceEnum;
                    IH_CreateHook(CreateInstanceEnum, HookCreateInstanceEnum, &TrueCreateInstanceEnum);
                    IH_EnableHook(CreateInstanceEnum);
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
        HRESULT hr = TrueCoCreateInstance(rclsid, pUnkOuter, dwClsContext, riid, ppv);

        //PrintLog(GUIDtoStringA(riid).c_str());

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;
        PrintLog("CoCreateInstance");

        //PrintLog("%x",hr);

        //if(hr == CO_E_NOTINITIALIZED) CoInitialize(NULL);

        if (hr != NO_ERROR) return hr;

        if (IsEqualCLSID(rclsid, CLSID_DirectInput8))
        {
            PrintLog("COM wants to create DirectInput8 instance");
            //MessageBoxA(NULL,"COM wants to create DirectInput8 instance","x360ce - Error",MB_ICONWARNING);
            //s_InputHook->HookDICOM(riid,ppv);
        }

        if (IsEqualIID(riid, IID_IWbemLocator))
        {
            IWbemLocator* pIWbemLocator = NULL;
            pIWbemLocator = static_cast<IWbemLocator*>(*ppv);

            if (pIWbemLocator)
            {
                if (pIWbemLocator->lpVtbl->ConnectServer)
                {
                    ConnectServer = pIWbemLocator->lpVtbl->ConnectServer;
                    IH_CreateHook(ConnectServer, HookConnectServer, &TrueConnectServer);
                    IH_EnableHook(ConnectServer);
                }
            }
        }
        return hr;
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void WINAPI HookCoUninitialize()
    {
        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return TrueCoUninitialize();
        PrintLog("CoUninitialize");

        if (TrueGet) MH_QueueDisableHook(Get);
        if (TrueNext) MH_QueueDisableHook(Next);
        if (TrueCreateInstanceEnum) MH_QueueDisableHook(CreateInstanceEnum);
        if (TrueConnectServer) MH_QueueDisableHook(ConnectServer);

        MH_ApplyQueued();

        TrueCoUninitialize();
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void InputHook::HookCOM()
{
    PrintLog("Hooking COM");
    HookCOM::s_InputHook = this;

    IH_CreateHook(CoCreateInstance, HookCOM::HookCoCreateInstance, &HookCOM::TrueCoCreateInstance);
    IH_CreateHook(CoUninitialize, HookCOM::HookCoUninitialize, &HookCOM::TrueCoUninitialize);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
