#ifndef TITANIUMHOOKS
#define TITANIUMHOOKS

#pragma once

#ifndef _USRDLL
#define TITANIUM_EXPORTS
#else
#define TITANIUM_EXPORTS __declspec(dllexport)
#endif

// Global.Engine.Hook.functions:
TITANIUM_EXPORTS bool __stdcall HooksSafeTransitionEx(LPVOID HookAddressArray, int NumberOfHooks, bool TransitionStart);
TITANIUM_EXPORTS bool __stdcall HooksSafeTransition(LPVOID HookAddress, bool TransitionStart);
TITANIUM_EXPORTS bool __stdcall HooksIsAddressRedirected(LPVOID HookAddress);
TITANIUM_EXPORTS void* __stdcall HooksGetTrampolineAddress(LPVOID HookAddress);
TITANIUM_EXPORTS void* __stdcall HooksGetHookEntryDetails(LPVOID HookAddress);
TITANIUM_EXPORTS bool __stdcall HooksInsertNewRedirection(LPVOID HookAddress, LPVOID RedirectTo, int HookType);
TITANIUM_EXPORTS bool __stdcall HooksInsertNewIATRedirectionEx(ULONG_PTR FileMapVA, ULONG_PTR LoadedModuleBase, char* szHookFunction, LPVOID RedirectTo);
TITANIUM_EXPORTS bool __stdcall HooksInsertNewIATRedirection(char* szModuleName, char* szHookFunction, LPVOID RedirectTo);
TITANIUM_EXPORTS bool __stdcall HooksRemoveRedirection(LPVOID HookAddress, bool RemoveAll);
TITANIUM_EXPORTS bool __stdcall HooksRemoveIATRedirection(char* szModuleName, char* szHookFunction, bool RemoveAll);
TITANIUM_EXPORTS bool __stdcall HooksDisableRedirection(LPVOID HookAddress, bool DisableAll);
TITANIUM_EXPORTS bool __stdcall HooksDisableIATRedirection(char* szModuleName, char* szHookFunction, bool DisableAll);
TITANIUM_EXPORTS bool __stdcall HooksEnableRedirection(LPVOID HookAddress, bool EnableAll);
TITANIUM_EXPORTS bool __stdcall HooksEnableIATRedirection(char* szModuleName, char* szHookFunction, bool EnableAll);
TITANIUM_EXPORTS void __stdcall HooksScanModuleMemory(HMODULE ModuleBase, LPVOID CallBack);
TITANIUM_EXPORTS void __stdcall HooksScanEntireProcessMemory(LPVOID CallBack);
TITANIUM_EXPORTS void __stdcall HooksScanEntireProcessMemoryEx();

// TODO: reference additional headers your program requires here

#define UE_ACCESS_READ 0
#define UE_ACCESS_WRITE 1
#define UE_ACCESS_ALL 2

#define MAXIMUM_INSTRUCTION_SIZE 40
#define MAX_DECODE_INSTRUCTIONS 32
#define MAX_INSTRUCTIONS (1000)

#define UE_EAX 1
#define UE_EBX 2
#define UE_ECX 3
#define UE_EDX 4
#define UE_EDI 5
#define UE_ESI 6
#define UE_EBP 7
#define UE_ESP 8
#define UE_EIP 9
#define UE_EFLAGS 10
#define UE_DR0 11
#define UE_DR1 12
#define UE_DR2 13
#define UE_DR3 14
#define UE_DR6 15
#define UE_DR7 16
#define UE_RAX 17
#define UE_RBX 18
#define UE_RCX 19
#define UE_RDX 20
#define UE_RDI 21
#define UE_RSI 22
#define UE_RBP 23
#define UE_RSP 24
#define UE_RIP 25
#define UE_RFLAGS 26
#define UE_R8 27
#define UE_R9 28
#define UE_R10 29
#define UE_R11 30
#define UE_R12 31
#define UE_R13 32
#define UE_R14 33
#define UE_R15 34
#define UE_CIP 35
#define UE_CSP 36
#define MAX_DEBUG_DATA 512

typedef struct{
	HANDLE hThread;
	DWORD dwThreadId;
	void* ThreadStartAddress;
	void* ThreadLocalBase;
}THREAD_ITEM_DATA, *PTHREAD_ITEM_DATA;

typedef struct{
	HANDLE hFile;
	void* BaseOfDll;
	HANDLE hFileMapping;
	void* hFileMappingView; 
	char szLibraryPath[MAX_PATH];
	char szLibraryName[MAX_PATH];
}LIBRARY_ITEM_DATA, *PLIBRARY_ITEM_DATA;

typedef struct{
	BYTE DataByte[50];
}MEMORY_CMP_HANDLER, *PMEMORY_CMP_HANDLER;

typedef struct MEMORY_COMPARE_HANDLER{
	union {
		BYTE bArrayEntry[1];		
		WORD wArrayEntry[1];
		DWORD dwArrayEntry[1];
		DWORD64 qwArrayEntry[1];
	} Array;
}MEMORY_COMPARE_HANDLER, *PMEMORY_COMPARE_HANDLER;

#define TEE_MAXIMUM_HOOK_SIZE 14
#if defined(_WIN64)
#define TEE_MAXIMUM_HOOK_INSERT_SIZE 14
#else
#define TEE_MAXIMUM_HOOK_INSERT_SIZE 5
#endif

#define TEE_HOOK_NRM_JUMP 1
#define TEE_HOOK_NRM_CALL 3
#define TEE_HOOK_IAT 5
#define TEE_MAXIMUM_HOOK_RELOCS 7

typedef struct HOOK_ENTRY{
	bool IATHook;
	BYTE HookType;
	DWORD HookSize;
	void* HookAddress;
	void* RedirectionAddress;
	BYTE HookBytes[TEE_MAXIMUM_HOOK_SIZE];
	BYTE OriginalBytes[TEE_MAXIMUM_HOOK_SIZE];
	void* IATHookModuleBase;
	DWORD IATHookNameHash;
	bool HookIsEnabled;
	void* PatchedEntry;
	DWORD RelocationInfo[TEE_MAXIMUM_HOOK_RELOCS];
	int RelocationCount;
}HOOK_ENTRY, *PHOOK_ENTRY;
#endif /*TITANIUMHOOKS*/