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

HINSTANCE hThis = NULL;
HINSTANCE hNative = NULL;
HINSTANCE hDinput8 = NULL;

extern HWND g_hWnd;
iHook g_iHook;

void LoadDinput8DLL()
{
	if(hDinput8) return;
	WCHAR sysdir[MAX_PATH];
	WCHAR buffer[MAX_PATH];

	// Getting path to system dir and to xinput1_3.dll
	GetSystemDirectory(sysdir,MAX_PATH);

	// Append dll name
	//wcscat_s(buffer,MAX_PATH,L"\\xinput1_3.dll");
	swprintf_s(buffer,L"%s\\%s",sysdir,L"dinput8.dll");

	// try to load the system's xinput dll, if pointer empty
	WriteLog(LOG_CORE,L"Loading %s",buffer);

	hDinput8 = LoadLibrary(buffer);

	//Debug
	if (!hDinput8)
	{
		HRESULT hr = GetLastError();
		swprintf_s(sysdir,L"Cannot load %s error: 0x%x", buffer, hr);
		WriteLog(LOG_CORE,L"%s", sysdir);
		MessageBox(NULL,sysdir,L"Error",MB_ICONERROR);
		ExitProcess(hr);
	}
}

void LoadXInputDLL(HMODULE &hMod = hNative)
{
	if(hMod) return;
    WCHAR sysdir[MAX_PATH];
    WCHAR buffer[MAX_PATH];

    // Getting path to system dir and to xinput1_3.dll
    GetSystemDirectory(sysdir,MAX_PATH);

    // Append dll name
    //wcscat_s(buffer,MAX_PATH,L"\\xinput1_3.dll");
    swprintf_s(buffer,L"%s\\%s",sysdir,GetFileName(hThis));

    // try to load the system's xinput dll, if pointer empty
    WriteLog(LOG_CORE,L"Loading %s",buffer);

	hMod = LoadLibrary(buffer);

    //Debug
    if (!hMod)
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
	if(g_iHook.GetState())
	{
		LoadDinput8DLL();
		g_iHook.SetDinput8(hDinput8);

		for(WORD i = 0; i < g_Gamepads.size(); i++)
		{
			iHookPadConfig padconf;
			padconf.Enable();
			padconf.SetProductGUID(g_Gamepads[i].productGUID);
			padconf.SetInstanceGUID(g_Gamepads[i].instanceGUID);
			g_iHook.AddHook(padconf);
		}
	}

	g_iHook.ExecuteHooks();
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

	InI ini;
    ini.SetIniFileName(L"x360ce.ini");
    ReadConfig(ini);
    Console();
    CreateLog();
    WriteStamp();

	DWORD dwAppPID = GetCurrentProcessId();
#if SVN_MODS != 0
    WriteLog(LOG_CORE,L"x360ce %d.%d.%d.%dM [%s - %d]",VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,GetFileName(),dwAppPID);
#else
    WriteLog(LOG_CORE,L"x360ce %d.%d.%d.%d [%s - %d]",VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,GetFileName(),dwAppPID);
#endif

    WriteLog(LOG_CORE,L"http://code.google.com/p/x360ce");

	LoadDinput8DLL();
	LoadXInputDLL();

    InstallInputHooks();
	LeaveCriticalSection(&cs);
}

VOID ExitInstance()
{
	EnterCriticalSection(&cs);

	FreeDinput();

    if (hNative)
    {
        FreeLibrary(hNative);
        hNative = NULL;
    }

	if (hDinput8)
	{
		FreeLibrary(hDinput8);
		hDinput8 = NULL;
	}

    if(IsWindow(g_hWnd)) DestroyWindow(g_hWnd);
    g_hWnd = NULL;

    WriteLog(LOG_CORE,L"x360ce terminating, bye");

    LogCleanup();

	LeaveCriticalSection(&cs);
	DeleteCriticalSection(&cs);
}

extern "C" VOID WINAPI reset()
{
	g_Gamepads.clear();
	GamepadMapping.clear();

	ExitInstance();
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