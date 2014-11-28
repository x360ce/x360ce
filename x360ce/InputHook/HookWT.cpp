#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include <Softpub.h>

#include "InputHook.h"

namespace HookWT
{
    static InputHook *s_InputHook = nullptr;

    typedef LONG(WINAPI* WinVerifyTrust_t)(HWND hwnd, GUID *pgActionID, LPVOID pWVTData);

    static WinVerifyTrust_t TrueWinVerifyTrust = nullptr;

    LONG WINAPI HookWinVerifyTrust(HWND hwnd, GUID *pgActionID, LPVOID pWVTData)
    {
        if (!s_InputHook->GetState(InputHook::HOOK_WT)) return TrueWinVerifyTrust(hwnd, pgActionID, pWVTData);
        PrintLog("*WinVerifyTrust*");

        UNREFERENCED_PARAMETER(hwnd);
        UNREFERENCED_PARAMETER(pgActionID);
        UNREFERENCED_PARAMETER(pWVTData);
        return 0;
    }
}

void InputHook::HookWT()
{
    HookWT::s_InputHook = this;
    IH_CreateHook(WinVerifyTrust, HookWT::HookWinVerifyTrust, &HookWT::TrueWinVerifyTrust);
}