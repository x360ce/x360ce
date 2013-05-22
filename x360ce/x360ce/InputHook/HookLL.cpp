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
#include "Log.h"

#include "InputHook.h"

static iHook *iHookThis = NULL;

typedef HMODULE (WINAPI* LoadLibraryA_t)(LPCSTR lpLibFileName);
typedef HMODULE (WINAPI* LoadLibraryW_t)(LPCWSTR lpLibFileName);

typedef HMODULE (WINAPI* LoadLibraryExA_t)(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
typedef HMODULE (WINAPI* LoadLibraryExW_t)(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);

typedef HMODULE (WINAPI* GetModuleHandleA_t)(LPCSTR lpModuleName);
typedef HMODULE (WINAPI* GetModuleHandleW_t)(LPCWSTR lpModuleName);

typedef BOOL (WINAPI* GetModuleHandleExA_t)(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule);
typedef BOOL (WINAPI* GetModuleHandleExW_t)(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule);

LoadLibraryW_t oLoadLibraryW = NULL;
LoadLibraryA_t oLoadLibraryA = NULL;

LoadLibraryExA_t oLoadLibraryExA = NULL;
LoadLibraryExW_t oLoadLibraryExW = NULL;

GetModuleHandleA_t oGetModuleHandleA = NULL;
GetModuleHandleW_t oGetModuleHandleW = NULL;

GetModuleHandleExA_t oGetModuleHandleExA = NULL;
GetModuleHandleExW_t oGetModuleHandleExW = NULL;


BOOL SelfCheckA(LPCSTR lpLibFileName)
{
    if(!lpLibFileName) return FALSE;
    if(strstr(lpLibFileName,"xinput1_3") ||
        strstr(lpLibFileName,"xinput1_2") ||
        strstr(lpLibFileName,"xinput1_1") ||
        strstr(lpLibFileName,"xinput9_1_0"))
    {
        return TRUE;
    }
    else return FALSE;
}

BOOL SelfCheckW(LPCWSTR lpLibFileName)
{
    if(!lpLibFileName) return FALSE;
    if(wcsstr(lpLibFileName,L"xinput1_3") ||
        wcsstr(lpLibFileName,L"xinput1_2") ||
        wcsstr(lpLibFileName,L"xinput1_1") ||
        wcsstr(lpLibFileName,L"xinput9_1_0"))
    {
        return TRUE;
    }
    else return FALSE;
}

HMODULE WINAPI HookLoadLibraryA(LPCSTR lpLibFileName)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryA(lpLibFileName);
    PrintLog(LOG_HOOKLL,"*LoadLibraryA*");

    if(SelfCheckA(lpLibFileName))
    {
        return oLoadLibraryA(ModuleFullPathA(iHookThis->GetEmulator()).c_str());
    }

    return oLoadLibraryA(lpLibFileName);
}

HMODULE WINAPI HookLoadLibraryW(LPCWSTR lpLibFileName)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryW(lpLibFileName);
    PrintLog(LOG_HOOKLL,"*LoadLibraryW*");

    if(SelfCheckW(lpLibFileName))
    {
        return oLoadLibraryW(ModuleFullPathW(iHookThis->GetEmulator()).c_str());
    }

    return oLoadLibraryW(lpLibFileName);
}

HMODULE WINAPI HookLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryExA(lpLibFileName,hFile,dwFlags);
    PrintLog(LOG_HOOKLL,"*LoadLibraryExA*");

    if(SelfCheckA(lpLibFileName))
    {
        return oLoadLibraryExA(ModuleFullPathA(iHookThis->GetEmulator()).c_str(),hFile,dwFlags);
    }

    return oLoadLibraryExA(lpLibFileName,hFile,dwFlags);
}

HMODULE WINAPI HookLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryExW(lpLibFileName,hFile,dwFlags);
    PrintLog(LOG_HOOKLL,"*LoadLibraryExW*");

    if(SelfCheckW(lpLibFileName))
    {
        return oLoadLibraryExW(ModuleFullPathW(iHookThis->GetEmulator()).c_str(),hFile,dwFlags);
    }

    return oLoadLibraryExW(lpLibFileName,hFile,dwFlags);
}

HMODULE WINAPI HookGetModuleHandleA(LPCSTR lpModuleName)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleA(lpModuleName);
    PrintLog(LOG_HOOKLL,"*GetModuleHandleA*");

    if(SelfCheckA(lpModuleName))
    {
        return iHookThis->GetEmulator();
    }

    return oGetModuleHandleA(lpModuleName);
}

HMODULE WINAPI HookGetModuleHandleW(LPCWSTR lpModuleName)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleW(lpModuleName);
    PrintLog(LOG_HOOKLL,"*GetModuleHandleW*");

    if(SelfCheckW(lpModuleName))
    {
        return iHookThis->GetEmulator();
    }

    return oGetModuleHandleW(lpModuleName);
}

BOOL WINAPI HookGetModuleHandleExA(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleExA(dwFlags,lpModuleName,phModule);
    PrintLog(LOG_HOOKLL,"*GetModuleHandleExA*");

    if(SelfCheckA(lpModuleName))
    {
        static HMODULE hModExA = iHookThis->GetEmulator();
        phModule = &hModExA;
        return TRUE;
    }

    return oGetModuleHandleExA(dwFlags,lpModuleName,phModule);
}

BOOL WINAPI HookGetModuleHandleExW(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule)
{
    if(!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleExW(dwFlags,lpModuleName,phModule);
    PrintLog(LOG_HOOKLL,"*GetModuleHandleExW*");

    if(SelfCheckW(lpModuleName))
    {
        static HMODULE hModExW = iHookThis->GetEmulator();
        phModule = &hModExW;
        return TRUE;
    }

    return oGetModuleHandleExW(dwFlags,lpModuleName,phModule);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookLL()
{
    PrintLog(LOG_HOOKLL,"Hooking DLL Loader");
    iHookThis = this;

#if 1
    if(MH_CreateHook(LoadLibraryA,HookLoadLibraryA,reinterpret_cast<void**>(&oLoadLibraryA)) == MH_OK)
        PrintLog(LOG_HOOKLL,"Hooking LoadLibraryA");

    if(MH_CreateHook(LoadLibraryW,HookLoadLibraryW,reinterpret_cast<void**>(&oLoadLibraryW)) == MH_OK) 
        PrintLog(LOG_HOOKLL,"Hooking LoadLibraryW");
#endif

#if 1
    if(MH_CreateHook(LoadLibraryExA,HookLoadLibraryExA,reinterpret_cast<void**>(&oLoadLibraryExA)) == MH_OK)
        PrintLog(LOG_HOOKLL,"Hooking LoadLibraryExA");

    if(MH_CreateHook(LoadLibraryExW,HookLoadLibraryExW,reinterpret_cast<void**>(&oLoadLibraryExW)) == MH_OK) 
        PrintLog(LOG_HOOKLL,"Hooking LoadLibraryExW");
#endif

#if 1
    if(MH_CreateHook(GetModuleHandleA,HookGetModuleHandleA,reinterpret_cast<void**>(&oGetModuleHandleA)) == MH_OK) 
        PrintLog(LOG_HOOKLL,"Hooking GetModuleHandleA");

    if(MH_CreateHook(GetModuleHandleW,HookGetModuleHandleW,reinterpret_cast<void**>(&oGetModuleHandleW)) == MH_OK)
        PrintLog(LOG_HOOKLL,"Hooking GetModuleHandleW");
#endif

#if 1
    if(MH_CreateHook(GetModuleHandleExA,HookGetModuleHandleExA,reinterpret_cast<void**>(&oGetModuleHandleExA)) == MH_OK) 
        PrintLog(LOG_HOOKLL,"Hooking GetModuleHandleExA");

    if(MH_CreateHook(GetModuleHandleExW,HookGetModuleHandleExW,reinterpret_cast<void**>(&oGetModuleHandleExW)) == MH_OK)
        PrintLog(LOG_HOOKLL,"Hooking GetModuleHandleExW");
#endif
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////