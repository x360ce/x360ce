#include "stdafx.h"
#include "Common.h"
#include "SWIP.h"
#include "Logger.h"
#include "Utils.h"
#include "InputHook.h"

#include "Controller.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

InputHook g_iHook;

void __cdecl ExitInstance()
{
    if (hMsgWnd && DestroyWindow(hMsgWnd))
        PrintLog("Message window destroyed");

    if (xinput.dll)
    {
        std::string xinput_path;
        ModuleFullPathA(&xinput_path, xinput.dll);
        PrintLog("Unloading %s", xinput_path.c_str());
        FreeLibrary(xinput.dll);
        xinput.dll = NULL;
    }

    PrintLog("Terminating x360ce, bye");
    LogShutdown();

    for (u32 i = 0; i < XUSER_MAX_COUNT; ++i)
        delete g_pControllers[i];
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
    g_iHook.Init(InitLogger);
}

extern "C" VOID WINAPI reset()
{
    PrintLog("Restarting");

    // Only x360ce.App will call this so InputHook is not required, disable it.
    g_iHook.Shutdown();

    for (u32 i = 0; i < XUSER_MAX_COUNT; ++i)
    {
        delete g_pControllers[i];
        g_pControllers[i] = nullptr;
    }

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