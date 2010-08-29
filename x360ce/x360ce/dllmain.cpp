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
#include "Utils.h"
#include "Config.h"
#include "DirectInput.h"
#include "FakeAPI.h"

HINSTANCE hX360ceInstance = NULL;
HINSTANCE hNativeInstance = NULL;

HWND hWnd = NULL;

void LoadOriginalDll()
{
	TCHAR buffer[MAX_PATH];

	// Getting path to system dir and to xinput1_3.dll
	GetSystemDirectory(buffer,MAX_PATH);

	// Append dll name
	wcscat_s(buffer,sizeof(buffer),L"\\xinput1_3.dll");

	// try to load the system's dinput.dll, if pointer empty
	if (!hNativeInstance) hNativeInstance = LoadLibrary(buffer);

	// Debug
	//if (!hNativeInstance) {
	//	ExitProcess(0); // exit the hard way
	//}
}

VOID AttachFakeAPI()
{
	if(wFakeMode) {
		if(wFakeMode) FakeWMI();
		if(wFakeMode>=2) FakeDI();
		if(wFakeWinTrust) FakeWinTrust();
	}
}

VOID DetachFakeAPI()
{
	if(wFakeMode) {
		if(wFakeMode) FakeWMI_Detach();
		if(wFakeMode>=2) FakeDI_Detach();
		if(wFakeWinTrust) FakeWinTrust_Detach();
	}
}

VOID InitInstance(HINSTANCE hinstDLL) 
{
#if defined(DEBUG) | defined(_DEBUG)
	_CrtSetDbgFlag( _CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF );
#endif

	hX360ceInstance =  hinstDLL;
	DWORD dwAppPID = GetCurrentProcessId();
	InitConfig(L"x360ce.ini");
	ReadConfig();
	CreateLog();

#if SVN_MODS != 0 
	WriteLog(_T("[CORE]    x360ce %d.%d.%d.%d (modded) started by process %s PID %d"),VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,ModuleFileName(),dwAppPID);
#else 
	WriteLog(_T("[CORE]    x360ce %d.%d.%d.%d started by process %s PID %d"),VERSION_MAJOR,VERSION_MINOR,VERSION_PATCH,SVN_REV,ModuleFileName(),dwAppPID);
#endif

	AttachFakeAPI();
}

VOID ExitInstance() 
{   
	DetachFakeAPI();

	ReleaseDirectInput();

	if (hNativeInstance) {
		FreeLibrary(hNativeInstance);
		hNativeInstance = NULL;  
		CloseHandle(hNativeInstance);
	}

	XDeInit();

	if(IsWindow(hWnd)) DestroyWindow(hWnd);
	hWnd = NULL;
	UnregisterClass(_T("x360ceWClass"),hX360ceInstance);
	WriteLog(_T("[CORE]    x360ce terminating, bye"));

	SAFE_DELETEARRAY(lpLogFileName);
	SAFE_DELETEARRAY(lpConfigFile);
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
