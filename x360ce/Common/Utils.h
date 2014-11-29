#pragma once

#include "Common.h"

#include <string>
#include <Shlwapi.h>
#include <Shlobj.h>
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")

HMODULE CurrentModule();

bool BuildPath(const std::string& filename, std::string* fullpath, bool check_exist = true);
bool ModuleFullPathA(std::string* out, HMODULE hModule = NULL);
bool ModuleFullPathW(std::wstring* out, HMODULE hModule = NULL);
bool ModulePathA(std::string* out, HMODULE hModule = NULL);
bool ModulePathW(std::wstring* out, HMODULE hModule = NULL);
bool ModuleFileNameA(std::string* out, HMODULE hModule = NULL);
bool ModuleFileNameW(std::wstring* out, HMODULE hModule = NULL);

void StringToGUID(GUID* id, const char* szBuf);
void StringToGUID(GUID* id, const wchar_t* szBuf);

bool GUIDtoStringA(std::string* out, const GUID &g);
bool GUIDtoStringW(std::wstring* out, const GUID &g);

inline LONG clamp(LONG val, LONG min, LONG max)
{
    if (val < min) return min;
    if (val > max) return max;

    return val;
}

/// <summary>Convert short [-32768;32767] to float range [-1.0f;1.0f].</summary>
inline FLOAT ConvertToFloat(SHORT val)
{
    FLOAT maxValue = val >= 0 ? (float)32767 : (FLOAT)32768;
    return ((float)val) / maxValue;
}

/// <summary>Convert float [-1.0f;1.0f] to short range [-32768;32767].</summary>
inline SHORT ConvertToShort(float val)
{
    FLOAT maxValue = val >= 0 ? (FLOAT)32767 : (FLOAT)32768;
    return (SHORT)(val * maxValue);
}

inline long deadzone(LONG val, LONG min, LONG max, LONG lowerDZ, LONG upperDZ)
{
    if (val < lowerDZ) return min;
    if (val > upperDZ) return max;

    return val;
}

inline static WORD flipShort(WORD s)
{
    return (WORD)((s >> 8) | (s << 8));
}

inline static DWORD flipLong(DWORD l)
{
    return (((DWORD)flipShort((WORD)l)) << 16) | flipShort((WORD)(l >> 16));
}



