#include "stdafx.h"

#include "Common.h"

#include <string>
#include <memory>
#include <Shlwapi.h>
#include <Shlobj.h>
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")

HMODULE LoadLibrarySystem(const std::string& library_name, std::string* out_path)
{
	std::unique_ptr<char[]> system_directory(new char[MAX_PATH]);
	GetSystemDirectoryA(system_directory.get(), MAX_PATH);

	std::string lib_path(system_directory.get());
	StringPathAppend(&lib_path, library_name);

	if (out_path)
		*out_path = lib_path;

	return LoadLibraryA(lib_path.c_str());
}

HMODULE LoadLibrarySystem(const std::wstring& library_name, std::wstring* out_path)
{
	std::unique_ptr<wchar_t[]> system_directory(new wchar_t[MAX_PATH]);
	GetSystemDirectoryW(system_directory.get(), MAX_PATH);

	std::wstring lib_path(system_directory.get());
	StringPathAppend(&lib_path, library_name);

	if (out_path)
		*out_path = lib_path;

	return LoadLibraryW(lib_path.c_str());
}

HMODULE LoadLibraryCurrent(const std::string& library_name, std::string* out_path)
{
	std::string lib_path;
	ModuleDirectory(&lib_path);
	StringPathAppend(&lib_path, library_name);

	if (out_path)
		*out_path = lib_path;

	return LoadLibraryA(lib_path.c_str());
}

HMODULE LoadLibraryCurrent(const std::wstring& library_name, std::wstring* out_path)
{
	std::wstring lib_path;
	ModuleDirectory(&lib_path);
	StringPathAppend(&lib_path, library_name);

	if (out_path)
		*out_path = lib_path;

	return LoadLibraryW(lib_path.c_str());
}

bool FileExist(const std::string& path)
{
	HANDLE hFile = CreateFileA(path.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		CloseHandle(hFile);
		return true;
	}
	return false;
}

bool CheckCommonDirectory(std::string* outpath, const std::string& dirname)
{
	std::unique_ptr<char[]> path(new char[MAX_PATH]);
	if (SHGetFolderPathA(NULL, CSIDL_COMMON_APPDATA, NULL, SHGFP_TYPE_CURRENT, path.get()) == S_OK)
	{
		PathAppendA(path.get(), dirname.c_str());
		PathAppendA(path.get(), outpath->c_str());
		if (FileExist(path.get()))
		{
			*outpath = path.get();
			return true;
		}
	}
	return false;
}

bool FullPathFromPath(std::string* path, const std::string& in_path)
{
	if (PathIsRelativeA(in_path.c_str()))
	{
		std::unique_ptr<char[]> buffer(new char[MAX_PATH]);
		if (GetModuleFileNameA(CurrentModule(), buffer.get(), MAX_PATH) &&
			PathRemoveFileSpecA(buffer.get()))
		{
			PathAppendA(buffer.get(), in_path.c_str());
			*path = buffer.get();
		}
	}
	else
	{
		*path = in_path;
	}

	if (FileExist(*path))
		return true;

	return false;
}

bool StringPathCombine(std::string* dest, const std::string& path, const std::string& more)
{
	std::unique_ptr<char[]> buffer(new char[MAX_PATH]);
	*dest = PathCombineA(buffer.get(), path.c_str(), more.c_str());
	return !dest->empty();
}

bool StringPathCombine(std::wstring* dest, const std::wstring& path, const std::wstring& more)
{
	std::unique_ptr<wchar_t[]> buffer(new wchar_t[MAX_PATH]);
	*dest = PathCombineW(buffer.get(), path.c_str(), more.c_str());
	return !dest->empty();
}

bool StringPathAppend(std::string* path, const std::string& more)
{
	std::unique_ptr<char[]> buffer(new char[MAX_PATH]);
	*path = PathCombineA(buffer.get(), path->c_str(), more.c_str());
	return !path->empty();
}

bool StringPathAppend(std::wstring* path, const std::wstring& more)
{
	std::unique_ptr<wchar_t[]> buffer(new wchar_t[MAX_PATH]);
	*path = PathCombineW(buffer.get(), path->c_str(), more.c_str());
	return !path->empty();
}

bool ModulePath(std::string* out, HMODULE hModule)
{
	std::unique_ptr<char[]> buffer(new char[MAX_PATH]);
	GetModuleFileNameA(hModule, buffer.get(), MAX_PATH);
	*out = buffer.get();
	return !out->empty();
}

bool ModulePath(std::wstring* out, HMODULE hModule)
{
	std::unique_ptr<wchar_t[]> buffer(new wchar_t[MAX_PATH]);
	GetModuleFileNameW(hModule, buffer.get(), MAX_PATH);
	*out = buffer.get();
	return !out->empty();
}

bool ModuleDirectory(std::string* out, HMODULE hModule)
{
	std::unique_ptr<char[]> buffer(new char[MAX_PATH]);
	GetModuleFileNameA(hModule, buffer.get(), MAX_PATH);
	PathRemoveFileSpecA(buffer.get());
	*out = buffer.get();
	return !out->empty();
}

bool ModuleDirectory(std::wstring* out, HMODULE hModule)
{
	std::unique_ptr<wchar_t[]> buffer(new wchar_t[MAX_PATH]);
	GetModuleFileNameW(hModule, buffer.get(), MAX_PATH);
	PathRemoveFileSpecW(buffer.get());
	*out = buffer.get();
	return !out->empty();
}

bool ModuleFileName(std::string* out, HMODULE hModule)
{
	std::unique_ptr<char[]> buffer(new char[MAX_PATH]);
	GetModuleFileNameA(hModule, buffer.get(), MAX_PATH);
	*out = PathFindFileNameA(buffer.get());
	return !out->empty();
}

bool ModuleFileName(std::wstring* out, HMODULE hModule)
{
	std::unique_ptr<wchar_t[]> buffer(new wchar_t[MAX_PATH]);
	GetModuleFileNameW(hModule, buffer.get(), MAX_PATH);
	*out = PathFindFileNameW(buffer.get());
	return !out->empty();
}

void StringToGUID(GUID* id, const std::string& szBuf)
{
	const char* p = szBuf.c_str();
	if (strchr(p, '{')) p++;

	u32 d1;
	s32 d2, d3;
	s32 b[8];

	if (sscanf_s(p, "%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
		&d1, &d2, &d3, &b[0], &b[1], &b[2], &b[3], &b[4], &b[5], &b[6], &b[7]) != 11)
	{
		*id = GUID_NULL;
		return;
	}

	id->Data1 = d1;
	id->Data2 = (u16)d2;
	id->Data3 = (u16)d3;

	for (int i = 0; i < 8; ++i)
		id->Data4[i] = (u8)b[i];

	return;
}

void StringToGUID(GUID* id, const std::wstring& szBuf)
{
	const wchar_t* p = szBuf.c_str();
	if (wcschr(p, L'{')) p++;

	u32 d1;
	s32 d2, d3;
	s32 b[8];

	if (swscanf_s(p, L"%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
		&d1, &d2, &d3, &b[0], &b[1], &b[2], &b[3], &b[4], &b[5], &b[6], &b[7]) != 11)
	{
		*id = GUID_NULL;
		return;
	}

	id->Data1 = d1;
	id->Data2 = (u16)d2;
	id->Data3 = (u16)d3;

	for (int i = 0; i < 8; ++i)
		id->Data4[i] = (u8)b[i];

	return;
}

bool GUIDtoString(std::string* out, const GUID &g)
{
	std::unique_ptr<char[]> buffer(new char[40]);
	sprintf_s(buffer.get(), 40, "{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
		g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

	*out = buffer.get();
	return !out->empty();
}

bool GUIDtoString(std::wstring* out, const GUID &g)
{
	std::unique_ptr<wchar_t[]> buffer(new wchar_t[40]);
	swprintf_s(buffer.get(), 40, L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
		g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

	*out = buffer.get();
	return !out->empty();
}


