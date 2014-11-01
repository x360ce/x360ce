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

inline std::string ModuleFullPathA(HMODULE hModule = NULL)
{
    char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    return strPath;
}

inline std::wstring ModuleFullPathW(HMODULE hModule = NULL)
{
    wchar_t strPath[MAX_PATH];
    GetModuleFileNameW(hModule, strPath, MAX_PATH);
    return strPath;
}

inline std::string ModulePathA(HMODULE hModule = NULL)
{
    char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    PathRemoveFileSpecA(strPath);
    return strPath;
}

inline std::wstring ModulePathW(HMODULE hModule = NULL)
{
    wchar_t strPath[MAX_PATH];
    GetModuleFileNameW(hModule, strPath, MAX_PATH);
    PathRemoveFileSpecW(strPath);
    return strPath;
}

inline std::string ModuleFileNameA(HMODULE hModule = NULL)
{
    char strPath[MAX_PATH];
    GetModuleFileNameA(hModule, strPath, MAX_PATH);
    return PathFindFileNameA(strPath);
}

inline std::wstring ModuleFileNameW(HMODULE hModule = NULL)
{
    wchar_t strPath[MAX_PATH];
    GetModuleFileNameW(hModule, strPath, MAX_PATH);
    return PathFindFileNameW(strPath);
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
    return (WORD) ((s>>8) | (s<<8));
}

inline static DWORD flipLong(DWORD l)
{
    return (((DWORD)flipShort((WORD)l))<<16) | flipShort((WORD)(l>>16));
}

inline void StringToGUID(GUID& id, LPCSTR szBuf)
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

inline void StringToGUID(GUID& id, LPCWSTR szBuf)
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


// Add Windows 8.1 support
typedef void (WINAPI *PGNSI)(LPSYSTEM_INFO);
typedef BOOL (WINAPI *PGPI)(DWORD, DWORD, DWORD, DWORD, PDWORD);
#define PRODUCT_PROFESSIONAL	0x00000030
#define VER_SUITE_WH_SERVER	0x00008000

inline std::string windowsVersionName()
{
    OSVERSIONINFOEXA osvi;
    SYSTEM_INFO si;
    BOOL bOsVersionInfoEx;
    DWORD dwType;
    ZeroMemory(&si, sizeof(SYSTEM_INFO));
    ZeroMemory(&osvi, sizeof(OSVERSIONINFOEXA));
    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXA);
    bOsVersionInfoEx = GetVersionExA((OSVERSIONINFOA*) &osvi);    // GetVersionExA is deprecate as Windows 8, should we worry ?
    if(bOsVersionInfoEx == 0)
        return ""; // Call GetNativeSystemInfo if supported or GetSystemInfo otherwise.
    PGNSI pGNSI = (PGNSI) GetProcAddress(GetModuleHandle(TEXT("kernel32.dll")), "GetNativeSystemInfo");
    if(NULL != pGNSI)
        pGNSI(&si);
    else GetSystemInfo(&si); // Check for unsupported OS
    if (VER_PLATFORM_WIN32_NT != osvi.dwPlatformId || osvi.dwMajorVersion <= 4 )
    {
        return "";
    }
    std::string buf;
    buf.append("Microsoft "); // Test for the specific product.
    if ( osvi.dwMajorVersion == 6 )
    {
        PGPI pGPI = (PGPI) GetProcAddress(GetModuleHandle(TEXT("kernel32.dll")), "GetProductInfo");
        pGPI( osvi.dwMajorVersion, osvi.dwMinorVersion, 0, 0, &dwType);

        if( osvi.dwMinorVersion == 0 )
        {
            if( osvi.wProductType == VER_NT_WORKSTATION )
                buf.append("Windows Vista ");
            else buf.append("Windows Server 2008 ");
        }
        if ( osvi.dwMinorVersion == 1 )
        {
            if( osvi.wProductType == VER_NT_WORKSTATION )
                buf.append("Windows 7 ");
            else buf.append("Windows Server 2008 R2 ");

            switch( dwType )
            {
            case PRODUCT_ULTIMATE:
                buf.append("Ultimate Edition");
                break;
            case PRODUCT_PROFESSIONAL:
                buf.append("Professional");
                break;
            case PRODUCT_HOME_PREMIUM:
                buf.append("Home Premium Edition");
                break;
            case PRODUCT_HOME_BASIC:
                buf.append("Home Basic Edition");
                break;
            case PRODUCT_ENTERPRISE:
                buf.append("Enterprise Edition");
                break;
            case PRODUCT_BUSINESS:
                buf.append("Business Edition");
                break;
            case PRODUCT_STARTER:
                buf.append("Starter Edition");
                break;
            case PRODUCT_CLUSTER_SERVER:
                buf.append("Cluster Server Edition");
                break;
            case PRODUCT_DATACENTER_SERVER:
                buf.append("Datacenter Edition");
                break;
            case PRODUCT_DATACENTER_SERVER_CORE:
                buf.append("Datacenter Edition (core installation)");
                break;
            case PRODUCT_ENTERPRISE_SERVER:
                buf.append("Enterprise Edition");
                break;
            case PRODUCT_ENTERPRISE_SERVER_CORE:
                buf.append("Enterprise Edition (core installation)");
                break;
            case PRODUCT_ENTERPRISE_SERVER_IA64:
                buf.append("Enterprise Edition for Itanium-based Systems");
                break;
            case PRODUCT_SMALLBUSINESS_SERVER:
                buf.append("Small Business Server");
                break;
            case PRODUCT_SMALLBUSINESS_SERVER_PREMIUM:
                buf.append("Small Business Server Premium Edition");
                break;
            case PRODUCT_STANDARD_SERVER:
                buf.append("Standard Edition");
                break;
            case PRODUCT_STANDARD_SERVER_CORE:
                buf.append("Standard Edition (core installation)");
                break;
            case PRODUCT_WEB_SERVER:
                buf.append("Web Server Edition");
                break;
            }
        }
        else if ( osvi.dwMinorVersion == 2 )
        {
            if( osvi.wProductType == VER_NT_WORKSTATION )
                buf.append("Windows 8 ");
            else buf.append("Windows Server 2012 ");

            switch( dwType )
            {
            case PRODUCT_PROFESSIONAL:
                buf.append("Pro");
                break;
            case PRODUCT_CLUSTER_SERVER:
                buf.append("Cluster Server Edition");
                break;
            case PRODUCT_DATACENTER_SERVER:
                buf.append("Datacenter Edition");
                break;
            case PRODUCT_ENTERPRISE_SERVER:
                buf.append("Enterprise Edition");
                break;
            case PRODUCT_ENTERPRISE_SERVER_IA64:
                buf.append("Enterprise Edition for Itanium-based Systems");
                break;
            case PRODUCT_SMALLBUSINESS_SERVER:
                buf.append("Small Business Server");
                break;
            case PRODUCT_SMALLBUSINESS_SERVER_PREMIUM:
                buf.append("Small Business Server Premium Edition");
                break;
            case PRODUCT_STANDARD_SERVER:
                buf.append("Standard Edition");
                break;
            case PRODUCT_WEB_SERVER:
                buf.append("Web Server Edition");
                break;
            }
        }
    }
    if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 2 )
    {
        if( GetSystemMetrics(SM_SERVERR2) )
            buf.append("Windows Server 2003 R2, ");
        else if ( osvi.wSuiteMask & VER_SUITE_STORAGE_SERVER )
            buf.append("Windows Storage Server 2003");
        else if ( osvi.wSuiteMask & VER_SUITE_WH_SERVER )
            buf.append("Windows Home Server");
        else if( osvi.wProductType == VER_NT_WORKSTATION &&
                 si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64)
        {
            buf.append("Windows XP Professional x64 Edition");
        }
        else buf.append("Windows Server 2003, ");  // Test for the server type.
        if ( osvi.wProductType != VER_NT_WORKSTATION )
        {
            if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_IA64 )
            {
                if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                    buf.append("Datacenter Edition for Itanium-based Systems");
                else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                    buf.append("Enterprise Edition for Itanium-based Systems");
            }
            else if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64 )
            {
                if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                    buf.append("Datacenter x64 Edition");
                else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                    buf.append("Enterprise x64 Edition");
                else buf.append("Standard x64 Edition");
            }
            else
            {
                if ( osvi.wSuiteMask & VER_SUITE_COMPUTE_SERVER )
                    buf.append("Compute Cluster Edition");
                else if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                    buf.append("Datacenter Edition");
                else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                    buf.append("Enterprise Edition");
                else if ( osvi.wSuiteMask & VER_SUITE_BLADE )
                    buf.append("Web Edition");
                else buf.append("Standard Edition");
            }
        }
    }
    if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 1 )
    {
        buf.append("Windows XP ");
        if( osvi.wSuiteMask & VER_SUITE_PERSONAL )
            buf.append("Home Edition");
        else buf.append("Professional");
    }
    if ( osvi.dwMajorVersion == 5 && osvi.dwMinorVersion == 0 )
    {
        buf.append("Windows 2000 ");
        if ( osvi.wProductType == VER_NT_WORKSTATION )
        {
            buf.append("Professional");
        }
        else
        {
            if( osvi.wSuiteMask & VER_SUITE_DATACENTER )
                buf.append("Datacenter Server");
            else if( osvi.wSuiteMask & VER_SUITE_ENTERPRISE )
                buf.append("Advanced Server");
            else buf.append("Server");
        }
    } // Include service pack (if any) and build number.
    if(osvi.szCSDVersion[0] != L'\0')
    {
        buf.append(" ");
        buf.append(osvi.szCSDVersion);
    }
    buf.append(" (build ");
    char tmp[2 * 32];
    sprintf_s(tmp, "%lu", osvi.dwBuildNumber);
    buf.append(tmp);
    buf.append(")");
    if ( osvi.dwMajorVersion >= 6 )
    {
        if ( si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_AMD64 )
            buf.append(", 64-bit");
        else if (si.wProcessorArchitecture==PROCESSOR_ARCHITECTURE_INTEL )
            buf.append(", 32-bit");
    }
    return buf;
}

#endif // _MISC_H
