/*  x360ce - XBOX360 Controller Emulator
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
#include "dinput8.h"

#include <string>

HINSTANCE hThis = NULL;
HINSTANCE hXInput = NULL;
HINSTANCE hDInput = NULL;

#pragma comment(lib,"Shlwapi.lib")

void LoadXinputDLL()
{
	LPVOID pReset = NULL;
    LPTSTR pFile = NULL;

    TCHAR path[MAX_PATH];
	TCHAR buffer[MAX_PATH];
    GetModuleFileName(hThis, path, MAX_PATH);
    PathRemoveFileSpec(path);
    PathAddBackslash(path);

	pFile = PathCombine(buffer,path,_T("xinput1_4.dll")); 
    if(pFile)
    {
        hXInput = LoadLibrary(pFile);
        pReset = GetProcAddress(hXInput,"reset");
        if(!pReset) FreeLibrary(hXInput);
    }

    pFile = PathCombine(buffer,path,_T("xinput1_3.dll")); 
    if(pFile)
    {
        hXInput = LoadLibrary(pFile);
        pReset = GetProcAddress(hXInput,"reset");
        if(!pReset) FreeLibrary(hXInput);
    }

    pFile = PathCombine(buffer,path,_T("xinput1_2.dll")); 
    if(pFile)
    {
        hXInput = LoadLibrary(pFile);
        pReset = GetProcAddress(hXInput,"reset");
        if(!pReset) FreeLibrary(hXInput);
    }

    pFile = PathCombine(buffer,path,_T("xinput1_1.dll")); 
    if(pFile)
    {
        hXInput = LoadLibrary(pFile);
        pReset = GetProcAddress(hXInput,"reset");
        if(!pReset) FreeLibrary(hXInput);
    }

    pFile = PathCombine(buffer,path,_T("xinput9_1_0.dll")); 
    if(pFile)
    {
        hXInput = LoadLibrary(pFile);
        pReset = GetProcAddress(hXInput,"reset");
        if(!pReset) FreeLibrary(hXInput);
    }

    if(!pReset)
    {
        pFile = PathCombine(buffer,path,_T("xinput9_1_0.dll")); 
        if(pFile) hXInput = LoadLibrary(pFile);

    }
}

void LoadDInputDll()
{
    LPTSTR pFile = NULL;
    TCHAR buffer[MAX_PATH];
    GetSystemDirectory(buffer,MAX_PATH);
    PathAddBackslash(buffer);
	pFile = PathCombine(buffer,buffer,_T("dinput8.dll"));
    if(pFile) hDInput = LoadLibrary(pFile);

    if (hDInput == NULL)
    {
        TCHAR error[MAX_PATH];
        HRESULT hr = GetLastError();
        _stprintf_s(error,MAX_PATH,_T("Cannot load %s error: 0x%x"), pFile, hr);
        MessageBox(NULL,error,_T("Error"),MB_ICONERROR);;
        ExitProcess(hr);
    }
}

void ExitInstance()
{
    if(hDInput) FreeLibrary(hDInput);
    if(hXInput) FreeLibrary(hXInput);
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
		hThis = hModule;
        DisableThreadLibraryCalls(hModule);
        break;
    case DLL_PROCESS_DETACH:
        ExitInstance();
        break;
    }
    return TRUE;
}

