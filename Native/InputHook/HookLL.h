#pragma once

#include "Common.h"
#include "Utils.h"
#include "Logger.h"

#include "InputHook.h"

#include <algorithm>

class HookLL
{
	friend class InputHook;
private:
	static HMODULE(WINAPI* TrueLoadLibraryA)(LPCSTR lpLibFileName);
	static HMODULE(WINAPI* TrueLoadLibraryW)(LPCWSTR lpLibFileName);
	static HMODULE(WINAPI* TrueLoadLibraryExA)(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
	static HMODULE(WINAPI* TrueLoadLibraryExW)(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
	static HMODULE(WINAPI* TrueGetModuleHandleA)(LPCSTR lpModuleName);
	static HMODULE(WINAPI* TrueGetModuleHandleW)(LPCWSTR lpModuleName);
	static BOOL(WINAPI* TrueGetModuleHandleExA)(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule);
	static BOOL(WINAPI* TrueGetModuleHandleExW)(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule);

	static BOOL SelfCheckA(LPCSTR lpLibFileName);
	static BOOL SelfCheckW(LPCWSTR lpLibFileName);

	static HMODULE WINAPI HookLoadLibraryA(LPCSTR lpLibFileName);
	static HMODULE WINAPI HookLoadLibraryW(LPCWSTR lpLibFileName);
	static HMODULE WINAPI HookLoadLibraryExA(LPCSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
	static HMODULE WINAPI HookLoadLibraryExW(LPCWSTR lpLibFileName, HANDLE hFile, DWORD dwFlags);
	static HMODULE WINAPI HookGetModuleHandleA(LPCSTR lpModuleName);
	static HMODULE WINAPI HookGetModuleHandleW(LPCWSTR lpModuleName);
	static BOOL WINAPI HookGetModuleHandleExA(DWORD dwFlags, LPCSTR lpModuleName, HMODULE* phModule);
	static BOOL WINAPI HookGetModuleHandleExW(DWORD dwFlags, LPCWSTR lpModuleName, HMODULE* phModule);
};
