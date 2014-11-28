#include "stdafx.h"
#include "globals.h"
#include "SWIP.h"
#include "Logger.h"
#include "Misc.h"
#include "InputHook.h"

#include "DirectInput.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

InputHook g_iHook;

INITIALIZE_LOGGER;

HMODULE g_CurrentModule = NULL;

VOID InstallInputHooks()
{
    for (auto device = g_Devices.begin(); device != g_Devices.end(); ++device)
    {
        g_iHook.AddHook(device->dwUserIndex, device->productid, device->instanceid);
    }

    g_iHook.ExecuteHooks();
}

void __cdecl ExitInstance()
{
    if (hMsgWnd && DestroyWindow(hMsgWnd))
        PrintLog("Message window destroyed");

    g_iHook.Reset();
    if (xinput.dll)
    {
        std::string xinput_path;
        ModuleFullPathA(&xinput_path, xinput.dll);
        PrintLog("Unloading %s", xinput_path.c_str());
        FreeLibrary(xinput.dll);
        xinput.dll = NULL;
    }

    PrintLog("Terminating x360ce, bye");
}

VOID InitInstance(HMODULE hModule)
{
#if defined(DEBUG) | defined(_DEBUG)
    int CurrentFlags;
    CurrentFlags = _CrtSetDbgFlag(_CRTDBG_REPORT_FLAG);
    CurrentFlags |= _CRTDBG_DELAY_FREE_MEM_DF;
    CurrentFlags |= _CRTDBG_LEAK_CHECK_DF;
    CurrentFlags |= _CRTDBG_CHECK_ALWAYS_DF;
    _CrtSetDbgFlag(CurrentFlags);
#endif

    g_CurrentModule = hModule;
    atexit(ExitInstance);
  
    ModuleFileNameA(&exename);
    ReadConfig();   

    InstallInputHooks();
}

extern "C" VOID WINAPI reset()
{
    PrintLog("Restarting");

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
        InitInstance(hModule);
        break;

    case DLL_PROCESS_DETACH:
        break;
    }

    return TRUE;
}