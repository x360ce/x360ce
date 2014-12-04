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

#include "Utils.h"

struct dinput8_dll
{
    HMODULE dll;

    HRESULT(WINAPI* DirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
    HRESULT(WINAPI* DllCanUnloadNow)(void);
    HRESULT(WINAPI* DllGetClassObject)(REFCLSID rclsid, REFIID riid, LPVOID FAR* ppv);
    HRESULT(WINAPI* DllRegisterServer)(void);
    HRESULT(WINAPI* DllUnregisterServer)(void);

    dinput8_dll() { ZeroMemory(this, sizeof(dinput8_dll)); }
} dinput8;

void __cdecl DirectInputShutdown()
{
    if (dinput8.dll) FreeLibrary(dinput8.dll);
}

bool DirectInputInitialize()
{
    if (dinput8.dll) return true;

    char system_directory[MAX_PATH];
    DWORD length = 0;
    length = GetSystemDirectoryA(system_directory, MAX_PATH);

    std::string dinput8_path(system_directory);
    StringPathAppend(&dinput8_path, "dinput8.dll");
    dinput8.dll = LoadLibraryA(dinput8_path.c_str());

    if (!dinput8.dll)
    {
        HRESULT hr = GetLastError();
        char error_msg[MAX_PATH];
        sprintf_s(error_msg, "Cannot load \"%s\" error: 0x%x", dinput8_path.c_str(), hr);
        MessageBoxA(NULL, error_msg, "Error", MB_ICONERROR);
        ExitProcess(hr);
    }

    atexit(DirectInputShutdown);

    LoadFunction(dinput8, DirectInput8Create);
    LoadFunction(dinput8, DllCanUnloadNow);
    LoadFunction(dinput8, DllGetClassObject);
    LoadFunction(dinput8, DllRegisterServer);
    LoadFunction(dinput8, DllUnregisterServer);

    return true;
}

extern "C" HRESULT WINAPI DirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
    DirectInputInitialize();
    LoadEmulator();

    return dinput8.DirectInput8Create(hinst, dwVersion, riidltf, ppvOut, punkOuter);
}

extern "C" HRESULT WINAPI DllCanUnloadNow(void)
{
    DirectInputInitialize();
    LoadEmulator();

    return dinput8.DllCanUnloadNow();
}

extern "C" HRESULT WINAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR* ppv)
{
    DirectInputInitialize();
    LoadEmulator();

    return dinput8.DllGetClassObject(rclsid, riid, ppv);
}

extern "C" HRESULT WINAPI DllRegisterServer(void)
{
    DirectInputInitialize();
    LoadEmulator();

    return dinput8.DllRegisterServer();
}

extern "C" HRESULT WINAPI DllUnregisterServer(void)
{
    DirectInputInitialize();
    LoadEmulator();

    return dinput8.DllUnregisterServer();
}