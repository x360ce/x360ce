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
#include <mscoree.h>

// prevent static NET binding...
typedef HRESULT __stdcall PROC_CorBindToRuntime(
                    LPCWSTR pwszVersion, 
                    LPCWSTR pwszBuildFlavor, 
                    REFCLSID rclsid, 
                    REFIID riid, 
                    LPVOID FAR *ppv);

// a macro to compress error information...
#define UNMANAGED_ERROR(code) {ErrorCode = ((code) & 0xFF) | 0xF0000000; goto ABORT_ERROR;}

EASYHOOK_NT_INTERNAL CompleteUnmanagedInjection(LPREMOTE_INFO InInfo)
{
/*
Description:

    Loads the user library and finally executes the user entry point.

*/
    ULONG		            ErrorCode = 0;
    HMODULE                 hUserLib = LoadLibraryW(InInfo->UserLibrary);
    REMOTE_ENTRY_INFO       EntryInfo;
    REMOTE_ENTRY_POINT*     EntryProc = (REMOTE_ENTRY_POINT*)GetProcAddress(
                                hUserLib, 
#ifdef _M_X64
                                "NativeInjectionEntryPoint");
#else
								"_NativeInjectionEntryPoint@4");
#endif

    if(hUserLib == NULL)
        UNMANAGED_ERROR(20);

    if(EntryProc == NULL)
        UNMANAGED_ERROR(21);

    // set and close event
	if(!SetEvent(InInfo->hRemoteSignal))
		UNMANAGED_ERROR(22);

    // invoke user defined entry point
    EntryInfo.HostPID = InInfo->HostProcess;
    EntryInfo.UserData = (InInfo->UserDataSize) ? InInfo->UserData : NULL;
    EntryInfo.UserDataSize = InInfo->UserDataSize;

    EntryProc(&EntryInfo);

    return ERROR_SUCCESS;

ABORT_ERROR:

    return ErrorCode;
}

EASYHOOK_NT_INTERNAL CompleteManagedInjection(LPREMOTE_INFO InInfo)
{
/*
Description:

    Loads the NET runtime into the calling process and invokes the
    managed injection entry point.
*/
	ICLRRuntimeHost*		ClrHost = NULL;
	DWORD					ErrorCode = 0;
    WCHAR                   ParamString[MAX_PATH];
    REMOTE_ENTRY_INFO       EntryInfo;
    HMODULE                 hMsCorEE = LoadLibraryA("mscoree.dll");
    PROC_CorBindToRuntime*  CorBindToRuntime = (PROC_CorBindToRuntime*)GetProcAddress(hMsCorEE, "CorBindToRuntime");
    DWORD                   RetVal;

    if(CorBindToRuntime == NULL)
        UNMANAGED_ERROR(10);

    // invoke user defined entry point
    EntryInfo.HostPID = InInfo->HostProcess;
    EntryInfo.UserData = InInfo->UserData;
    EntryInfo.UserDataSize = InInfo->UserDataSize;

	// load NET-Runtime and execute user defined method
	if(!RTL_SUCCESS(CorBindToRuntime(NULL, NULL, CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (void**)&ClrHost)))
        UNMANAGED_ERROR(11);

	ClrHost->Start();

	/*
		Test library code.
		This is because if once we have set the remote signal, there is no way to
		notify the host about errors. If the following call succeeds, then it will
		also do so some lines later... If not, then we are still able to report an error.

        The EasyHook managed injection loader will establish a connection to the
        host, so that further error reporting is still possible after we set the event!
	*/
    RtlLongLongToUnicodeHex((LONGLONG)&EntryInfo, ParamString);

	if(!RTL_SUCCESS(ClrHost->ExecuteInDefaultAppDomain(
            InInfo->UserLibrary, 
            L"EasyHook.InjectionLoader", 
            L"Main", 
            ParamString, 
            &RetVal)))
		UNMANAGED_ERROR(12);

	if(!RetVal)
        UNMANAGED_ERROR(13);

	// set and close event
	if(!SetEvent(InInfo->hRemoteSignal))
		UNMANAGED_ERROR(22);

	CloseHandle(InInfo->hRemoteSignal);

	InInfo->hRemoteSignal = NULL;

	// execute library code (no way for error reporting, so we dont need to check)
	ClrHost->ExecuteInDefaultAppDomain(
        InInfo->UserLibrary, 
        L"EasyHook.InjectionLoader", 
        L"Main", 
        ParamString, 
        &RetVal);

ABORT_ERROR:

	// release resources
	if(ClrHost != NULL)
		ClrHost->Release();

    if(hMsCorEE != NULL)
        FreeLibrary(hMsCorEE);

	return ErrorCode;
}


EASYHOOK_NT_EXPORT HookCompleteInjection(LPREMOTE_INFO InInfo)
{
/*
Description:

    This method is directly called from assembler code. It will update
    the symbols with the ones of the current process, adjust the PATH
    variable and invoke one of two above injection completions.

*/

    WCHAR*	    	PATH = NULL;
	ULONG		    ErrorCode = 0;
	HMODULE         hMod = GetModuleHandleA("kernel32.dll");
    ULONG           DirSize;
    ULONG           EnvSize;

    /*
		To increase stability we will now update all symbols with the
		real local ones...
	*/
	InInfo->LoadLibraryW = GetProcAddress(hMod, "LoadLibraryW");
	InInfo->FreeLibrary = GetProcAddress(hMod, "FreeLibrary");
	InInfo->GetProcAddress = GetProcAddress(hMod, "GetProcAddress");
	InInfo->VirtualFree = GetProcAddress(hMod, "VirtualFree");
	InInfo->VirtualProtect = GetProcAddress(hMod, "VirtualProtect");
	InInfo->ExitThread = GetProcAddress(hMod, "ExitThread");
	InInfo->GetLastError = GetProcAddress(hMod, "GetLastError");

    

	/* 
        Make directory of user library path available to current process...
	    This is to find dependencies without copying them into a global
	    directory which might cause trouble.
    */

	DirSize = RtlUnicodeLength(InInfo->PATH);
	EnvSize = GetEnvironmentVariableW(L"PATH", NULL, 0) + DirSize;

	if((PATH = (wchar_t*)RtlAllocateMemory(TRUE, EnvSize * 2 + 10)) == NULL)
		UNMANAGED_ERROR(1);

	GetEnvironmentVariableW(L"PATH", PATH, EnvSize);

	// add library path to environment variable
	if(!RtlMoveMemory(PATH + DirSize, PATH, (EnvSize - DirSize) * 2))
        UNMANAGED_ERROR(1);

	RtlCopyMemory(PATH, InInfo->PATH, DirSize * 2);

	if(!SetEnvironmentVariableW(L"PATH", PATH))
		UNMANAGED_ERROR(2);

    if(!RTL_SUCCESS(RhSetWakeUpThreadID(InInfo->WakeUpThreadID)))
        UNMANAGED_ERROR(3);

	// load and execute user library...
	if(InInfo->IsManaged)
        ErrorCode = CompleteManagedInjection(InInfo);
    else
        ErrorCode = CompleteUnmanagedInjection(InInfo);

    return ErrorCode;

ABORT_ERROR:

    // release resources
	if(PATH != NULL)
		RtlFreeMemory(PATH);

	if(InInfo->hRemoteSignal != NULL)
		CloseHandle(InInfo->hRemoteSignal);

	return ErrorCode;
}
