/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2011 Robert Krawczyk
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
#include "Utilities\Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"

CRITICAL_SECTION cs;
DWORD startThread = NULL;
BOOL cleanDeinit = FALSE;

HINSTANCE hThis = NULL;
HINSTANCE hNative = NULL;

iHook* g_iHook;

extern WNDPROC oldWndProc;
extern HWND g_hWnd;

void LoadXInputDLL()
{
	if(hNative) return;
    WCHAR sysdir[MAX_PATH];
    WCHAR buffer[MAX_PATH];

    // Getting path to system dir and to xinput1_3.dll
    GetSystemDirectory(sysdir,MAX_PATH);

    // Append dll name
    swprintf_s(buffer,L"%s\\%s",sysdir,GetFileName(hThis).c_str());

    // try to load the system's xinput dll, if pointer empty
    WriteLog(LOG_CORE,L"Loading %s",buffer);

	hNative = LoadLibrary(buffer);

    //Debug
    if (!hNative)
    {
		HRESULT hr = GetLastError();
		swprintf_s(sysdir,L"Cannot load %s error: 0x%x", buffer, hr);
		WriteLog(LOG_CORE,L"%s", sysdir);
		MessageBox(NULL,sysdir,L"Error",MB_ICONERROR);
		ExitProcess(hr);
    }
}

VOID InstallInputHooks()
{
	if(g_iHook->GetState())
	{
		for(WORD i = 0; i < g_Gamepads.size(); i++)
		{
			iHookPadConfig padconf;
			padconf.Enable();
			padconf.SetProductGUID(g_Gamepads[i].productGUID);
			padconf.SetInstanceGUID(g_Gamepads[i].instanceGUID);
			g_iHook->AddHook(padconf);
		}
	}

	g_iHook->ExecuteHooks();
}

VOID ExitInstance()
{
	EnterCriticalSection(&cs);

	FreeDinput();
	SAFE_DELETE(g_iHook);

	//we can deinit here
	if(startThread == GetCurrentThreadId())
	{
		if(hNative)
		{
			WriteLog(LOG_CORE,L"Unloading %s",GetFilePath(hNative).c_str());
			FreeLibrary(hNative);
			hNative = NULL;
		}
		if(IsWindow(g_hWnd))
		{
			SetWindowLongPtr(g_hWnd, GWLP_WNDPROC, (LONG_PTR) oldWndProc);
			WriteLog(LOG_CORE,L"Destroying message window");
			DestroyWindow(g_hWnd);
			g_hWnd = NULL;
		}
		cleanDeinit = TRUE;
	}
	//else - try deinit in window proc
	else if(IsWindow(g_hWnd)) SendMessage(g_hWnd,MYQUITMSG,NULL,NULL);

	if(!cleanDeinit) WriteLog(LOG_CORE,L"Dirty deinit detected, please report issue");
	WriteLog(LOG_CORE,L"x360ce terminating, bye");

	LogCleanup();

	LeaveCriticalSection(&cs);
	DeleteCriticalSection(&cs);
}

VOID InitInstance(HINSTANCE hinstDLL)
{
#if defined(DEBUG) | defined(_DEBUG)
    int CurrentFlags;
    CurrentFlags = _CrtSetDbgFlag(_CRTDBG_REPORT_FLAG);
    CurrentFlags |= _CRTDBG_DELAY_FREE_MEM_DF;
    CurrentFlags |= _CRTDBG_LEAK_CHECK_DF;
    CurrentFlags |= _CRTDBG_CHECK_ALWAYS_DF;
    _CrtSetDbgFlag(CurrentFlags);
#endif

	InitializeCriticalSection(&cs);
	EnterCriticalSection(&cs);
    hThis =  hinstDLL;

	startThread = GetCurrentThreadId();

	g_iHook = new iHook;

	InI ini;
    ini.SetIniFileName(L"x360ce.ini");
    ReadConfig(ini);
    Console();
    CreateLog();
    WriteStamp();

	DWORD dwAppPID = GetCurrentProcessId();
#if SVN_MODS != 0
    WriteLog(LOG_CORE,L"x360ce %d.%d.%d.%dM [%s - %d]",VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,GetFileName().c_str(),dwAppPID);
#else
    WriteLog(LOG_CORE,L"x360ce %d.%d.%d.%d [%s - %d]",VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,GetFileName().c_str(),dwAppPID);
#endif

    WriteLog(LOG_CORE,L"http://code.google.com/p/x360ce");

    InstallInputHooks();
	LeaveCriticalSection(&cs);
}

extern "C" VOID WINAPI reset()
{
	FreeDinput();
	SAFE_DELETE(g_iHook);

	g_Gamepads.clear();
	GamepadMapping.clear();

	InitInstance(hThis);
}

extern "C" BOOL WINAPI DllMain( HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved )
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch( fdwReason )
    {
    case DLL_PROCESS_ATTACH:
        InitInstance(hinstDLL);
        break;

    case DLL_PROCESS_DETACH:
        ExitInstance();
        break;
    }

    return TRUE;
}