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
#include "Log.h"
#include <Softpub.h>

#include "InputHook.h"

static iHook *iHookThis = NULL;

typedef LONG (WINAPI* WinVerifyTrust_t)(HWND hwnd, GUID *pgActionID,LPVOID pWVTData);

WinVerifyTrust_t oWinVerifyTrust = NULL;

LONG WINAPI HookWinVerifyTrust(HWND hwnd, GUID *pgActionID,LPVOID pWVTData)
{
    if(!iHookThis->GetState(iHook::HOOK_WT)) return oWinVerifyTrust(hwnd,pgActionID,pWVTData);
    PrintLog(LOG_HOOKWT,"*WinVerifyTrust*");

    UNREFERENCED_PARAMETER(hwnd);
    UNREFERENCED_PARAMETER(pgActionID);
    UNREFERENCED_PARAMETER(pWVTData);
    return 0;
}

void iHook::HookWT()
{
    iHookThis = this;
    if(MH_CreateHook(WinVerifyTrust,HookWinVerifyTrust,reinterpret_cast<void**>(&oWinVerifyTrust)) == MH_OK) 
        PrintLog(LOG_IHOOK,"Hooking WinVerifyTrust");
}