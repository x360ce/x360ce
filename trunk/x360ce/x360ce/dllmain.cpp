#include "stdafx.h"
#include "globals.h"
#include "version.h"
#include "SWIP.h"
#include "Logger.h"
#include "Misc.h"
#include "InputHook\InputHook.h"

#include "DirectInput.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

std::string exename;
iHook g_iHook;

INITIALIZE_LOGGER;

VOID InstallInputHooks()
{
    for (auto & device = g_Devices.begin(); device != g_Devices.end(); ++device)
        g_iHook.AddHook(device->dwUserIndex, device->productid, device->instanceid);

    g_iHook.ExecuteHooks();
}

void __cdecl ExitInstance()
{
    if (hMsgWnd && DestroyWindow(hMsgWnd))
        PrintLog("Message window destroyed");

    g_iHook.Reset();
    if (xinput.dll)
    {
        PrintLog("Unloading %s", ModuleFullPathA(xinput.dll).c_str());
        FreeLibrary(xinput.dll);
        xinput.dll = NULL;
    }

    PrintLog("Terminating x360ce, bye");
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

    DWORD startProcessId = GetCurrentProcessId();
    exename = ModuleFileNameA();

    ReadConfig();

    PrintLog("x360ce %s [%s - %d]", PRODUCT_VERSION, exename.c_str(), startProcessId);
    PrintLog("%s", windowsVersionName().c_str());

    InstallInputHooks();
}

extern "C" VOID WINAPI reset()
{
    PrintLog("%s", "Restarting");

    g_iHook.Reset();

    g_Devices.clear();
    g_Mappings.clear();

    ReadConfig(true);
    InstallInputHooks();
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