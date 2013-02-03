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
#include "dinput8.h"

HINSTANCE hThis = NULL;
CRITICAL_SECTION cs;

#pragma comment(lib,"Shlwapi.lib")

DirectInput8Create_t hDirectInput8Create = NULL;
DllCanUnloadNow_t hDllCanUnloadNow = NULL;
DllGetClassObject_t hDllGetClassObject = NULL;
DllRegisterServer_t hDllRegisterServer = NULL;
DllUnregisterServer_t hDllUnregisterServer = NULL;

void LoadXinputDLL()
{
    wchar_t buffer[MAX_PATH];
    wchar_t strPath[MAX_PATH];

    GetModuleFileName(hThis, strPath, MAX_PATH);
    PathRemoveFileSpec(strPath);

    swprintf_s(buffer,L"%s\\%s",strPath,L"xinput9_1_0.dll");
    LoadLibrary(buffer);

    swprintf_s(buffer,L"%s\\%s",strPath,L"xinput1_1.dll");
    LoadLibrary(buffer);

    swprintf_s(buffer,L"%s\\%s",strPath,L"xinput1_2.dll");
    LoadLibrary(buffer);

    swprintf_s(buffer,L"%s\\%s",strPath,L"xinput1_3.dll");
    LoadLibrary(buffer);
}

void LoadDInputDll()
{
    WCHAR sysdir[MAX_PATH];
    WCHAR buffer[MAX_PATH];

    GetSystemDirectory(sysdir,MAX_PATH);
    swprintf_s(buffer,L"%s\\%s",sysdir,L"dinput8.dll");

    HINSTANCE hDInput = LoadLibrary(buffer);

    //Debug
    if (!hDInput)
    {
        HRESULT hr = GetLastError();
        swprintf_s(sysdir,L"Cannot load %s error: 0x%x", buffer, hr);
        MessageBox(NULL,sysdir,L"Error",MB_ICONERROR);
        ExitProcess(hr);
    }

    hDirectInput8Create = (DirectInput8Create_t) GetProcAddress(hDInput,"DirectInput8Create");
    hDllCanUnloadNow = (DllCanUnloadNow_t) GetProcAddress(hDInput,"DllCanUnloadNow");
    hDllGetClassObject = (DllGetClassObject_t) GetProcAddress(hDInput,"DllGetClassObject");
    hDllRegisterServer = (DllRegisterServer_t) GetProcAddress(hDInput,"DllRegisterServer");
    hDllUnregisterServer = (DllUnregisterServer_t) GetProcAddress(hDInput,"DllUnregisterServer");
}

void InitInstance(HMODULE hMod)
{ 
    __try 
    {
        InitializeCriticalSection(&cs);
    }
    __except(GetExceptionCode() == STATUS_NO_MEMORY ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        MessageBoxA(NULL,"Cannot initialize critical section, fatal error","Error",MB_ICONERROR);
        ExitProcess(1);
    }

    EnterCriticalSection(&cs);
    LoadDInputDll();
    LoadXinputDLL();
    hThis = hMod;
    LeaveCriticalSection(&cs);
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
        DeleteCriticalSection(&cs);
        break;
    }
    return TRUE;
}

