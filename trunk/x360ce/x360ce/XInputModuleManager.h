#pragma once

#include <string>

#include <windows.h>
#include <xinput.h>

#include "Common.h"

#include "InputHookManager.h"
#include "ControllerManager.h"

class XInputModuleManager : NonCopyable
{
public:
    // XInput 1.3 and older functions
    DWORD(WINAPI* XInputGetState)(DWORD dwUserIndex, XINPUT_STATE* pState);
    DWORD(WINAPI* XInputSetState)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
    DWORD(WINAPI* XInputGetCapabilities)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
    VOID(WINAPI* XInputEnable)(BOOL enable);
    DWORD(WINAPI* XInputGetDSoundAudioDeviceGuids)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
    DWORD(WINAPI* XInputGetBatteryInformation)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
    DWORD(WINAPI* XInputGetKeystroke)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);

    // XInput 1.3 undocumented functions
    DWORD(WINAPI* XInputGetStateEx)(DWORD dwUserIndex, XINPUT_STATE *pState); // 100
    DWORD(WINAPI* XInputWaitForGuideButton)(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid); // 101
    DWORD(WINAPI* XInputCancelGuideButtonWait)(DWORD dwUserIndex); // 102
    DWORD(WINAPI* XInputPowerOffController)(DWORD dwUserIndex); // 103

    // XInput 1.4 functions
    DWORD(WINAPI* XInputGetAudioDeviceIds)(DWORD dwUserIndex, LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount);

    // XInput 1.4 undocumented functionss
    DWORD(WINAPI* XInputGetBaseBusInformation)(DWORD dwUserIndex, struct XINPUT_BUSINFO* pBusinfo); // 104
    DWORD(WINAPI* XInputGetCapabilitiesEx)(DWORD unk1, DWORD dwUserIndex, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx); // 108

    XInputModuleManager()
    {
        char system_directory[MAX_PATH];
        DWORD length = 0;
        length = GetSystemDirectoryA(system_directory, MAX_PATH);

        std::string current_module;
        ModuleFileName(&current_module, CurrentModule());

        std::string xinput_path(system_directory);
        StringPathAppend(&xinput_path, current_module);

        bool bHookLL = false;

        bHookLL = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_LL);
        if (bHookLL) InputHookManager::Get().GetInputHook().DisableHook(InputHook::HOOK_LL);

        PrintLog("Loading \"%s\"", xinput_path.c_str());
        m_module = LoadLibraryA(xinput_path.c_str());

        if (!m_module)
        {
            HRESULT hr = GetLastError();
            char error_msg[MAX_PATH];

            sprintf_s(error_msg, "Cannot load \"%s\" error: 0x%x", xinput_path.c_str(), hr);
            PrintLog(error_msg);
            MessageBoxA(NULL, error_msg, "Error", MB_ICONERROR);
            exit(hr);
        }
        if (bHookLL) InputHookManager::Get().GetInputHook().EnableHook(InputHook::HOOK_LL);


        // XInput 1.3 and older functions
        GetProcAddress("XInputGetState", &XInputGetState);
        GetProcAddress("XInputSetState", &XInputSetState);
        GetProcAddress("XInputGetCapabilities", &XInputGetCapabilities);
        GetProcAddress("XInputEnable", &XInputEnable);
        GetProcAddress("XInputGetDSoundAudioDeviceGuids", &XInputGetDSoundAudioDeviceGuids);
        GetProcAddress("XInputGetBatteryInformation", &XInputGetBatteryInformation);
        GetProcAddress("XInputGetKeystroke", &XInputGetKeystroke);

        // XInput 1.3 undocumented functions
        GetProcAddress((const char*)100, &XInputGetStateEx);
        GetProcAddress((const char*)101, &XInputWaitForGuideButton);
        GetProcAddress((const char*)102, &XInputCancelGuideButtonWait);
        GetProcAddress((const char*)103, &XInputPowerOffController);

        // XInput 1.4 functions
        GetProcAddress("XInputGetAudioDeviceIds", &XInputGetAudioDeviceIds);

        // XInput 1.4 undocumented functionss
        GetProcAddress((const char*)104, &XInputGetBaseBusInformation);
        GetProcAddress((const char*)108, &XInputGetCapabilitiesEx);
    }

    ~XInputModuleManager()
    {
        if (m_module)
        {
            std::string xinput_path;
            ModulePath(&xinput_path, m_module);
            PrintLog("Unloading %s", xinput_path.c_str());
            FreeLibrary(m_module);
        }
    }

    static XInputModuleManager& Get()
    {
        static XInputModuleManager instance;
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