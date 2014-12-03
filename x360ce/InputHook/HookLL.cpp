#include "stdafx.h"
#include "Common.h"
#include "Utils.h"
#include "Logger.h"

#include "InputHook.h"

#include <algorithm>

namespace HookLL
{
    static InputHook *s_InputHook = nullptr;

    typedef HMODULE(WINAPI* LoadLibraryA_t)(LPCSTR lpLibFileName);
    typedef HMODULE(WINAPI* LoadLibraryW_t)(LPCWSTR lpLibFileName);
    typedef HMODULE(WINAPI* LoadLibraryExA_t)(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
    typedef HMODULE(WINAPI* LoadLibraryExW_t)(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
    typedef HMODULE(WINAPI* GetModuleHandleA_t)(LPCSTR lpModuleName);
    typedef HMODULE(WINAPI* GetModuleHandleW_t)(LPCWSTR lpModuleName);
    typedef BOOL(WINAPI* GetModuleHandleExA_t)(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule);
    typedef BOOL(WINAPI* GetModuleHandleExW_t)(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule);

    static LoadLibraryW_t TrueLoadLibraryW = nullptr;
    static LoadLibraryA_t TrueLoadLibraryA = nullptr;
    static LoadLibraryExA_t TrueLoadLibraryExA = nullptr;
    static LoadLibraryExW_t TrueLoadLibraryExW = nullptr;
    static GetModuleHandleA_t TrueGetModuleHandleA = nullptr;
    static GetModuleHandleW_t TrueGetModuleHandleW = nullptr;
    static GetModuleHandleExA_t TrueGetModuleHandleExA = nullptr;
    static GetModuleHandleExW_t TrueGetModuleHandleExW = nullptr;

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
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueLoadLibraryA(lpLibFileName);

        if (SelfCheckA(lpLibFileName))
        {
            PrintLog("LoadLibraryA");
            s_InputHook->StartTimeoutThread();

            std::string path;

            if (ModuleFullPath(&path, s_InputHook->GetEmulator()))
                return TrueLoadLibraryA(path.c_str());
            else
                return TrueLoadLibraryA(lpLibFileName);
        }

        return TrueLoadLibraryA(lpLibFileName);
    }

    HMODULE WINAPI HookLoadLibraryW(LPCWSTR lpLibFileName)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueLoadLibraryW(lpLibFileName);

        if (SelfCheckW(lpLibFileName))
        {
            PrintLog("LoadLibraryW");
            s_InputHook->StartTimeoutThread();

            std::wstring path;

            if (ModuleFullPath(&path, s_InputHook->GetEmulator()))
                return TrueLoadLibraryW(path.c_str());
            else
                return TrueLoadLibraryW(lpLibFileName);
        }

        return TrueLoadLibraryW(lpLibFileName);
    }

    HMODULE WINAPI HookLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueLoadLibraryExA(lpLibFileName, hFile, dwFlags);

        if (SelfCheckA(lpLibFileName))
        {
            PrintLog("LoadLibraryExA");
            s_InputHook->StartTimeoutThread();

            std::string path;

            if (ModuleFullPath(&path, s_InputHook->GetEmulator()))
                return TrueLoadLibraryExA(path.c_str(), hFile, dwFlags);
            else
                return TrueLoadLibraryExA(lpLibFileName, hFile, dwFlags);
        }

        return TrueLoadLibraryExA(lpLibFileName, hFile, dwFlags);
    }

    HMODULE WINAPI HookLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueLoadLibraryExW(lpLibFileName, hFile, dwFlags);

        if (SelfCheckW(lpLibFileName))
        {
            PrintLog("LoadLibraryExW");
            s_InputHook->StartTimeoutThread();

            std::wstring path;

            if (ModuleFullPath(&path, s_InputHook->GetEmulator()))
                return TrueLoadLibraryExW(path.c_str(), hFile, dwFlags);
            else
                return TrueLoadLibraryExW(lpLibFileName, hFile, dwFlags);
        }

        return TrueLoadLibraryExW(lpLibFileName, hFile, dwFlags);
    }

    HMODULE WINAPI HookGetModuleHandleA(LPCSTR lpModuleName)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleA(lpModuleName);

        if (SelfCheckA(lpModuleName))
        {
            PrintLog("GetModuleHandleA");
            s_InputHook->StartTimeoutThread();
            return s_InputHook->GetEmulator();
        }

        return TrueGetModuleHandleA(lpModuleName);
    }

    HMODULE WINAPI HookGetModuleHandleW(LPCWSTR lpModuleName)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleW(lpModuleName);

        if (SelfCheckW(lpModuleName))
        {
            PrintLog("GetModuleHandleW");
            s_InputHook->StartTimeoutThread();
            return s_InputHook->GetEmulator();
        }

        return TrueGetModuleHandleW(lpModuleName);
    }

    BOOL WINAPI HookGetModuleHandleExA(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleExA(dwFlags, lpModuleName, phModule);

        if (SelfCheckA(lpModuleName))
        {
            PrintLog("GetModuleHandleExA");
            s_InputHook->StartTimeoutThread();
            phModule = &s_InputHook->GetEmulator();
            return TRUE;
        }

        return TrueGetModuleHandleExA(dwFlags, lpModuleName, phModule);
    }

    BOOL WINAPI HookGetModuleHandleExW(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_LL)) return TrueGetModuleHandleExW(dwFlags, lpModuleName, phModule);

        if (SelfCheckW(lpModuleName))
        {
            PrintLog("GetModuleHandleExW");
            s_InputHook->StartTimeoutThread();
            phModule = &s_InputHook->GetEmulator();
            return TRUE;
        }

        return TrueGetModuleHandleExW(dwFlags, lpModuleName, phModule);
    }
}

void InputHook::HookLL()
{
    PrintLog("Hooking DLL Loader");
    HookLL::s_InputHook = this;

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
