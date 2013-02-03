/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Found-
 *  ation, either version 3 of the License, or (at your option) any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#ifndef _MISC_H
#define _MISC_H

#include <Shlwapi.h>

namespace Misc
{

inline char* GetFilePathA(HMODULE hModule = NULL)
{
    static char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    return strPath;
}

inline char* GetFileNameA(HMODULE hModule = NULL)
{
    static char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    return PathFindFileNameA(strPath);
}

inline wchar_t* GetFilePathW(HMODULE hModule = NULL)
{
    static wchar_t strPath[MAX_PATH];
    GetModuleFileName(hModule, strPath, MAX_PATH);
    return strPath;
}

inline wchar_t* GetFileNameW(HMODULE hModule = NULL)
{
    static wchar_t strPath[MAX_PATH];
    GetModuleFileName(hModule, strPath, MAX_PATH);
    return PathFindFileName(strPath);
}

inline LONG clamp(LONG val, LONG min, LONG max)
{
    if (val < min) return min;
    if (val > max) return max;

    return val;
}

inline LONG deadzone(LONG val, LONG min, LONG max, LONG lowerDZ, LONG upperDZ)
{
    if (val < lowerDZ) return min;
    if (val > upperDZ) return max;

    return val;
}

inline static WORD flipShort(WORD s)
{
    return (WORD) ((s>>8) | (s<<8));
}

inline static DWORD flipLong(DWORD l)
{
    return (((DWORD)flipShort((WORD)l))<<16) | flipShort((WORD)(l>>16));
}

inline void StringToGUID(LPCSTR szBuf, GUID& id)
{
    if(!szBuf) return;

    if (strchr(szBuf,'{')) szBuf++;

    sscanf_s(szBuf,"%8X-%4hX-%4hX-%2hX%2hX-%2hX%2hX%2hX%2hX%2hX%2hX",
        &id.Data1,&id.Data2,&id.Data3,
        &id.Data4[0],&id.Data4[1],&id.Data4[2],&id.Data4[3],&id.Data4[4],&id.Data4[5],&id.Data4[6],&id.Data4[7]);

    return;
}

inline void StringToGUID(LPCWSTR szBuf, GUID& id)
{
    if(!szBuf) return;

    if (wcschr(szBuf,L'{')) szBuf++;

    swscanf_s(szBuf,L"%8X-%4hX-%4hX-%2hX%2hX-%2hX%2hX%2hX%2hX%2hX%2hX",
        &id.Data1,&id.Data2,&id.Data3,
        &id.Data4[0],&id.Data4[1],&id.Data4[2],&id.Data4[3],&id.Data4[4],&id.Data4[5],&id.Data4[6],&id.Data4[7]);

    return;
}

inline const std::string GUIDtoStringA(const GUID &g)
{
    char tmp[40];

    sprintf_s(tmp,40,"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
              g.Data1,g.Data2,g.Data3,g.Data4[0],g.Data4[1],g.Data4[2],g.Data4[3],g.Data4[4],g.Data4[5],g.Data4[6],g.Data4[7]);
    return tmp;
}

inline const std::wstring GUIDtoStringW(const GUID &g)
{
    WCHAR str[40];

    swprintf_s(str,40,L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
               g.Data1,g.Data2,g.Data3,g.Data4[0],g.Data4[1],g.Data4[2],g.Data4[3],g.Data4[4],g.Data4[5],g.Data4[6],g.Data4[7]);
    return str;
}
}

#endif // _MISC_H
