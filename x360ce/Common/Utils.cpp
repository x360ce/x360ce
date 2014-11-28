#include "stdafx.h"

#include "Common.h"

#include <string>
#include <Shlwapi.h>
#include <Shlobj.h>
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")

NOINLINE BOOL CurrentModule(HMODULE* phModule)
{
    BOOL(*pCurrentModule)(HMODULE* phModule) = CurrentModule;
    return GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)pCurrentModule, phModule);
}

NOINLINE HMODULE CurrentModule()
{
    HMODULE hModule = 0;
    HMODULE(*pCurrentModule)() = CurrentModule;
    GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, (LPCTSTR)pCurrentModule, &hModule);
    return hModule;
}

bool BuildPath(const std::string& filename, std::string* fullpath, bool check_exist)
{
    if (!fullpath) return false;

    std::string path;
    path.resize(MAX_PATH);

    if (PathIsRelativeA(filename.c_str()))
    {
        DWORD dwLen = GetModuleFileNameA(CurrentModule(), &path[0], MAX_PATH);
        if (dwLen > 0 && PathRemoveFileSpecA(&path[0]))
        {
            PathAppendA(&path[0], filename.c_str());
            if (!check_exist)
            {
                *fullpath = path;
                return true;
            }
            else if (PathFileExistsA(&path[0]) && PathIsDirectoryA(&path[0]) == FALSE)
            {
                *fullpath = path;
                return true;
            }
        }
    }
    else
    {
        if (!check_exist)
        {
            *fullpath = path;
            return true;
        }
        else if (PathFileExistsA(filename.c_str()) && PathIsDirectoryA(filename.c_str()) == FALSE)
        {
            *fullpath = filename;
            return true;
        }
    }
    return false;
}

bool ModuleFullPathA(std::string* out, HMODULE hModule)
{
    if (!out) return false;

    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    *out = buffer;
    return !out->empty();
}

bool ModuleFullPathW(std::wstring* out, HMODULE hModule)
{
    if (!out) return false;

    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    *out = buffer;
    return !out->empty();
}

bool ModulePathA(std::string* out, HMODULE hModule)
{
    if (!out) return false;

    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    PathRemoveFileSpecA(buffer);
    *out = buffer;
    return !out->empty();
}

bool ModulePathW(std::wstring* out, HMODULE hModule)
{
    if (!out) return false;

    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    PathRemoveFileSpecW(buffer);
    *out = buffer;
    return !out->empty();
}

bool ModuleFileNameA(std::string* out, HMODULE hModule)
{
    if (!out) return false;

    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    *out = PathFindFileNameA(buffer);
    return !out->empty();
}

bool ModuleFileNameW(std::wstring* out, HMODULE hModule)
{
    if (!out) return false;

    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    *out = PathFindFileNameW(buffer);
    return !out->empty();
}

void StringToGUID(GUID* id, const char* szBuf)
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

void StringToGUID(GUID* id, const wchar_t* szBuf)
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

bool GUIDtoStringA(std::string* out, const GUID &g)
{
    if (!out) return false;
    out->resize(40);

    sprintf_s(&(*out)[0], 40, "{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

    out->resize(38);
    return (*out)[0] != '\0';
}

bool GUIDtoStringW(std::wstring* out, const GUID &g)
{
    if (!out) return false;
    out->resize(40);

    swprintf_s(&(*out)[0], 40, L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

    out->resize(38);
    return (*out)[0] != '\0';
}


