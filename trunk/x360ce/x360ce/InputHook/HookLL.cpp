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

#include "InputHook.h"

static iHook *iHookThis = NULL;

typedef HMODULE (WINAPI* LoadLibraryW_t)(LPCWSTR lpLibFileName);
typedef HMODULE (WINAPI* LoadLibraryA_t)(LPCSTR lpLibFileName);

LoadLibraryW_t oLoadLibraryW;
LoadLibraryA_t oLoadLibraryA;

HMODULE WINAPI HookLoadLibraryW(LPCWSTR lpLibFileName)
{
    if(!iHookThis->CheckHook(iHook::HOOK_LL)) return oLoadLibraryW(lpLibFileName);
    PrintLog(LOG_HOOKLL,"*LoadLibraryW*");

    if(wcsstr(lpLibFileName,L"xinput9_1_0")) return iHookThis->GetEmulator();
    if(wcsstr(lpLibFileName,L"xinput1_1")) return iHookThis->GetEmulator();
    if(wcsstr(lpLibFileName,L"xinput1_2")) return iHookThis->GetEmulator();
    if(wcsstr(lpLibFileName,L"xinput1_3")) return iHookThis->GetEmulator();

    return oLoadLibraryW(lpLibFileName);
}

HMODULE WINAPI HookLoadLibraryA(LPCSTR lpLibFileName)
{
    if(!iHookThis->CheckHook(iHook::HOOK_LL)) return oLoadLibraryA(lpLibFileName);
    PrintLog(LOG_HOOKLL,"*LoadLibraryA*");

    if(strstr(lpLibFileName,"xinput9_1_0")) return iHookThis->GetEmulator();
    if(strstr(lpLibFileName,"xinput1_1")) return iHookThis->GetEmulator();
    if(strstr(lpLibFileName,"xinput1_2")) return iHookThis->GetEmulator();
    if(strstr(lpLibFileName,"xinput1_3")) return iHookThis->GetEmulator();

    return oLoadLibraryA(lpLibFileName);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookLL()
{
    PrintLog(LOG_HOOKLL,"Hooking LoadLibrary");
    iHookThis = this;

    MH_CreateHook(LoadLibraryW,HookLoadLibraryW,reinterpret_cast<void**>(&oLoadLibraryW));
    MH_EnableHook(LoadLibraryW);

    MH_CreateHook(LoadLibraryA,HookLoadLibraryA,reinterpret_cast<void**>(&oLoadLibraryA));
    MH_EnableHook(LoadLibraryA);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////