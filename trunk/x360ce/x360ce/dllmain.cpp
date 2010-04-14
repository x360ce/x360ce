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
#include "utils.h"
#include "config.h"
#include "directinput.h"
#include "FakeAPI.h"
#include "svnrev.h"

HINSTANCE hX360ceInstance = NULL;
HINSTANCE hNativeInstance = NULL;

HWND hWnd = NULL;
DWORD dwAppPID = NULL;

extern void FakeWMI();
extern void FakeDI();

BOOL RegisterWindowClass(HINSTANCE hinstance) 
{ 
	WNDCLASSEX wcx; 

	// Fill in the window class structure with parameters 
	// that describe the main window. 

	wcx.cbSize = sizeof(wcx);          // size of structure 
	wcx.style = CS_HREDRAW | CS_VREDRAW;
	wcx.lpfnWndProc = DefWindowProc;     // points to window procedure 
	wcx.cbClsExtra = 0;                // no extra class memory 
	wcx.cbWndExtra = 0;                // no extra window memory 
	wcx.hInstance = hinstance;         // handle to instance 
	wcx.hIcon = LoadIcon(NULL, IDI_APPLICATION);              // predefined app. icon 
	wcx.hCursor = LoadCursor(NULL, IDC_ARROW);                    // predefined arrow 
	wcx.hbrBackground = (HBRUSH) GetStockObject(WHITE_BRUSH);    // white background brush 
	wcx.hIconSm = (HICON) LoadImage(hinstance, // small class icon 
		MAKEINTRESOURCE(5),
		IMAGE_ICON, 
		GetSystemMetrics(SM_CXSMICON), 
		GetSystemMetrics(SM_CYSMICON), 
		LR_DEFAULTCOLOR); 
	wcx.lpszMenuName =  _T("x360ceMenu");    // name of menu resource
	wcx.lpszClassName = _T("x360ceWClass");  // name of window class 

	// Register the window class. 

	return RegisterClassEx(&wcx); 
} 

BOOL Createx360ceWindow(HINSTANCE hInst)
{
	if(RegisterWindowClass(hInst))
	{
		hWnd = CreateWindow( 
			_T("x360ceWClass"),  // name of window class
			_T("x360ce"),        // title-bar string 
			WS_OVERLAPPEDWINDOW, // top-level window 
			CW_USEDEFAULT,       // default horizontal position 
			CW_USEDEFAULT,       // default vertical position 
			CW_USEDEFAULT,       // default width 
			CW_USEDEFAULT,       // default height 
			(HWND) NULL,         // no owner window 
			(HMENU) NULL,        // use class menu 
			hX360ceInstance,     // handle to application instance 
			(LPVOID) NULL);      // no window-creation data 

		return TRUE;
	}
	else
	{
		WriteLog(_T("RegisterWindowClass Failed"));
		return FALSE;
	}
	return FALSE;
}

VOID InitInstance(HMODULE hModule) 
{
#if defined(DEBUG) | defined(_DEBUG)
	_CrtSetDbgFlag( _CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF );
#endif

	hX360ceInstance = (HINSTANCE) hModule;

	DisableThreadLibraryCalls(hX360ceInstance);

	dwAppPID=GetCurrentProcessId();

	InitConfig();

#if SVN_MODS == 1 
	WriteLog(_T("x360ce SVN Revision %d (modded) started by process %s PID %d"),SVN_REV,PIDName(dwAppPID),dwAppPID);
#else 
	WriteLog(_T("x360ce SVN Revision %d started by process %s PID %d"),SVN_REV,PIDName(dwAppPID),dwAppPID);
#endif

	Createx360ceWindow(hX360ceInstance);

	if(!hWnd)
	{
		WriteLog(_T("x360ce window not created, ForceFeedback will be disabled !"));
	}

	if(wFakeAPI)
	{
		if(wFakeWMI) FakeWMI();
		if(wFakeDI) FakeDI();
	}
}

VOID ExitInstance() 
{   
	UnregisterClass(_T("x360ceWClass"),hX360ceInstance);

	delete[] logfilename;
	for(INT i=0;i<EnumPadCount();i++)
	{
		Gamepad[i].g_pGamepad = NULL;
	}
	g_pDI = NULL;

	if (hNativeInstance)
	{
		::FreeLibrary(hNativeInstance);
		hNativeInstance = NULL;  
		CloseHandle(hNativeInstance);
	}

	if(hWnd)CloseHandle(hWnd);
	if(hX360ceInstance)CloseHandle(hX360ceInstance);
}

extern "C" BOOL WINAPI DllMain(HINSTANCE hinst, DWORD dwReason, LPVOID reserved)
{
	UNREFERENCED_PARAMETER(reserved);

	if (dwReason == DLL_PROCESS_ATTACH ) 
	{
		InitInstance(hinst);
	}

	else if (dwReason == DLL_PROCESS_DETACH) 
	{
		WriteLog(_T("x360ce terminating, bye"));
		ExitInstance();
	}
	return TRUE;
}

//Deutored.dll
HMODULE WINAPI Detoured()
{
	return (HMODULE) hX360ceInstance;
}