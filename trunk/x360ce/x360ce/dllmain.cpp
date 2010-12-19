/*  x360ce - XBOX360 Controler Emulator
*  Copyright (C) 2002-2010 ToCA Edit
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

HINSTANCE g_hX360ceInstance = NULL;
HINSTANCE g_hNativeInstance = NULL;

HWND g_hWnd = NULL;

void LoadOriginalDll()
{
	WCHAR buffer[MAX_PATH];

	// Getting path to system dir and to xinput1_3.dll
	GetSystemDirectory(buffer,MAX_PATH);

	// Append dll name
	wcscat_s(buffer,MAX_PATH,L"\\xinput1_3.dll");

	// try to load the system's dinput.dll, if pointer empty
	if (!g_hNativeInstance) g_hNativeInstance = LoadLibrary(buffer);

	// Debug
	//if (!hNativeInstance) {
	//	ExitProcess(0); // exit the hard way
	//}
}

VOID InstallInputHooks()
{
	if(x360ce_InputHookConfig.bEnabled) {

		for(WORD i = 0; i < 4; i++)
		{
			x360ce_InputHookGamepadConfig[i].bEnabled = g_Gamepad[i].configured;
			x360ce_InputHookGamepadConfig[i].productGUID = g_Gamepad[i].productGUID;
			x360ce_InputHookGamepadConfig[i].instanceGUID = g_Gamepad[i].instanceGUID;
		}
	}
	InputHook_Init( &x360ce_InputHookConfig,  x360ce_InputHookGamepadConfig);
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

	g_hX360ceInstance =  hinstDLL;
	DWORD dwAppPID = GetCurrentProcessId();
	SetIniFileName(L"x360ce.ini");
	ReadConfig();
	Console();
	LogEnable(CreateLog(L"x360ce",sizeof(L"x360ce"),L"x360ce",sizeof(L"x360ce")));

#if SVN_MODS != 0 
	WriteLog(LOG_CORE,L"x360ce %d.%d.%d.%dM [%s - %d]",VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,ModuleFileName(),dwAppPID);
#else 
	WriteLog(LOG_CORE,L"x360ce %d.%d.%d.%d [%s - %d]",VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,ModuleFileName(),dwAppPID);
#endif

	InstallInputHooks();
}

VOID ExitInstance() 
{   
	InputHook_Clean();

	if (g_hNativeInstance) {
		FreeLibrary(g_hNativeInstance); 
		CloseHandle(g_hNativeInstance);
		g_hNativeInstance = NULL; 
	}

	if(IsWindow(g_hWnd)) DestroyWindow(g_hWnd);
	g_hWnd = NULL;
	UnregisterClass(L"x360ceWClass",g_hX360ceInstance);

	WriteLog(LOG_CORE,L"x360ce terminating, bye");

	LogCleanup();
	IniCleanup();
}

extern "C" BOOL WINAPI DllMain( HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved ) 
{
	UNREFERENCED_PARAMETER(lpReserved);

	switch( fdwReason ) 
	{ 
	case DLL_PROCESS_ATTACH:
		LhInitializeLibrary(hinstDLL);
		InitInstance(hinstDLL);
		break;

	case DLL_THREAD_DETACH:
		LhBarrierThreadDetach();
		break;

	case DLL_PROCESS_DETACH:
		ExitInstance();
		break;
	}
	return TRUE;
}