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
    char* buffer = new char[MAX_PATH];
    GetModuleFileNameA(hThis, buffer, MAX_PATH);
    PathRemoveFileSpecA(buffer);
    PathAddBackslashA(buffer);
    std::string path(buffer);
    delete [] buffer;

    hXInput = LoadLibraryA((path + "xinput1_4.dll").c_str());
    LPVOID hReset = GetProcAddress(hXInput,"reset");
    if(!hReset) FreeLibrary(hXInput);

    hXInput = LoadLibraryA((path + "xinput1_3.dll").c_str());
    hReset = GetProcAddress(hXInput,"reset");
    if(!hReset) FreeLibrary(hXInput);

    hXInput = LoadLibraryA((path + "xinput1_2.dll").c_str());
    hReset = GetProcAddress(hXInput,"reset");
    if(!hReset) FreeLibrary(hXInput);

    hXInput = LoadLibraryA((path + "xinput1_1.dll").c_str());
    hReset = GetProcAddress(hXInput,"reset");
    if(!hReset) FreeLibrary(hXInput);

    hXInput = LoadLibraryA((path + "xinput9_1_0.dll").c_str());
    hReset = GetProcAddress(hXInput,"reset");
    if(!hReset) FreeLibrary(hXInput);

    if(!hReset)
    {
        hXInput = LoadLibraryA((path + "x360ce.dll").c_str());
    }
}

void LoadDInputDll()
{
    char* buffer = new char[MAX_PATH];
    GetSystemDirectoryA(buffer,MAX_PATH);
    PathAddBackslashA(buffer);
    std::string path(buffer);
    delete [] buffer;

    path.append("dinput8.dll");
    hDInput = LoadLibraryA(path.c_str());

    if (!hDInput)
    {
        char* buf = new char[MAX_PATH];
        HRESULT hr = GetLastError();
        sprintf_s(buf,MAX_PATH,"Cannot load %s error: 0x%x", path.c_str(), hr);
        MessageBoxA(NULL,buf,"Error",MB_ICONERROR);
        delete [] buf;
        ExitProcess(hr);
    }
}

void ExitInstance()
{
    if(hDInput) FreeLibrary(hDInput);
    if(hXInput) FreeLibrary(hXInput);
}

void InitInstance(HMODULE hMod)
{
    hThis = hMod;

    // TODO: Find better place for this if possible
    LoadXinputDLL();
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
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

