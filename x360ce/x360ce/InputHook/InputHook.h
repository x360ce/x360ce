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

#ifndef _InputHook_H_
#define _InputHook_H_

#include <CGuid.h>
#include "detours.h"

extern ULONG ACLEntries[1];

enum HookMode{ HOOK_NONE, HOOK_NORMAL, HOOK_COMPAT, HOOK_ALL};

struct IHOOK_GAMEPAD_CONIFG
{
	BOOL  bEnabled;
	GUID  productGUID;
	GUID  instanceGUID;
	DWORD dwVID;
	DWORD dwPID;
	IHOOK_GAMEPAD_CONIFG()
	{
		ZeroMemory(this,sizeof(IHOOK_GAMEPAD_CONIFG));
	}
};

struct IHOOK_CONIFG
{
	BOOL  bEnabled;
	DWORD dwHookMode;
	DWORD dwHookVID;
	DWORD dwHookPID;
	DWORD dwHookWMIANSI;
	DWORD dwHookWinTrust;
	SHORT sConfiguredPads;
	IHOOK_CONIFG()
	{
		ZeroMemory(this,sizeof(IHOOK_CONIFG));
	}
};

extern IHOOK_CONIFG InputHookConfig;
extern IHOOK_GAMEPAD_CONIFG GamepadConfig[4];

void HookWMI_UNI();
void HookWMI_ANSI();
void HookDI();
void HookWinTrust();

void HookWMI_UNI_Clean();
void HookWMI_ANSI_Clean();
void HookDIClean();
void HookWinTrustClean();

VOID InputHook_Enable(BOOL state);
DWORD InputHook_Mode();
BOOL InputHook_Enable();

BOOL InputHook_Init(IHOOK_CONIFG* fconfig, IHOOK_GAMEPAD_CONIFG* gconfig);
BOOL InputHook_Clean();

#endif
