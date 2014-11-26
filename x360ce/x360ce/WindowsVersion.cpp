#include "stdafx.h"

#include <Windows.h>

BOOL EqualsMajorVersion(DWORD majorVersion)
{
    OSVERSIONINFOEX osVersionInfo;
    ::ZeroMemory(&osVersionInfo, sizeof(OSVERSIONINFOEX));
    osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
    osVersionInfo.dwMajorVersion = majorVersion;
    ULONGLONG maskCondition = ::VerSetConditionMask(0, VER_MAJORVERSION, VER_EQUAL);
    return ::VerifyVersionInfo(&osVersionInfo, VER_MAJORVERSION, maskCondition);
}
BOOL EqualsMinorVersion(DWORD minorVersion)
{
    OSVERSIONINFOEX osVersionInfo;
    ::ZeroMemory(&osVersionInfo, sizeof(OSVERSIONINFOEX));
    osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
    osVersionInfo.dwMinorVersion = minorVersion;
    ULONGLONG maskCondition = ::VerSetConditionMask(0, VER_MINORVERSION, VER_EQUAL);
    return ::VerifyVersionInfo(&osVersionInfo, VER_MINORVERSION, maskCondition);
}

BOOL EqualsServicePack(WORD servicePackMajor)
{
    OSVERSIONINFOEX osVersionInfo;
    ::ZeroMemory(&osVersionInfo, sizeof(OSVERSIONINFOEX));
    osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
    osVersionInfo.wServicePackMajor = servicePackMajor;
    ULONGLONG maskCondition = ::VerSetConditionMask(0, VER_SERVICEPACKMAJOR, VER_EQUAL);
    return ::VerifyVersionInfo(&osVersionInfo, VER_SERVICEPACKMAJOR, maskCondition);
}

BOOL EqualsProductType(BYTE productType)
{
    OSVERSIONINFOEX osVersionInfo;
    ::ZeroMemory(&osVersionInfo, sizeof(OSVERSIONINFOEX));
    osVersionInfo.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);
    osVersionInfo.wProductType = productType;
    ULONGLONG maskCondition = ::VerSetConditionMask(0, VER_PRODUCT_TYPE, VER_EQUAL);
    return ::VerifyVersionInfo(&osVersionInfo, VER_PRODUCT_TYPE, maskCondition);
}

BYTE GetProductType()
{
    if (EqualsProductType(VER_NT_WORKSTATION))
    {
        return VER_NT_WORKSTATION;
    }
    else if (EqualsProductType(VER_NT_SERVER))
    {
        return VER_NT_SERVER;
    }
    return 0;
}

typedef BOOL(WINAPI* GetVersionExA_t) (LPOSVERSIONINFOA lpVersionInformation);
typedef BOOL(WINAPI* GetProductInfo_t)(DWORD dwOSMajorVersion, DWORD dwOSMinorVersion, DWORD dwSpMajorVersion, DWORD dwSpMinorVersion, PDWORD pdwReturnedProductType);

BOOL MyGetVersionExA(LPOSVERSIONINFOA lpVersionInformation)
{
    GetVersionExA_t RealGetVersionExA = (GetVersionExA_t)GetProcAddress(GetModuleHandle(TEXT("kernel32.dll")), "GetVersionExA");
    return RealGetVersionExA(lpVersionInformation);
}

BOOL MyGetProductInfo(DWORD dwOSMajorVersion, DWORD dwOSMinorVersion, DWORD dwSpMajorVersion, DWORD dwSpMinorVersion, PDWORD pdwReturnedProductType)
{
    GetProductInfo_t RealGetProductInfo = (GetProductInfo_t)GetProcAddress(GetModuleHandle(TEXT("kernel32.dll")), "GetProductInfo");
    return RealGetProductInfo(dwOSMajorVersion, dwOSMinorVersion, dwSpMajorVersion, dwSpMinorVersion, pdwReturnedProductType);
}

bool GetWindowsVersionName(std::string* out)
{
    if (!out) return false;

    *out = "Microsoft";

    BYTE dwType = GetProductType();
    DWORD dwSubType = 0;

    SYSTEM_INFO si;
    ZeroMemory(&si, sizeof(SYSTEM_INFO));
    GetNativeSystemInfo(&si);

    if (EqualsMajorVersion(6))
    {
        if (EqualsMinorVersion(3))
        {
            MyGetProductInfo(6, 3, 0, 0, &dwSubType);

            if (dwType == VER_NT_WORKSTATION)
            {
                out->append(" Windows 8.1");
                if (dwSubType == PRODUCT_PROFESSIONAL)
                    out->append(" Pro");
            }
            else if (dwType == VER_NT_SERVER)
            {
                out->append(" Windows Server 2012 R2");
            }
        }
        else if (EqualsMinorVersion(2))
        {
            MyGetProductInfo(6, 2, 0, 0, &dwSubType);

            if (dwType == VER_NT_WORKSTATION)
            {
                out->append(" Windows 8");
                if (dwSubType == PRODUCT_PROFESSIONAL)
                    out->append(" Pro");
            }
            else if (dwType == VER_NT_SERVER)
            {
                out->append(" Windows Server 2012");
            }
        }
        else if (EqualsMinorVersion(1))
        {
            MyGetProductInfo(6, 1, 0, 0, &dwSubType);

            if (dwType == VER_NT_WORKSTATION)
                out->append( "Windows 7");
            else out->append(" Windows Server 2008 R2");

            switch (dwType)
            {
            case PRODUCT_ULTIMATE:
                out->append(" Ultimate Edition");
                break;
            case PRODUCT_PROFESSIONAL:
                out->append(" Professional");
                break;
            case PRODUCT_HOME_PREMIUM:
                out->append(" Home Premium Edition");
                break;
            case PRODUCT_HOME_BASIC:
                out->append(" Home Basic Edition");
                break;
            case PRODUCT_ENTERPRISE:
                out->append(" Enterprise Edition");
                break;
            case PRODUCT_BUSINESS:
                out->append(" Business Edition");
                break;
            case PRODUCT_STARTER:
                out->append(" Starter Edition");
                break;
            case PRODUCT_CLUSTER_SERVER:
                out->append(" Cluster Server Edition");
                break;
            case PRODUCT_DATACENTER_SERVER:
                out->append(" Datacenter Edition");
                break;
            case PRODUCT_DATACENTER_SERVER_CORE:
                out->append(" Datacenter Edition (core installation)");
                break;
            case PRODUCT_ENTERPRISE_SERVER:
                out->append(" Enterprise Edition");
                break;
            case PRODUCT_ENTERPRISE_SERVER_CORE:
                out->append(" Enterprise Edition (core installation)");
                break;
            case PRODUCT_ENTERPRISE_SERVER_IA64:
                out->append(" Enterprise Edition for Itanium-based Systems");
                break;
            case PRODUCT_SMALLBUSINESS_SERVER:
                out->append(" Small Business Server");
                break;
            case PRODUCT_SMALLBUSINESS_SERVER_PREMIUM:
                out->append(" Small Business Server Premium Edition");
                break;
            case PRODUCT_STANDARD_SERVER:
                out->append(" Standard Edition");
                break;
            case PRODUCT_STANDARD_SERVER_CORE:
                out->append(" Standard Edition (core installation)");
                break;
            case PRODUCT_WEB_SERVER:
                out->append(" Web Server Edition");
                break;
            }
        }
        else if (EqualsMinorVersion(0))
        {
            MyGetProductInfo(6, 0, 0, 0, &dwSubType);

            if (dwType == VER_NT_WORKSTATION)
                out->append(" Windows Vista");
            else out->append(" Windows Server 2008");
        }
    }
    else if (EqualsMajorVersion(5))
    {
        OSVERSIONINFOEXA osvi;
        ZeroMemory(&osvi, sizeof(OSVERSIONINFOEXA));
        osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEXA);

        if (osvi.dwMinorVersion == 2)
        {
            if (GetSystemMetrics(SM_SERVERR2))
                out->append(" Windows Server 2003 R2");
            else if (osvi.wSuiteMask & VER_SUITE_STORAGE_SERVER)
                out->append(" Windows Storage Server 2003");
            else if (osvi.wSuiteMask & VER_SUITE_WH_SERVER)
                out->append(" Windows Home Server");
            else if (osvi.wProductType == VER_NT_WORKSTATION &&
                si.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
            {
                out->append(" Windows XP Professional x64 Edition");
            }
            else out->append(" Windows Server 2003, ");
            if (osvi.wProductType != VER_NT_WORKSTATION)
            {
                if (si.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
                {
                    if (osvi.wSuiteMask & VER_SUITE_DATACENTER)
                        out->append(" Datacenter x64 Edition");
                    else if (osvi.wSuiteMask & VER_SUITE_ENTERPRISE)
                        out->append(" Enterprise x64 Edition");
                    else out->append(" Standard x64 Edition");
                }
                else
                {
                    if (osvi.wSuiteMask & VER_SUITE_COMPUTE_SERVER)
                        out->append(" Compute Cluster Edition");
                    else if (osvi.wSuiteMask & VER_SUITE_DATACENTER)
                        out->append(" Datacenter Edition");
                    else if (osvi.wSuiteMask & VER_SUITE_ENTERPRISE)
                        out->append(" Enterprise Edition");
                    else if (osvi.wSuiteMask & VER_SUITE_BLADE)
                        out->append(" Web Edition");
                    else out->append(" Standard Edition");
                }
            }
        }
        else if (osvi.dwMinorVersion == 1)
        {
            out->append( "Windows XP");
            if (osvi.wSuiteMask & VER_SUITE_PERSONAL)
                out->append(" Home Edition");
            else out->append(" Professional");
        }
    }

    if (EqualsMajorVersion(6))
    {
        if (si.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64)
            out->append(" (x64)");
        else if (si.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_INTEL)
            out->append(" (x86)");
    }

    return true;
}