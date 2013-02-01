/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
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

#ifndef _DINPUT8_H_
#define _DINPUT8_H_

#include <windows.h>

typedef HRESULT (WINAPI* DirectInput8Create_t)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
typedef HRESULT (WINAPI* DllCanUnloadNow_t)(void);
typedef HRESULT (WINAPI* DllGetClassObject_t)(_In_ REFCLSID rclsid, _In_ REFIID riid, _Out_ LPVOID FAR* ppv);
typedef HRESULT (WINAPI* DllRegisterServer_t)(void);
typedef HRESULT (WINAPI* DllUnregisterServer_t)(void);

extern DirectInput8Create_t hDirectInput8Create ;
extern DllCanUnloadNow_t hDllCanUnloadNow;
extern DllGetClassObject_t hDllGetClassObject;
extern DllRegisterServer_t hDllRegisterServer;
extern DllUnregisterServer_t hDllUnregisterServer;

#endif