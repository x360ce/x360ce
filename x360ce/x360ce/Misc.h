#pragma once

#include <Shlwapi.h>

inline bool ModuleFullPathA(std::string* out, HMODULE hModule = NULL)
{
    if (!out) return false;

    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    *out = buffer;
    return !out->empty();
}

inline bool ModuleFullPathW(std::wstring* out, HMODULE hModule = NULL)
{
    if (!out) return false;

    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    *out = buffer;
    return !out->empty();
}

inline bool ModulePathA(std::string* out, HMODULE hModule = NULL)
{
    if (!out) return false;

    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    PathRemoveFileSpecA(buffer);
    *out = buffer;
    return !out->empty();
}

inline bool ModulePathW(std::wstring* out, HMODULE hModule = NULL)
{
    if (!out) return false;

    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    PathRemoveFileSpecW(buffer);
    *out = buffer;
    return !out->empty();
}

inline bool ModuleFileNameA(std::string* out, HMODULE hModule = NULL)
{
    if (!out) return false;

    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    *out = PathFindFileNameA(buffer);
    return !out->empty();
}

inline bool ModuleFileNameW(std::wstring* out, HMODULE hModule = NULL)
{
    if (!out) return false;

    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    *out = PathFindFileNameW(buffer);
    return !out->empty();
}

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

inline LONG deadzone(LONG val, LONG min, LONG max, LONG lowerDZ, LONG upperDZ)
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

inline void StringToGUID(GUID* id, const char* szBuf)
{
    if (!szBuf || !id) return;

    if (strchr(szBuf, '{')) szBuf++;

    u32 d1;
    s32 d2, d3;
    s32 b[8];

    sscanf_s(szBuf, "%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
        &d1, &d2, &d3, &b[0], &b[1], &b[2], &b[3], &b[4], &b[5], &b[6], &b[7]);

    id->Data1 = d1;
    id->Data2 = (u16)d2;
    id->Data3 = (u16)d3;

    for (int i = 0; i < 8; ++i)
        id->Data4[i] = (u8)b[i];

    return;
}

inline void StringToGUID(GUID* id, const wchar_t* szBuf)
{
    if (!szBuf || !id) return;

    if (wcschr(szBuf, L'{')) szBuf++;

    u32 d1;
    s32 d2, d3;
    s32 b[8];

    swscanf_s(szBuf, L"%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
        &d1, &d2, &d3, &b[0], &b[1], &b[2], &b[3], &b[4], &b[5], &b[6], &b[7]);

    id->Data1 = d1;
    id->Data2 = (u16)d2;
    id->Data3 = (u16)d3;

    for (int i = 0; i < 8; ++i)
        id->Data4[i] = (u8)b[i];

    return;
}

inline bool GUIDtoStringA(std::string* out, const GUID &g)
{
    if (!out) return false;
    out->resize(40);

    sprintf_s(&(*out)[0], 40, "{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

    out->resize(38);
    return (*out)[0] != '\0';
}

inline const bool GUIDtoStringW(std::wstring* out, const GUID &g)
{
    if (!out) return false;
    out->resize(40);

    swprintf_s(&(*out)[0], 40, L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

    out->resize(38);
    return (*out)[0] != '\0';
}


