#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include <Softpub.h>

#include "InputHook.h"

class HookWT
{
	friend class InputHook;
private:
	static LONG(WINAPI* TrueWinVerifyTrust)(HWND hwnd, GUID *pgActionID, LPVOID pWVTData);
	static LONG WINAPI HookWinVerifyTrust(HWND hwnd, GUID *pgActionID, LPVOID pWVTData);
};
