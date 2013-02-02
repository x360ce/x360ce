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
#include "globals.h"
#include "Log.h"
#include "Utilities\Misc.h"

#include <Setupapi.h>
#pragma comment(lib,"Setupapi.lib")

#include "InputHook.h"

static iHook *iHookThis = NULL;

typedef BOOL (WINAPI* SetupDiGetDeviceInstanceIdW_t)(
    _In_       HDEVINFO DeviceInfoSet,
    _In_       PSP_DEVINFO_DATA DeviceInfoData,
    _Out_opt_  PWSTR DeviceInstanceId,
    _In_       DWORD DeviceInstanceIdSize,
    _Out_opt_  PDWORD RequiredSize
);

// NOTE: SetupDiGetDeviceInstanceIdW is called inside SetupDiGetDeviceInstanceIdA
SetupDiGetDeviceInstanceIdW_t oSetupDiGetDeviceInstanceIdW = NULL;

BOOL WINAPI HookSetupDiGetDeviceInstanceIdW(
    _In_       HDEVINFO DeviceInfoSet,
    _In_       PSP_DEVINFO_DATA DeviceInfoData,
    _Out_opt_  PWSTR DeviceInstanceId,
    _In_       DWORD DeviceInstanceIdSize,
    _Out_opt_  PDWORD RequiredSize
)
{
    BOOL ret = oSetupDiGetDeviceInstanceIdW(DeviceInfoSet,DeviceInfoData,DeviceInstanceId,DeviceInstanceIdSize,RequiredSize);
    if(!iHookThis->CheckHook(iHook::HOOK_SA)) return ret;
    PrintLog(LOG_HOOKSA,"*SetupDiGetDeviceInstanceIdW*");

    if(GetLastError() == ERROR_INSUFFICIENT_BUFFER) return ret;

    if(DeviceInstanceId && ret)
    {
        DWORD dwPid = 0, dwVid = 0;

        wchar_t* strVid = wcsstr( DeviceInstanceId, L"VID_" );
        if(!strVid || swscanf_s( strVid, L"VID_%4X", &dwVid ) < 1 )
            return ret;

        wchar_t* strPid = wcsstr( DeviceInstanceId, L"PID_" );
        if(!strPid || swscanf_s( strPid, L"PID_%4X", &dwPid ) < 1 )
            return ret;

        for(WORD i = 0; i < iHookThis->GetHookCount(); i++)
        {
            iHookDevice &padconf = iHookThis->GetPadConfig(i);
            if(padconf.GetHookState() && padconf.GetProductVIDPID() == (DWORD)MAKELONG(dwVid,dwPid))
            {
                wchar_t* strUSB = wcsstr( DeviceInstanceId, L"USB\\" );
                wchar_t tempstr[MAX_PATH];

                DWORD dwHookVid = iHookThis->CheckHook(iHook::HOOK_VIDPID) ? LOWORD(iHookThis->GetFakePIDVID()) : LOWORD(padconf.GetProductVIDPID());
                DWORD dwHookPid = iHookThis->CheckHook(iHook::HOOK_VIDPID) ? HIWORD(iHookThis->GetFakePIDVID()) : HIWORD(padconf.GetProductVIDPID());

                if( strUSB && dwHookVid && dwHookPid)
                {
                    wchar_t* p = wcsrchr(DeviceInstanceId,L'\\');
                    swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );

                    if(DeviceInstanceIdSize < wcslen(tempstr))
                    {
                        SetLastError(ERROR_INSUFFICIENT_BUFFER);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        //return FALSE; //NOTE: return FALSE here breaks Beat Hazard
                        continue;
                    }

                    if(DeviceInstanceIdSize > wcslen(tempstr))
                    {
                        PrintLog(LOG_HOOKSA,"Device string change:",DeviceInstanceId);
                        PrintLog(LOG_HOOKSA,"%ls",DeviceInstanceId);
                        wcscpy_s(DeviceInstanceId,DeviceInstanceIdSize,tempstr);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        PrintLog(LOG_HOOKSA,"%ls",DeviceInstanceId);
                        continue;
                    }
                }

                wchar_t* strHID = wcsstr( DeviceInstanceId, L"HID\\" );

                if( strHID && dwHookVid && dwHookPid )
                {
                    wchar_t* p = wcsrchr(DeviceInstanceId,L'\\');
                    swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid,i, p );

                    if(DeviceInstanceIdSize < wcslen(tempstr))
                    {
                        SetLastError(ERROR_INSUFFICIENT_BUFFER);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        //return FALSE; //NOTE: return FALSE here breaks Beat Hazard
                        continue;
                    }

                    if(DeviceInstanceIdSize > wcslen(tempstr))
                    {
                        PrintLog(LOG_HOOKSA,"Device string change:",DeviceInstanceId);
                        PrintLog(LOG_HOOKSA,"%ls",DeviceInstanceId);
                        wcscpy_s(DeviceInstanceId,DeviceInstanceIdSize,tempstr);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        PrintLog(LOG_HOOKSA,"%ls",DeviceInstanceId);
                        continue;
                    }
                }
            }
        }
    }

    return ret;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookSA()
{
    PrintLog(LOG_IHOOK,"Hooking SetupApi");
    iHookThis = this;

    MH_CreateHook(SetupDiGetDeviceInstanceIdW,HookSetupDiGetDeviceInstanceIdW,reinterpret_cast<void**>(&oSetupDiGetDeviceInstanceIdW));
    MH_EnableHook(SetupDiGetDeviceInstanceIdW);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////