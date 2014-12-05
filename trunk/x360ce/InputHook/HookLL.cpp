#include "stdafx.h"
#include "Common.h"
#include "Utils.h"
#include "Logger.h"

#include "InputHookManager.h"
#include "InputHook.h"
#include "HookLL.h"

#include <algorithm>

HMODULE(WINAPI* HookLL::TrueLoadLibraryA)(LPCSTR lpLibFileName) = nullptr;
HMODULE(WINAPI* HookLL::TrueLoadLibraryW)(LPCWSTR lpLibFileName) = nullptr;
HMODULE(WINAPI* HookLL::TrueLoadLibraryExA)(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags) = nullptr;
HMODULE(WINAPI* HookLL::TrueLoadLibraryExW)(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags) = nullptr;
HMODULE(WINAPI* HookLL::TrueGetModuleHandleA)(LPCSTR lpModuleName) = nullptr;
HMODULE(WINAPI* HookLL::TrueGetModuleHandleW)(LPCWSTR lpModuleName) = nullptr;
BOOL(WINAPI* HookLL::TrueGetModuleHandleExA)(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule) = nullptr;
BOOL(WINAPI* HookLL::TrueGetModuleHandleExW)(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule) = nullptr;

BOOL HookLL::SelfCheckA(LPCSTR lpLibFileName)
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

BOOL HookLL::SelfCheckW(LPCWSTR lpLibFileName)
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

HMODULE WINAPI HookLL::HookLoadLibraryA(LPCSTR lpLibFileName)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueLoadLibraryA(lpLibFileName);

    if (SelfCheckA(lpLibFileName))
    {
        PrintLog("LoadLibraryA");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();

        std::string path;

        if (ModulePath(&path, InputHookManager::Get().GetInputHook().GetEmulator()))
            return TrueLoadLibraryA(path.c_str());
        else
            return TrueLoadLibraryA(lpLibFileName);
    }

    return TrueLoadLibraryA(lpLibFileName);
}

HMODULE WINAPI HookLL::HookLoadLibraryW(LPCWSTR lpLibFileName)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueLoadLibraryW(lpLibFileName);

    if (SelfCheckW(lpLibFileName))
    {
        PrintLog("LoadLibraryW");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();

        std::wstring path;

        if (ModulePath(&path, InputHookManager::Get().GetInputHook().GetEmulator()))
            return TrueLoadLibraryW(path.c_str());
        else
            return TrueLoadLibraryW(lpLibFileName);
    }

    return TrueLoadLibraryW(lpLibFileName);
}

HMODULE WINAPI HookLL::HookLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueLoadLibraryExA(lpLibFileName, hFile, dwFlags);

    if (SelfCheckA(lpLibFileName))
    {
        PrintLog("LoadLibraryExA");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();

        std::string path;

        if (ModulePath(&path, InputHookManager::Get().GetInputHook().GetEmulator()))
            return TrueLoadLibraryExA(path.c_str(), hFile, dwFlags);
        else
            return TrueLoadLibraryExA(lpLibFileName, hFile, dwFlags);
    }

    return TrueLoadLibraryExA(lpLibFileName, hFile, dwFlags);
}

HMODULE WINAPI HookLL::HookLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueLoadLibraryExW(lpLibFileName, hFile, dwFlags);

    if (SelfCheckW(lpLibFileName))
    {
        PrintLog("LoadLibraryExW");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();

        std::wstring path;

        if (ModulePath(&path, InputHookManager::Get().GetInputHook().GetEmulator()))
            return TrueLoadLibraryExW(path.c_str(), hFile, dwFlags);
        else
            return TrueLoadLibraryExW(lpLibFileName, hFile, dwFlags);
    }

    return TrueLoadLibraryExW(lpLibFileName, hFile, dwFlags);
}

HMODULE WINAPI HookLL::HookGetModuleHandleA(LPCSTR lpModuleName)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleA(lpModuleName);

    if (SelfCheckA(lpModuleName))
    {
        PrintLog("GetModuleHandleA");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();
        return InputHookManager::Get().GetInputHook().GetEmulator();
    }

    return TrueGetModuleHandleA(lpModuleName);
}

HMODULE WINAPI HookLL::HookGetModuleHandleW(LPCWSTR lpModuleName)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleW(lpModuleName);

    if (SelfCheckW(lpModuleName))
    {
        PrintLog("GetModuleHandleW");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();
        return InputHookManager::Get().GetInputHook().GetEmulator();
    }

    return TrueGetModuleHandleW(lpModuleName);
}

BOOL WINAPI HookLL::HookGetModuleHandleExA(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleExA(dwFlags, lpModuleName, phModule);

    if (SelfCheckA(lpModuleName))
    {
        PrintLog("GetModuleHandleExA");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();
        phModule = &InputHookManager::Get().GetInputHook().GetEmulator();
        return TRUE;
    }

    return TrueGetModuleHandleExA(dwFlags, lpModuleName, phModule);
}

BOOL WINAPI HookLL::HookGetModuleHandleExW(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule)
{
    if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleExW(dwFlags, lpModuleName, phModule);

    if (SelfCheckW(lpModuleName))
    {
        PrintLog("GetModuleHandleExW");
        InputHookManager::Get().GetInputHook().StartTimeoutThread();
        phModule = &InputHookManager::Get().GetInputHook().GetEmulator();
        return TRUE;
    }

    return TrueGetModuleHandleExW(dwFlags, lpModuleName, phModule);
}


void InputHook::HookLL()
{
    PrintLog("Hooking DLL Loader");

#if 1
    IH_CreateHook(LoadLibraryA, HookLL::HookLoadLibraryA, &HookLL::TrueLoadLibraryA);
    IH_CreateHook(LoadLibraryW, HookLL::HookLoadLibraryW, &HookLL::TrueLoadLibraryW);
#endif

#if 1
    IH_CreateHook(LoadLibraryExA, HookLL::HookLoadLibraryExA, &HookLL::TrueLoadLibraryExA);
    IH_CreateHook(LoadLibraryExW, HookLL::HookLoadLibraryExW, &HookLL::TrueLoadLibraryExW);
#endif

#if 1
    IH_CreateHook(GetModuleHandleA, HookLL::HookGetModuleHandleA, &HookLL::TrueGetModuleHandleA);
    IH_CreateHook(GetModuleHandleW, HookLL::HookGetModuleHandleW, &HookLL::TrueGetModuleHandleW);
#endif

#if 1
    IH_CreateHook(GetModuleHandleExA, HookLL::HookGetModuleHandleExA, &HookLL::TrueGetModuleHandleExA);
    IH_CreateHook(GetModuleHandleExW, HookLL::HookGetModuleHandleExW, &HookLL::TrueGetModuleHandleExW);
#endif
}
