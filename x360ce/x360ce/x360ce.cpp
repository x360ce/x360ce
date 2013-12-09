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
#include "Utilities\Misc.h"
#include "x360ce.h"
#include "Config.h"
#include "Log.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"

extern std::vector<Mapping> g_Mappings;
extern std::vector<DInputDevice> g_Devices;
extern DWORD startProcessId;
extern DWORD startThreadId;
extern iHook* pHooks;
extern bool g_bNative;
extern bool g_bInitBeep;
extern bool g_bDisable;

XInputEnabled XInputIsEnabled;
WNDPROC oldWndProc = NULL;
HWND hMsgWnd = NULL;

LRESULT CALLBACK WndProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch ( uMsg )
    {
    case WM_DESTROY:
        if (InSendMessage())
            ReplyMessage(TRUE);
        else
        {
            g_Devices.clear();
            g_Mappings.clear();

            SAFE_DELETE(pHooks);
            if(hNative)
            {
                PrintLog(LOG_CORE,"Unloading %s",ModuleFullPathA(hNative).c_str());
                FreeLibrary(hNative);
                hNative = NULL;
            }
        }
        SetWindowLongPtr(hWnd, GWLP_WNDPROC, (LONG_PTR) oldWndProc);
        break;
    }
    return CallWindowProc(oldWndProc, hWnd, uMsg, wParam, lParam);
}

VOID CreateMsgWnd()
{
    hMsgWnd = CreateWindow(
                  L"Message",	// name of window class
                  L"x360ce",			// title-bar std::string
                  WS_TILED,			// normal window
                  CW_USEDEFAULT,		// default horizontal position
                  CW_USEDEFAULT,		// default vertical position
                  CW_USEDEFAULT,		// default width
                  CW_USEDEFAULT,		// default height
                  HWND_MESSAGE,		// message-only window
                  NULL,				// no class menu
                  hThis,	// handle to application instance
                  NULL);				// no window-creation data

    if(!hMsgWnd) PrintLog(LOG_CORE,"CreateWindow failed with code 0x%x", HRESULT_FROM_WIN32(GetLastError()));
    else oldWndProc = (WNDPROC) SetWindowLongPtr(hMsgWnd,GWLP_WNDPROC,(LONG_PTR) WndProc);
}

// XInput 1.3 function pointers
XInputGetState_t nXInputGetState = NULL;
XInputSetState_t nXInputSetState = NULL;
XInputGetCapabilities_t nXInputGetCapabilities = NULL;
XInputEnable_t nXInputEnable = NULL;
XInputGetDSoundAudioDeviceGuids_t nXInputGetDSoundAudioDeviceGuids = NULL;
XInputGetBatteryInformation_t nXInputGetBatteryInformation = NULL;
XInputGetKeystroke_t nXInputGetKeystroke = NULL;

// XInput 1.3 undocumented function pointers
XInputGetStateEx_t nXInputGetStateEx = NULL;
XInputWaitForGuideButton_t nXInputWaitForGuideButton = NULL;
XInputCancelGuideButtonWait_t nXInputCancelGuideButtonWait = NULL;
XInputPowerOffController_t nXInputPowerOffController = NULL;

// XInput 1.4 function pointers
XInputGetAudioDeviceIds_t nXInputGetAudioDeviceIds = NULL;

// XInput 1.4 undocumented function pointers
XInputGetBaseBusInformation_t nXInputGetBaseBusInformation = NULL;
XInputGetCapabilitiesEx_t nXInputGetCapabilitiesEx = NULL;

bool XInputInitialize()
{
    if(hNative) return true;
    WCHAR buffer[MAX_PATH];
    GetSystemDirectoryW(buffer,MAX_PATH);

    std::wstring str(buffer);
    str.append(L"\\");
    str.append(ModuleFileNameW(hThis));

    bool bHookLL = false;
    if(pHooks)
    {
        bHookLL = pHooks->GetState(iHook::HOOK_LL);
        if(bHookLL) pHooks->DisableHook(iHook::HOOK_LL);
    }

    PrintLog(LOG_CORE,"Loading %ls",str.c_str());
    hNative = LoadLibraryW(str.c_str());
    if(bHookLL) pHooks->EnableHook(iHook::HOOK_LL);

    if (!hNative)
    {
        HRESULT hr = GetLastError();
        swprintf_s(buffer,L"Cannot load %s error: 0x%x", str.c_str(), hr);
        PrintLog(LOG_CORE,"%s", buffer);
        MessageBoxW(NULL,buffer,L"Error",MB_ICONERROR);
        ExitProcess(hr);
    }

    nXInputGetState = (XInputGetState_t)GetProcAddress(hNative,"XInputGetState");
    nXInputSetState = (XInputSetState_t)GetProcAddress(hNative,"XInputSetState");
    nXInputGetCapabilities = (XInputGetCapabilities_t)GetProcAddress(hNative,"XInputGetCapabilities");
    nXInputEnable = (XInputEnable_t)GetProcAddress(hNative,"XInputEnable");
    nXInputGetDSoundAudioDeviceGuids = (XInputGetDSoundAudioDeviceGuids_t)GetProcAddress(hNative,"XInputGetDSoundAudioDeviceGuids");
    nXInputGetBatteryInformation = (XInputGetBatteryInformation_t)GetProcAddress(hNative,"XInputGetBatteryInformation");
    nXInputGetKeystroke = (XInputGetKeystroke_t)GetProcAddress(hNative,"XInputGetKeystroke");

    nXInputGetStateEx = (XInputGetStateEx_t)GetProcAddress(hNative,(LPCSTR)100);
    nXInputWaitForGuideButton = (XInputWaitForGuideButton_t)GetProcAddress(hNative,(LPCSTR)101);
    nXInputCancelGuideButtonWait = (XInputCancelGuideButtonWait_t)GetProcAddress(hNative,(LPCSTR)102);
    nXInputPowerOffController = (XInputPowerOffController_t)GetProcAddress(hNative,(LPCSTR)103);

    nXInputGetAudioDeviceIds = (XInputGetAudioDeviceIds_t)GetProcAddress(hNative,"XInputGetAudioDeviceIds");
    nXInputGetBaseBusInformation = (XInputGetBaseBusInformation_t)GetProcAddress(hNative,(LPCSTR)104);
    nXInputGetCapabilitiesEx = (XInputGetCapabilitiesEx_t)GetProcAddress(hNative,(LPCSTR)108);

    return true;
}

static void DeviceInitialize(DInputDevice& device)
{
    PrintLog(LOG_CORE,"[PAD%d] Starting",device.dwUserIndex+1);
    PrintLog(LOG_CORE,"[PAD%d] Initializing as UserIndex %d",device.dwUserIndex+1,device.dwUserIndex);

    HRESULT hr = E_FAIL;
    hr = InitDirectInput(hMsgWnd,device);
    if(FAILED(hr)) PrintLog(LOG_CORE,"[PAD%d] Fail with 0x%08X",device.dwUserIndex+1,hr);

    if(SUCCEEDED(hr)) 
    {
        PrintLog(LOG_CORE,"[PAD%d] Done",device.dwUserIndex+1);
        if(g_bInitBeep) MessageBeep(MB_OK);
    }
}

extern "C" DWORD WINAPI XInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
    //PrintLog(LOG_XINPUT,"XInputGetState");
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetState(dwUserIndex,pState);

    DInputDevice& device = g_Devices[dwUserIndex];
    if (!pState || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

    HRESULT hr = E_FAIL;

    if(hMsgWnd == NULL) CreateMsgWnd();

    if(device.device == NULL && device.dwUserIndex == dwUserIndex)
        DeviceInitialize(device);
    if(!device.device) return ERROR_DEVICE_NOT_CONNECTED;

    //Update device state if enabled or we not use enable
    if(XInputIsEnabled.bEnabled || !XInputIsEnabled.bUseEnabled)
        hr = UpdateState(device);

#if defined(DEBUG) | defined(_DEBUG)
    PrintLog(LOG_XINPUT,"UpdateState %d %d",dwUserIndex,hr);
#endif

    if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

    Mapping& mapping = g_Mappings[dwUserIndex];
    XINPUT_STATE& xstate = *pState;

    xstate.Gamepad.wButtons = 0;
    xstate.Gamepad.bLeftTrigger = 0;
    xstate.Gamepad.bRightTrigger = 0;
    xstate.Gamepad.sThumbLX = 0;
    xstate.Gamepad.sThumbLY = 0;
    xstate.Gamepad.sThumbRX = 0;
    xstate.Gamepad.sThumbRY = 0;

    if(!XInputIsEnabled.bEnabled && XInputIsEnabled.bUseEnabled) return ERROR_SUCCESS;

    // timestamp packet
    xstate.dwPacketNumber=GetTickCount();

    // --- Map buttons ---
    for (int i = 0; i < 10; ++i)
    {
        if (((int)mapping.Button[i] >= 0) && ButtonPressed(mapping.Button[i],device))
            xstate.Gamepad.wButtons |= buttonIDs[i];
    }

    // --- Map POV to the D-pad ---
    if (mapping.DpadPOV > 0 && mapping.PovIsButton == false)
    {
        //INT pov = POVState(mapping.DpadPOV,dwUserIndex,Gamepad[dwUserIndex].povrotation);

        int povdeg = device.state.rgdwPOV[mapping.DpadPOV-1];
        if(povdeg >= 0)
        {
            // Up-left, up, up-right, up (at 360 degrees)
            if (IN_RANGE2(povdeg,mapping.pov[GAMEPAD_DPAD_LEFT]+1,mapping.pov[GAMEPAD_DPAD_UP]) || IN_RANGE2(povdeg,0,mapping.pov[GAMEPAD_DPAD_RIGHT]-1))
                xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

            // Up-right, right, down-right
            if (IN_RANGE(povdeg,0,mapping.pov[GAMEPAD_DPAD_DOWN]))
                xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

            // Down-right, down, down-left
            if (IN_RANGE(povdeg,mapping.pov[GAMEPAD_DPAD_RIGHT],mapping.pov[GAMEPAD_DPAD_LEFT]))
                xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;

            // Down-left, left, up-left
            if (IN_RANGE(povdeg,mapping.pov[GAMEPAD_DPAD_DOWN],mapping.pov[GAMEPAD_DPAD_UP]))
                xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
        }
    }
    else if(mapping.PovIsButton == true)
    {
        for (int i = 0; i < 4; ++i)
        {
            if (((int)mapping.pov[i] >= 0) && ButtonPressed(mapping.pov[i],device))
            {
                xstate.Gamepad.wButtons |= povIDs[i];
            }
        }
    }

    // Created so we can refer to each axis with an ID
    LONG axis[] =
    {
        device.state.lX,
        device.state.lY,
        device.state.lZ,
        device.state.lRx,
        device.state.lRy,
        device.state.lRz,
        0
    };
    LONG slider[] =
    {
        device.state.rglSlider[0],
        device.state.rglSlider[1]
    };

    // --- Map triggers ---
    BYTE *targetTrigger[] =
    {
        & xstate.Gamepad.bLeftTrigger,
        & xstate.Gamepad.bRightTrigger
    };

    for (size_t i = 0; i < 2; ++i)
    {
        MappingType triggerType = mapping.Trigger[i].type;

        if (triggerType == DIGITAL)
        {
            if(ButtonPressed(mapping.Trigger[i].id,device))*(targetTrigger[i]) = 255;
        }
        else
        {
            LONG *values;

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

            LONG v = 0;

            if(mapping.Trigger[i].id > 0) v = values[mapping.Trigger[i].id -1];
            else v = -values[-mapping.Trigger[i].id -1] - 1;

            /* FIXME: axis negative max should be -32768
            --- v is the full range (-32767 .. +32767) that should be projected to 0...255

            --- Full ranges
            AXIS:	(	0 to 255 from -32767 to 32767) using axis
            SLIDER:	(	0 to 255 from -32767 to 32767) using slider
            ------------------------------------------------------------------------------
            --- Half ranges
            HAXIS:	(	0 to 255 from 0 to 32767) using axis
            HSLIDER:	(	0 to 255 from 0 to 32767) using slider
            */

            LONG v2=0;
            LONG offset=0;
            LONG scaling=1;

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
            //*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, device.triggerdz, 255);

            /////////////////////////////////////////////////////////////////////////////////////////
            if (triggerType == CBUT)
            {

                if (ButtonPressed(mapping.Trigger[0].but,device)
                        && ButtonPressed(mapping.Trigger[1].but,device))
                {
                    *(targetTrigger[0]) = 255;
                    *(targetTrigger[1]) = 255;
                }

                if (ButtonPressed(mapping.Trigger[0].but,device)
                        && !ButtonPressed(mapping.Trigger[1].but,device))
                {
                    v2 = (offset-v) / scaling;
                    *(targetTrigger[0]) = 255;
                    *(targetTrigger[1]) = 255 - (BYTE) deadzone(v2, 0, 255, device.triggerdz[1], 255);
                }

                if (!ButtonPressed(mapping.Trigger[0].but,device)
                        && ButtonPressed(mapping.Trigger[1].but,device))
                {
                    v2 = (offset+v) / scaling;
                    *(targetTrigger[0]) = 255 - (BYTE) deadzone(v2, 0, 255, device.triggerdz[0], 255);
                    *(targetTrigger[1]) = 255;
                }

                if (!ButtonPressed(mapping.Trigger[0].but,device)
                        && !ButtonPressed(mapping.Trigger[1].but,device))
                {
                    v2 = (offset+v) / scaling;
                    *(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, device.triggerdz[i], 255);
                }

            }
            else
            {
                v2 = (offset+v) / scaling;
                *(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, device.triggerdz[i], 255);
            }

            /////////////////////////////////////////////////////////////////////////////////////////
        }
    }

    // --- Map thumbsticks ---

    // Created so we can refer to each axis with an ID
    SHORT *targetAxis[4] =
    {
        & xstate.Gamepad.sThumbLX,
        & xstate.Gamepad.sThumbLY,
        & xstate.Gamepad.sThumbRX,
        & xstate.Gamepad.sThumbRY
    };

    // NOTE: Could add symbolic constants as indexers, such as
    // THUMB_LX_AXIS, THUMB_LX_POSITIVE, THUMB_LX_NEGATIVE
    if(device.axistodpad==0)
    {


        for (INT i = 0; i < 4; ++i)
        {
            LONG *values = axis;

            // Analog input
            if (mapping.Axis[i].analogType == AXIS) values = axis;

            if (mapping.Axis[i].analogType == SLIDER) values = slider;

            if (mapping.Axis[i].analogType != NONE)
            {

                if(mapping.Axis[i].id > 0 )
                {
                    SHORT val = (SHORT) values[mapping.Axis[i].id - 1];
                    *(targetAxis[i])= (SHORT) clamp(val,-32767,32767);
                }
                else if(mapping.Axis[i].id < 0 )
                {
                    SHORT val = (SHORT) -values[-mapping.Axis[i].id - 1];
                    *(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
                }
            }

            // Digital input, positive direction
            if (mapping.Axis[i].hasDigital && mapping.Axis[i].positiveButtonID >= 0)
            {

                if (ButtonPressed(mapping.Axis[i].positiveButtonID,device))
                    *(targetAxis[i]) = 32767;
            }

            // Digital input, negative direction
            if (mapping.Axis[i].hasDigital && mapping.Axis[i].negativeButtonID >= 0)
            {

                if (ButtonPressed(mapping.Axis[i].negativeButtonID,device))
                    *(targetAxis[i]) = -32767;
            }
        }
    }

    //WILDS - Axis to D-Pad
    if(device.axistodpad==1)
    {
        //PrintLog("x: %d, y: %d, z: %d",Gamepad[dwUserIndex].state.lX,Gamepad[dwUserIndex].state.lY,Gamepad[dwUserIndex].state.lZ);

        if(device.state.lX - device.a2doffset > device.a2ddeadzone)
            xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;

        if(device.state.lX - device.a2doffset < -device.a2ddeadzone)
            xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

        if(device.state.lY - device.a2doffset < -device.a2ddeadzone)
            xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

        if(device.state.lY - device.a2doffset > device.a2ddeadzone)
            xstate.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
    }

    //WILDS END

    for (int i = 0; i < 4; ++i)
    {

        if (device.antideadzone[i])
        {
            SHORT antidz = device.antideadzone[i];
            LONG val = *(targetAxis[i]);
            SHORT direction = val > 0 ? 1 : -1;
            val = (LONG)(abs(val) / (32767 / (32767 - antidz * 1.0)) + antidz);
            val = min(val, 32767);

            if(val == device.antideadzone[i] || val == -device.antideadzone[i]) val = 0;

            *(targetAxis[i]) = (SHORT) (direction * val);
        }

        if (device.axisdeadzone[i])
        {
            SHORT dz = device.axisdeadzone[i];
            LONG val = *(targetAxis[i]);

            if((val <= dz) && (val >= -dz) ) val = 0;

            *(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
        }

        // --- Do Linears ---

        if (device.axislinear[i])
        {

            SHORT absval = (SHORT)((abs(*(targetAxis[i])) + (((32767.0 / 2.0) - (((abs((abs(*(targetAxis[i]))) - (32767.0 / 2.0)))))) * (device.axislinear[i] * 0.01))));
            *(targetAxis[i]) = *(targetAxis[i]) > 0 ? absval : -absval;
        }
    }

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputSetState(dwUserIndex,pVibration);

    DInputDevice& device = g_Devices[dwUserIndex];
    if (!pVibration || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

    HRESULT hr=ERROR_SUCCESS;

    XINPUT_VIBRATION &xvib = *pVibration;

    //PrintLog(LOG_XINPUT,"%u",xvib.wLeftMotorSpeed);
    //PrintLog(LOG_XINPUT,"%u",xvib.wRightMotorSpeed);

    if(hMsgWnd == NULL) CreateMsgWnd();

    if(device.device == NULL && device.dwUserIndex == dwUserIndex)
        DeviceInitialize(device);
    if(!device.device) return ERROR_DEVICE_NOT_CONNECTED;

    if(!device.useforce) return ERROR_SUCCESS;

    WORD wLeftMotorSpeed = 0;
    WORD wRightMotorSpeed = 0;

    PrepareForce(device,FFB_LEFTMOTOR);
    PrepareForce(device,FFB_RIGHTMOTOR);

    if(!XInputIsEnabled.bEnabled && XInputIsEnabled.bUseEnabled)
    {
        SetDeviceForces(device,0,FFB_LEFTMOTOR);
        SetDeviceForces(device,0,FFB_RIGHTMOTOR);
        return ERROR_SUCCESS;
    }

    WORD left =  static_cast<WORD>(xvib.wLeftMotorSpeed * device.ff.forcepercent);
    WORD right = static_cast<WORD>(xvib.wRightMotorSpeed * device.ff.forcepercent);

    wLeftMotorSpeed = device.swapmotor ? right : left;
    wRightMotorSpeed = device.swapmotor ? left : right;

    hr = SetDeviceForces(device,wLeftMotorSpeed,FFB_LEFTMOTOR);

    if(FAILED(hr))
        PrintLog(LOG_XINPUT,"SetDeviceForces for pad %d failed with code HR = %X", dwUserIndex, hr);

    hr = SetDeviceForces(device,wRightMotorSpeed,FFB_RIGHTMOTOR);

    if(FAILED(hr))
        PrintLog(LOG_XINPUT,"SetDeviceForces for pad %d failed with code HR = %X", dwUserIndex, hr);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetCapabilities(dwUserIndex,dwFlags,pCapabilities);

    if (!pCapabilities || !(dwUserIndex < XUSER_MAX_COUNT) || (dwFlags > XINPUT_FLAG_GAMEPAD) ) return ERROR_BAD_ARGUMENTS;

    DInputDevice& device = g_Devices[dwUserIndex];

    if(hMsgWnd == NULL) CreateMsgWnd();

    if(device.device == NULL && device.dwUserIndex == dwUserIndex)
        DeviceInitialize(device);
    if(!device.device) return ERROR_DEVICE_NOT_CONNECTED;

    XINPUT_CAPABILITIES& xcaps = *pCapabilities;
    xcaps.Type = 0;
    xcaps.SubType = device.gamepadtype; //customizable subtype
    xcaps.Flags = 0; // we do not support sound
    xcaps.Vibration.wLeftMotorSpeed = xcaps.Vibration.wRightMotorSpeed = 0xFF;
    xcaps.Gamepad.bLeftTrigger = xcaps.Gamepad.bRightTrigger = 0xFF;

    xcaps.Gamepad.sThumbLX = (SHORT) -64;
    xcaps.Gamepad.sThumbLY = (SHORT) -64;
    xcaps.Gamepad.sThumbRX = (SHORT) -64;
    xcaps.Gamepad.sThumbRY = (SHORT) -64;
    xcaps.Gamepad.wButtons = (WORD) 0xF3FF;

    return ERROR_SUCCESS;
}

extern "C" VOID WINAPI XInputEnable(BOOL enable)
{
    if(g_bDisable) return;

    if(hMsgWnd == NULL) CreateMsgWnd();

    if(g_bNative)
        nXInputEnable(enable);

    /*
    Nasty trick to support XInputEnable states, because not every game calls it so:
    - must support games that use it, and do enable/disable as needed by game
    if bEnabled is FALSE and bUseEnabled is TRUE = device is disabled -> return Hook S_OK, ie. connected but state not updating
    if bEnabled is TRUE and bUseEnabled is TRUE = device is enabled -> continue, ie. connected and updating state
    - must support games that not call it
    if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states
    */

    XInputIsEnabled.bEnabled = (enable != 0);
    XInputIsEnabled.bUseEnabled = true;

    if(enable) PrintLog(LOG_XINPUT,"XInput Enabled");
    else PrintLog(LOG_XINPUT,"XInput Disabled");

}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetDSoundAudioDeviceGuids(dwUserIndex,pDSoundRenderGuid,pDSoundCaptureGuid);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    if(!pDSoundRenderGuid || !pDSoundCaptureGuid || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

    DInputDevice& device = g_Devices[dwUserIndex];

    if(hMsgWnd == NULL) CreateMsgWnd();

    if(device.device == NULL && device.dwUserIndex == dwUserIndex)
        DeviceInitialize(device);
    if(!device.device) return ERROR_DEVICE_NOT_CONNECTED;

    *pDSoundRenderGuid = GUID_NULL;
    *pDSoundCaptureGuid = GUID_NULL;

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetBatteryInformation(dwUserIndex,devType,pBatteryInformation);

    if (!pBatteryInformation || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

    DInputDevice& device = g_Devices[dwUserIndex];

    if(device.device == NULL && device.dwUserIndex == dwUserIndex)
        DeviceInitialize(device);
    if(!device.device) return ERROR_DEVICE_NOT_CONNECTED;

    // Report a wired controller
    XINPUT_BATTERY_INFORMATION &xBatInfo = *pBatteryInformation;
    xBatInfo.BatteryLevel = BATTERY_LEVEL_FULL;
    xBatInfo.BatteryType = BATTERY_TYPE_WIRED;

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
    {
        //PrintLog(LOG_XINPUT,"flags: %u, hidcode: %u, unicode: %c, user: %u, vk: 0x%X",pKeystroke->Flags,pKeystroke->HidCode,pKeystroke->Unicode,pKeystroke->UserIndex,pKeystroke->VirtualKey);
        return nXInputGetKeystroke(dwUserIndex,dwReserved,pKeystroke);
    }

    if (!pKeystroke || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

    DInputDevice& device = g_Devices[dwUserIndex];

    if(hMsgWnd == NULL) CreateMsgWnd();

    if(device.device == NULL && device.dwUserIndex == dwUserIndex)
        DeviceInitialize(device);
    if(!device.device) return ERROR_DEVICE_NOT_CONNECTED;

    XINPUT_KEYSTROKE& xkey = *pKeystroke;

    XINPUT_STATE xstate;
    ZeroMemory(&xstate,sizeof(XINPUT_STATE));
    XInputGetState(dwUserIndex,&xstate);

    static WORD repeat[14];
    static WORD flags[14];

    WORD vkey = NULL;
    WORD curretFlags = NULL;

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

    static const uint16_t keyIDs[14] =
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

    for(int i = 0; i < 14; i++)
    {
        if(xstate.Gamepad.wButtons & allButtonIDs[i])
        {
            if(flags[i] == NULL)
            {
                vkey = keyIDs[i];
                curretFlags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN;
                break;
            }
            if((flags[i] & XINPUT_KEYSTROKE_KEYDOWN))
            {
                if(repeat[i] <= 0)
                {
                    repeat[i] = 5;
                    vkey = keyIDs[i];
                    curretFlags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN | XINPUT_KEYSTROKE_REPEAT;
                    break;
                }
                else
                {
                    repeat[i]--;
                    continue;
                }
            }
        }
        if(!(xstate.Gamepad.wButtons & allButtonIDs[i]))
        {
            if(flags[i] & XINPUT_KEYSTROKE_KEYDOWN)
            {
                repeat[i] = 5*4;
                vkey = keyIDs[i];
                curretFlags = flags[i] = XINPUT_KEYSTROKE_KEYUP;
                break;
            }
            if(flags[i] & XINPUT_KEYSTROKE_KEYUP)
            {
                curretFlags = flags[i] = NULL;
                break;
            }
        }
    }

    DWORD ret = ERROR_EMPTY;

    if(vkey)
    {
        xkey.UserIndex = (BYTE)dwUserIndex;
        xkey.Unicode = NULL;
        xkey.HidCode = NULL;
        xkey.Flags = curretFlags;
        xkey.VirtualKey = vkey;
        ret = ERROR_SUCCESS;
    }

    //PrintLog(LOG_XINPUT,"ret: %u, flags: %u, hid: %u, unicode: %c, user: %u, vk: 0x%X",ret,pKeystroke->Flags,pKeystroke->HidCode,pKeystroke->Unicode,pKeystroke->UserIndex,pKeystroke->VirtualKey);

    return ret;
}

//undocumented
extern "C" DWORD WINAPI XInputGetStateEx(DWORD dwUserIndex, XINPUT_STATE *pState)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetStateEx(dwUserIndex,pState);

    DInputDevice& device = g_Devices[dwUserIndex];
    Mapping& mapping = g_Mappings[dwUserIndex];
    XINPUT_STATE& xState = *pState;

    if (mapping.guide && ButtonPressed(mapping.guide,device))
        xState.Gamepad.wButtons |= 0x400;

    //PrintLog(LOG_XINPUT,"XInputGetStateEx %u",xstate.Gamepad.wButtons);

    return XInputGetState(dwUserIndex,pState);
}

extern "C" DWORD WINAPI XInputWaitForGuideButton(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputWaitForGuideButton(dwUserIndex,dwFlag,pVoid);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    //DInputDevice& device = g_Devices[dwUserIndex];

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputCancelGuideButtonWait(DWORD dwUserIndex)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputCancelGuideButtonWait(dwUserIndex);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    //DInputDevice& device = g_Devices[dwUserIndex];

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputPowerOffController(DWORD dwUserIndex)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputCancelGuideButtonWait(dwUserIndex);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    //DInputDevice& device = g_Devices[dwUserIndex];

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetAudioDeviceIds(DWORD dwUserIndex, LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetAudioDeviceIds(dwUserIndex,pRenderDeviceId,pRenderCount,pCaptureDeviceId,pCaptureCount);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBaseBusInformation(DWORD dwUserIndex, struct XINPUT_BUSINFO* pBusinfo)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetBaseBusInformation(dwUserIndex,pBusinfo);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    return ERROR_SUCCESS;
}

// XInput 1.4 uses this in XInputGetCapabilities and calls memcpy(pCapabilities, &CapabilitiesEx, 20u);
// so XINPUT_CAPABILITIES is first 20 bytes of XINPUT_CAPABILITIESEX
extern "C" DWORD WINAPI XInputGetCapabilitiesEx(DWORD unk1, DWORD dwUserIndex, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx)
{
    if(g_bDisable) return ERROR_DEVICE_NOT_CONNECTED;

    if((dwUserIndex+1 > g_Devices.size() || g_Devices[dwUserIndex].passthrough) && XInputInitialize())
        return nXInputGetCapabilitiesEx(unk1,dwUserIndex,dwFlags,pCapabilitiesEx);

    PrintLog(LOG_XINPUT,"%s %s","Call to unimplemented function", __FUNCTION__);

    return ERROR_SUCCESS;
}