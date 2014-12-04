#pragma once

#include <dinput.h>

class DirectInputManager : NonCopyable
{

public:
    DirectInputManager() : m_pDI(nullptr), m_hWnd(NULL)
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
            ExitProcess(result);
        }
    }

    ~DirectInputManager()
    {
        if (m_hWnd && DestroyWindow(m_hWnd))
            PrintLog("Message window destroyed");

        if (m_pDI)
        {
            m_pDI->Release();
        }
    }

    static DirectInputManager& Get()
    {
        static DirectInputManager instance;
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

private:
    LPDIRECTINPUT8 m_pDI;
    HWND m_hWnd;
};