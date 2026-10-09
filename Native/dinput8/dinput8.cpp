/*  x360ce - XBOX360 Controller Emulator
*  Copyright (C) 2002-2010 Racer_S
*  Copyright (C) 2010-2014 Robert Krawczyk
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

#include "DirectInputModuleManager.h"
#include "Utils.h"

extern "C" HRESULT WINAPI DirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	LoadEmulator();

	return DirectInputModuleManager::Get().DirectInput8Create(hinst, dwVersion, riidltf, ppvOut, punkOuter);
}

extern "C" HRESULT WINAPI DllCanUnloadNow(void)
{
	LoadEmulator();

	return DirectInputModuleManager::Get().DllCanUnloadNow();
}

extern "C" HRESULT WINAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, LPVOID FAR* ppv)
{
	LoadEmulator();

	return DirectInputModuleManager::Get().DllGetClassObject(rclsid, riid, ppv);
}

extern "C" HRESULT WINAPI DllRegisterServer(void)
{
	LoadEmulator();

	return DirectInputModuleManager::Get().DllRegisterServer();
}

extern "C" HRESULT WINAPI DllUnregisterServer(void)
{
	LoadEmulator();

	return DirectInputModuleManager::Get().DllUnregisterServer();
}
