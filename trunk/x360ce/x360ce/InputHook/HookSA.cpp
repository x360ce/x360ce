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
    if(!iHookThis->GetState(iHook::HOOK_SA)) return ret;
    PrintLog("*SetupDiGetDeviceInstanceIdW*");

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

		for(auto padcfg = iHookThis->begin(); padcfg != iHookThis->end(); ++padcfg)
        {
            if(padcfg->GetHookState() && padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid,dwPid))
            {
                const wchar_t* strUSB = wcsstr( DeviceInstanceId, L"USB\\" );
				const wchar_t* strRoot = wcsstr( DeviceInstanceId, L"root\\" );
                OLECHAR tempstr[MAX_PATH];

                DWORD dwHookVid = iHookThis->GetState(iHook::HOOK_PIDVID) ? LOWORD(iHookThis->GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
                DWORD dwHookPid = iHookThis->GetState(iHook::HOOK_PIDVID) ? HIWORD(iHookThis->GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

                if(strUSB || strRoot)
                {
                    const wchar_t* p = wcsrchr(DeviceInstanceId,L'\\');
					if(p) swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid, padcfg->GetUserIndex(), p );
					else swprintf_s(tempstr,L"USB\\VID_%04X&PID_%04X&IG_%02d",dwHookVid,dwHookPid, padcfg->GetUserIndex() );

                    if(DeviceInstanceIdSize < wcslen(tempstr))
                    {
                        SetLastError(ERROR_INSUFFICIENT_BUFFER);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        //return FALSE; //NOTE: return FALSE here breaks Beat Hazard
                        continue;
                    }

                    if(DeviceInstanceIdSize > wcslen(tempstr))
                    {
                        PrintLog("Device string change:",DeviceInstanceId);
                        PrintLog("%ls",DeviceInstanceId);
                        wcscpy_s(DeviceInstanceId,DeviceInstanceIdSize,tempstr);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        PrintLog("%ls",DeviceInstanceId);
                        continue;
                    }
                }

                wchar_t* strHID = wcsstr( DeviceInstanceId, L"HID\\" );

                if(strHID)
                {
                    wchar_t* p = wcsrchr(DeviceInstanceId,L'\\');
                    swprintf_s(tempstr,L"HID\\VID_%04X&PID_%04X&IG_%02d%s",dwHookVid,dwHookPid, padcfg->GetUserIndex(), p );

                    if(DeviceInstanceIdSize < wcslen(tempstr))
                    {
                        SetLastError(ERROR_INSUFFICIENT_BUFFER);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        //return FALSE; //NOTE: return FALSE here breaks Beat Hazard
                        continue;
                    }

                    if(DeviceInstanceIdSize > wcslen(tempstr))
                    {
                        PrintLog("Device string change:",DeviceInstanceId);
                        PrintLog("%ls",DeviceInstanceId);
                        wcscpy_s(DeviceInstanceId,DeviceInstanceIdSize,tempstr);
                        if(RequiredSize) *RequiredSize = (DWORD) wcslen(tempstr)+1;
                        PrintLog("%ls",DeviceInstanceId);
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
    PrintLog("Hooking SetupApi");
    iHookThis = this;

    if(MH_CreateHook(SetupDiGetDeviceInstanceIdW,HookSetupDiGetDeviceInstanceIdW,reinterpret_cast<void**>(&oSetupDiGetDeviceInstanceIdW)) == MH_OK)
        PrintLog("Hooking SetupDiGetDeviceInstanceId");
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////