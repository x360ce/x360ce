#pragma once

#include <vector>
#include <dinput.h>

#include "NonCopyable.h"
#include "Controller.h"

#include "InputHookManager.h"

class ControllerManager : NonCopyable
{
public:
    ControllerManager()
    {
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

private:
    HWND m_hWnd;
    LPDIRECTINPUT8 m_pDirectInput;
    std::vector<Controller> m_controllers;
};
