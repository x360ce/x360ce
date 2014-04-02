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

#ifndef _X360CE_H_
#define _X360CE_H_

// hack for compiling on vs2012/13 without DXSDK by using xinput 1.4
// this will cause some macro redefinitions !
#if _MSC_VER >= 1700
#define OLD_WIN32_WINNT _WIN32_WINNT
#define _WIN32_WINNT _WIN32_WINNT_WIN8
#include <xinput.h>
#define _WIN32_WINNT OLD_WIN32_WINNT
#define WINVER _WIN32_WINNT
#else
#include <xinput.h>
#endif

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

// XInput 1.3 function types
typedef DWORD (WINAPI* XInputGetState_t)(DWORD dwUserIndex, XINPUT_STATE* pState);
typedef DWORD (WINAPI* XInputSetState_t)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
typedef DWORD (WINAPI* XInputGetCapabilities_t)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
typedef VOID  (WINAPI* XInputEnable_t)(BOOL enable);
typedef DWORD (WINAPI* XInputGetDSoundAudioDeviceGuids_t)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
typedef DWORD (WINAPI* XInputGetBatteryInformation_t)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
typedef DWORD (WINAPI* XInputGetKeystroke_t)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);

// XInput 1.3 undocumented function types
typedef DWORD (WINAPI* XInputGetStateEx_t)(DWORD dwUserIndex, XINPUT_STATE *pState); // 100
typedef DWORD (WINAPI* XInputWaitForGuideButton_t)(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid); // 101
typedef DWORD (WINAPI* XInputCancelGuideButtonWait_t)(DWORD dwUserIndex); // 102
typedef DWORD (WINAPI* XInputPowerOffController_t)(DWORD dwUserIndex); // 103

// XInput 1.4 function types
typedef DWORD (WINAPI* XInputGetAudioDeviceIds_t)(DWORD dwUserIndex, LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount);

// XInput 1.4 undocumented functions types
typedef DWORD (WINAPI* XInputGetBaseBusInformation_t)(DWORD dwUserIndex, struct XINPUT_BUSINFO* pBusinfo); // 104
typedef DWORD (WINAPI* XInputGetCapabilitiesEx_t)(DWORD unk1, DWORD dwUserIndex, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx); // 108

#endif
