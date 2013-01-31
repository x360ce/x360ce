/*  x360ce - XBOX360 Controler Emulator
*  Copyright (C) 2002-2010 Racer_S
*  Copyright (C) 2010-2013 Robert Krawczyk
*
*  x360ce is free software: you can redistribute it and/or modify it under the terms
*  of the GNU Lesser General Public License as published by the Free Software Found-
*  ation, either version 3 of the License, or (at your option) any later version.
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

DWORD startProcessId = NULL;
DWORD startThreadId = NULL;

HINSTANCE hThis = NULL;
HINSTANCE hNative = NULL;

iHook* pHooks;

extern WNDPROC oldWndProc;
extern HWND hMsgWnd;

void LoadXInputDLL()
{
    if(hNative) return;
    WCHAR sysdir[MAX_PATH];
    WCHAR buffer[MAX_PATH];

    // Getting path to system dir and to xinput1_3.dll
    GetSystemDirectory(sysdir,MAX_PATH);

    // Append dll name
    swprintf_s(buffer,L"%s\\%s",sysdir,Misc::GetFileNameW(hThis));

    // try to load the system's xinput dll, if pointer empty
    PrintLog(LOG_CORE,"Loading %ls",buffer);

    hNative = LoadLibrary(buffer);

    //Debug
    if (!hNative)
    {
        HRESULT hr = GetLastError();
        swprintf_s(sysdir,L"Cannot load %s error: 0x%x", buffer, hr);
        PrintLog(LOG_CORE,"%s", sysdir);
        MessageBox(NULL,sysdir,L"Error",MB_ICONERROR);
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

#if _MSC_VER < 1700
            iHookDevice hdevice(device.productid,device.instanceid);
            pHooks->AddHook(hdevice);
#else
            pHooks->AddHook(device.productid,device.instanceid);
#endif
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
            PrintLog(LOG_CORE,"Unloading %s",Misc::GetFilePathA(hNative));
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

    hThis =  instance;
    startThreadId = GetCurrentThreadId();
    startProcessId = GetCurrentProcessId();

    pHooks = new iHook();
    ReadConfig();

#if SVN_MODS != 0
    PrintLog(LOG_CORE,"x360ce %d.%d.%d.%dM [%s - %d]",VERSION_MAJOR,VERSION_MINOR,
             VERSION_PATCH,SVN_REV,Misc::GetFileNameA(),startProcessId);
#else
    PrintLog(LOG_CORE,"x360ce %d.%d.%d.%d [%s - %d]",VERSION_MAJOR,VERSION_MINOR,
             VERSION_PATCH,SVN_REV,Misc::GetFileNameA(),startProcessId);
#endif

    InstallInputHooks();
}

extern "C" VOID WINAPI reset()
{
    PrintLog(LOG_CORE,"%s", "Restarting");

    SAFE_DELETE(pHooks);

    g_Devices.clear();
    g_Mappings.clear();

    InitInstance(hThis);
}

extern "C" BOOL WINAPI DllMain( HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved )
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch( fdwReason )
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hinstDLL);
        InitInstance(hinstDLL);
        break;

    case DLL_PROCESS_DETACH:
        ExitInstance();
        break;
    }

    return TRUE;
}