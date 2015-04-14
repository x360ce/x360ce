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
#include <Shlwapi.h>

HINSTANCE hXInput = NULL;

#pragma comment(lib,"Shlwapi.lib")

bool IsEmulator(const std::string& filename)
{
    hXInput = LoadLibraryA(filename.c_str());
    void* pReset = GetProcAddress(hXInput, "Reset");

    if (pReset)
        return true;
    else
        FreeLibrary(hXInput);
    return false;
}

void LoadEmulator()
{
    if (hXInput) return;

    std::string module_directory;
    ModuleDirectory(&module_directory, CurrentModule());

    std::string module_name;
    StringPathCombine(&module_name, module_directory, "xinput1_4.dll");
    if (IsEmulator(module_name)) return;

    StringPathCombine(&module_name, module_directory, "xinput1_3.dll");
    if (IsEmulator(module_name)) return;

    StringPathCombine(&module_name, module_directory, "xinput1_2.dll");
    if (IsEmulator(module_name)) return;

    StringPathCombine(&module_name, module_directory, "xinput1_1.dll");
    if (IsEmulator(module_name)) return;

    StringPathCombine(&module_name, module_directory, "xinput9_1_0.dll");
    if (IsEmulator(module_name)) return;
}

void _cdecl ExitInstance()
{
    if (hXInput) FreeLibrary(hXInput);
}

BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
    )
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls(hModule);
            atexit(ExitInstance);
            break;
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}

