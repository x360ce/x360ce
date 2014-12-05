#pragma once

#include <string>

#include <windows.h>
#include <xinput.h>

#include "Common.h"

class DirectInputModuleManager : NonCopyable
{
public:
    HRESULT(WINAPI* DirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
    HRESULT(WINAPI* DllCanUnloadNow)(void);
    HRESULT(WINAPI* DllGetClassObject)(REFCLSID rclsid, REFIID riid, LPVOID FAR* ppv);
    HRESULT(WINAPI* DllRegisterServer)(void);
    HRESULT(WINAPI* DllUnregisterServer)(void);

    DirectInputModuleManager()
    {
        char system_directory[MAX_PATH];
        DWORD length = 0;
        length = GetSystemDirectoryA(system_directory, MAX_PATH);

        std::string dinput8_path(system_directory);
        StringPathAppend(&dinput8_path, "dinput8.dll");
        m_module = LoadLibraryA(dinput8_path.c_str());

        if (!m_module)
        {
            HRESULT hr = GetLastError();
            char error_msg[MAX_PATH];
            sprintf_s(error_msg, "Cannot load \"%s\" error: 0x%x", dinput8_path.c_str(), hr);
            MessageBoxA(NULL, error_msg, "Error", MB_ICONERROR);
            exit(hr);
        }

        GetProcAddress("DirectInput8Create", &DirectInput8Create);
        GetProcAddress("DllCanUnloadNow", &DllCanUnloadNow);
        GetProcAddress("DllGetClassObject", &DllGetClassObject);
        GetProcAddress("DllRegisterServer", &DllRegisterServer);
        GetProcAddress("DllUnregisterServer", &DllUnregisterServer);
    }

    ~DirectInputModuleManager()
    {
        if (m_module)
        {
            std::string xinput_path;
            ModulePath(&xinput_path, m_module);
            PrintLog("Unloading %s", xinput_path.c_str());
            FreeLibrary(m_module);
        }
    }

    static DirectInputModuleManager& Get()
    {
        static DirectInputModuleManager instance;
        return instance;
    }

private:
    template<typename T>
    inline void GetProcAddress(const char* funcname, T* ppfunc)
    {
        *ppfunc = reinterpret_cast<T>(::GetProcAddress(m_module, funcname));
    }

    HMODULE m_module;
};