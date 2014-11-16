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
#include "Misc.h"
#include "Logger.h"

#include "InputHook.h"

#include <algorithm>

static iHook *iHookThis = NULL;

typedef HMODULE(WINAPI* LoadLibraryA_t)(LPCSTR lpLibFileName);
typedef HMODULE(WINAPI* LoadLibraryW_t)(LPCWSTR lpLibFileName);

typedef HMODULE(WINAPI* LoadLibraryExA_t)(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
typedef HMODULE(WINAPI* LoadLibraryExW_t)(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);

typedef HMODULE(WINAPI* GetModuleHandleA_t)(LPCSTR lpModuleName);
typedef HMODULE(WINAPI* GetModuleHandleW_t)(LPCWSTR lpModuleName);

typedef BOOL(WINAPI* GetModuleHandleExA_t)(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule);
typedef BOOL(WINAPI* GetModuleHandleExW_t)(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule);

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
    if (!lpLibFileName) return FALSE;

    std::string strLib(lpLibFileName);
    std::transform(strLib.begin(), strLib.end(), strLib.begin(), tolower);

    if (strLib.find("xinput1_4") != std::string::npos ||
        strLib.find("xinput1_3") != std::string::npos ||
        strLib.find("xinput1_2") != std::string::npos ||
        strLib.find("xinput1_1") != std::string::npos ||
        strLib.find("xinput9_1_0") != std::string::npos)
    {
        return TRUE;
    }
    else return FALSE;
}

BOOL SelfCheckW(LPCWSTR lpLibFileName)
{
    if (!lpLibFileName) return FALSE;

    std::wstring strLib(lpLibFileName);
    std::transform(strLib.begin(), strLib.end(), strLib.begin(), towlower);

    if (strLib.find(L"xinput1_4") != std::wstring::npos ||
        strLib.find(L"xinput1_3") != std::wstring::npos ||
        strLib.find(L"xinput1_2") != std::wstring::npos ||
        strLib.find(L"xinput1_1") != std::wstring::npos ||
        strLib.find(L"xinput9_1_0") != std::wstring::npos)
    {
        return TRUE;
    }
    else return FALSE;
}

HMODULE WINAPI HookLoadLibraryA(LPCSTR lpLibFileName)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryA(lpLibFileName);

    if (SelfCheckA(lpLibFileName))
    {
        PrintLog("*LoadLibraryA*");
        return oLoadLibraryA(ModuleFullPathA(iHookThis->GetEmulator()).c_str());
    }

    return oLoadLibraryA(lpLibFileName);
}

HMODULE WINAPI HookLoadLibraryW(LPCWSTR lpLibFileName)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryW(lpLibFileName);

    if (SelfCheckW(lpLibFileName))
    {
        PrintLog("*LoadLibraryW*");
        return oLoadLibraryW(ModuleFullPathW(iHookThis->GetEmulator()).c_str());
    }

    return oLoadLibraryW(lpLibFileName);
}

HMODULE WINAPI HookLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryExA(lpLibFileName, hFile, dwFlags);

    if (SelfCheckA(lpLibFileName))
    {
        PrintLog("*LoadLibraryExA*");
        return oLoadLibraryExA(ModuleFullPathA(iHookThis->GetEmulator()).c_str(), hFile, dwFlags);
    }

    return oLoadLibraryExA(lpLibFileName, hFile, dwFlags);
}

HMODULE WINAPI HookLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oLoadLibraryExW(lpLibFileName, hFile, dwFlags);

    if (SelfCheckW(lpLibFileName))
    {
        PrintLog("*LoadLibraryExW*");
        return oLoadLibraryExW(ModuleFullPathW(iHookThis->GetEmulator()).c_str(), hFile, dwFlags);
    }

    return oLoadLibraryExW(lpLibFileName, hFile, dwFlags);
}

HMODULE WINAPI HookGetModuleHandleA(LPCSTR lpModuleName)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleA(lpModuleName);

    if (SelfCheckA(lpModuleName))
    {
        PrintLog("*GetModuleHandleA*");
        return iHookThis->GetEmulator();
    }

    return oGetModuleHandleA(lpModuleName);
}

HMODULE WINAPI HookGetModuleHandleW(LPCWSTR lpModuleName)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleW(lpModuleName);

    if (SelfCheckW(lpModuleName))
    {
        PrintLog("*GetModuleHandleW*");
        return iHookThis->GetEmulator();
    }

    return oGetModuleHandleW(lpModuleName);
}

BOOL WINAPI HookGetModuleHandleExA(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleExA(dwFlags, lpModuleName, phModule);

    if (SelfCheckA(lpModuleName))
    {
        PrintLog("*GetModuleHandleExA*");
        static HMODULE hModExA = iHookThis->GetEmulator();
        phModule = &hModExA;
        return TRUE;
    }

    return oGetModuleHandleExA(dwFlags, lpModuleName, phModule);
}

BOOL WINAPI HookGetModuleHandleExW(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule)
{
    if (!iHookThis->GetState(iHook::HOOK_LL)) return oGetModuleHandleExW(dwFlags, lpModuleName, phModule);

    if (SelfCheckW(lpModuleName))
    {
        PrintLog("*GetModuleHandleExW*");
        static HMODULE hModExW = iHookThis->GetEmulator();
        phModule = &hModExW;
        return TRUE;
    }

    return oGetModuleHandleExW(dwFlags, lpModuleName, phModule);
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookLL()
{
    PrintLog("Hooking DLL Loader");
    iHookThis = this;

#if 1
    IH_CreateHook(LoadLibraryA, HookLoadLibraryA, &oLoadLibraryA);
    IH_CreateHook(LoadLibraryW, HookLoadLibraryW, &oLoadLibraryW);
#endif

#if 1
    IH_CreateHook(LoadLibraryExA, HookLoadLibraryExA, &oLoadLibraryExA);
    IH_CreateHook(LoadLibraryExW, HookLoadLibraryExW, &oLoadLibraryExW);
#endif

#if 1
    IH_CreateHook(GetModuleHandleA, HookGetModuleHandleA, &oGetModuleHandleA);
    IH_CreateHook(GetModuleHandleW, HookGetModuleHandleW, &oGetModuleHandleW);
#endif

#if 1
    IH_CreateHook(GetModuleHandleExA, HookGetModuleHandleExA, &oGetModuleHandleExA);
    IH_CreateHook(GetModuleHandleExW, HookGetModuleHandleExW, &oGetModuleHandleExW);
#endif
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////