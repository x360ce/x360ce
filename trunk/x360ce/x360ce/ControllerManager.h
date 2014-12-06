#pragma once

#include "version.h"

#include <vector>
#include <dinput.h>

#include "NonCopyable.h"
#include "Controller.h"

#include "InputHookManager.h"

static const u32 PASSTROUGH = (u32)-2;

class ControllerManager : NonCopyable
{
public:
    ControllerManager()
    {
        std::string processName;
        ModuleFileName(&processName);
#ifndef _M_X64
        PrintLog("x360ce (x86) %s started for \"%s\"", PRODUCT_VERSION, processName.c_str());
#else
        PrintLog("x360ce (x64) %s started for \"%s\"", PRODUCT_VERSION, processName.c_str());
#endif
        std::string windows_name;
        if (GetWindowsVersionName(&windows_name))
            PrintLog("OS: \"%s\"", windows_name.c_str());

        ReadConfig();

        bool bHookDI = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_DI);
        if (bHookDI) InputHookManager::Get().GetInputHook().DisableHook(InputHook::HOOK_DI);
        HRESULT ret = DirectInput8Create(CurrentModule(), DIRECTINPUT_VERSION, IID_IDirectInput8A, (void**)&m_pDirectInput, NULL);       
        if (bHookDI) InputHookManager::Get().GetInputHook().EnableHook(InputHook::HOOK_DI);

        if (m_pDirectInput)
        {
            m_hWnd = CreateWindowExA(0L,
                "Message",	// name of window class
                "x360ce",			// title-bar std::string
                WS_TILED,			// normal window
                CW_USEDEFAULT,		// default horizontal position
                CW_USEDEFAULT,		// default vertical position
                CW_USEDEFAULT,		// default width
                CW_USEDEFAULT,		// default height
                HWND_MESSAGE,		// message-only window
                NULL,				// no class menu
                CurrentModule(),	// handle to application instance
                NULL);				// no window-creation data

            if (!m_hWnd)
                PrintLog("CreateWindow failed with code 0x%x", HRESULT_FROM_WIN32(GetLastError()));
        }

        if (FAILED(ret) || !m_pDirectInput)
        {
            PrintLog("DirectInput cannot be initialized");
            MessageBox(NULL, "DirectInput cannot be initialized", "x360ce - Error", MB_ICONERROR);
            exit(ret);
        }
    }

    ~ControllerManager()
    {
        m_controllers.clear();

        if (m_hWnd)
            DestroyWindow(m_hWnd);

        if (m_pDirectInput)
        {
            m_pDirectInput->Release();
            PrintLog("DirectInput shutdown");
        }
    }

    u32 DeviceInitialize(DWORD dwUserIndex, Controller** ppController)
    {
        // Global disable
        if (g_bDisable)
            return ERROR_DEVICE_NOT_CONNECTED;

        // Invalid dwUserIndex
        if (!(dwUserIndex < XUSER_MAX_COUNT))
            return ERROR_BAD_ARGUMENTS;

        Controller* pController = nullptr;
        for (auto it = m_controllers.begin(); it != m_controllers.end(); ++it)
        {
            if (it->m_user == dwUserIndex)
                pController = &(*it);
        }

        if (!pController)
            return ERROR_DEVICE_NOT_CONNECTED;
        if (ppController) *ppController = pController;

        // passtrough
        if (pController->m_passthrough)
            return PASSTROUGH;

        if (!pController->Initalized())
        {
            DWORD result = pController->CreateDevice();
            if (result == ERROR_SUCCESS)
            {
                PrintLog("[PAD%d] Initialized for user %d", dwUserIndex + 1, dwUserIndex);
                if (g_bInitBeep) MessageBeep(MB_OK);
            }
            else
                return result;
        }

        if (!pController->Initalized())
            return ERROR_DEVICE_NOT_CONNECTED;
        else return ERROR_SUCCESS;
    }

    static ControllerManager& Get()
    {
        static ControllerManager instance;
        return instance;
    }

    LPDIRECTINPUT8& GetDirectInput()
    {
        return m_pDirectInput;
    }

    HWND& GetWindow()
    {
        return m_hWnd;
    }

    std::vector<Controller>& GetControllers()
    {
        return m_controllers;
    }

    bool XInputEnabled()
    {
        if (!enabled && useEnabled)
            return false;
        else
            return true;
    }

    void XInputEnable(BOOL enable)
    {
        /*
        Trick to support XInputEnable states, because not every game calls it, so:
        - must support games that call it:
        if bEnabled is FALSE and bUseEnabled is TRUE = device is disabled -> return S_OK, ie. connected but state not updating
        if bEnabled is TRUE and bUseEnabled is TRUE = device is enabled -> continue, ie. connected and updating state
        - must support games that not call it:
        if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states
        */

        enabled = (enable != 0);
        useEnabled = true;

        if (enabled) PrintLog("XInput Enabled");
        else PrintLog("XInput Disabled");
    }

private:
    HWND m_hWnd;
    LPDIRECTINPUT8 m_pDirectInput;
    std::vector<Controller> m_controllers;

    bool enabled;
    bool useEnabled;
};
