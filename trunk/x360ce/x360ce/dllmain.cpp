#include "stdafx.h"
#include "Common.h"
#include "SWIP.h"
#include "Logger.h"
#include "Utils.h"

#include "SWIP.h"
#include "Config.h"

#include "ControllerManager.h"
#include "InputHookManager.h"

#if 1
#include <vld.h> 
#endif

VOID InitInstance()
{

    InitLogger();

    // Get will initalize static InputHookManager object and we want to initialize it ASAP
    InputHookManager::Get();
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