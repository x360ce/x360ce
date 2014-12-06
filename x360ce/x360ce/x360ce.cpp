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

#include "InputHook.h"
#include "Config.h"

#include "ForceFeedback.h"
#include "Controller.h"

#include "XInputModuleManager.h"

extern "C" DWORD WINAPI XInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
    //PrintLog("XInputGetState");

    Controller* pController = nullptr;
    if (!pState)
        return ERROR_BAD_ARGUMENTS;
    u32 initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetState(dwUserIndex, pState);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    return pController->GetState(pState);
}

extern "C" DWORD WINAPI XInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
    Controller* pController = nullptr;
    if (!pVibration)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputSetState(dwUserIndex, pVibration);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    if (!pController->m_useforce || !pController->m_pForceFeedback)
        return ERROR_SUCCESS;

    pController->m_pForceFeedback->SetState(pVibration);
    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
    Controller* pController = nullptr;
    if (!pCapabilities || dwFlags != XINPUT_FLAG_GAMEPAD)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetCapabilities(dwUserIndex, dwFlags, pCapabilities);
    else if (initFlag != ERROR_SUCCESS)
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

extern "C" VOID WINAPI XInputEnable(BOOL enable)
{
    // If any controller is native XInput then use state too.
    for (auto it = ControllerManager::Get().GetControllers().begin(); it != ControllerManager::Get().GetControllers().end(); ++it)
    {
        if (it->m_passthrough)
            XInputModuleManager::Get().XInputEnable(enable);
    }

    ControllerManager::Get().XInputEnable(enable);
}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
    Controller* pController = nullptr;
    if (!pDSoundRenderGuid || !pDSoundCaptureGuid)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetDSoundAudioDeviceGuids(dwUserIndex, pDSoundRenderGuid, pDSoundCaptureGuid);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    *pDSoundRenderGuid = GUID_NULL;
    *pDSoundCaptureGuid = GUID_NULL;

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(DWORD dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
    Controller* pController = nullptr;
    if (!pBatteryInformation)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetBatteryInformation(dwUserIndex, devType, pBatteryInformation);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    // Report a wired controller
    XINPUT_BATTERY_INFORMATION &xBatInfo = *pBatteryInformation;
    xBatInfo.BatteryLevel = BATTERY_LEVEL_FULL;
    xBatInfo.BatteryType = BATTERY_TYPE_WIRED;

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke)
{
    Controller* pController = nullptr;
    if (!pKeystroke)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetKeystroke(dwUserIndex, dwReserved, pKeystroke);
    else if (initFlag != ERROR_SUCCESS)
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
extern "C" DWORD WINAPI XInputGetStateEx(DWORD dwUserIndex, XINPUT_STATE *pState)
{
    Controller* pController = nullptr;
    if (!pState)
        return ERROR_BAD_ARGUMENTS;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetStateEx(dwUserIndex, pState);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    //PrintLog("XInputGetStateEx %u",xstate.Gamepad.wButtons);

    return pController->GetState(pState);
}

extern "C" DWORD WINAPI XInputWaitForGuideButton(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid)
{
    Controller* pController = nullptr;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputWaitForGuideButton(dwUserIndex, dwFlag, pVoid);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputCancelGuideButtonWait(DWORD dwUserIndex)
{
    Controller* pController = nullptr;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputCancelGuideButtonWait(dwUserIndex);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputPowerOffController(DWORD dwUserIndex)
{
    Controller* pController = nullptr;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputPowerOffController(dwUserIndex);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetAudioDeviceIds(DWORD dwUserIndex, LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount)
{
    Controller* pController = nullptr;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetAudioDeviceIds(dwUserIndex, pRenderDeviceId, pRenderCount, pCaptureDeviceId, pCaptureCount);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBaseBusInformation(DWORD dwUserIndex, struct XINPUT_BUSINFO* pBusinfo)
{
    Controller* pController = nullptr;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetBaseBusInformation(dwUserIndex, pBusinfo);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}

// XInput 1.4 uses this in XInputGetCapabilities and calls memcpy(pCapabilities, &CapabilitiesEx, 20u);
// so XINPUT_CAPABILITIES is first 20 bytes of XINPUT_CAPABILITIESEX
extern "C" DWORD WINAPI XInputGetCapabilitiesEx(DWORD unk1 /*seems that only 1 is valid*/, DWORD dwUserIndex, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx)
{
    Controller* pController = nullptr;
    DWORD initFlag = ControllerManager::Get().DeviceInitialize(dwUserIndex, &pController);
    if (initFlag == PASSTROUGH)
        return XInputModuleManager::Get().XInputGetCapabilitiesEx(unk1, dwUserIndex, dwFlags, pCapabilitiesEx);
    else if (initFlag != ERROR_SUCCESS)
        return initFlag;

    PrintLog("Call to unimplemented function "__FUNCTION__);

    return ERROR_SUCCESS;
}