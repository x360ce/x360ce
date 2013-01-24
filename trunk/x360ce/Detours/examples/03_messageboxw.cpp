#include <windows.h>
#include "../detours.h"

typedef int (WINAPI *tMessageBoxW)(HWND, LPCWSTR, LPCWSTR, UINT);
MologieDetours::Detour<tMessageBoxW>* detour_MessageBoxW = NULL;

int WINAPI hook_MessageBoxW(HWND hWnd, LPCWSTR lpText, LPCWSTR lpCaption,
		UINT uType) {
	return detour_MessageBoxW->GetOriginalFunction()(hWnd, L"hook_MessageBoxW",
			lpCaption, uType);
}

extern "C" BOOL WINAPI DllMain(HINSTANCE hinstDLL,DWORD fdwReason,LPVOID lpvReserved)
{
	switch(fdwReason)
	{
		case DLL_PROCESS_ATTACH:
			try
			{
				detour_MessageBoxW = new MologieDetours::Detour<tMessageBoxW>("User32.dll", "MessageBoxW", hook_MessageBoxW);
			}
			catch(MologieDetours::DetourException &e)
			{
				// Something went wrong
				return FALSE;
			}
			break;
		case DLL_PROCESS_DETACH:
			delete detour_MessageBoxW;
			break;
	}
	return TRUE;
}