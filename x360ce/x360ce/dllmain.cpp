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
#include "Utilities\Ini.h"
#include "Log.h"
#include "Utilities\Misc.h"
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

void LoadXInputDLL()
{
    if(hNative) return;
    WCHAR sysdir[MAX_PATH];
    WCHAR buffer[MAX_PATH];

    GetSystemDirectoryW(sysdir,MAX_PATH);
    PathCombineW(buffer,sysdir,ModuleFileNameW(hThis));

    bool bHookLL = false;
    if(pHooks)
    {
        bHookLL = pHooks->GetState(iHook::HOOK_LL);
        if(bHookLL) pHooks->DisableHook(iHook::HOOK_LL);
    }

    PrintLog(LOG_CORE,"Loading %ls",buffer);
    hNative = LoadLibraryW(buffer);
    if(bHookLL) pHooks->EnableHook(iHook::HOOK_LL);

    //Debug
    if (!hNative)
    {
        HRESULT hr = GetLastError();
        swprintf_s(sysdir,L"Cannot load %s error: 0x%x", buffer, hr);
        PrintLog(LOG_CORE,"%s", sysdir);
        MessageBoxW(NULL,sysdir,L"Error",MB_ICONERROR);
        ExitProcess(hr);
    }
}

VOID InstallInputHooks()
{
    if(pHooks->GetState())
    {
        for(size_t i = 0; i < g_Devices.size(); i++)
        {
            DInputDevice& device = g_Devices[i];
            pHooks->AddHook(device.productid,device.instanceid);
        }
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
            PrintLog(LOG_CORE,"Unloading %s",ModuleFullPathA(hNative));
            FreeLibrary(hNative);
            hNative = NULL;
        }
    }

    PrintLog(LOG_CORE,"Terminating x360ce, bye");
    DestroyLog();
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
    ReadConfig(false);

#if SVN_MODS != 0
    PrintLog(LOG_CORE,"x360ce %d.%d.%d.%dM [%s - %d]",VERSION_MAJOR,VERSION_MINOR,
             VERSION_PATCH,SVN_REV,exename.c_str(),startProcessId);
#else
    PrintLog(LOG_CORE,"x360ce %d.%d.%d.%d [%s - %d]",VERSION_MAJOR,VERSION_MINOR,
             VERSION_PATCH,SVN_REV,exename.c_str(),startProcessId);
#endif

    char osname[128];
    windowsVersionName(osname,MAX_PATH);
    PrintLog(LOG_CORE,"%s",osname);

    InstallInputHooks();
}

extern "C" VOID WINAPI reset()
{
    PrintLog(LOG_CORE,"%s", "Restarting");
    SAFE_DELETE(pHooks);

    g_Devices.clear();
    g_Mappings.clear();

    ReadConfig(true);
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