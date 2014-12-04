#include "stdafx.h"
#include "Common.h"
#include "SWIP.h"
#include "Logger.h"
#include "Utils.h"

#include "SWIP.h"
#include "Config.h"

#include "ControllerManager.h"
#include "InputHookManager.h"

void __cdecl ExitInstance()
{
    PrintLog("Process terminating, bye");
}

VOID InitInstance()
{
#if defined(DEBUG) | defined(_DEBUG)
    int CurrentFlags;
    CurrentFlags = _CrtSetDbgFlag(_CRTDBG_REPORT_FLAG);
    CurrentFlags |= _CRTDBG_DELAY_FREE_MEM_DF;
    CurrentFlags |= _CRTDBG_LEAK_CHECK_DF;
    CurrentFlags |= _CRTDBG_CHECK_ALWAYS_DF;
    _CrtSetDbgFlag(CurrentFlags);
#endif

    atexit(ExitInstance);
    InitLogger();

    // GetInputHook will initalize static InputHook object and we want to initialize it ASAP
    InputHookManager::Get().GetInputHook();
}

extern "C" VOID WINAPI Reset()
{
    PrintLog("Restarting");

    // Only x360ce.App will call this so InputHook is not required, disable it.
    InputHookManager::Get().GetInputHook().Shutdown();
    ControllerManager::Get().GetControllers().clear();
    ReadConfig();
}

extern "C" BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls(hModule);
            InitInstance();
            break;

        case DLL_PROCESS_DETACH:
            break;
    }

    return TRUE;
}