#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

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
    typedef HRESULT(WINAPI *CoCreateInstance_t)(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext, REFIID riid, LPVOID FAR* ppv);
    typedef HRESULT(WINAPI *CoCreateInstanceEx_t)(REFCLSID Clsid, IUnknown * punkOuter, DWORD dwClsCtx, COSERVERINFO * pServerInfo, DWORD dwCount, MULTI_QI * pResults);
    typedef HRESULT(WINAPI * CoGetClassObject_t)(REFCLSID rclsid, DWORD dwClsContext, LPVOID pvReserved, REFIID riid, LPVOID FAR * ppv);
    typedef HRESULT(STDMETHODCALLTYPE *ConnectServer_t)(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
        const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace);
    typedef HRESULT(STDMETHODCALLTYPE *CreateInstanceEnum_t)(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum);
    typedef HRESULT(STDMETHODCALLTYPE *Next_t)(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned);
    typedef HRESULT(STDMETHODCALLTYPE *Get_t)(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor);

    static ConnectServer_t ConnectServer = nullptr;
    static CreateInstanceEnum_t CreateInstanceEnum = nullptr;
    static Next_t Next = nullptr;
    static Get_t Get = nullptr;

    static CoCreateInstance_t TrueCoCreateInstance = nullptr;
    static CoCreateInstanceEx_t TrueCoCreateInstanceEx = nullptr;
    static CoGetClassObject_t TrueCoGetClassObject = nullptr;
    static CoUninitialize_t TrueCoUninitialize = nullptr;
    static ConnectServer_t TrueConnectServer = nullptr;
    static CreateInstanceEnum_t TrueCreateInstanceEnum = nullptr;
    static Next_t TrueNext = nullptr;
    static Get_t TrueGet = nullptr;

    HRESULT STDMETHODCALLTYPE HookGet(IWbemClassObject * This, LPCWSTR wszName, long lFlags, VARIANT *pVal, CIMTYPE *pType, long *plFlavor)
    {
        HRESULT hr = TrueGet(This, wszName, lFlags, pVal, pType, plFlavor);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;

        PrintLog("IWbemClassObject::Get");

        if (hr != NO_ERROR) return hr;

        //PrintLog( "wszName %ls pVal->vt %d pType %d", wszName, pVal->vt, &pType);
        //if( pVal->vt == VT_BSTR) PrintLog( L"%s",pVal->bstrVal);

        if (pVal->vt == VT_BSTR && pVal->bstrVal != NULL)
        {
            //PrintLog( "  Got device ID '%ls'", pVal->bstrVal);
            DWORD dwPid = 0, dwVid = 0, dummy = 0;

            OLECHAR* strVid = wcsstr(pVal->bstrVal, L"VID_");
            if (!strVid || swscanf_s(strVid, L"VID_%4X", &dwVid) < 1)
            {
                // Fallback VID match for OUYA style device IDs
                strVid = wcsstr(pVal->bstrVal, L"VID&");
                if (!strVid || swscanf_s(strVid, L"VID&%4X%4X", &dummy, &dwVid) < 1)
                    return hr;
            }

            OLECHAR* strPid = wcsstr(pVal->bstrVal, L"PID_");
            if (!strPid || swscanf_s(strPid, L"PID_%4X", &dwPid) < 1)
            {
                // Fallback PID match for OUYA style device IDs
                strPid = wcsstr(pVal->bstrVal, L"PID&");
                if (!strPid || swscanf_s(strPid, L"PID&%4X", &dwPid) < 1)
                    return hr;
            }

            for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
            {
                if (padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid, dwPid))
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

    HRESULT STDMETHODCALLTYPE HookNext(IEnumWbemClassObject * This, long lTimeout, ULONG uCount, IWbemClassObject **apObjects, ULONG *puReturned)
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

    HRESULT STDMETHODCALLTYPE HookCreateInstanceEnum(IWbemServices * This, const BSTR strFilter, long lFlags, IWbemContext *pCtx, IEnumWbemClassObject **ppEnum)
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

    HRESULT STDMETHODCALLTYPE HookConnectServer(IWbemLocator * This, const BSTR strNetworkResource, const BSTR strUser, const BSTR strPassword,
        const BSTR strLocale, long lSecurityFlags, const BSTR strAuthority, IWbemContext *pCtx, IWbemServices **ppNamespace)

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

    HRESULT WINAPI HookCoCreateInstanceEx(REFCLSID Clsid, IUnknown * punkOuter, DWORD dwClsCtx, COSERVERINFO * pServerInfo, DWORD dwCount, MULTI_QI * pResults)
    {
        HRESULT hr = TrueCoCreateInstanceEx(Clsid, punkOuter, dwClsCtx, pServerInfo, dwCount, pResults);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;
        PrintLog("CoCreateInstanceEx");

        s_InputHook->StartTimeoutThread();

        if (!pResults) return hr;

        if (IsEqualCLSID(Clsid, CLSID_DirectInput8))
            PrintLog("COM wants to create DirectInput8 instance");

        if (pResults->pIID && IsEqualIID(*pResults->pIID, IID_IWbemLocator) && pResults->pItf)
        {
            IWbemLocator* pIWbemLocator = NULL;
            pIWbemLocator = reinterpret_cast<IWbemLocator*>(pResults->pItf);

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

    HRESULT WINAPI HookCoCreateInstance(REFCLSID rclsid, LPUNKNOWN pUnkOuter, DWORD dwClsContext, REFIID riid, LPVOID FAR* ppv)
    {
        HRESULT hr = TrueCoCreateInstance(rclsid, pUnkOuter, dwClsContext, riid, ppv);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;
        PrintLog("CoCreateInstance");

        s_InputHook->StartTimeoutThread();

        if (hr != NO_ERROR) return hr;

        if (IsEqualCLSID(rclsid, CLSID_DirectInput8))
            PrintLog("COM wants to create DirectInput8 instance");

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

    HRESULT WINAPI HookCoGetClassObject(REFCLSID rclsid, DWORD dwClsContext, LPVOID pvReserved, REFIID riid, LPVOID FAR * ppv)
    {
        HRESULT hr = TrueCoGetClassObject(rclsid, dwClsContext, pvReserved, riid, ppv);

        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return hr;
        PrintLog("CoGetClassObject");

        s_InputHook->StartTimeoutThread();

        if (hr != NO_ERROR) return hr;

        if (IsEqualCLSID(rclsid, CLSID_DirectInput8))
            PrintLog("COM wants to create DirectInput8 instance");

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

    void WINAPI HookCoUninitialize()
    {
        if (!s_InputHook->GetState(InputHook::HOOK_COM)) return TrueCoUninitialize();
        PrintLog("CoUninitialize");

        s_InputHook->StartTimeoutThread();

        if (TrueGet) MH_QueueDisableHook(Get);
        if (TrueNext) MH_QueueDisableHook(Next);
        if (TrueCreateInstanceEnum) MH_QueueDisableHook(CreateInstanceEnum);
        if (TrueConnectServer) MH_QueueDisableHook(ConnectServer);

        MH_ApplyQueued();

        TrueCoUninitialize();
    }
}

void InputHook::HookCOM()
{
    PrintLog("Hooking COM");
    HookCOM::s_InputHook = this;

    IH_CreateHook(CoCreateInstance, HookCOM::HookCoCreateInstance, &HookCOM::TrueCoCreateInstance);
    IH_CreateHook(CoCreateInstanceEx, HookCOM::HookCoCreateInstanceEx, &HookCOM::TrueCoCreateInstanceEx);
    IH_CreateHook(CoGetClassObject, HookCOM::HookCoGetClassObject, &HookCOM::TrueCoGetClassObject);
    IH_CreateHook(CoUninitialize, HookCOM::HookCoUninitialize, &HookCOM::TrueCoUninitialize);
}

