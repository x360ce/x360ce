#pragma once

#include <vector>
#include <dinput.h>

#include "NonCopyable.h"
#include "Controller.h"

class ControllerManager : NonCopyable
{
public:
    ControllerManager() : m_pDI(nullptr), m_hWnd(NULL)
    {
        if (!m_hWnd) m_hWnd = CreateWindow(
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

        HRESULT result = DirectInput8Create(CurrentModule(), DIRECTINPUT_VERSION, IID_IDirectInput8, (VOID**)&m_pDI, NULL);;

        if (FAILED(result))
        {
            PrintLog("DirectInput cannot be initialized");
            MessageBox(NULL, "DirectInput cannot be initialized", "x360ce - Error", MB_ICONERROR);
            exit(result);
        }
    }

    ~ControllerManager()
    {
        m_controllers.clear();

        if (m_hWnd)
            DestroyWindow(m_hWnd);

        if (m_pDI)
        {
            m_pDI->Release();
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
        return m_pDI;
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
    LPDIRECTINPUT8 m_pDI;

    std::vector<Controller> m_controllers;
};
