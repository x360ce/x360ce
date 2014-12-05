#include "stdafx.h"
#define CINTERFACE

#include <dinput.h>
#include "Common.h"

#include "InputHook.h"

#include "Logger.h"
#include "Utils.h"

namespace HookDI
{
    static InputHook* s_InputHook;

    typedef HRESULT(WINAPI *DirectInput8Create_t)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
    typedef HRESULT(STDMETHODCALLTYPE *CreateDeviceA_t) (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A *lplpDirectInputDeviceA, LPUNKNOWN pUnkOuter);
    typedef HRESULT(STDMETHODCALLTYPE *CreateDeviceW_t) (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W *lplpDirectInputDeviceW, LPUNKNOWN pUnkOuter);
    typedef HRESULT(STDMETHODCALLTYPE *GetPropertyA_t) (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
    typedef HRESULT(STDMETHODCALLTYPE *GetPropertyW_t) (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
    typedef HRESULT(STDMETHODCALLTYPE *GetDeviceInfoA_t) (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi);
    typedef HRESULT(STDMETHODCALLTYPE *GetDeviceInfoW_t) (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi);
    typedef HRESULT(STDMETHODCALLTYPE *EnumDevicesA_t) (LPDIRECTINPUT8A This, DWORD dwDevType, LPDIENUMDEVICESCALLBACKA lpCallback, LPVOID pvRef, DWORD dwFlags);
    typedef HRESULT(STDMETHODCALLTYPE *EnumDevicesW_t) (LPDIRECTINPUT8W This, DWORD dwDevType, LPDIENUMDEVICESCALLBACKW lpCallback, LPVOID pvRef, DWORD dwFlags);

    typedef  HRESULT(STDMETHODCALLTYPE *SetCooperativeLevelA_t)(LPDIRECTINPUTDEVICE8A This, HWND hWnd, DWORD dwFlags);
    typedef  HRESULT(STDMETHODCALLTYPE *SetCooperativeLevelW_t)(LPDIRECTINPUTDEVICE8W This, HWND hWnd, DWORD dwFlags);


    static CreateDeviceA_t CreateDeviceA = nullptr;
    static CreateDeviceW_t CreateDeviceW = nullptr;
    static GetPropertyA_t GetPropertyA = nullptr;
    static GetPropertyW_t GetPropertyW = nullptr;
    static GetDeviceInfoA_t GetDeviceInfoA = nullptr;
    static GetDeviceInfoW_t GetDeviceInfoW = nullptr;
    static EnumDevicesA_t EnumDevicesA = nullptr;
    static EnumDevicesW_t EnumDevicesW = nullptr;
    static SetCooperativeLevelA_t SetCooperativeLevelA;
    static SetCooperativeLevelW_t SetCooperativeLevelW;

    static DirectInput8Create_t TrueDirectInput8Create = nullptr;
    static CreateDeviceA_t TrueCreateDeviceA = nullptr;
    static CreateDeviceW_t TrueCreateDeviceW = nullptr;
    static GetPropertyA_t TrueGetPropertyA = nullptr;
    static GetPropertyW_t TrueGetPropertyW = nullptr;
    static GetDeviceInfoA_t TrueGetDeviceInfoA = nullptr;
    static GetDeviceInfoW_t TrueGetDeviceInfoW = nullptr;
    static EnumDevicesA_t TrueEnumDevicesA = nullptr;
    static EnumDevicesW_t TrueEnumDevicesW = nullptr;
    static SetCooperativeLevelA_t TrueSetCooperativeLevelA = nullptr;
    static SetCooperativeLevelW_t TrueSetCooperativeLevelW = nullptr;
    static LPDIENUMDEVICESCALLBACKA TrueCallbackA = nullptr;
    static LPDIENUMDEVICESCALLBACKW TrueCallbackW = nullptr;

    template<typename T>
    bool DeviceCheck(T* lpddi)
    {
        // Bad arguments
        if (!lpddi || lpddi->dwSize != sizeof(T))
        {
            PrintLog("Bad arguments - skipping");
            return false;
        }

        // Fast return if keyboard or mouse
        if (((lpddi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
        {
            PrintLog("Keyboard detected - skipping");
            return false;
        }
        else if (((lpddi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
        {
            PrintLog("Mouse detected - skipping");
            return false;
        }

        return true;
    }

    BOOL FAR PASCAL HookEnumCallbackA(LPCDIDEVICEINSTANCEA lpddi, LPVOID pvRef)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return TrueCallbackA(lpddi, pvRef);
        PrintLog("EnumCallbackA");

        if (!DeviceCheck(lpddi)) return TrueCallbackA(lpddi, pvRef);

        if (s_InputHook->GetState(InputHook::HOOK_STOP)) return DIENUM_STOP;

        for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
        {
            if (IsEqualGUID(padcfg->GetProductGUID(), lpddi->guidProduct))
            {
                DIDEVICEINSTANCEA fakedev = *lpddi;
                if (s_InputHook->GetState(InputHook::HOOK_PIDVID))
                {
                    std::string strTrueguidProduct;
                    std::string strHookguidProduct;

                    GUIDtoString(&strTrueguidProduct, fakedev.guidProduct);
                    fakedev.guidProduct.Data1 = s_InputHook->GetFakePIDVID();
                    GUIDtoString(&strHookguidProduct, fakedev.guidProduct);

                    PrintLog("GUID change:");
                    PrintLog("%s", strTrueguidProduct.c_str());
                    PrintLog("%s", strHookguidProduct.c_str());
                }

                // This should not be required
                // fakedev.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                // fakedev.wUsage = 0x05;
                // fakedev.wUsagePage = 0x01;

                if (s_InputHook->GetState(InputHook::HOOK_NAME))
                {
                    std::string OldProductName = fakedev.tszProductName;
                    std::string OldInstanceName = fakedev.tszInstanceName;

                    strcpy_s(fakedev.tszProductName, "XBOX 360 For Windows (Controller)");
                    strcpy_s(fakedev.tszInstanceName, "XBOX 360 For Windows (Controller)");

                    PrintLog("Product Name change:");
                    PrintLog("\"%s\"", OldProductName.c_str());
                    PrintLog("\"%s\"", fakedev.tszProductName);

                    PrintLog("Instance Name change:");
                    PrintLog("\"%s\"", OldInstanceName.c_str());
                    PrintLog("\"%s\"", fakedev.tszInstanceName);
                }

                return TrueCallbackA(&fakedev, pvRef);
            }
        }
        return TrueCallbackA(lpddi, pvRef);
    }

    BOOL FAR PASCAL HookEnumCallbackW(LPCDIDEVICEINSTANCEW lpddi, LPVOID pvRef)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return TrueCallbackW(lpddi, pvRef);
        PrintLog("EnumCallbackW");

        if (!DeviceCheck(lpddi)) return TrueCallbackW(lpddi, pvRef);

        if (s_InputHook->GetState(InputHook::HOOK_STOP)) return DIENUM_STOP;

        for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
        {
            if (IsEqualGUID(padcfg->GetProductGUID(), lpddi->guidProduct))
            {
                DIDEVICEINSTANCEW fakedev = *lpddi;
                if (s_InputHook->GetState(InputHook::HOOK_PIDVID))
                {
                    std::wstring strTrueguidProduct;
                    std::wstring strHookguidProduct;

                    GUIDtoString(&strTrueguidProduct, fakedev.guidProduct);
                    fakedev.guidProduct.Data1 = s_InputHook->GetFakePIDVID();
                    GUIDtoString(&strHookguidProduct, fakedev.guidProduct);

                    PrintLog("GUID change:");
                    PrintLog("%ls", strTrueguidProduct.c_str());
                    PrintLog("%ls", strHookguidProduct.c_str());
                }

                // This should not be required
                // fakedev.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                // fakedev.wUsage = 0x05;
                // fakedev.wUsagePage = 0x01;

                if (s_InputHook->GetState(InputHook::HOOK_NAME))
                {
                    std::wstring OldProductName(fakedev.tszProductName);
                    std::wstring OldInstanceName(fakedev.tszInstanceName);

                    wcscpy_s(fakedev.tszProductName, L"XBOX 360 For Windows (Controller)");
                    wcscpy_s(fakedev.tszInstanceName, L"XBOX 360 For Windows (Controller)");

                    PrintLog("Product Name change:");
                    PrintLog("\"%ls\"", OldProductName.c_str());
                    PrintLog("\"%ls\"", fakedev.tszProductName);

                    PrintLog("Instance Name change:");
                    PrintLog("\"%ls\"", OldInstanceName.c_str());
                    PrintLog("\"%ls\"", fakedev.tszInstanceName);
                }

                return TrueCallbackW(&fakedev, pvRef);
            }
        }

        return TrueCallbackW(lpddi, pvRef);
    }

    HRESULT STDMETHODCALLTYPE HookEnumDevicesA(LPDIRECTINPUT8A This, DWORD dwDevType, LPDIENUMDEVICESCALLBACKA lpCallback, LPVOID pvRef, DWORD dwFlags)
    {
        if (s_InputHook->GetState(InputHook::HOOK_DI))
        {
            PrintLog("IDirectInput8A::EnumDevicesA");

            if (lpCallback)
            {
                TrueCallbackA = lpCallback;
                return TrueEnumDevicesA(This, dwDevType, HookEnumCallbackA, pvRef, dwFlags);
            }
        }
        return TrueEnumDevicesA(This, dwDevType, lpCallback, pvRef, dwFlags);
    }

    HRESULT STDMETHODCALLTYPE HookEnumDevicesW(LPDIRECTINPUT8W This, DWORD dwDevType, LPDIENUMDEVICESCALLBACKW lpCallback, LPVOID pvRef, DWORD dwFlags)
    {
        if (s_InputHook->GetState(InputHook::HOOK_DI))
        {
            PrintLog("IDirectInput8W::EnumDevicesW");

            if (lpCallback)
            {
                TrueCallbackW = lpCallback;
                return TrueEnumDevicesW(This, dwDevType, HookEnumCallbackW, pvRef, dwFlags);
            }
        }
        return TrueEnumDevicesW(This, dwDevType, lpCallback, pvRef, dwFlags);
    }

    HRESULT STDMETHODCALLTYPE HookGetDeviceInfoA(LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
    {
        HRESULT hr = TrueGetDeviceInfoA(This, pdidi);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("IDirectInputDevice8A::GetDeviceInfoA*");

        if (!DeviceCheck(pdidi)) return TrueGetDeviceInfoA(This, pdidi);

        for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
        {
            if (IsEqualGUID(padcfg->GetProductGUID(), pdidi->guidProduct))
            {
                if (s_InputHook->GetState(InputHook::HOOK_PIDVID))
                {
                    std::string strTrueguidProduct;
                    std::string strHookguidProduct;

                    GUIDtoString(&strTrueguidProduct, pdidi->guidProduct);
                    pdidi->guidProduct.Data1 = s_InputHook->GetFakePIDVID();
                    GUIDtoString(&strHookguidProduct, pdidi->guidProduct);

                    PrintLog("GUID change:");
                    PrintLog("%s", strTrueguidProduct.c_str());
                    PrintLog("%s", strHookguidProduct.c_str());
                }

                // This should not be required
                //pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                //pdidi->wUsage = 0x05;
                //pdidi->wUsagePage = 0x01;

                if (s_InputHook->GetState(InputHook::HOOK_NAME))
                {
                    std::string OldProductName(pdidi->tszProductName);
                    std::string OldInstanceName(pdidi->tszInstanceName);

                    strcpy_s(pdidi->tszProductName, "XBOX 360 For Windows (Controller)");
                    strcpy_s(pdidi->tszInstanceName, "XBOX 360 For Windows (Controller)");

                    PrintLog("Product Name change:");
                    PrintLog("\"%s\"", OldProductName.c_str());
                    PrintLog("\"%s\"", pdidi->tszProductName);

                    PrintLog("Instance Name change:");
                    PrintLog("\"%s\"", OldInstanceName.c_str());
                    PrintLog("\"%s\"", pdidi->tszInstanceName);
                }
            }
        }

        return hr;
    }

    HRESULT STDMETHODCALLTYPE HookGetDeviceInfoW(LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi)
    {
        HRESULT hr = TrueGetDeviceInfoW(This, pdidi);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("IDirectInputDevice8W::GetDeviceInfoW");

        if (!DeviceCheck(pdidi)) return TrueGetDeviceInfoW(This, pdidi);

        for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
        {
            if (IsEqualGUID(padcfg->GetProductGUID(), pdidi->guidProduct))
            {
                if (s_InputHook->GetState(InputHook::HOOK_PIDVID))
                {
                    std::wstring strTrueguidProduct;
                    std::wstring strHookguidProduct;

                    GUIDtoString(&strTrueguidProduct, pdidi->guidProduct);
                    pdidi->guidProduct.Data1 = s_InputHook->GetFakePIDVID();
                    GUIDtoString(&strHookguidProduct, pdidi->guidProduct);

                    PrintLog("GUID change:");
                    PrintLog("%ls", strTrueguidProduct.c_str());
                    PrintLog("%ls", strHookguidProduct.c_str());
                }

                // This should not be required
                //pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                //pdidi->wUsage = 0x05;
                //pdidi->wUsagePage = 0x01;

                if (s_InputHook->GetState(InputHook::HOOK_NAME))
                {
                    std::wstring OldProductName(pdidi->tszProductName);
                    std::wstring OldInstanceName(pdidi->tszInstanceName);

                    wcscpy_s(pdidi->tszProductName, L"XBOX 360 For Windows (Controller)");
                    wcscpy_s(pdidi->tszInstanceName, L"XBOX 360 For Windows (Controller)");

                    PrintLog("Product Name change:");
                    PrintLog("\"%ls\"", OldProductName.c_str());
                    PrintLog("\"%ls\"", pdidi->tszProductName);

                    PrintLog("Instance Name change:");
                    PrintLog("\"%ls\"", OldInstanceName.c_str());
                    PrintLog("\"%ls\"", pdidi->tszInstanceName);
                }
            }
        }

        return hr;
    }

    HRESULT STDMETHODCALLTYPE HookGetPropertyA(LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
    {
        HRESULT hr = TrueGetPropertyA(This, rguidProp, pdiph);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("IDirectInputDevice8A::GetPropertyA");

        if (hr != DI_OK) return hr;

        if (s_InputHook->GetState(InputHook::HOOK_PIDVID) && &rguidProp == &DIPROP_VIDPID)
        {
            LPDIPROPDWORD pdipw = reinterpret_cast<LPDIPROPDWORD>(pdiph);

            DWORD dwHookPIDVID = s_InputHook->GetFakePIDVID();
            DWORD dwTruePIDVID = pdipw->dwData;

            pdipw->dwData = dwHookPIDVID;
            PrintLog("VIDPID change:");
            PrintLog("%08X", dwTruePIDVID);
            PrintLog("%08X", pdipw->dwData);
        }

        if (s_InputHook->GetState(InputHook::HOOK_NAME) && &rguidProp == &DIPROP_PRODUCTNAME)
        {
            LPDIPROPSTRING pdips = reinterpret_cast<LPDIPROPSTRING>(pdiph);

            wchar_t TrueName[MAX_PATH];
            wcscpy_s(TrueName, pdips->wsz);

            swprintf_s(pdips->wsz, L"%s", L"XBOX 360 For Windows (Controller)");
            PrintLog("Product Name change:");
            PrintLog("\"%ls\"", TrueName);
            PrintLog("\"%ls\"", pdips->wsz);
        }

        return hr;
    }

    HRESULT STDMETHODCALLTYPE HookGetPropertyW(LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
    {
        HRESULT hr = TrueGetPropertyW(This, rguidProp, pdiph);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("IDirectInputDevice8W::GetPropertyW");

        if (hr != DI_OK) return hr;

        if (s_InputHook->GetState(InputHook::HOOK_PIDVID) && &rguidProp == &DIPROP_VIDPID)
        {
            LPDIPROPDWORD pdipw = reinterpret_cast<LPDIPROPDWORD>(pdiph);

            DWORD dwHookPIDVID = s_InputHook->GetFakePIDVID();
            DWORD dwTruePIDVID = pdipw->dwData;

            pdipw->dwData = dwHookPIDVID;
            PrintLog("VIDPID change:");
            PrintLog("%08X", dwTruePIDVID);
            PrintLog("%08X", pdipw->dwData);
        }

        if (s_InputHook->GetState(InputHook::HOOK_NAME) && &rguidProp == &DIPROP_PRODUCTNAME)
        {
            LPDIPROPSTRING pdips = reinterpret_cast<LPDIPROPSTRING>(pdiph);

            wchar_t TrueName[MAX_PATH];
            wcscpy_s(TrueName, pdips->wsz);

            swprintf_s(pdips->wsz, L"%s", L"XBOX 360 For Windows (Controller)");
            PrintLog("Product Name change:");
            PrintLog("\"%ls\"", TrueName);
            PrintLog("\"%ls\"", pdips->wsz);
        }

        return hr;
    }

    HRESULT STDMETHODCALLTYPE HookSetCooperativeLevelA(LPDIRECTINPUTDEVICE8A This, HWND hWnd, DWORD dwFlags)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return TrueSetCooperativeLevelA(This, hWnd, dwFlags);
        PrintLog("IDirectInputDevice8A::SetCooperativeLevelA");

        if (dwFlags & DISCL_EXCLUSIVE)
        {
            dwFlags &= ~DISCL_EXCLUSIVE;
            dwFlags |= DISCL_NONEXCLUSIVE;
        }
        return TrueSetCooperativeLevelA(This, hWnd, dwFlags);
    }

    HRESULT STDMETHODCALLTYPE HookSetCooperativeLevelW(LPDIRECTINPUTDEVICE8W This, HWND hWnd, DWORD dwFlags)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return TrueSetCooperativeLevelW(This, hWnd, dwFlags);
        PrintLog("IDirectInputDevice8W::SetCooperativeLevelW");

        if (dwFlags & DISCL_EXCLUSIVE)
        {
            dwFlags &= ~DISCL_EXCLUSIVE;
            dwFlags |= DISCL_NONEXCLUSIVE;
        }
        return TrueSetCooperativeLevelW(This, hWnd, dwFlags);
    }

    HRESULT STDMETHODCALLTYPE HookCreateDeviceA(LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
    {
        HRESULT hr = TrueCreateDeviceA(This, rguid, lplpDirectInputDevice, pUnkOuter);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("IDirectInput8A::CreateDeviceA");

        if (hr != DI_OK) return hr;

        if (lplpDirectInputDevice && *lplpDirectInputDevice)
        {
            LPDIRECTINPUTDEVICE8A pDirectInputDevice8A = *lplpDirectInputDevice;
            if (pDirectInputDevice8A->lpVtbl->GetDeviceInfo)
            {

                GetDeviceInfoA = pDirectInputDevice8A->lpVtbl->GetDeviceInfo;
                IH_CreateHook(GetDeviceInfoA, HookGetDeviceInfoA, &TrueGetDeviceInfoA);
                IH_EnableHook(GetDeviceInfoA);
            }

            if (pDirectInputDevice8A->lpVtbl->GetProperty)
            {
                PrintLog("Hooking GetPropertyA");
                GetPropertyA = pDirectInputDevice8A->lpVtbl->GetProperty;
                IH_CreateHook(GetPropertyA, HookGetPropertyA, &TrueGetPropertyA);
                IH_EnableHook(GetPropertyA);
            }

            if (pDirectInputDevice8A->lpVtbl->SetCooperativeLevel)
            {
                SetCooperativeLevelA = pDirectInputDevice8A->lpVtbl->SetCooperativeLevel;
                IH_CreateHook(SetCooperativeLevelA, HookSetCooperativeLevelA, &TrueSetCooperativeLevelA);
                IH_EnableHook(SetCooperativeLevelA);
            }
        }

        return hr;
    }


    HRESULT STDMETHODCALLTYPE HookCreateDeviceW(LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
    {
        HRESULT hr = TrueCreateDeviceW(This, rguid, lplpDirectInputDevice, pUnkOuter);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("IDirectInput8W::CreateDeviceW");

        if (hr != DI_OK) return hr;

        if (*lplpDirectInputDevice)
        {
            LPDIRECTINPUTDEVICE8W pDirectInputDevice8W = *lplpDirectInputDevice;
            if (pDirectInputDevice8W->lpVtbl->GetDeviceInfo)
            {
                GetDeviceInfoW = pDirectInputDevice8W->lpVtbl->GetDeviceInfo;
                IH_CreateHook(GetDeviceInfoW, HookGetDeviceInfoW, &TrueGetDeviceInfoW);
                IH_EnableHook(GetDeviceInfoW);
            }

            if (pDirectInputDevice8W->lpVtbl->GetProperty)
            {
                GetPropertyW = pDirectInputDevice8W->lpVtbl->GetProperty;
                IH_CreateHook(GetPropertyW, HookGetPropertyW, &TrueGetPropertyW);
                IH_EnableHook(GetPropertyW);
            }

            if (pDirectInputDevice8W->lpVtbl->SetCooperativeLevel)
            {
                SetCooperativeLevelW = pDirectInputDevice8W->lpVtbl->SetCooperativeLevel;
                IH_CreateHook(SetCooperativeLevelW, HookSetCooperativeLevelW, &TrueSetCooperativeLevelW);
                IH_EnableHook(SetCooperativeLevelW);
            }
        }

        return hr;
    }

#if 0
    void InputHook::HookDICOM(REFIID riidltf, LPVOID *ppv)
    {
        PrintLog("Hooking HookDICOM");
        s_InputHook = this;

        if (IsEqualIID(riidltf, IID_IDirectInput8A))
        {
            LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppv);

            if (pDIA)
            {
                PrintLog("DirectInput8Create - ANSI interface");
                if (pDIA->lpVtbl->CreateDevice)
                {
                    CreateDeviceA = pDIA->lpVtbl->CreateDevice;
                    IH_CreateHook(CreateDeviceA, HookCreateDeviceA, &TrueCreateDeviceA);
                    IH_EnableHook(CreateDeviceA);
                }
                if (pDIA->lpVtbl->EnumDevices)
                {

                    EnumDevicesA = pDIA->lpVtbl->EnumDevices;
                    IH_CreateHook(EnumDevicesA, HookEnumDevicesA, &TrueEnumDevicesA);
                    IH_EnableHook(EnumDevicesA);
                }
            }
        }

        if (IsEqualIID(riidltf, IID_IDirectInput8W))
        {
            LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppv);

            if (pDIW)
            {
                PrintLog("DirectInput8Create - UNICODE interface");
                if (pDIW->lpVtbl->CreateDevice)
                {
                    CreateDeviceW = pDIW->lpVtbl->CreateDevice;
                    IH_CreateHook(CreateDeviceW, HookCreateDeviceW, &TrueCreateDeviceW);
                    IH_EnableHook(CreateDeviceW);
                }
                if (pDIW->lpVtbl->EnumDevices)
                {
                    EnumDevicesW = pDIW->lpVtbl->EnumDevices;
                    IH_CreateHook(EnumDevicesW, HookEnumDevicesW, &TrueEnumDevicesW);
                    IH_EnableHook(EnumDevicesW);
                }
            }
        }
    }
#endif

    HRESULT WINAPI HookDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
    {
        HRESULT hr = TrueDirectInput8Create(hinst, dwVersion, riidltf, ppvOut, punkOuter);

        if (!s_InputHook->GetState(InputHook::HOOK_DI)) return hr;
        PrintLog("DirectInput8Create");

        if (hr != DI_OK) return hr;

        s_InputHook->StartTimeoutThread();

        if (IsEqualIID(riidltf, IID_IDirectInput8A))
        {
            LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppvOut);

            if (pDIA)
            {
                PrintLog("DirectInput8Create - ANSI interface");
                if (pDIA->lpVtbl->CreateDevice)
                {
                    CreateDeviceA = pDIA->lpVtbl->CreateDevice;
                    IH_CreateHook(CreateDeviceA, HookCreateDeviceA, &TrueCreateDeviceA);
                    IH_EnableHook(CreateDeviceA);
                }
                if (pDIA->lpVtbl->EnumDevices)
                {
                    EnumDevicesA = pDIA->lpVtbl->EnumDevices;
                    IH_CreateHook(EnumDevicesA, HookEnumDevicesA, &TrueEnumDevicesA);
                    IH_EnableHook(EnumDevicesA);
                }
            }
        }
        else if (IsEqualIID(riidltf, IID_IDirectInput8W))
        {
            LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);

            if (pDIW)
            {
                PrintLog("DirectInput8Create - UNICODE interface");
                if (pDIW->lpVtbl->CreateDevice)
                {
                    CreateDeviceW = pDIW->lpVtbl->CreateDevice;
                    IH_CreateHook(CreateDeviceW, HookCreateDeviceW, &TrueCreateDeviceW);
                    IH_EnableHook(CreateDeviceW);
                }
                if (pDIW->lpVtbl->EnumDevices)
                {
                    EnumDevicesW = pDIW->lpVtbl->EnumDevices;
                    IH_CreateHook(EnumDevicesW, HookEnumDevicesW, &TrueEnumDevicesW);
                    IH_EnableHook(EnumDevicesW);
                }
            }
        }

        return hr;
    }
}

void InputHook::HookDI()
{
    PrintLog("Hooking DirectInput");
    HookDI::s_InputHook = this;

    IH_CreateHook(DirectInput8Create, HookDI::HookDirectInput8Create, &HookDI::TrueDirectInput8Create);
}
