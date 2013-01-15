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
#include "externals.h"
#include "InputHook.h"

IHOOK_CONIFG InputHookConfig;
IHOOK_GAMEPAD_CONIFG GamepadConfig[4];
BOOL laststate = FALSE;

ULONG ACLEntries[1];

VOID InputHook_Enable(BOOL state)
{
    InputHookConfig.bEnabled = state;
    laststate = state;
}

BOOL InputHook_Enable()
{
    return InputHookConfig.bEnabled;
}

DWORD InputHook_Mode()
{
    return InputHookConfig.dwHookMode;
}

BOOL InputHook_Init(IHOOK_CONIFG* fconfig, IHOOK_GAMEPAD_CONIFG* gconfig)
{

    if(!fconfig) return FALSE;

    if(!fconfig->bEnabled) return FALSE;

    memcpy(&InputHookConfig,fconfig,sizeof(IHOOK_CONIFG));

    for(WORD i = 0; i < 4; i++)
    {

        memcpy(&GamepadConfig[i],gconfig,sizeof(IHOOK_GAMEPAD_CONIFG));
        gconfig++;

        if(!IsEqualGUID(GamepadConfig[i].productGUID, GUID_NULL) && !IsEqualGUID(GamepadConfig[i].instanceGUID, GUID_NULL))
        {
            if(!GamepadConfig[i].dwVID) GamepadConfig[i].dwVID = LOWORD(GamepadConfig[i].productGUID.Data1);

            if(!GamepadConfig[i].dwPID) GamepadConfig[i].dwPID = HIWORD(GamepadConfig[i].productGUID.Data1);
        }
        else GamepadConfig[i].bEnabled = FALSE;
    }

    if(InputHookConfig.bEnabled)
    {
        if(InputHookConfig.dwHookWMIANSI) HookWMI_ANSI();
        else HookWMI_UNI();

        if(InputHookConfig.dwHookMode >= HOOK_COMPAT) HookDI();

        if(InputHookConfig.dwHookWinTrust) HookWinTrust();
    }

    return TRUE;
}

BOOL InputHook_Clean()
{

    HookWMI_UNI_Clean();
    HookWMI_ANSI_Clean();
    HookDIClean();
    HookWinTrustClean();

    return TRUE;
}
