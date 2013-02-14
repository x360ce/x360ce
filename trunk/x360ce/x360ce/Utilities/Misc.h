/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
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

inline char* ModuleFullPathA(HMODULE hModule = NULL)
{
    static char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    return strPath;
}

inline wchar_t* ModuleFullPathW(HMODULE hModule = NULL)
{
    static wchar_t strPath[MAX_PATH];
    GetModuleFileNameW(hModule, strPath, MAX_PATH);
    return strPath;
}

inline char* ModulePathA(HMODULE hModule = NULL)
{
    static char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    PathRemoveFileSpecA(strPath);
    return strPath;
}

inline wchar_t* ModulePathW(HMODULE hModule = NULL)
{
    static wchar_t strPath[MAX_PATH];
    GetModuleFileNameW(hModule, strPath, MAX_PATH);
    PathRemoveFileSpecW(strPath);
    return strPath;
}

inline char* ModuleFileNameA(HMODULE hModule = NULL)
{
    static char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    return PathFindFileNameA(strPath);
}

inline wchar_t* ModuleFileNameW(HMODULE hModule = NULL)
{
    static wchar_t strPath[MAX_PATH];
    GetModuleFileNameW(hModule, strPath, MAX_PATH);
    return PathFindFileNameW(strPath);
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

    uint32_t d1;
    int32_t d2, d3;
    int32_t b[8];

    sscanf_s(szBuf,"%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
             &d1,&d2,&d3,&b[0],&b[1],&b[2],&b[3],&b[4],&b[5],&b[6],&b[7]);

    id.Data1 = d1;
    id.Data2 = (uint16_t) d2;
    id.Data3 = (uint16_t) d3;

    for(int i = 0; i < 8; ++i)
        id.Data4[i] = (uint8_t) b[i];

    return;
}

inline void StringToGUID(LPCWSTR szBuf, GUID& id)
{
    if(!szBuf) return;

    if (wcschr(szBuf,L'{')) szBuf++;

    uint32_t d1;
    int32_t d2, d3;
    int32_t b[8];

    swscanf_s(szBuf,L"%08lX-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
              &d1,&d2,&d3,&b[0],&b[1],&b[2],&b[3],&b[4],&b[5],&b[6],&b[7]);

    id.Data1 = d1;
    id.Data2 = (uint16_t) d2;
    id.Data3 = (uint16_t) d3;

    for(int i = 0; i < 8; ++i)
        id.Data4[i] = (uint8_t) b[i];

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

typedef void (WINAPI *PGNSI)(LPSYSTEM_INFO);
typedef BOOL (WINAPI *PGPI)(DWORD, DWORD, DWORD, DWORD, PDWORD);
#define PRODUCT_PROFESSIONAL	0x00000030
#define VER_SUITE_WH_SERVER	0x00008000

inline bool windowsVersionName(wchar_t* str, int bufferSize)
{
    OSVERSIONINFOEX osvi;
    SYSTEM_INFO si;
    BOOL bOsVersionInfoEx;
    DWORD dwType;
    ZeroMemory(&si, sizeof(SYSTEM_INFO));
    ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
    bOsVersionInfoEx = GetVersionEx((OSVERSIONINFO*) &osvi);
    if(bOsVersionInfoEx == 0)
        return false; // Call GetNativeSystemInfo if supported or GetSystemInfo otherwise.
    PGNSI pGNSI = (PGNSI) GetProcAddress(GetModuleHandle(TEXT("kernel32.dll")), "GetNativeSystemInfo");
    if(NULL != pGNSI)
        pGNSI(&si);
    else GetSystemInfo(&si); // Check for unsupported OS
    if (VER_PLATFORM_WIN32_NT != osvi.dwPlatformId || osvi.dwMajorVersion <= 4 )
    {
        return false;
    }
    std::wstringstream os;
    os << L"Microsoft "; // Test for the specific product.
    if ( osvi.dwMajorVersion == 6 )
    {
        if( osvi.dwMinorVersion == 0 )
        {
            if( osvi.wProductType == VER_NT_WORKSTATION )
                os << "Windows Vista ";
            else os << "Windows Server 2008 ";
        }
        if ( osvi.dwMinorVersion == 1 )
        {
            if( osvi.wProductType == VER_NT_WORKSTATION )
                os << "Windows 7 ";
            else os << "Windows Server 2008 R2 ";
        }
        PGPI pGPI = (PGPI) GetProcAddress(GetModuleHandle(TEXT("kernel32.dll")), "GetProductInfo");
        pGPI( osvi.dwMajorVersion, osvi.dwMinorVersion, 0, 0, &dwType);
        switch( dwType )
        {
        case PRODUCT_ULTIMATE:
            os << "Ultimate Edition";
            break;
        case PRODUCT_PROFESSIONAL:
            os << "Professional";
            break;
        case PRODUCT_HOME_PREMIUM:
            os << "Home Premium Edition";
            break;
        case PRODUCT_HOME_BASIC:
            os << "Home Basic Edition";
            break;
        case PRODUCT_ENTERPRISE:
            os << "Enterprise Edition";
            break;
        case PRODUCT_BUSINESS:
            os << "Business Edition";
            break;
        case PRODUCT_STARTER:
            os << "Starter Edition";
            break;
        case PRODUCT_CLUSTER_SERVER:
            os << "Cluster Server Edition";
            break;
        case PRODUCT_DATACENTER_SERVER:
            os << "Datacenter Edition";
            break;
        case PRODUCT_DATACENTER_SERVER_CORE:
            os << "Datacenter Edition (core installation)";
            break;
        case PRODUCT_ENTERPRISE_SERVER:
            os << "Enterprise Edition";
            break;
        case PRODUCT_ENTERPRISE_SERVER_CORE:
            os << "Enterprise Edition (core installation)";
            break;
        case PRODUCT_ENTERPRISE_SERVER_IA64:
            os << "Enterprise Edition for Itanium-based Systems";
            break;
        case PRODUCT_SMALLBUSINESS_SERVER:
            os << "Small Business Server";
            break;
        case PRODUCT_SMALLBUSINESS_SERVER_PREMIUM:
            os << "Small Business Server Premium Edition";
            break;
        case PRODUCT_STANDARD_SERVER:
            os << "Standard Edition";
            break;
        case PRODUCT_STANDARD_SERVER_CORE:
            os << "Standard Edition (core installation)";
            break;
        case PRODUCT_WEB_SERVER:
            os << "Web Server Edition";
            break;
        }
    }
    if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 2 )
    {
        if( GetSystemMetrics(SM_SERVERR2) )
            os <<  "Windows Server 2003 R2, ";
        else if ( osvi.wSuiteMask & VER_SUITE_STORAGE_SERVER )
            os <<  "Windows Storage Server 2003";
        else if ( osvi.wSuiteMask & VER_SUITE_WH_SERVER )
            os <<  "Windows Home Server";
        else if( osvi.wProductType == VER_NT_WORKSTATION &&
                 si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64)
        {
            os <<  "Windows XP Professional x64 Edition";
        }
        else os << "Windows Server 2003, ";  // Test for the server type.
        if ( osvi.wProductType != VER_NT_WORKSTATION )
        {
            if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_IA64 )
            {
                if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                    os <<  "Datacenter Edition for Itanium-based Systems";
                else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                    os <<  "Enterprise Edition for Itanium-based Systems";
            }
            else if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64 )
            {
                if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                    os <<  "Datacenter x64 Edition";
                else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                    os <<  "Enterprise x64 Edition";
                else os <<  "Standard x64 Edition";
            }
            else
            {
                if ( osvi.wSuiteMask & VER_SUITE_COMPUTE_SERVER )
                    os <<  "Compute Cluster Edition";
                else if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                    os <<  "Datacenter Edition";
                else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                    os <<  "Enterprise Edition";
                else if ( osvi.wSuiteMask & VER_SUITE_BLADE )
                    os <<  "Web Edition";
                else os <<  "Standard Edition";
            }
        }
    }
    if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 1 )
    {
        os << "Windows XP ";
        if( osvi.wSuiteMask & VER_SUITE_PERSONAL )
            os <<  "Home Edition";
        else os <<  "Professional";
    }
    if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 0 )
    {
        os << "Windows 2000 ";
        if ( osvi.wProductType == VER_NT_WORKSTATION )
        {
            os <<  "Professional";
        }
        else
        {
            if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                os <<  "Datacenter Server";
            else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                os <<  "Advanced Server";
            else os <<  "Server";
        }
    } // Include service pack (if any) and build number.
    if(osvi.szCSDVersion[0] != L'\0')
    {
        os << " " << osvi.szCSDVersion;
    }
    os << L" (build " << osvi.dwBuildNumber << L")";
    if ( osvi.dwMajorVersion >= 6 )
    {
        if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64 )
            os <<  ", 64-bit";
        else if (si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_INTEL )
            os << ", 32-bit";
    }
    wcscpy_s(str, bufferSize, os.str().c_str());
    return true;
}

#endif // _MISC_H
