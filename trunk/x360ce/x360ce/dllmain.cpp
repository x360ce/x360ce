/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#include "stdafx.h"
#include "globals.h"
#include "version.h"
#include "x360ce.h"
#include "SWIP.h"
#include "Log.h"
#include "Misc.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"

extern WNDPROC oldWndProc;
extern HWND hMsgWnd;
extern std::vector<DInputDevice> g_Devices;
extern std::vector<Mapping> g_Mappings;

DWORD startProcessId = NULL;
DWORD startThreadId = NULL;
std::string exename;
HINSTANCE hThis = NULL;
HINSTANCE hNative = NULL;
iHook* pHooks = NULL;

INITIALIZE_LOGGER;

VOID InstallInputHooks()
{
    if(pHooks)
    {
        for(auto device = g_Devices.begin(); device != g_Devices.end(); ++device)
            pHooks->AddHook(device->dwUserIndex, device->productid, device->instanceid);
    }
    pHooks->ExecuteHooks();
}

VOID ExitInstance()
{
    if(IsWindow(hMsgWnd))
    {
        if(DestroyWindow(hMsgWnd)) PrintLog(LOG_CORE,"Message window destroyed");
    }
    else
    {
        SAFE_DELETE(pHooks);
        if(hNative)
        {
            PrintLog(LOG_CORE,"Unloading %s",ModuleFullPathA(hNative).c_str());
            FreeLibrary(hNative);
            hNative = NULL;
        }
    }

    PrintLog(LOG_CORE,"Terminating x360ce, bye");
}

VOID InitInstance(HINSTANCE instance)
{
#if defined(DEBUG) | defined(_DEBUG)
    int CurrentFlags;
    CurrentFlags = _CrtSetDbgFlag(_CRTDBG_REPORT_FLAG);
    CurrentFlags |= _CRTDBG_DELAY_FREE_MEM_DF;
    CurrentFlags |= _CRTDBG_LEAK_CHECK_DF;
    CurrentFlags |= _CRTDBG_CHECK_ALWAYS_DF;
    _CrtSetDbgFlag(CurrentFlags);
#endif

    hThis = instance;
    startThreadId = GetCurrentThreadId();
    startProcessId = GetCurrentProcessId();
    exename = ModuleFileNameA();

    pHooks = new iHook(instance);
    ReadConfig();

	PrintNotice();
    PrintLog(LOG_CORE,"x360ce %s [%s - %d]",PRODUCT_VERSION,exename.c_str(),startProcessId);
    PrintLog(LOG_CORE,"%s",windowsVersionName().c_str());

    InstallInputHooks();
}

extern "C" VOID WINAPI reset()
{
    PrintLog(LOG_CORE,"%s", "Restarting");
    SAFE_DELETE(pHooks);

    g_Devices.clear();
    g_Mappings.clear();

    ReadConfig();
}

extern "C" BOOL APIENTRY DllMain( HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch(ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        InitInstance(hModule);
        break;

    case DLL_PROCESS_DETACH:
        ExitInstance();
        break;
    }

    return TRUE;
}