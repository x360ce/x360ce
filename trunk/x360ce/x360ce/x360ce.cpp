/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2014 Robert Krawczyk
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
#include "Common.h"
#include "Logger.h"
#include "Utils.h"
#include "InputHook.h"

#include "Controller.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

#include "version.h"
#include "WindowsVersion.h"

XInputEnabled XInputIsEnabled;
HWND hMsgWnd = NULL;

xinput_dll xinput;

static const u32 PASSTROUGH = (u32)-2;

VOID CreateMsgWnd()
{
    hMsgWnd = CreateWindow(
        "Message",	// name of window class
        "x360ce",			// title-bar std::string
        WS_TILED,			// normal window
        CW_USEDEFAULT,		// default horizontal position
        CW_USEDEFAULT,		// default vertical position
        CW_USEDEFAULT,		// default width
        CW_USEDEFAULT,		// default height
        HWND_MESSAGE,		// message-only window
        NULL,				// no class menu
        CurrentModule(),	// handle to application instance
        NULL);				// no window-creation data

    if (!hMsgWnd) PrintLog("CreateWindow failed with code 0x%x", HRESULT_FROM_WIN32(GetLastError()));
}

bool XInputInitialize()
{
    if (xinput.dll) return true;

    std::wstring xinput_path;
    xinput_path.resize(MAX_PATH);
    DWORD length = 0;
    length = GetSystemDirectoryW(&xinput_path[0], MAX_PATH);
    xinput_path.resize(length);

    std::wstring current_module;
    ModuleFileName(&current_module, CurrentModule());

    xinput_path.append(L"\\");
    xinput_path.append(current_module);

    bool bHookLL = false;

    bHookLL = g_iHook.GetState(InputHook::HOOK_LL);
    if (bHookLL) g_iHook.DisableHook(InputHook::HOOK_LL);

    PrintLog("Loading %ls", xinput_path.c_str());
    xinput.dll = LoadLibraryW(xinput_path.c_str());
    if (bHookLL) g_iHook.EnableHook(InputHook::HOOK_LL);

    if (!xinput.dll)
    {
        HRESULT hr = GetLastError();
        std::wstring error_msg;
        error_msg.resize(MAX_PATH);

        swprintf_s(&error_msg[0], MAX_PATH, L"Cannot load %s error: 0x%x", xinput_path.c_str(), hr);
        PrintLog("%S", error_msg.c_str());
        MessageBoxW(NULL, error_msg.c_str(), L"Error", MB_ICONERROR);
        ExitProcess(hr);
    }

    // XInput 1.3 and older functions
    LoadFunction(xinput, XInputGetState);
    LoadFunction(xinput, XInputSetState);
    LoadFunction(xinput, XInputGetCapabilities);
    LoadFunction(xinput, XInputEnable);
    LoadFunction(xinput, XInputGetDSoundAudioDeviceGuids);
    LoadFunction(xinput, XInputGetBatteryInformation);
    LoadFunction(xinput, XInputGetKeystroke);

    // XInput 1.3 undocumented functions
    LoadFunctionOrdinal(xinput, 100, XInputGetStateEx);
    LoadFunctionOrdinal(xinput, 101, XInputWaitForGuideButton);
    LoadFunctionOrdinal(xinput, 102, XInputCancelGuideButtonWait);
    LoadFunctionOrdinal(xinput, 103, XInputPowerOffController);

    // XInput 1.4 functions
    LoadFunction(xinput, XInputGetAudioDeviceIds);

    // XInput 1.4 undocumented functionss
    LoadFunctionOrdinal(xinput, 104, XInputGetBaseBusInformation);
    LoadFunctionOrdinal(xinput, 108, XInputGetCapabilitiesEx);
    return true;
}

u32 DeviceInitialize(DWORD dwUserIndex, Controller** ppController)
{
    static bool once_flag = false;
    if (!once_flag)
    {
        std::string processName;
        ModuleFileName(&processName);
#ifndef _M_X64
        PrintLog("x360ce (x86) %s started for \"%s\"", PRODUCT_VERSION, processName.c_str());
#else
        PrintLog("x360ce (x64) %s started for \"%s\"", PRODUCT_VERSION, processName.c_str());
#endif
        std::string windows_name;
        if (GetWindowsVersionName(&windows_name))
            PrintLog("OS: \"%s\"", windows_name.c_str());

        ReadConfig();
        once_flag = true;
    }

    // Global disable
    if (g_bDisable)
        return ERROR_DEVICE_NOT_CONNECTED;

    // Invalid dwUserIndex
    if (!(dwUserIndex < XUSER_MAX_COUNT))
        return ERROR_BAD_ARGUMENTS;

    Controller* pController = nullptr;
    for (auto it = g_Controllers.begin(); it != g_Controllers.end(); ++it)
    {
        if (it->m_user == dwUserIndex)
            pController = &(*it);
    }

    if (!pController)
        return ERROR_DEVICE_NOT_CONNECTED;
    if (ppController) *ppController = pController;

    // passtrough
    if (pController->m_passthrough && XInputInitialize())
        return PASSTROUGH;

    HRESULT hr = E_FAIL;

    if (!hMsgWnd)
        CreateMsgWnd();

    if (!pController->Initalized())
    {
        PrintLog("[PAD%d] Starting", dwUserIndex + 1);
        PrintLog("[PAD%d] Initializing as UserIndex %d", dwUserIndex + 1, dwUserIndex);

        hr = pController->InitDirectInput(hMsgWnd);
        if (FAILED(hr)) PrintLog("[PAD%d] Fail with 0x%08X", dwUserIndex + 1, hr);

        if (SUCCEEDED(hr))
        {
            PrintLog("[PAD%d] Done", dwUserIndex + 1);
            if (g_bInitBeep) MessageBeep(MB_OK);
        }
    }

    if (!pController->Initalized())
        return ERROR_DEVICE_NOT_CONNECTED;
    else return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetState(__in DWORD dwUserIndex, __out XINPUT_STATE* pState)
{
    //PrintLog("XInputGetState");

    Controller* pController = nullptr;
    if (!pState)
        return ERROR_BAD_ARGUMENTS;
    u32 initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetState(dwUserIndex, pState);
    else if (initFlag)
        return initFlag;

    HRESULT hr = E_FAIL;
    //Update device state if enabled or we not use enable
    if (XInputIsEnabled.bEnabled || !XInputIsEnabled.bUseEnabled)
        hr = pController->UpdateState();
    else return ERROR_SUCCESS;

#ifdef _DEBUG
    PrintLog("UpdateState %u %u", dwUserIndex, hr);
#endif

    if (FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

    const DIJOYSTATE2& state = pController->GetState();

    pState->Gamepad.wButtons = 0;
    pState->Gamepad.bLeftTrigger = 0;
    pState->Gamepad.bRightTrigger = 0;
    pState->Gamepad.sThumbLX = 0;
    pState->Gamepad.sThumbLY = 0;
    pState->Gamepad.sThumbRX = 0;
    pState->Gamepad.sThumbRY = 0;

    if (!XInputIsEnabled.bEnabled && XInputIsEnabled.bUseEnabled) return ERROR_SUCCESS;

    // timestamp packet
    pState->dwPacketNumber = GetTickCount();

    Mapping* pMapping = &pController->m_mapping;

    // --- Map buttons ---
    for (u32 i = 0; i < _countof(pMapping->Button); ++i)
    {
        if (pController->ButtonPressed(pMapping->Button[i]))
            pState->Gamepad.wButtons |= buttonIDs[i];
    }

    // --- Map POV to the D-pad ---
    if (pMapping->DpadPOV > 0 && pMapping->PovIsButton == false)
    {
        //INT pov = POVState(pMapping->DpadPOV,dwUserIndex,Gamepad[dwUserIndex].povrotation);

        int povdeg = state.rgdwPOV[pMapping->DpadPOV - 1];
        if (povdeg >= 0)
        {
            // Up-left, up, up-right, up (at 360 degrees)
            if (IN_RANGE2(povdeg, pMapping->pov[GAMEPAD_DPAD_LEFT] + 1, pMapping->pov[GAMEPAD_DPAD_UP]) || IN_RANGE2(povdeg, 0, pMapping->pov[GAMEPAD_DPAD_RIGHT] - 1))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

            // Up-right, right, down-right
            if (IN_RANGE(povdeg, 0, pMapping->pov[GAMEPAD_DPAD_DOWN]))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

            // Down-right, down, down-left
            if (IN_RANGE(povdeg, pMapping->pov[GAMEPAD_DPAD_RIGHT], pMapping->pov[GAMEPAD_DPAD_LEFT]))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;

            // Down-left, left, up-left
            if (IN_RANGE(povdeg, pMapping->pov[GAMEPAD_DPAD_DOWN], pMapping->pov[GAMEPAD_DPAD_UP]))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
        }
    }
    else if (pMapping->PovIsButton == true)
    {
        for (int i = 0; i < _countof(pMapping->pov); ++i)
        {
            if (pController->ButtonPressed(pMapping->pov[i]))
            {
                pState->Gamepad.wButtons |= povIDs[i];
            }
        }
    }

    // Created so we can refer to each axis with an ID
    s32 axis[] =
    {
        state.lX,
        state.lY,
        state.lZ,
        state.lRx,
        state.lRy,
        state.lRz
    };

    s32 slider[] =
    {
        state.rglSlider[0],
        state.rglSlider[1]
    };

    // --- Map triggers ---
    u8 *targetTrigger[] =
    {
        &pState->Gamepad.bLeftTrigger,
        &pState->Gamepad.bRightTrigger
    };

    for (u32 i = 0; i < _countof(pMapping->Trigger); ++i)
    {
        MappingType triggerType = pMapping->Trigger[i].type;

        if (triggerType == DIGITAL)
        {
            if (pController->ButtonPressed(pMapping->Trigger[i].id - 1))*(targetTrigger[i]) = 255;
        }
        else
        {
            s32 *values;

            switch (triggerType)
            {
                case AXIS:
                case HAXIS:
                case CBUT:
                    values = axis;
                    break;

                case SLIDER:
                case HSLIDER:
                    values = slider;
                    break;

                default:
                    values = axis;
                    break;
            }

            s32 v = 0;

            if (pMapping->Trigger[i].id > 0) v = values[pMapping->Trigger[i].id - 1];
            else if (pMapping->Trigger[i].id < 0) v = -values[-pMapping->Trigger[i].id - 1] - 1;

            /* FIXME: axis negative max should be -32768
            --- v is the full range (-32768 .. +32767) that should be projected to 0...255

            --- Full ranges
            AXIS:	(	0 to 255 from -32768 to 32767) using axis
            SLIDER:	(	0 to 255 from -32768 to 32767) using slider
            ------------------------------------------------------------------------------
            --- Half ranges
            HAXIS:	(	0 to 255 from 0 to 32767) using axis
            HSLIDER:	(	0 to 255 from 0 to 32767) using slider
            */

            s32 v2 = 0;
            s32 offset = 0;
            s32 scaling = 1;

            switch (triggerType)
            {
                // Full range
                case AXIS:
                case SLIDER:
                    scaling = 255;
                    offset = 32767;
                    break;

                    // Half range
                case HAXIS:
                case HSLIDER:
                case CBUT: // add /////////////////////////////////////////////////////////
                    scaling = 127;
                    offset = 0;
                    break;

                default:
                    scaling = 1;
                    offset = 0;
                    break;
            }

            //v2 = (v + offset) / scaling;
            // Add deadzones
            //*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, pController->triggerdz, 255);

            /////////////////////////////////////////////////////////////////////////////////////////
            if (triggerType == CBUT)
            {

                if (pController->ButtonPressed(pMapping->Trigger[0].but)
                    && pController->ButtonPressed(pMapping->Trigger[1].but))
                {
                    *(targetTrigger[0]) = 255;
                    *(targetTrigger[1]) = 255;
                }

                if (pController->ButtonPressed(pMapping->Trigger[0].but)
                    && !pController->ButtonPressed(pMapping->Trigger[1].but))
                {
                    v2 = (offset - v) / scaling;
                    *(targetTrigger[0]) = 255;
                    *(targetTrigger[1]) = 255 - (u8)deadzone(v2, 0, 255, pMapping->Trigger[1].triggerdz, 255);
                }

                if (!pController->ButtonPressed(pMapping->Trigger[0].but)
                    && pController->ButtonPressed(pMapping->Trigger[1].but))
                {
                    v2 = (offset + v) / scaling;
                    *(targetTrigger[0]) = 255 - (u8)deadzone(v2, 0, 255, pMapping->Trigger[0].triggerdz, 255);
                    *(targetTrigger[1]) = 255;
                }

                if (!pController->ButtonPressed(pMapping->Trigger[0].but)
                    && !pController->ButtonPressed(pMapping->Trigger[1].but))
                {
                    v2 = (offset + v) / scaling;
                    *(targetTrigger[i]) = (u8)deadzone(v2, 0, 255, pMapping->Trigger[0].triggerdz, 255);
                }

            }
            else
            {
                v2 = (offset + v) / scaling;
                *(targetTrigger[i]) = (u8)deadzone(v2, 0, 255, pMapping->Trigger[0].triggerdz, 255);
            }

            /////////////////////////////////////////////////////////////////////////////////////////
        }
    }

    // --- Map thumbsticks ---

    // Created so we can refer to each axis with an ID
    SHORT *targetAxis[4] =
    {
        &pState->Gamepad.sThumbLX,
        &pState->Gamepad.sThumbLY,
        &pState->Gamepad.sThumbRX,
        &pState->Gamepad.sThumbRY
    };

    for (u32 i = 0; i < _countof(pMapping->Axis); ++i)
    {
        if (pMapping->Axis[i].axistodpad == 0)
        {
            s32 *values = axis;

            // Analog input
            if (pMapping->Axis[i].analogType == AXIS) values = axis;

            if (pMapping->Axis[i].analogType == SLIDER) values = slider;

            if (pMapping->Axis[i].analogType != NONE)
            {

                if (pMapping->Axis[i].id > 0)
                {
                    s32 val = (s32)values[pMapping->Axis[i].id - 1];
                    *(targetAxis[i]) = (s16)clamp(val, -32768, 32767);
                }
                else if (pMapping->Axis[i].id < 0)
                {
                    s32 val = (s32)(-1 - values[-pMapping->Axis[i].id - 1]);
                    *(targetAxis[i]) = (s16)clamp(val, -32768, 32767);
                }
            }

            // Digital input, positive direction
            if (pMapping->Axis[i].hasDigital && pMapping->Axis[i].positiveButtonID >= 0)
            {
                if (pController->ButtonPressed(pMapping->Axis[i].positiveButtonID))
                    *(targetAxis[i]) = 32767;
            }

            // Digital input, negative direction
            if (pMapping->Axis[i].hasDigital && pMapping->Axis[i].negativeButtonID >= 0)
            {
                if (pController->ButtonPressed(pMapping->Axis[i].negativeButtonID))
                    *(targetAxis[i]) = -32768;
            }
        }
        else
        {
            //PrintLog("x: %d, y: %d, z: %d",Gamepad[dwUserIndex].state.lX,Gamepad[dwUserIndex].state.lY,Gamepad[dwUserIndex].state.lZ);

            if (state.lX - pMapping->Axis[i].a2doffset > pMapping->Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;

            if (state.lX - pMapping->Axis[i].a2doffset < -pMapping->Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

            if (state.lY - pMapping->Axis[i].a2doffset < -pMapping->Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

            if (state.lY - pMapping->Axis[i].a2doffset > pMapping->Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
        }

        //        [ 32768 steps | 32768 steps ]
        // DInput [ 0     32767 | 32768 65535 ] 
        // XInput [ 32768    -1 | 0     32767 ]
        //
        //int xInput = dInputValue;
        s32 xInput = *(targetAxis[i]);
        s32 deadZone = (s32)pMapping->Axis[i].axisdeadzone;
        s32 antiDeadZone = (s32)pMapping->Axis[i].antideadzone;
        s32 linear = (s32)pMapping->Axis[i].axislinear;
        s32 min = -32768;
        s32 max = 32767;
        // If deadzone value is set then...
        bool invert = xInput < 0;
        // Convert [-32768;-1] -> [32767;0]
        if (invert) xInput = -1 - xInput;
        //if  invert 
        if (deadZone > 0)
        {
            if (xInput > deadZone)
            {
                // [deadZone;32767] => [0;32767];
                xInput = (s32)((float)(xInput - deadZone) / (float)(max - deadZone) * (float)max);
            }
            else
            {
                xInput = 0;
            }
        }
        // If anti-deadzone value is set then...
        if (antiDeadZone > 0)
        {
            if (xInput > 0)
            {
                // [0;32767] => [antiDeadZone;32767];
                xInput = (s32)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
            }
        }
        // If linear value is set then...
        if (linear != 0 && xInput > 0)
        {
            // [antiDeadZone;32767] => [0;32767];
            float xInputF = (float)(xInput - antiDeadZone) / (float)(max - antiDeadZone) * (float)max;
            float linearF = (float)linear / 100.f;
            xInputF = ConvertToFloat((short)xInputF);
            float x = -xInputF;
            if (linearF < 0.f) x = 1.f + x;
            float v = ((float)sqrt(1.f - x * x));
            if (linearF < 0.f) v = 1.f - v;
            xInputF = xInputF + (2.f - v - xInputF - 1.f) * abs(linearF);
            xInput = ConvertToShort(xInputF);
            // [0;32767] => [antiDeadZone;32767];
            xInput = (s32)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
        }
        // Convert [32767;0] -> [-32768;-1]
        if (invert) xInput = -1 - xInput;
        *(targetAxis[i]) = (s16)clamp(xInput, min, max);
        //return (short)xInput;
    }
    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputSetState(__in DWORD dwUserIndex, __in XINPUT_VIBRATION* pVibration)
{
    Controller* pController = nullptr;
    if (!pVibration)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputSetState(dwUserIndex, pVibration);
    else if (initFlag)
        return initFlag;

    if (!pController->m_useforce || !pController->m_pForceFeedback)
        return ERROR_SUCCESS;

    WORD wLeftMotorSpeed = 0;
    WORD wRightMotorSpeed = 0;

    if (XInputIsEnabled.bEnabled || !XInputIsEnabled.bUseEnabled)
    {
        WORD left = static_cast<WORD>(pVibration->wLeftMotorSpeed * pController->m_pForceFeedback->m_ForcePercent);
        WORD right = static_cast<WORD>(pVibration->wRightMotorSpeed * pController->m_pForceFeedback->m_ForcePercent);
        wLeftMotorSpeed = pController->m_pForceFeedback->m_SwapMotors ? right : left;
        wRightMotorSpeed = pController->m_pForceFeedback->m_SwapMotors ? left : right;
    }

    pController->m_pForceFeedback->SetState(wLeftMotorSpeed, wRightMotorSpeed);
    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(__in DWORD dwUserIndex, __in DWORD dwFlags, __out XINPUT_CAPABILITIES* pCapabilities)
{
    Controller* pController = nullptr;
    if (!pCapabilities || dwFlags != XINPUT_FLAG_GAMEPAD)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
    else if (initFlag)
        return initFlag;

    pCapabilities->Type = 0;
    pCapabilities->SubType = pController->m_gamepadtype; //customizable subtype
    pCapabilities->Flags = 0; // we do not support sound
    pCapabilities->Vibration.wLeftMotorSpeed = pCapabilities->Vibration.wRightMotorSpeed = 0xFF;
    pCapabilities->Gamepad.bLeftTrigger = pCapabilities->Gamepad.bRightTrigger = 0xFF;

    pCapabilities->Gamepad.sThumbLX = (SHORT)-64;
    pCapabilities->Gamepad.sThumbLY = (SHORT)-64;
    pCapabilities->Gamepad.sThumbRX = (SHORT)-64;
    pCapabilities->Gamepad.sThumbRY = (SHORT)-64;
    pCapabilities->Gamepad.wButtons = (WORD)0xF3FF;

    return ERROR_SUCCESS;
}

extern "C" VOID WINAPI XInputEnable(__in BOOL enable)
{
    if (g_bDisable) return;

    if (hMsgWnd == NULL)
        CreateMsgWnd();

    // If any controller is native XInput then use state too.
    for (auto it = g_Controllers.begin(); it != g_Controllers.end(); ++it)
    {
        if (it->m_passthrough)
            xinput.XInputEnable(enable);
    }

    /*
    Trick to support XInputEnable states, because not every game calls it, so:
    - must support games that call it:
    if bEnabled is FALSE and bUseEnabled is TRUE = device is disabled -> return S_OK, ie. connected but state not updating
    if bEnabled is TRUE and bUseEnabled is TRUE = device is enabled -> continue, ie. connected and updating state
    - must support games that not call it:
    if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states
    */

    XInputIsEnabled.bEnabled = (enable != 0);
    XInputIsEnabled.bUseEnabled = true;

    if (enable) PrintLog("XInput Enabled");
    else PrintLog("XInput Disabled");

}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids(__in DWORD dwUserIndex, __out GUID* pDSoundRenderGuid, __out GUID* pDSoundCaptureGuid)
{
    Controller* pController = nullptr;
    if (!pDSoundRenderGuid || !pDSoundCaptureGuid)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetDSoundAudioDeviceGuids(dwUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    *pDSoundRenderGuid = GUID_NULL;
    *pDSoundCaptureGuid = GUID_NULL;

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(__in DWORD  dwUserIndex, __in BYTE devType, __out XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
    Controller* pController = nullptr;
    if (!pBatteryInformation)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetBatteryInformation(dwUserIndex, devType, pBatteryInformation);
    else if (initFlag)
        return initFlag;

    // Report a wired controller
    XINPUT_BATTERY_INFORMATION &xBatInfo = *pBatteryInformation;
    xBatInfo.BatteryLevel = BATTERY_LEVEL_FULL;
    xBatInfo.BatteryType = BATTERY_TYPE_WIRED;

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetKeystroke(__in DWORD dwUserIndex, __in DWORD dwReserved, __out XINPUT_KEYSTROKE* pKeystroke)
{
    Controller* pController = nullptr;
    if (!pKeystroke)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetKeystroke(dwUserIndex, dwReserved, pKeystroke);
    else if (initFlag)
        return initFlag;

    XINPUT_STATE xstate;
    ZeroMemory(&xstate, sizeof(XINPUT_STATE));
    XInputGetState(dwUserIndex, &xstate);

    static WORD repeat[14];
    static WORD flags[14];

    pKeystroke->UserIndex = (BYTE)dwUserIndex;
    pKeystroke->Unicode = NULL;
    pKeystroke->HidCode = NULL;

    static const WORD allButtonIDs[14] =
    {
        XINPUT_GAMEPAD_A,
        XINPUT_GAMEPAD_B,
        XINPUT_GAMEPAD_X,
        XINPUT_GAMEPAD_Y,
        XINPUT_GAMEPAD_LEFT_SHOULDER,
        XINPUT_GAMEPAD_RIGHT_SHOULDER,
        XINPUT_GAMEPAD_BACK,
        XINPUT_GAMEPAD_START,
        XINPUT_GAMEPAD_LEFT_THUMB,
        XINPUT_GAMEPAD_RIGHT_THUMB,
        XINPUT_GAMEPAD_DPAD_UP,
        XINPUT_GAMEPAD_DPAD_DOWN,
        XINPUT_GAMEPAD_DPAD_LEFT,
        XINPUT_GAMEPAD_DPAD_RIGHT
    };

    static const u16 keyIDs[14] =
    {
        VK_PAD_A,
        VK_PAD_B,
        VK_PAD_X,
        VK_PAD_Y,
        VK_PAD_LSHOULDER,
        VK_PAD_RSHOULDER,
        VK_PAD_BACK,
        VK_PAD_START,
        VK_PAD_LTHUMB_PRESS,
        VK_PAD_RTHUMB_PRESS,
        VK_PAD_DPAD_UP,
        VK_PAD_DPAD_DOWN,
        VK_PAD_DPAD_LEFT,
        VK_PAD_DPAD_RIGHT
    };

    for (int i = 0; i < 14; i++)
    {
        if (xstate.Gamepad.wButtons & allButtonIDs[i])
        {
            if (flags[i] == NULL)
            {
                pKeystroke->VirtualKey = keyIDs[i];
                pKeystroke->Flags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN;
                break;
            }
            if ((flags[i] & XINPUT_KEYSTROKE_KEYDOWN))
            {
                if (repeat[i] <= 0)
                {
                    repeat[i] = 5;
                    pKeystroke->VirtualKey = keyIDs[i];
                    pKeystroke->Flags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN | XINPUT_KEYSTROKE_REPEAT;
                    break;
                }
                else
                {
                    repeat[i]--;
                    continue;
                }
            }
        }
        if (!(xstate.Gamepad.wButtons & allButtonIDs[i]))
        {
            if (flags[i] & XINPUT_KEYSTROKE_KEYDOWN)
            {
                repeat[i] = 5 * 4;
                pKeystroke->VirtualKey = keyIDs[i];
                pKeystroke->Flags = flags[i] = XINPUT_KEYSTROKE_KEYUP;
                break;
            }
            if (flags[i] & XINPUT_KEYSTROKE_KEYUP)
            {
                pKeystroke->Flags = flags[i] = NULL;
                break;
            }
        }
    }

    //PrintLog("ret: %u, flags: %u, hid: %u, unicode: %c, user: %u, vk: 0x%X",ret,pKeystroke->Flags,pKeystroke->HidCode,pKeystroke->Unicode,pKeystroke->UserIndex,pKeystroke->VirtualKey);

    if (pKeystroke->VirtualKey)
        return ERROR_SUCCESS;
    else
        return ERROR_EMPTY;
}

//undocumented
extern "C" DWORD WINAPI XInputGetStateEx(__in DWORD dwUserIndex, __out XINPUT_STATE *pState)
{
    Controller* pController = nullptr;
    if (!pState)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetStateEx(dwUserIndex, pState);
    else if (initFlag)
        return initFlag;

    Mapping* pMapping = &pController->m_mapping;

    if (pMapping->guide && pController->ButtonPressed(pMapping->guide))
        pState->Gamepad.wButtons |= 0x400;

    //PrintLog("XInputGetStateEx %u",xstate.Gamepad.wButtons);

    return XInputGetState(dwUserIndex, pState);
}

extern "C" DWORD WINAPI XInputWaitForGuideButton(__in DWORD dwUserIndex, __in DWORD dwFlag, __inout LPVOID pVoid)
{
    Controller* pController = nullptr;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputWaitForGuideButton(dwUserIndex, dwFlag, pVoid);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputCancelGuideButtonWait(__in DWORD dwUserIndex)
{
    Controller* pController = nullptr;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputCancelGuideButtonWait(dwUserIndex);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputPowerOffController(__in DWORD dwUserIndex)
{
    Controller* pController = nullptr;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputPowerOffController(dwUserIndex);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetAudioDeviceIds(__in DWORD dwUserIndex, __in LPWSTR pRenderDeviceId, __inout UINT* pRenderCount, __in LPWSTR pCaptureDeviceId, __inout UINT* pCaptureCount)
{
    Controller* pController = nullptr;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetAudioDeviceIds(dwUserIndex, pRenderDeviceId, pRenderCount, pCaptureDeviceId, pCaptureCount);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBaseBusInformation(__in DWORD dwUserIndex, __out struct XINPUT_BUSINFO* pBusinfo)
{
    Controller* pController = nullptr;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetBaseBusInformation(dwUserIndex, pBusinfo);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

// XInput 1.4 uses this in XInputGetCapabilities and calls memcpy(pCapabilities, &CapabilitiesEx, 20u);
// so XINPUT_CAPABILITIES is first 20 bytes of XINPUT_CAPABILITIESEX
extern "C" DWORD WINAPI XInputGetCapabilitiesEx(__inout DWORD unk1, __in DWORD dwUserIndex, __in DWORD dwFlags, __out struct XINPUT_CAPABILITIESEX* pCapabilitiesEx)
{
    Controller* pController = nullptr;
    DWORD initFlag = DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return xinput.XInputGetCapabilitiesEx(unk1, dwUserIndex, dwFlags, pCapabilitiesEx);
    else if (initFlag)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}