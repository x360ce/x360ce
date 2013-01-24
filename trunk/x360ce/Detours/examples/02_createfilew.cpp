#include <windows.h>
#include "../detours.h"

typedef HANDLE ( WINAPI* tCreateFileW )(LPCWSTR lpFileName,DWORD dwDesiredAccess,DWORD dwShareMode,LPSECURITY_ATTRIBUTES lpSecurityAttributes,DWORD dwCreationDisposition,DWORD dwFlagsAndAttributes,HANDLE hTemplateFile);
HANDLE WINAPI hook_CreateFileW(LPCWSTR lpFileName,DWORD dwDesiredAccess,DWORD dwShareMode,LPSECURITY_ATTRIBUTES lpSecurityAttributes,DWORD dwCreationDisposition,DWORD dwFlagsAndAttributes,HANDLE hTemplateFile);
MologieDetours::Detour<tCreateFileW>* detour_CreateFileW = NULL;

HANDLE WINAPI hook_CreateFileW(LPCWSTR lpFileName,DWORD dwDesiredAccess,DWORD dwShareMode,LPSECURITY_ATTRIBUTES lpSecurityAttributes,DWORD dwCreationDisposition,DWORD dwFlagsAndAttributes,HANDLE hTemplateFile) {
	MessageBoxW(NULL, lpFileName, L"opened", MB_OK);
	return detour_CreateFileW->GetOriginalFunction()(lpFileName,dwDesiredAccess,dwShareMode,lpSecurityAttributes,dwCreationDisposition,dwFlagsAndAttributes,hTemplateFile);
}

extern "C" BOOL WINAPI DllMain(HINSTANCE hinstDLL,DWORD fdwReason,LPVOID lpvReserved)
{
	switch(fdwReason)
	{
		case DLL_PROCESS_ATTACH:
			try
			{
				detour_CreateFileW = new MologieDetours::Detour<tCreateFileW>("Kernel32.dll", "CreateFileW", hook_CreateFileW);
			}
			catch(MologieDetours::DetourException &e)
			{
				// Something went wrong
				
				return FALSE;
			}
			break;
		case DLL_PROCESS_DETACH:
			delete detour_CreateFileW;
			break;
	}
	return TRUE;
}