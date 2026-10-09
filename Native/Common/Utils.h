#pragma once

#include <string>
#include "Windows.h"
#include "Types.h"

#define IN_RANGE(val, min, max) ((val) > (min) && (val) < (max))
#define IN_RANGE2(val, min, max) ((val) >= (min) && (val) <= (max))

HMODULE LoadLibrarySystem(const std::string& library_name, std::string* out_path);
HMODULE LoadLibrarySystem(const std::wstring& library_name, std::wstring* out_path);
HMODULE LoadLibraryCurrent(const std::string& library_name, std::string* out_path);
HMODULE LoadLibraryCurrent(const std::wstring& library_name, std::wstring* out_path);

inline HMODULE& CurrentModule()
{
	static HMODULE hModule = 0;
	if (!hModule)
		GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)&hModule, &hModule);
	return hModule;
}

bool FileExist(const std::string& path);

bool CheckCommonDirectory(std::string* outpath, const std::string& dirname);
bool FullPathFromPath(std::string* fullpath, const std::string& name);

bool StringPathCombine(std::string* dest, const std::string& path, const std::string& more);
bool StringPathCombine(std::wstring* dest, const std::wstring& path, const std::wstring& more);
bool StringPathAppend(std::string* path, const std::string& more);
bool StringPathAppend(std::wstring* path, const std::wstring& more);

bool ModulePath(std::string* out, HMODULE hModule = NULL);
bool ModulePath(std::wstring* out, HMODULE hModule = NULL);
bool ModuleDirectory(std::string* out, HMODULE hModule = NULL);
bool ModuleDirectory(std::wstring* out, HMODULE hModule = NULL);
bool ModuleFileName(std::string* out, HMODULE hModule = NULL);
bool ModuleFileName(std::wstring* out, HMODULE hModule = NULL);

void StringToGUID(GUID* id, const std::string& szBuf);
void StringToGUID(GUID* id, const std::wstring& szBuf);

bool GUIDtoString(std::string* out, const GUID &g);
bool GUIDtoString(std::wstring* out, const GUID &g);

inline const s32& clamp(const s32& val, const s32& min, const s32& max)
{
	if (val < min) return min;
	if (val > max) return max;

	return val;
}

/// <summary>Convert short [-32768;32767] to float range [-1.0f;1.0f].</summary>
inline const float ConvertToFloat(const s16& val)
{
	float maxValue = val >= 0 ? 32767.0f : 32768.0f;
	return ((float)val) / maxValue;
}

/// <summary>Convert float [-1.0f;1.0f] to short range [-32768;32767].</summary>
inline const s16 ConvertToShort(const float& val)
{
	float maxValue = val >= 0 ? 32767.0f : 32768.0f;
	return (s16)(val * maxValue);
}

inline const s32& deadzone(const s32& val, const s32& min, const s32& max, const s32& lowerDZ, const s32& upperDZ)
{
	if (val < lowerDZ) return min;
	if (val > upperDZ) return max;

	return val;
}

inline const u16 flipShort(u16 s)
{
	return (u16)((s >> 8) | (s << 8));
}

inline const u32 flipLong(u32 l)
{
	return (((u32)flipShort((u16)l)) << 16) | flipShort((u16)(l >> 16));
}



