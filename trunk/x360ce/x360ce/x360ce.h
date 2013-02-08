/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
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

#ifndef _X360CE_H_
#define _X360CE_H_

extern HINSTANCE hThis;
extern HINSTANCE hNative;
extern HWND hMsgWnd;

class XInputEnabled
{
public:
    bool bEnabled;
    bool bUseEnabled;
    XInputEnabled():
        bEnabled(false),
        bUseEnabled(false)
    {}
    virtual ~XInputEnabled() {};
};

void LoadXInputDLL();



/**********************************************************************************************/
/**********************************************************************************************/
/**********************************************************************************************/

// XINPUT FUNCTIONS TYPES
typedef DWORD (WINAPI* XInputGetState_t)(DWORD dwUserIndex, XINPUT_STATE* pState);
typedef DWORD (WINAPI* XInputSetState_t)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
typedef DWORD (WINAPI* XInputGetCapabilities_t)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
typedef VOID (WINAPI* XInputEnable_t)(BOOL enable);
typedef DWORD (WINAPI* XInputGetDSoundAudioDeviceGuids_t)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
typedef DWORD (WINAPI* XInputGetBatteryInformation_t)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
typedef DWORD (WINAPI* XInputGetKeystroke_t)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);

typedef DWORD (WINAPI* XInputGetStateEx_t)(DWORD dwUserIndex, XINPUT_STATE *pState);
typedef DWORD (WINAPI* XInputWaitForGuideButton_t)(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid);
typedef DWORD (WINAPI* XInputCancelGuideButtonWait_t)(DWORD dwUserIndex);
typedef DWORD (WINAPI* XInputPowerOffController_t)(DWORD dwUserIndex);

/**********************************************************************************************/
/**********************************************************************************************/
/**********************************************************************************************/

namespace Native
{
enum funcType {GETSTATE, SETSTATE, GETCAPS, ENABLE, AUDIO, BATTERY, KEYSTROKE, GETSTATEEX, WAITGUIDE, CANCELGUIDE, POWEROFF};
}

inline FARPROC& GetXInputFunc(Native::funcType func)
{
    LoadXInputDLL();

    switch(func)
    {
    case Native::GETSTATE:
    {
        static FARPROC nXInputGetState = GetProcAddress(hNative,"XInputGetState");
        return nXInputGetState;
    }
    case Native::SETSTATE:
    {
        static FARPROC nXInputSetState = GetProcAddress(hNative,"XInputSetState");
        return nXInputSetState;
    }

    case Native::GETCAPS:
    {
        static FARPROC nXInputGetCapabilities = GetProcAddress(hNative,"XInputGetCapabilities");
        return nXInputGetCapabilities;
    }

    case Native::ENABLE:
    {
        static FARPROC nXInputEnable = GetProcAddress(hNative,"XInputEnable");
        return nXInputEnable;
    }

    case Native::AUDIO:
    {
        static FARPROC nXInputGetDSoundAudioDeviceGuids = GetProcAddress(hNative,"XInputGetDSoundAudioDeviceGuids");
        return nXInputGetDSoundAudioDeviceGuids;
    }

    case Native::BATTERY:
    {
        static FARPROC nXInputGetBatteryInformation = GetProcAddress(hNative,"XInputGetBatteryInformation");
        return nXInputGetBatteryInformation;
    }

    case Native::KEYSTROKE:
    {
        static FARPROC nXInputGetKeystroke = GetProcAddress(hNative,"XInputGetKeystroke");
        return nXInputGetKeystroke;
    }


    case Native::GETSTATEEX:
    {
        static FARPROC nXInputGetStateEx = GetProcAddress(hNative,(LPCSTR)100);
        return nXInputGetStateEx;
    }

    case Native::WAITGUIDE:
    {
        static FARPROC nXInputWaitForGuideButton = GetProcAddress(hNative,(LPCSTR)101);
        return nXInputWaitForGuideButton;
    }

    case Native::CANCELGUIDE:
    {
        static FARPROC nXInputCancelGuideButtonWait = GetProcAddress(hNative,(LPCSTR)102);
        return nXInputCancelGuideButtonWait;
    }

    case Native::POWEROFF:
    {
        static FARPROC nXInputPowerOffController = GetProcAddress(hNative,(LPCSTR)103);
        return nXInputPowerOffController;
    }
    default:
    {
        MessageBox(NULL,L"Cannot initalize xinput function!",L"Error",MB_ICONERROR);
        ExitProcess(1);
    }
    }
}

#endif
