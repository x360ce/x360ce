#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include <Softpub.h>

#include "InputHookManager.h"
#include "InputHook.h"
#include "HookWT.h"

LONG(WINAPI* HookWT::TrueWinVerifyTrust)(HWND hwnd, GUID *pgActionID, LPVOID pWVTData) = nullptr;


LONG WINAPI HookWT::HookWinVerifyTrust(HWND hwnd, GUID *pgActionID, LPVOID pWVTData)
{
	if (!InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_WT)) return TrueWinVerifyTrust(hwnd, pgActionID, pWVTData);
	PrintLog("*WinVerifyTrust*");

	InputHookManager::Get().GetInputHook().StartTimeoutThread();

	UNREFERENCED_PARAMETER(hwnd);
	UNREFERENCED_PARAMETER(pgActionID);
	UNREFERENCED_PARAMETER(pWVTData);
	return 0;
}


void InputHook::HookWT()
{
	IH_CreateHook(WinVerifyTrust, HookWT::HookWinVerifyTrust, &HookWT::TrueWinVerifyTrust);
}
