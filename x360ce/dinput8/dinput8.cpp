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

typedef HRESULT (WINAPI* DirectInput8Create_t)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
typedef HRESULT (WINAPI* DllCanUnloadNow_t)(void);
typedef HRESULT (WINAPI* DllGetClassObject_t)(_In_ REFCLSID rclsid, _In_ REFIID riid, _Out_ LPVOID FAR* ppv);
typedef HRESULT (WINAPI* DllRegisterServer_t)(void);
typedef HRESULT (WINAPI* DllUnregisterServer_t)(void);

DirectInput8Create_t hDirectInput8Create = NULL;
DllCanUnloadNow_t hDllCanUnloadNow = NULL;
DllGetClassObject_t hDllGetClassObject = NULL;
DllRegisterServer_t hDllRegisterServer = NULL;
DllUnregisterServer_t hDllUnregisterServer = NULL;

extern "C" HRESULT WINAPI DirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
    if(hXInput == NULL) LoadXinputDLL();

    if(hDirectInput8Create == NULL)
    {
        if(hDInput == NULL) LoadDInputDll();
        hDirectInput8Create = (DirectInput8Create_t) GetProcAddress(hDInput,"DirectInput8Create");
    }
    return hDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);
}

extern "C" HRESULT WINAPI DllCanUnloadNow(void)
{
    if(hXInput == NULL) LoadXinputDLL();

    if(hDllCanUnloadNow == NULL)
    {
        if(hDInput == NULL) LoadDInputDll();
        hDllCanUnloadNow = (DllCanUnloadNow_t) GetProcAddress(hDInput,"DllCanUnloadNow");
    }
    return hDllCanUnloadNow();
}

extern "C" HRESULT WINAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR* ppv)
{
    if(hXInput == NULL) LoadXinputDLL();

    if(hDllGetClassObject == NULL)
    {
        if(hDInput == NULL) LoadDInputDll();
        hDllGetClassObject = (DllGetClassObject_t) GetProcAddress(hDInput,"DllGetClassObject");
    }
    return hDllGetClassObject(rclsid,riid,ppv);
}

extern "C" HRESULT WINAPI DllRegisterServer(void)
{
    if(hDllRegisterServer == NULL)
    {
        if(hDInput == NULL) LoadDInputDll();
        hDllRegisterServer = (DllRegisterServer_t) GetProcAddress(hDInput,"DllRegisterServer");
    }
    return hDllRegisterServer();
}

extern "C" HRESULT WINAPI DllUnregisterServer(void)
{
    if(hDllUnregisterServer == NULL)
    {
        if(hDInput == NULL) LoadDInputDll();
        hDllUnregisterServer = (DllUnregisterServer_t) GetProcAddress(hDInput,"DllUnregisterServer");
    }
    return hDllUnregisterServer();
}