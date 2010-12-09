/*
    EasyHook - The reinvention of Windows API hooking
 
    Copyright (C) 2009 Christoph Husse

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

    Please visit http://www.codeplex.com/easyhook for more information
    about the project and latest updates.
*/
#include "stdafx.h"

HMODULE             hNtDll = NULL;
HMODULE             hKernel32 = NULL;
HMODULE             hCurrentModule = NULL;
DWORD               RhTlsIndex;
HANDLE              hEasyHookHeap = NULL;

BOOL LhInitializeLibrary( HMODULE hModule)
{
	hCurrentModule = hModule;


	if(((hNtDll = LoadLibraryA("ntdll.dll")) == NULL) ||
		((hKernel32 = LoadLibraryA("kernel32.dll")) == NULL))
		return FALSE;

	hEasyHookHeap = HeapCreate(0, 0, 0);

	DbgCriticalInitialize();

	LhBarrierProcessAttach();

	LhCriticalInitialize();

	// allocate tls slot
	if((RhTlsIndex = TlsAlloc()) == TLS_OUT_OF_INDEXES)
		return FALSE;

	return TRUE;
}

BOOL LhUninitializeLibrary()
{
	// free tls slot
	TlsFree(RhTlsIndex);

	// remove all hooks and shutdown thread barrier...
	LhCriticalFinalize();

	LhModuleInfoFinalize();

	LhBarrierProcessDetach();

	DbgCriticalFinalize();

	HeapDestroy(hEasyHookHeap);

	FreeLibrary(hNtDll);
	FreeLibrary(hKernel32);
	return TRUE;
}

