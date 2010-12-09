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

BYTE* GetInjectionPtr();
DWORD GetInjectionSize();

extern DWORD               RhTlsIndex;

EASYHOOK_NT_INTERNAL RhSetWakeUpThreadID(ULONG InThreadID)
{
/*
Description:
    
    Used in conjunction with RhCreateAndInject(). If the given thread
    later is resumed in RhWakeUpProcess(), the injection target will
    start its usual execution.

*/
    NTSTATUS            NtStatus;

    if(!TlsSetValue(RhTlsIndex, (LPVOID)(size_t)InThreadID))
        THROW(STATUS_INTERNAL_ERROR, L"Unable to set TLS value.");

    RETURN(STATUS_SUCCESS);

THROW_OUTRO:
FINALLY_OUTRO:
    return NtStatus;
}







EASYHOOK_NT_EXPORT RhWakeUpProcess()
{
/*
Description:

    Used in conjunction with RhCreateAndInject(). Wakes up the
    injection target. You should call this method after all hooks
    (or whatever) are applied.
*/

    NTSTATUS            NtStatus;
    ULONG               ThreadID = (ULONG)TlsGetValue(RhTlsIndex);
    HANDLE              hThread = NULL;

    if(ThreadID == 0)
        RETURN(STATUS_SUCCESS);
    
    if((hThread = OpenThread(THREAD_SUSPEND_RESUME, FALSE, ThreadID)) == NULL)
        THROW(STATUS_INTERNAL_ERROR, L"Unable to open wake up thread.");

    if(!ResumeThread(hThread))
        THROW(STATUS_INTERNAL_ERROR, L"Unable to resume process main thread.");

    RETURN(STATUS_SUCCESS);
    
THROW_OUTRO:
FINALLY_OUTRO:
    {
        if(hThread != NULL)
            CloseHandle(hThread);

        return NtStatus;
    }
}







EASYHOOK_NT_EXPORT RhGetProcessToken(
            ULONG InProcessId,
            HANDLE* OutToken)
{
/*
Description:

     This method is intended for the managed layer and has no special
     advantage in an unmanaged environment!

Parameters:

    - InProcessId

        The target process shall be accessible with PROCESS_QUERY_INFORMATION.

    - OutToken

        The identity token for the session the process was created in.
*/
    HANDLE			    hProc = NULL;
    NTSTATUS            NtStatus;

    if(!IsValidPointer(OutToken, sizeof(HANDLE)))
        THROW(STATUS_INVALID_PARAMETER_2, L"The given token storage is invalid.");

	if((hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, InProcessId)) == NULL)
	{
		if(GetLastError() == ERROR_ACCESS_DENIED)
			THROW(STATUS_ACCESS_DENIED, L"The given process is not accessible.")
		else
			THROW(STATUS_NOT_FOUND, L"The given process does not exist.");
	}

	if(!OpenProcessToken(hProc, TOKEN_READ, OutToken))
	    THROW(STATUS_INTERNAL_ERROR, L"Unable to query process token.");

	RETURN(STATUS_SUCCESS);

THROW_OUTRO:
FINALLY_OUTRO:
    {
		if(hProc != NULL)
			CloseHandle(hProc);

        return NtStatus;
	}
}






EASYHOOK_BOOL_EXPORT RhIsAdministrator()
{
/*
Description:

    If someone is able to open the SCM with all access he is also able to create and start system services
    and so he is also able to act as a part of the system! We are just letting
    windows decide and don't enforce that the user is in the builtin admin group.
*/

    SC_HANDLE			hSCManager = NULL;

    if((hSCManager = OpenSCManagerW(NULL, NULL, SC_MANAGER_ALL_ACCESS)) == NULL)
	    return FALSE;

    CloseServiceHandle(hSCManager);

    return TRUE;
}






 typedef LONG WINAPI NtCreateThreadEx_PROC(
	PHANDLE ThreadHandle,
	ACCESS_MASK DesiredAccess,
	LPVOID ObjectAttributes,
	HANDLE ProcessHandle,
	LPTHREAD_START_ROUTINE lpStartAddress,
	LPVOID lpParameter,
	BOOL CreateSuspended,
	DWORD dwStackSize,
	LPVOID Unknown1,
	LPVOID Unknown2,
	LPVOID Unknown3
); 

EASYHOOK_NT_INTERNAL NtCreateThreadEx(
	HANDLE InProcess,
	LPTHREAD_START_ROUTINE InRemoteThreadStart,
	void* InRemoteCallback,
	BOOLEAN InCreateSuspended,
    HANDLE* OutThread)
{
/*
Description:

	Only intended for Vista and later... Will return NULL for all others which
	should use CreateRemoteThread() and services instead!

	In contrast to RtlCreateUserThread() this one will fortunately setup a proper activation
	context stack, which is required to load the NET framework and many other
	common APIs. This is why RtlCreateUserThread() can't be used for Windows XP
	,for example, even if it would replace the windows service approach which is required
	in order to get CreateRemoteThread() working.

	Injection through WOW64 boundaries is still not directly supported and requires
	a WOW64 bypass helper process.

Parameters:

    - InProcess

        A target process opened with PROCESS_ALL_ACCESS.

    - InRemoteThreadStart

        The method executed by the remote thread. Must be valid in the
        context of the given process.

    - InRemoteCallback

        An uninterpreted callback passed to the remote start routine. 
        Must be valid in the context of the given process.

    - OutThread

        Receives a handle to the remote thread. This handle is valid
        in the calling process.

Returns:

    STATUS_NOT_SUPPORTED

        Only Windows Vista and later supports NtCreateThreadEx, all other
        platforms will return this error code.
*/
    HANDLE			        hRemoteThread;
	NTSTATUS            NtStatus;
    NtCreateThreadEx_PROC*  VistaCreateThread;

    if(!IsValidPointer(OutThread, sizeof(HANDLE)))
        THROW(STATUS_INVALID_PARAMETER_4, L"The given handle storage is invalid.");

	// this will only work for vista and later...
	if((VistaCreateThread = (NtCreateThreadEx_PROC*)GetProcAddress(hNtDll, "NtCreateThreadEx")) == NULL)
		THROW(STATUS_NOT_SUPPORTED, L"NtCreateThreadEx() is not supported.");

	FORCE(VistaCreateThread(
			&hRemoteThread,
			0x1FFFFF, // all access
			NULL,
			InProcess,
			(LPTHREAD_START_ROUTINE)InRemoteThreadStart,
			InRemoteCallback,
			InCreateSuspended,
			0,
			NULL,
			NULL,
			NULL
			));

    *OutThread = hRemoteThread;

	RETURN;

THROW_OUTRO:
FINALLY_OUTRO:
    return NtStatus;
}






typedef BOOL __stdcall IsWow64Process_PROC(HANDLE InProc, BOOL* OutResult);
typedef void GetNativeSystemInfo_PROC(LPSYSTEM_INFO OutSysInfo);

EASYHOOK_NT_EXPORT RhIsX64Process(
            ULONG InProcessId,
            BOOL* OutResult)
{
/*
Description:

    Detects the bitness of a given process.

Parameters:

    - InProcessId

        The calling process must have PROCESS_QUERY_INFORMATION access
        to the process represented by this ID.

    - OutResult

        Is set to TRUE if the given process is running under 64-Bit,
        FALSE otherwise.
*/
	BOOL			            IsTarget64Bit = FALSE;
	HANDLE			            hProc = NULL;
    IsWow64Process_PROC*        pIsWow64Process;
    NTSTATUS            NtStatus;

#ifndef _M_X64
    GetNativeSystemInfo_PROC*   pGetNativeSystemInfo;
    SYSTEM_INFO		            SysInfo;
#endif

    if(!IsValidPointer(OutResult, sizeof(BOOL)))
        THROW(STATUS_INVALID_PARAMETER_2, L"The given result storage is invalid.");

	// open target process
	if((hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, InProcessId)) == NULL)
	{
		if(GetLastError() == ERROR_ACCESS_DENIED)
			THROW(STATUS_ACCESS_DENIED, L"The given process is not accessible.")
		else
			THROW(STATUS_NOT_FOUND, L"The given process does not exist.");
	}

	// if WOW64 is not available then target must be 32-bit
	pIsWow64Process = (IsWow64Process_PROC*)GetProcAddress(hKernel32, "IsWow64Process");

#ifdef _M_X64
	// if the target is not WOW64, then it is 64-bit
	if(!pIsWow64Process(hProc, &IsTarget64Bit))
		THROW(STATUS_INTERNAL_ERROR, L"Unable to detect wether target process is 64-bit or not.");

	IsTarget64Bit = !IsTarget64Bit;

#else

	IsTarget64Bit = FALSE;

	if(pIsWow64Process != NULL)
	{
		// check if we are running on a 32-bit OS
		pGetNativeSystemInfo = (GetNativeSystemInfo_PROC*)GetProcAddress(hKernel32, "GetNativeSystemInfo");

		if(pGetNativeSystemInfo != NULL)
		{
			pGetNativeSystemInfo(&SysInfo);

			if(SysInfo.wProcessorArchitecture != PROCESSOR_ARCHITECTURE_INTEL)
			{
				// if not, then and only then a 32-bit process will be marked as WOW64 process!
				if(!pIsWow64Process(hProc, &IsTarget64Bit))
					THROW(STATUS_INTERNAL_ERROR, L"Unable to detect wether target process is 64-bit or not.");

				IsTarget64Bit = !IsTarget64Bit;
			}
		}
	}
#endif

	*OutResult = IsTarget64Bit;

    RETURN(STATUS_SUCCESS);

THROW_OUTRO:
FINALLY_OUTRO:
    {
		if(hProc != NULL)
			CloseHandle(hProc);

        return NtStatus;
	}
}

#ifndef _DEBUG
    #pragma optimize ("", off) // suppress _memcpy
#endif

EASYHOOK_BOOL_EXPORT RhIsX64System()
{
/*
Description:

	Determines whether the calling PC is running a 64-Bit version
	of windows.
*/
#ifndef _M_X64
    
	GetNativeSystemInfo_PROC*   pGetNativeSystemInfo;
    SYSTEM_INFO		            SysInfo;

	pGetNativeSystemInfo = (GetNativeSystemInfo_PROC*)GetProcAddress(hKernel32, "GetNativeSystemInfo");

	if(pGetNativeSystemInfo == NULL)
		return FALSE;

	pGetNativeSystemInfo(&SysInfo);

	if(SysInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_INTEL)
		return FALSE;

#endif

	return TRUE;
}

EASYHOOK_NT_EXPORT RtlCreateSuspendedProcess(
		WCHAR* InEXEPath,
        WCHAR* InCommandLine,
		ULONG InCustomFlags,
        ULONG* OutProcessId,
        ULONG* OutThreadId)
{
/*
Description:

    Creates a suspended process with the given parameters.
    This is only intended for the managed layer.

Parameters:

    - InEXEPath

        A relative or absolute path to the EXE file of the process being created.

    - InCommandLine

        Optional command line parameters passed to the newly created process.

	- InCustomFlags

		Additional process creation flags.

    - OutProcessId

        Receives the PID of the newly created process.

    - OutThreadId

        Receives the initial TID of the newly created process.
*/
    STARTUPINFO				StartInfo;
	PROCESS_INFORMATION		ProcessInfo;
	WCHAR					FullExePath[MAX_PATH + 1];
	WCHAR					CurrentDir[MAX_PATH + 1];
    WCHAR*					FilePart;
    NTSTATUS            NtStatus;

    // must be executed before any THROW or RETURN!
    RtlZeroMemory(&StartInfo, sizeof(StartInfo));
    RtlZeroMemory(&ProcessInfo, sizeof(ProcessInfo));

    if(!IsValidPointer(OutProcessId, sizeof(ULONG)))
        THROW(STATUS_INVALID_PARAMETER_3, L"The given process ID storage is invalid.");

    if(!IsValidPointer(OutThreadId, sizeof(ULONG)))
        THROW(STATUS_INVALID_PARAMETER_4, L"The given thread ID storage is invalid.");

    // parse path
    if(!RtlFileExists(InEXEPath))
        THROW(STATUS_INVALID_PARAMETER_1, L"The given process file does not exist.");

    if(GetFullPathName(InEXEPath, MAX_PATH, CurrentDir, &FilePart) > MAX_PATH)
        THROW(STATUS_INVALID_PARAMETER_1, L"Full path information exceeds MAX_PATH characters.");

    // compute current directory...
    RtlCopyMemory(FullExePath, CurrentDir, sizeof(FullExePath));
    
    *FilePart = 0;

    // create suspended process
    StartInfo.cb = sizeof(StartInfo);
	StartInfo.wShowWindow = TRUE;

    if(!CreateProcessW(
		    FullExePath, 
		    InCommandLine, 
            NULL, NULL,  
            FALSE, 
		    InCustomFlags | CREATE_SUSPENDED,
		    NULL,
		    CurrentDir,
		    &StartInfo,
		    &ProcessInfo))
	    THROW(STATUS_INVALID_PARAMETER, L"Unable to start process; please check the given parameters.");

    *OutProcessId = ProcessInfo.dwProcessId;
    *OutThreadId = ProcessInfo.dwThreadId;

    RETURN;

THROW_OUTRO:
FINALLY_OUTRO:
    {
        if(ProcessInfo.hProcess != NULL)
		    CloseHandle(ProcessInfo.hProcess);

        if(ProcessInfo.hThread != NULL)
		    CloseHandle(ProcessInfo.hThread);

        return NtStatus;
	}
}





EASYHOOK_NT_EXPORT RhCreateAndInject(
		WCHAR* InEXEPath,
        WCHAR* InCommandLine,
		ULONG InProcessCreationFlags,
		ULONG InInjectionOptions,
		WCHAR* InLibraryPath_x86,
		WCHAR* InLibraryPath_x64,
		PVOID InPassThruBuffer,
        ULONG InPassThruSize,
        ULONG* OutProcessId)
{
/*
Description:

    Creates a suspended process and immediately injects the user library.
    This is done BEFORE any of the usual process initialization is called.
    When the injection is made, NO thread has actually executed any instruction 
    so far... It is just like your library entry point is the first thing
    executed in such a process and you can allow the original execution to
    take place by calling RhWakeUpProcess() in the injected library. But even
    that is no requirement for the process to work...

Parameters:

    - InEXEPath

        A relative or absolute path to the EXE file of the process being created.

    - InCommandLine

        Optional command line parameters passed to the newly created process.

	- InProcessCreationFlags

		Custom process creation flags.

    - InInjectionOptions

        All flags can be combined.

        EASYHOOK_INJECT_DEFAULT: 
            
            No special behavior. The given libraries are expected to be unmanaged DLLs.
            Further they should export an entry point named 
            "NativeInjectionEntryPoint" (in case of 64-bit) and
            "_NativeInjectionEntryPoint@4" (in case of 32-bit). The expected entry point 
            signature is REMOTE_ENTRY_POINT.

        EASYHOOK_INJECT_MANAGED: 
        
            The given user library is a NET assembly. Further they should export a class
            named "EasyHook.InjectionLoader" with a static method named "Main". The
            signature of this method is expected to be "int (String)". Please refer
            to the managed injection loader of EasyHook for more information about
            writing such managed entry points.

        EASYHOOK_INJECT_STEALTH:

            Uses the experimental stealth thread creation. If it fails
            you may try it with default settings. 

    - InLibraryPath_x86

        A relative or absolute path to the 32-bit version of the user library being injected.
        If you don't want to inject into 32-Bit processes, you may set this parameter to NULL.

    - InLibraryPath_x64

        A relative or absolute path to the 64-bit version of the user library being injected.
        If you don't want to inject into 64-Bit processes, you may set this parameter to NULL.

    - InPassThruBuffer

        An optional buffer containg data to be passed to the injection entry point. Such data
        is available in both, the managed and unmanaged user library entry points.
        Set to NULL if no used.

    - InPassThruSize

        Specifies the size in bytes of the pass thru data. If "InPassThruBuffer" is NULL, this
        parameter shall also be zero.

    - OutProcessId

        Receives the PID of the newly created process.

*/
    ULONG       ProcessId = 0;
    ULONG       ThreadId = 0;
    HANDLE      hProcess = NULL;
    NTSTATUS    NtStatus;

    if(!IsValidPointer(OutProcessId, sizeof(ULONG)))
        THROW(STATUS_INVALID_PARAMETER_8, L"The given process ID storage is invalid.");

    // all other parameters are validate by called APIs...
	FORCE(RtlCreateSuspendedProcess(InEXEPath, InCommandLine, InProcessCreationFlags, &ProcessId, &ThreadId));


    // inject library
    FORCE(RhInjectLibrary(
		    ProcessId,
		    ThreadId,
		    InInjectionOptions,
		    InLibraryPath_x86,
		    InLibraryPath_x64,
		    InPassThruBuffer,
            InPassThruSize));

    *OutProcessId = ProcessId;

    RETURN;

THROW_OUTRO:
    {
        if(ProcessId != 0)
        {
            hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, ProcessId);
            
            TerminateProcess(hProcess, 0);

            CloseHandle(hProcess);
        }
    }
FINALLY_OUTRO:
    return NtStatus;
}

#ifndef _DEBUG
    #pragma optimize ("", on)
#endif





EASYHOOK_NT_EXPORT RhInjectLibrary(
		ULONG InTargetPID,
		ULONG InWakeUpTID,
		ULONG InInjectionOptions,
		WCHAR* InLibraryPath_x86,
		WCHAR* InLibraryPath_x64,
		PVOID InPassThruBuffer,
        ULONG InPassThruSize)
{
/*
Description:

    Injects a library into the target process. This is a very stable operation.
    The problem so far is, that only the NET layer will support injection
    through WOW64 boundaries and into other terminal sessions. It is quite
    complex to realize with unmanaged code and that's why it is not supported!

    If you really need this feature I highly recommend to at least look at C++.NET
    because using the managed injection can speed up your development progress
    about orders of magnitudes. I know by experience that writing the required
    multi-process injection code in any unmanaged language is a rather daunting task!

Parameters:

    - InTargetPID

        The process in which the library should be injected.
    
    - InWakeUpTID

        If the target process was created suspended (RhCreateAndInject), then
        this parameter should be set to the main thread ID of the target.
        You may later resume the process from within the injected library
        by calling RhWakeUpProcess(). If the process is already running, you
        should specify zero.

    - InInjectionOptions

        All flags can be combined.

        EASYHOOK_INJECT_DEFAULT: 
            
            No special behavior. The given libraries are expected to be unmanaged DLLs.
            Further they should export an entry point named 
            "NativeInjectionEntryPoint" (in case of 64-bit) and
            "_NativeInjectionEntryPoint@4" (in case of 32-bit). The expected entry point 
            signature is REMOTE_ENTRY_POINT.

        EASYHOOK_INJECT_MANAGED: 
        
            The given user library is a NET assembly. Further they should export a class
            named "EasyHook.InjectionLoader" with a static method named "Main". The
            signature of this method is expected to be "int (String)". Please refer
            to the managed injection loader of EasyHook for more information about
            writing such managed entry points.

        EASYHOOK_INJECT_STEALTH:

            Uses the experimental stealth thread creation. If it fails
            you may try it with default settings. 

		EASYHOOK_INJECT_HEART_BEAT:
			
			Is only used internally to workaround the managed process creation bug.
			For curiosity, NET seems to hijack our remote thread if a managed process
			is created suspended. It doesn't do anything with the suspended main thread,


    - InLibraryPath_x86

        A relative or absolute path to the 32-bit version of the user library being injected.
        If you don't want to inject into 32-Bit processes, you may set this parameter to NULL.

    - InLibraryPath_x64

        A relative or absolute path to the 64-bit version of the user library being injected.
        If you don't want to inject into 64-Bit processes, you may set this parameter to NULL.

    - InPassThruBuffer

        An optional buffer containg data to be passed to the injection entry point. Such data
        is available in both, the managed and unmanaged user library entry points.
        Set to NULL if no used.

    - InPassThruSize

        Specifies the size in bytes of the pass thru data. If "InPassThruBuffer" is NULL, this
        parameter shall also be zero.

Returns:

    

*/
	HANDLE					hProc = NULL;
	HANDLE					hRemoteThread = NULL;
	HANDLE					hSignal = NULL;
	UCHAR*					RemoteInjectCode = NULL;
	LPREMOTE_INFO			Info = NULL;
    LPREMOTE_INFO           RemoteInfo = NULL;
	ULONG					RemoteInfoSize = 0;
	BYTE*					Offset = 0;
    ULONG                   CodeSize;
    BOOL                    Is64BitTarget;
    NTSTATUS				NtStatus;
    LONGLONG                Diff;
    HANDLE                  Handles[2];

    ULONG                   UserLibrarySize;
    ULONG                   PATHSize;
    ULONG                   EasyHookPathSize;
    ULONG                   EasyHookEntrySize;
    ULONG                   Code;

    SIZE_T                  BytesWritten;
    WCHAR                   UserLibrary[MAX_PATH+1];
    WCHAR					PATH[MAX_PATH + 1];
    WCHAR					EasyHookPath[MAX_PATH + 1];
#ifdef _M_X64
	CHAR*					EasyHookEntry = "HookCompleteInjection";
#else
	CHAR*					EasyHookEntry = "_HookCompleteInjection@4";
#endif

    // validate parameters
    if(InPassThruSize > MAX_PASSTHRU_SIZE)
        THROW(STATUS_INVALID_PARAMETER_7, L"The given pass thru buffer is too large.");

    if(InPassThruBuffer != NULL)
    {
        if(!IsValidPointer(InPassThruBuffer, InPassThruSize))
            THROW(STATUS_INVALID_PARAMETER_6, L"The given pass thru buffer is invalid.");
    }
    else if(InPassThruSize != 0)
        THROW(STATUS_INVALID_PARAMETER_7, L"If no pass thru buffer is specified, the pass thru length also has to be zero.");

	if(InTargetPID == GetCurrentProcessId())
		THROW(STATUS_NOT_SUPPORTED, L"For stability reasons it is not supported to inject into the calling process.");

	// open target process
	if((hProc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, InTargetPID)) == NULL)
	{
		if(GetLastError() == ERROR_ACCESS_DENIED)
		    THROW(STATUS_ACCESS_DENIED, L"Unable to open target process. Consider using a system service.")
		else
			THROW(STATUS_NOT_FOUND, L"The given target process does not exist!");
	}

	/*
		Check bitness...

		After this we can assume hooking a target that is running in the same
		WOW64 level.
	*/
#ifdef _M_X64
	FORCE(RhIsX64Process(InTargetPID, &Is64BitTarget));
      
    if(!Is64BitTarget)
        THROW(STATUS_WOW_ASSERTION, L"It is not supported to directly hook through the WOW64 barrier.");

    if(!GetFullPathNameW(InLibraryPath_x64, MAX_PATH, UserLibrary, NULL))
        THROW(STATUS_INVALID_PARAMETER_5, L"Unable to get full path to the given 64-bit library.");
#else
	FORCE(RhIsX64Process(InTargetPID, &Is64BitTarget));
      
    if(Is64BitTarget)
        THROW(STATUS_WOW_ASSERTION, L"It is not supported to directly hook through the WOW64 barrier.");

	if(!GetFullPathNameW(InLibraryPath_x86, MAX_PATH, UserLibrary, NULL))
        THROW(STATUS_INVALID_PARAMETER_4, L"Unable to get full path to the given 32-bit library.");
#endif

	/*
		Validate library path...
	*/
	if(!RtlFileExists(UserLibrary))
    {
    #ifdef _M_X64
        THROW(STATUS_INVALID_PARAMETER_5, L"The given 64-Bit library does not exist!");
    #else
        THROW(STATUS_INVALID_PARAMETER_4, L"The given 32-Bit library does not exist!");
    #endif
    }

	// import strings...
    RtlGetWorkingDirectory(PATH, MAX_PATH - 1);
    RtlGetCurrentModulePath(EasyHookPath, MAX_PATH);

	// allocate remote information block
    EasyHookPathSize = (RtlUnicodeLength(EasyHookPath) + 1) * 2;
    EasyHookEntrySize = (RtlAnsiLength(EasyHookEntry) + 1);
    PATHSize = (RtlUnicodeLength(PATH) + 1 + 1) * 2;
    UserLibrarySize = (RtlUnicodeLength(UserLibrary) + 1 + 1) * 2;

    PATH[PATHSize / 2 - 2] = ';';
    PATH[PATHSize / 2 - 1] = 0;

	RemoteInfoSize = EasyHookPathSize + EasyHookEntrySize + PATHSize + InPassThruSize + UserLibrarySize;

	RemoteInfoSize += sizeof(REMOTE_INFO);

	if((Info = (LPREMOTE_INFO)RtlAllocateMemory(TRUE, RemoteInfoSize)) == NULL)
		THROW(STATUS_NO_MEMORY, L"Unable to allocate memory in current process.");

	Info->LoadLibraryW = (PVOID)GetProcAddress(hKernel32, "LoadLibraryW");
	Info->FreeLibrary = (PVOID)GetProcAddress(hKernel32, "FreeLibrary");
	Info->GetProcAddress = (PVOID)GetProcAddress(hKernel32, "GetProcAddress");
	Info->VirtualFree = (PVOID)GetProcAddress(hKernel32, "VirtualFree");
	Info->VirtualProtect = (PVOID)GetProcAddress(hKernel32, "VirtualProtect");
	Info->ExitThread = (PVOID)GetProcAddress(hKernel32, "ExitThread");
	Info->GetLastError = (PVOID)GetProcAddress(hKernel32, "GetLastError");

    Info->WakeUpThreadID = InWakeUpTID;
    Info->IsManaged = InInjectionOptions & EASYHOOK_INJECT_MANAGED;

	// allocate memory in target process
	CodeSize = GetInjectionSize();

	if((RemoteInjectCode = (BYTE*)VirtualAllocEx(hProc, NULL, CodeSize + RemoteInfoSize, MEM_COMMIT, PAGE_EXECUTE_READWRITE)) == NULL)
        THROW(STATUS_NO_MEMORY, L"Unable to allocate memory in target process.");

	// save strings
	Offset = (BYTE*)(Info + 1);

	Info->EasyHookEntry = (char*)Offset;
	Info->EasyHookPath = (wchar_t*)(Offset += EasyHookEntrySize);
	Info->PATH = (wchar_t*)(Offset += EasyHookPathSize);
	Info->UserData = (BYTE*)(Offset += PATHSize);
    Info->UserLibrary = (WCHAR*)(Offset += InPassThruSize);

	Info->Size = RemoteInfoSize;
	Info->HostProcess = GetCurrentProcessId();
	Info->UserDataSize = 0;

	Offset += UserLibrarySize;

	if((ULONG)(Offset - ((BYTE*)Info)) > Info->Size)
        THROW(STATUS_BUFFER_OVERFLOW, L"A buffer overflow in internal memory was detected.");

	RtlCopyMemory(Info->EasyHookPath, EasyHookPath, EasyHookPathSize);
	RtlCopyMemory(Info->PATH, PATH, PATHSize);
	RtlCopyMemory(Info->EasyHookEntry, EasyHookEntry, EasyHookEntrySize);
    RtlCopyMemory(Info->UserLibrary, UserLibrary, UserLibrarySize);


	if(InPassThruBuffer != NULL)
	{
		RtlCopyMemory(Info->UserData, InPassThruBuffer, InPassThruSize);

		Info->UserDataSize = InPassThruSize;
	}

	// copy code into target process
	if(!WriteProcessMemory(hProc, RemoteInjectCode, GetInjectionPtr(), CodeSize, &BytesWritten) || (BytesWritten != CodeSize))
		THROW(STATUS_INTERNAL_ERROR, L"Unable to write into target process memory.");

	// create and export signal event>
	if((hSignal = CreateEvent(NULL, TRUE, FALSE, NULL)) == NULL)
        THROW(STATUS_INSUFFICIENT_RESOURCES, L"Unable to create event.");

	// Possible resource leck: the remote handles cannt be closed here if an error occurs
	if(!DuplicateHandle(GetCurrentProcess(), hSignal, hProc, &Info->hRemoteSignal, EVENT_ALL_ACCESS, FALSE, 0))
		THROW(STATUS_INTERNAL_ERROR, L"Failed to duplicate remote event.");

	// relocate remote information
	RemoteInfo = (LPREMOTE_INFO)(RemoteInjectCode + CodeSize);
	Diff = ((BYTE*)RemoteInfo - (BYTE*)Info);

	Info->EasyHookEntry = (char*)(((BYTE*)Info->EasyHookEntry) + Diff);
	Info->EasyHookPath = (wchar_t*)(((BYTE*)Info->EasyHookPath) + Diff);
	Info->PATH = (wchar_t*)(((BYTE*)Info->PATH) + Diff);
    Info->UserLibrary = (wchar_t*)(((BYTE*)Info->UserLibrary) + Diff);

	if(Info->UserData != NULL)
		Info->UserData = (BYTE*)(((BYTE*)Info->UserData) + Diff);

	Info->RemoteEntryPoint = RemoteInjectCode;

	if(!WriteProcessMemory(hProc, RemoteInfo, Info, RemoteInfoSize, &BytesWritten) || (BytesWritten != RemoteInfoSize))
		THROW(STATUS_INTERNAL_ERROR, L"Unable to write into target process memory.");

	if((InInjectionOptions & EASYHOOK_INJECT_STEALTH) != 0)
	{
		FORCE(RhCreateStealthRemoteThread(InTargetPID, (LPTHREAD_START_ROUTINE)RemoteInjectCode, RemoteInfo, &hRemoteThread));
	}
	else
	{
		if(!RTL_SUCCESS(NtCreateThreadEx(hProc, (LPTHREAD_START_ROUTINE)RemoteInjectCode, RemoteInfo, FALSE, &hRemoteThread)))
		{
			// create remote thread and wait for injection completion
			if((hRemoteThread = CreateRemoteThread(hProc, NULL, 0, (LPTHREAD_START_ROUTINE)RemoteInjectCode, RemoteInfo, 0, NULL)) == NULL)
				THROW(STATUS_ACCESS_DENIED, L"Unable to create remote thread.");
		}
	}

	/*
	 * The assembler codes are designed to let us derive extensive error information...
	*/
    Handles[1] = hSignal;
	Handles[0] = hRemoteThread;

	Code = WaitForMultipleObjects(2, Handles, FALSE, INFINITE);

	if(Code == WAIT_OBJECT_0)
	{
		// parse error code
		GetExitCodeThread(hRemoteThread, &Code);

		SetLastError(Code & 0x0FFFFFFF);

		switch(Code & 0xF0000000)
		{
		case 0x10000000: THROW(STATUS_INTERNAL_ERROR, L"Unable to find internal entry point.");
		case 0x20000000: THROW(STATUS_INTERNAL_ERROR, L"Unable to make stack executable.");
		case 0x30000000: THROW(STATUS_INTERNAL_ERROR, L"Unable to release injected library.");
		case 0x40000000: THROW(STATUS_INTERNAL_ERROR, L"Unable to find EasyHook library in target process context.");
		case 0xF0000000: // error in C++ injection completion
			{
				switch(Code & 0xFF)
				{
#ifdef _M_X64
                case 20: THROW(STATUS_INVALID_PARAMETER_5, L"Unable to load the given 64-bit library into target process.");
                case 21: THROW(STATUS_INVALID_PARAMETER_5, L"Unable to find the required native entry point in the given 64-bit library.");
                case 12: THROW(STATUS_INVALID_PARAMETER_5, L"Unable to find the required managed entry point in the given 64-bit library.");
#else
                case 20: THROW(STATUS_INVALID_PARAMETER_4, L"Unable to load the given 32-bit library into target process.");
                case 21: THROW(STATUS_INVALID_PARAMETER_4, L"Unable to find the required native entry point in the given 32-bit library.");
                case 12: THROW(STATUS_INVALID_PARAMETER_4, L"Unable to find the required managed entry point in the given 32-bit library.");
#endif
                
                case 13: THROW(STATUS_DLL_INIT_FAILED, L"The user defined managed entry point failed in the target process. Make sure that EasyHook is registered in the GAC. Refer to event logs for more information.");
				case 1: THROW(STATUS_INTERNAL_ERROR, L"Unable to allocate memory in target process.");
				case 2: THROW(STATUS_INTERNAL_ERROR, L"Unable to adjust target's PATH variable.");
                case 10: THROW(STATUS_INTERNAL_ERROR, L"Unable to load 'mscoree.dll' into target process.");
				case 11: THROW(STATUS_INTERNAL_ERROR, L"Unable to bind NET Runtime to target process.");
				case 22: THROW(STATUS_INTERNAL_ERROR, L"Unable to signal remote event.");
				default: THROW(STATUS_INTERNAL_ERROR, L"Unknown error in injected C++ completion routine.");
				}
			}break;
		case 0:
			THROW(STATUS_INTERNAL_ERROR, L"C++ completion routine has returned success but didn't raise the remote event.");
		default:
			THROW(STATUS_INTERNAL_ERROR, L"Unknown error in injected assembler code.");
		}
	}
	else if(Code != WAIT_OBJECT_0 + 1)
		THROW(STATUS_INTERNAL_ERROR, L"Unable to wait for injection completion due to timeout. ");

    RETURN;

THROW_OUTRO:
FINALLY_OUTRO:
    {
		// release resources
		if(hProc != NULL)
			CloseHandle(hProc);

		if(Info != NULL)
			RtlFreeMemory(Info);

		if(hRemoteThread != NULL)
			CloseHandle(hRemoteThread);

		if(hSignal != NULL)
			CloseHandle(hSignal);

        return NtStatus;
	}
}

/*/////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////// GetInjectionSize
///////////////////////////////////////////////////////////////////////////////////////

Dynamically retrieves the size of the trampoline method.
*/
static DWORD ___InjectionSize = 0;

#ifdef _M_X64
	EXTERN_C void Injection_ASM_x64();
#else
	EXTERN_C void __stdcall Injection_ASM_x86();
#endif

BYTE* GetInjectionPtr()
{
#ifdef _M_X64
	BYTE* Ptr = (BYTE*)Injection_ASM_x64;
#else
	BYTE* Ptr = (BYTE*)Injection_ASM_x86;
#endif

// bypass possible VS2008 debug jump table
	if(*Ptr == 0xE9)
		Ptr += *((int*)(Ptr + 1)) + 5;

	return Ptr;
}

ULONG GetInjectionSize()
{
    UCHAR*          Ptr;
    UCHAR*          BasePtr;
    ULONG           Index;
    ULONG           Signature;

	if(___InjectionSize != 0)
		return ___InjectionSize;
	
	// search for signature
	BasePtr = Ptr = GetInjectionPtr();

	for(Index = 0; Index < 2000 /* some always large enough value*/; Index++)
	{
		Signature = *((ULONG*)Ptr);

		if(Signature == 0x12345678)	
		{
			___InjectionSize = (ULONG)(Ptr - BasePtr);

			return ___InjectionSize;
		}

		Ptr++;
	}

	ASSERT(FALSE);

    return 0;
}

