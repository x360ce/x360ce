#include "stdafx.h"

#include "Common.h"

#include <string>
#include <Shlwapi.h>
#include <Shlobj.h>
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "shell32.lib")

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

bool CheckCommonDirectory(std::string* fullpath, const std::string& filename, const std::string& dirname)
{
    char path[MAX_PATH];
    if (SHGetFolderPathA(NULL, CSIDL_COMMON_APPDATA, NULL, SHGFP_TYPE_CURRENT, path) == S_OK)
    {
        PathAppendA(path, dirname.c_str());
        PathAppendA(path, filename.c_str());
        if (FileExist(path))
        {
            *fullpath = path;
            return true;
        }
    }

    *fullpath = filename;
    return false;
}

bool FullPathFromPath(std::string* out_path, const std::string& in_path)
{
    if (PathIsRelativeA(in_path.c_str()))
    {
        char path[MAX_PATH];    
        if (GetModuleFileNameA(CurrentModule(), path, MAX_PATH) && 
            PathRemoveFileSpecA(path))
        {
            PathAppendA(path, in_path.c_str());
            *out_path = path;
        }
    }
    else
    {
        *out_path = in_path;
    }

    if (FileExist(*out_path))
        return true;
    
    return false;
}

bool ModulePath(std::string* out, HMODULE hModule)
{
    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    *out = buffer;
    return !out->empty();
}

bool ModulePath(std::wstring* out, HMODULE hModule)
{
    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    *out = buffer;
    return !out->empty();
}

bool ModuleDirectory(std::string* out, HMODULE hModule)
{
    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    PathRemoveFileSpecA(buffer);
    *out = buffer;
    return !out->empty();
}

bool ModuleDirectory(std::wstring* out, HMODULE hModule)
{
    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    PathRemoveFileSpecW(buffer);
    *out = buffer;
    return !out->empty();
}

bool ModuleFileName(std::string* out, HMODULE hModule)
{
    char buffer[MAX_PATH];
    GetModuleFileNameA(hModule, buffer, MAX_PATH);
    *out = PathFindFileNameA(buffer);
    return !out->empty();
}

bool ModuleFileName(std::wstring* out, HMODULE hModule)
{
    wchar_t buffer[MAX_PATH];
    GetModuleFileNameW(hModule, buffer, MAX_PATH);
    *out = PathFindFileNameW(buffer);
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
    char id[40];
    sprintf_s(id, 40, "{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

    *out = id;
    return !out->empty();
}

bool GUIDtoString(std::wstring* out, const GUID &g)
{
    wchar_t id[40];
    swprintf_s(id, 40, L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);

    *out = id;
    return !out->empty();
}


