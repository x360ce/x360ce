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
#define CINTERFACE

#include <dinput.h>
#include "globals.h"

#include "InputHook.h"

#include "Log.h"
#include "Utilities\Misc.h"


static iHook* iHookThis;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef HRESULT (WINAPI *DirectInput8Create_t)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
typedef HRESULT (STDMETHODCALLTYPE *CreateDeviceA_t) (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A *lplpDirectInputDeviceA, LPUNKNOWN pUnkOuter);
typedef HRESULT (STDMETHODCALLTYPE *CreateDeviceW_t) (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W *lplpDirectInputDeviceW, LPUNKNOWN pUnkOuter);
typedef HRESULT (STDMETHODCALLTYPE *GetPropertyA_t) (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
typedef HRESULT (STDMETHODCALLTYPE *GetPropertyW_t) (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
typedef HRESULT (STDMETHODCALLTYPE *GetDeviceInfoA_t) (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi);
typedef HRESULT (STDMETHODCALLTYPE *GetDeviceInfoW_t) (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi);
typedef HRESULT (STDMETHODCALLTYPE *EnumDevicesA_t) (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags);
typedef HRESULT (STDMETHODCALLTYPE *EnumDevicesW_t) (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags);
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

CreateDeviceA_t hCreateDeviceA = NULL;
CreateDeviceW_t hCreateDeviceW = NULL;
GetPropertyA_t hGetPropertyA = NULL;
GetPropertyW_t hGetPropertyW = NULL;
GetDeviceInfoA_t hGetDeviceInfoA = NULL;
GetDeviceInfoW_t hGetDeviceInfoW = NULL;
EnumDevicesA_t hEnumDevicesA = NULL;
EnumDevicesW_t hEnumDevicesW = NULL;

DirectInput8Create_t oDirectInput8Create = NULL;
CreateDeviceA_t oCreateDeviceA = NULL;
CreateDeviceW_t oCreateDeviceW = NULL;
GetPropertyA_t oGetPropertyA = NULL;
GetPropertyW_t oGetPropertyW = NULL;
GetDeviceInfoA_t oGetDeviceInfoA = NULL;
GetDeviceInfoW_t oGetDeviceInfoW = NULL;
EnumDevicesA_t oEnumDevicesA = NULL;
EnumDevicesW_t oEnumDevicesW = NULL;

LPDIENUMDEVICESCALLBACKA lpTrueCallbackA= NULL;
LPDIENUMDEVICESCALLBACKW lpTrueCallbackW= NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL FAR PASCAL HookEnumCallbackA( const DIDEVICEINSTANCEA* pInst,VOID* pContext )
{
    if(!iHookThis->GetState(iHook::HOOK_DI)) return lpTrueCallbackA(pInst,pContext);
    PrintLog(LOG_HOOKDI,"*EnumCallbackA*");

    // Fast return if keyboard or mouse
    if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
    {
        PrintLog(LOG_HOOKDI,"Keyboard detected - skipping");
        return lpTrueCallbackA(pInst,pContext);
    }

    if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
    {
        PrintLog(LOG_HOOKDI,"Mouse detected - skipping");
        return lpTrueCallbackA(pInst,pContext);
    }

    if(iHookThis->GetState(iHook::HOOK_STOP)) return DIENUM_STOP;

    if(pInst && pInst->dwSize == sizeof(DIDEVICEINSTANCEA))
    {
        for(auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
        {
            if(padcfg->GetHookState() && IsEqualGUID(padcfg->GetProductGUID(),pInst->guidProduct))
            {
                DIDEVICEINSTANCEA& HookInst = *(const_cast<DIDEVICEINSTANCEA*>(pInst));
                //DIDEVICEINSTANCEA HookInst;
                //memcpy(&HookInst,pInst,pInst->dwSize);

                if(iHookThis->GetState(iHook::HOOK_PIDVID))
                {
                    std::string strTrueguidProduct;
                    std::string strHookguidProduct;

                    strTrueguidProduct = GUIDtoStringA(HookInst.guidProduct);
                    HookInst.guidProduct.Data1 = iHookThis->GetFakePIDVID();
                    strHookguidProduct = GUIDtoStringA(HookInst.guidProduct);

                    PrintLog(LOG_HOOKDI,"%s","GUID change:");
                    PrintLog(LOG_HOOKDI,"%s",strTrueguidProduct.c_str());
                    PrintLog(LOG_HOOKDI,"%s",strHookguidProduct.c_str());
                }

                // This should not be required
                //HookInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                //HookInst.wUsage = 0x05;
                //HookInst.wUsagePage = 0x01;

                if(iHookThis->GetState(iHook::HOOK_NAME))
                {
                    std::string OldProductName = HookInst.tszProductName;
                    std::string OldInstanceName = HookInst.tszInstanceName;

                    strcpy_s(HookInst.tszProductName, "XBOX 360 For Windows (Controller)");
                    strcpy_s(HookInst.tszInstanceName, "XBOX 360 For Windows (Controller)");

                    PrintLog(LOG_HOOKDI,"%s","Product Name change:");
                    PrintLog(LOG_HOOKDI,"\"%s\"",OldProductName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%s\"",HookInst.tszProductName);

                    PrintLog(LOG_HOOKDI,"%s","Instance Name change:");
                    PrintLog(LOG_HOOKDI,"\"%s\"",OldInstanceName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%s\"",HookInst.tszInstanceName);
                }

                return lpTrueCallbackA(&HookInst,pContext);
            }
        }
    }

    return lpTrueCallbackA(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL FAR PASCAL HookEnumCallbackW( const DIDEVICEINSTANCEW* pInst,VOID* pContext )
{
    if(!iHookThis->GetState(iHook::HOOK_DI)) return lpTrueCallbackW(pInst,pContext);
    PrintLog(LOG_HOOKDI,"*EnumCallbackW*");

    // Fast return if keyboard or mouse
    if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
    {
        PrintLog(LOG_HOOKDI,"Keyboard detected - skipping");
        return lpTrueCallbackW(pInst,pContext);
    }

    if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
    {
        PrintLog(LOG_HOOKDI,"Mouse detected - skipping");
        return lpTrueCallbackW(pInst,pContext);
    }

    if(iHookThis->GetState(iHook::HOOK_STOP)) return DIENUM_STOP;

    if(pInst && pInst->dwSize == sizeof(DIDEVICEINSTANCEW))
    {
		for(auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
        {
            if(padcfg->GetHookState() && IsEqualGUID(padcfg->GetProductGUID(),pInst->guidProduct))
            {
                DIDEVICEINSTANCEW& HookInst = *(const_cast<DIDEVICEINSTANCEW*>(pInst));
                //DIDEVICEINSTANCEW HookInst;
                //memcpy(&HookInst,pInst,pInst->dwSize);

                if(iHookThis->GetState(iHook::HOOK_PIDVID))
                {
                    std::wstring strTrueguidProduct;
                    std::wstring strHookguidProduct;

                    strTrueguidProduct = GUIDtoStringW(HookInst.guidProduct);
                    HookInst.guidProduct.Data1 = iHookThis->GetFakePIDVID();
                    strHookguidProduct = GUIDtoStringW(HookInst.guidProduct);

                    PrintLog(LOG_HOOKDI,"%s","GUID change:");
                    PrintLog(LOG_HOOKDI,"%ls",strTrueguidProduct.c_str());
                    PrintLog(LOG_HOOKDI,"%ls",strHookguidProduct.c_str());
                }

                // This should not be required
                //HookInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                //HookInst.wUsage = 0x05;
                //HookInst.wUsagePage = 0x01;

                if(iHookThis->GetState(iHook::HOOK_NAME))
                {
                    std::wstring OldProductName(HookInst.tszProductName);
                    std::wstring OldInstanceName(HookInst.tszInstanceName);

                    wcscpy_s(HookInst.tszProductName, L"XBOX 360 For Windows (Controller)");
                    wcscpy_s(HookInst.tszInstanceName, L"XBOX 360 For Windows (Controller)");

                    PrintLog(LOG_HOOKDI,"%s","Product Name change:");
                    PrintLog(LOG_HOOKDI,"\"%ls\"",OldProductName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%ls\"",HookInst.tszProductName);

                    PrintLog(LOG_HOOKDI,"%s","Instance Name change:");
                    PrintLog(LOG_HOOKDI,"\"%ls\"",OldInstanceName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%ls\"",HookInst.tszInstanceName);
                }

                return lpTrueCallbackW(&HookInst,pContext);
            }
        }
    }

    return lpTrueCallbackW(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesA (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags)
{
    if(iHookThis->GetState(iHook::HOOK_DI))
    {
        PrintLog(LOG_HOOKDI,"*EnumDevicesA*");

        if (lpCallback)
        {
            lpTrueCallbackA = lpCallback;
            return oEnumDevicesA(This,dwDevType,HookEnumCallbackA,pvRef,dwFlags);
        }
    }
    return oEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
    if(iHookThis->GetState(iHook::HOOK_DI))
    {
        PrintLog(LOG_HOOKDI,"*EnumDevicesW*");

        if (lpCallback)
        {
            lpTrueCallbackW = lpCallback;
            return oEnumDevicesW(This,dwDevType,HookEnumCallbackW,pvRef,dwFlags);
        }
    }
    return oEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoA (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
{
    HRESULT hr = oGetDeviceInfoA( This, pdidi );

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*GetDeviceInfoA*");

    if(hr != NO_ERROR) return hr;

    if(pdidi)
    {
        // Fast return if keyboard or mouse
        if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
        {
            PrintLog(LOG_HOOKDI,"Keyboard detected - skipping");
            return hr;
        }

        if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
        {
            PrintLog(LOG_HOOKDI,"Mouse detected - skipping");
            return hr;
        }

        for(auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
        {
            if(padcfg->GetHookState() && IsEqualGUID(padcfg->GetProductGUID(), pdidi->guidProduct))
            {
                if(iHookThis->GetState(iHook::HOOK_PIDVID))
                {
                    std::string strTrueguidProduct;
                    std::string strHookguidProduct;

                    strTrueguidProduct = GUIDtoStringA(pdidi->guidProduct);
                    pdidi->guidProduct.Data1 = iHookThis->GetFakePIDVID();
                    strHookguidProduct = GUIDtoStringA(pdidi->guidProduct);

                    PrintLog(LOG_HOOKDI,"%s","GUID change:");
                    PrintLog(LOG_HOOKDI,"%s",strTrueguidProduct.c_str());
                    PrintLog(LOG_HOOKDI,"%s",strHookguidProduct.c_str());
                }

                // This should not be required
                //pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                //pdidi->wUsage = 0x05;
                //pdidi->wUsagePage = 0x01;

                if(iHookThis->GetState(iHook::HOOK_NAME))
                {
                    std::string OldProductName(pdidi->tszProductName);
                    std::string OldInstanceName(pdidi->tszInstanceName);

                    strcpy_s(pdidi->tszProductName, "XBOX 360 For Windows (Controller)");
                    strcpy_s(pdidi->tszInstanceName, "XBOX 360 For Windows (Controller)");

                    PrintLog(LOG_HOOKDI,"%s","Product Name change:");
                    PrintLog(LOG_HOOKDI,"\"%s\"",OldProductName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%s\"",pdidi->tszProductName);

                    PrintLog(LOG_HOOKDI,"%s","Instance Name change:");
                    PrintLog(LOG_HOOKDI,"\"%s\"",OldInstanceName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%s\"",pdidi->tszInstanceName);
                }

                hr=DI_OK;
            }
        }
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoW (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi)
{
    HRESULT hr = oGetDeviceInfoW( This, pdidi );

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*GetDeviceInfoW*");

    if(hr != NO_ERROR) return hr;

    if(pdidi)
    {
        // Fast return if keyboard or mouse
        if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
        {
            PrintLog(LOG_HOOKDI,"Keyboard detected - skipping");
            return hr;
        }

        if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
        {
            PrintLog(LOG_HOOKDI,"Mouse detected - skipping");
            return hr;
        }

        for(auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
        {
            if(padcfg->GetHookState() && IsEqualGUID(padcfg->GetProductGUID(), pdidi->guidProduct))
            {
                if(iHookThis->GetState(iHook::HOOK_PIDVID))
                {
                    std::wstring strTrueguidProduct;
                    std::wstring strHookguidProduct;

                    strTrueguidProduct = GUIDtoStringW(pdidi->guidProduct);
                    pdidi->guidProduct.Data1 = iHookThis->GetFakePIDVID();
                    strHookguidProduct = GUIDtoStringW(pdidi->guidProduct);

                    PrintLog(LOG_HOOKDI,"%s","GUID change:");
                    PrintLog(LOG_HOOKDI,"%ls",strTrueguidProduct.c_str());
                    PrintLog(LOG_HOOKDI,"%ls",strHookguidProduct.c_str());
                }

                // This should not be required
                //pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
                //pdidi->wUsage = 0x05;
                //pdidi->wUsagePage = 0x01;

                if(iHookThis->GetState(iHook::HOOK_NAME))
                {
                    std::wstring OldProductName(pdidi->tszProductName);
                    std::wstring OldInstanceName(pdidi->tszInstanceName);

                    wcscpy_s(pdidi->tszProductName, L"XBOX 360 For Windows (Controller)");
                    wcscpy_s(pdidi->tszInstanceName, L"XBOX 360 For Windows (Controller)");

                    PrintLog(LOG_HOOKDI,"%s","Product Name change:");
                    PrintLog(LOG_HOOKDI,"\"%ls\"",OldProductName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%ls\"",pdidi->tszProductName);

                    PrintLog(LOG_HOOKDI,"%s","Instance Name change:");
                    PrintLog(LOG_HOOKDI,"\"%ls\"",OldInstanceName.c_str());
                    PrintLog(LOG_HOOKDI,"\"%ls\"",pdidi->tszInstanceName);
                }

                hr=DI_OK;
            }
        }
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetPropertyA (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
    HRESULT hr = oGetPropertyA(This, rguidProp, pdiph);

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*GetPropertyA*");

    if(hr != NO_ERROR) return hr;

    if(iHookThis->GetState(iHook::HOOK_PIDVID) && &rguidProp == &DIPROP_VIDPID)
    {
        DWORD dwHookPIDVID = iHookThis->GetFakePIDVID();
        DWORD dwTruePIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

        reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
        PrintLog(LOG_HOOKDI,"%s","VIDPID change:");
        PrintLog(LOG_HOOKDI,"%08X",dwTruePIDVID);
        PrintLog(LOG_HOOKDI,"%08X",reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
    }

    if(iHookThis->GetState(iHook::HOOK_NAME) && &rguidProp == &DIPROP_PRODUCTNAME)
    {
        wchar_t TrueName[MAX_PATH];
        wcscpy_s(TrueName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

        swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
        PrintLog(LOG_HOOKDI,"%s","Product Name change:");
        PrintLog(LOG_HOOKDI,"\"%ls\"",TrueName);
        PrintLog(LOG_HOOKDI,"\"%ls\"",reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetPropertyW (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
    HRESULT hr = oGetPropertyW(This, rguidProp, pdiph);

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*GetPropertyW*");

    if(hr != NO_ERROR) return hr;

    if(iHookThis->GetState(iHook::HOOK_PIDVID) && &rguidProp == &DIPROP_VIDPID)
    {
        DWORD dwHookPIDVID = iHookThis->GetFakePIDVID();
        DWORD dwTruePIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

        reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
        PrintLog(LOG_HOOKDI,"%s","VIDPID change:");
        PrintLog(LOG_HOOKDI,"%08X",dwTruePIDVID);
        PrintLog(LOG_HOOKDI,"%08X",reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
    }

    if(iHookThis->GetState(iHook::HOOK_NAME) && &rguidProp == &DIPROP_PRODUCTNAME)
    {
        wchar_t TrueName[MAX_PATH];
        wcscpy_s(TrueName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

        swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
        PrintLog(LOG_HOOKDI,"%s","Product Name change:");
        PrintLog(LOG_HOOKDI,"\"%ls\"",TrueName);
        PrintLog(LOG_HOOKDI,"\"%ls\"",reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceA (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
    HRESULT hr = oCreateDeviceA(This, rguid, lplpDirectInputDevice, pUnkOuter);

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*CreateDeviceA*");

    if(hr != NO_ERROR) return hr;

    if(*lplpDirectInputDevice)
    {
        LPDIRECTINPUTDEVICE8A &ref = *lplpDirectInputDevice;
        if(ref->lpVtbl->GetDeviceInfo)
        {

            hGetDeviceInfoA = ref->lpVtbl->GetDeviceInfo;
            MH_CreateHook(hGetDeviceInfoA,HookGetDeviceInfoA,reinterpret_cast<void**>(&oGetDeviceInfoA));
            if(MH_EnableHook(hGetDeviceInfoA) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking GetDeviceInfoA");
        }

        if(ref->lpVtbl->GetProperty)
        {
            PrintLog(LOG_HOOKDI,"Hooking GetPropertyA");
            hGetPropertyA = ref->lpVtbl->GetProperty;
            MH_CreateHook(hGetPropertyA,HookGetPropertyA,reinterpret_cast<void**>(&oGetPropertyA));
            if(MH_EnableHook(hGetPropertyA) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking GetPropertyA");
        }
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceW (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
    HRESULT hr = oCreateDeviceW(This, rguid, lplpDirectInputDevice, pUnkOuter);

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*CreateDeviceW*");

    if(hr != NO_ERROR) return hr;

    if(*lplpDirectInputDevice)
    {
        LPDIRECTINPUTDEVICE8W &ref = *lplpDirectInputDevice;
        if(ref->lpVtbl->GetDeviceInfo)
        {
            hGetDeviceInfoW = ref->lpVtbl->GetDeviceInfo;
            MH_CreateHook(hGetDeviceInfoW,HookGetDeviceInfoW,reinterpret_cast<void**>(&oGetDeviceInfoW));
            if(MH_EnableHook(hGetDeviceInfoW) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking GetDeviceInfoW");
        }

        if(ref->lpVtbl->GetProperty)
        {
            hGetPropertyW = ref->lpVtbl->GetProperty;
            MH_CreateHook(hGetPropertyW,HookGetPropertyW,reinterpret_cast<void**>(&oGetPropertyW));
            if(MH_EnableHook(hGetPropertyW) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking GetPropertyW");
        }
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#if 0
void iHook::HookDICOM(REFIID riidltf, LPVOID *ppv)
{
    PrintLog(LOG_IHOOK,"Hooking HookDICOM");
    iHookThis = this;

    if(IsEqualIID(riidltf,IID_IDirectInput8A))
    {
        LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppv);

        if(pDIA)
        {
            PrintLog(LOG_HOOKDI,"DirectInput8Create - ANSI interface");
            if(pDIA->lpVtbl->CreateDevice)
            {
                hCreateDeviceA = pDIA->lpVtbl->CreateDevice;
                MH_CreateHook(hCreateDeviceA,HookCreateDeviceA,reinterpret_cast<void**>(&oCreateDeviceA));
                if(MH_EnableHook(hCreateDeviceA) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking CreateDeviceA");
            }
            if(pDIA->lpVtbl->EnumDevices)
            {

                hEnumDevicesA = pDIA->lpVtbl->EnumDevices;
                MH_CreateHook(hEnumDevicesA,HookEnumDevicesA,reinterpret_cast<void**>(&oEnumDevicesA));
                if(MH_EnableHook(hEnumDevicesA) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking EnumDevicesA");
            }
        }
    }

    if (IsEqualIID(riidltf,IID_IDirectInput8W))
    {
        LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppv);

        if(pDIW)
        {
            PrintLog(LOG_HOOKDI,"DirectInput8Create - UNICODE interface");
            if(pDIW->lpVtbl->CreateDevice)
            {
                hCreateDeviceW = pDIW->lpVtbl->CreateDevice;
                MH_CreateHook(hCreateDeviceW,HookCreateDeviceW,reinterpret_cast<void**>(&oCreateDeviceW));
                if(MH_EnableHook(hCreateDeviceW) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking CreateDeviceW");
            }
            if(pDIW->lpVtbl->EnumDevices)
            {
                hEnumDevicesW = pDIW->lpVtbl->EnumDevices;
                MH_CreateHook(hEnumDevicesW,HookEnumDevicesW,reinterpret_cast<void**>(&oEnumDevicesW));
                if(MH_EnableHook(hEnumDevicesW) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking EnumDevicesW");
            }
        }
    }
}
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
    HRESULT hr = oDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

    if(!iHookThis->GetState(iHook::HOOK_DI)) return hr;
    PrintLog(LOG_HOOKDI,"*DirectInput8Create*");

    if(IsEqualIID(riidltf,IID_IDirectInput8A))
    {
        LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppvOut);

        if(pDIA)
        {
            PrintLog(LOG_HOOKDI,"DirectInput8Create - ANSI interface");
            if(pDIA->lpVtbl->CreateDevice)
            {
                hCreateDeviceA = pDIA->lpVtbl->CreateDevice;
                MH_CreateHook(hCreateDeviceA,HookCreateDeviceA,reinterpret_cast<void**>(&oCreateDeviceA));
                if(MH_EnableHook(hCreateDeviceA) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking CreateDeviceA");
            }
            if(pDIA->lpVtbl->EnumDevices)
            {
                hEnumDevicesA = pDIA->lpVtbl->EnumDevices;
                MH_CreateHook(hEnumDevicesA,HookEnumDevicesA,reinterpret_cast<void**>(&oEnumDevicesA));
                if(MH_EnableHook(hEnumDevicesA) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking EnumDevicesA");
            }
        }
    }

    if (IsEqualIID(riidltf,IID_IDirectInput8W))
    {
        LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);

        if(pDIW)
        {
            PrintLog(LOG_HOOKDI,"DirectInput8Create - UNICODE interface");
            if(pDIW->lpVtbl->CreateDevice)
            {
                hCreateDeviceW = pDIW->lpVtbl->CreateDevice;
                MH_CreateHook(hCreateDeviceW,HookCreateDeviceW,reinterpret_cast<void**>(&oCreateDeviceW));
                if(MH_EnableHook(hCreateDeviceW) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking CreateDeviceW");
            }
            if(pDIW->lpVtbl->EnumDevices)
            {
                hEnumDevicesW = pDIW->lpVtbl->EnumDevices;
                MH_CreateHook(hEnumDevicesW,HookEnumDevicesW,reinterpret_cast<void**>(&oEnumDevicesW));
                if(MH_EnableHook(hEnumDevicesW) == MH_OK) PrintLog(LOG_HOOKDI,"Hooking EnumDevicesW");
            }
        }
    }

    return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookDI()
{
    PrintLog(LOG_IHOOK,"Hooking DirectInput");
    iHookThis = this;

    if(MH_CreateHook(DirectInput8Create,HookDirectInput8Create,reinterpret_cast<void**>(&oDirectInput8Create)) == MH_OK)
        PrintLog(LOG_IHOOK,"Hooking DirectInput8Create");
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////