#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

class Controller;
class ForceFeedback;

struct ForceFeedbackCaps
{
    bool ConstantForce;
    bool PeriodicForce;
    bool RampForce;
};

class ForceFeedback
{
    friend class Controller;
public:
    ForceFeedback(Controller* pController);
    virtual ~ForceFeedback();

    // required because mutex is not copyable
    ForceFeedback(const ForceFeedback &other)
    {
        m_effects = other.m_effects;
        m_LeftPeriod = other.m_LeftPeriod;
        m_RightPeriod = other.m_RightPeriod;
        m_ForcePercent = other.m_ForcePercent;
        m_Type = other.m_Type;
        m_SwapMotors = other.m_SwapMotors;
        m_Axes = other.m_Axes;
        m_Caps = other.m_Caps;
    }

    ForceFeedback& operator=(const ForceFeedback& other)
    {
        if (this != &other)
        {
            m_effects = other.m_effects;
            m_LeftPeriod = other.m_LeftPeriod;
            m_RightPeriod = other.m_RightPeriod;
            m_ForcePercent = other.m_ForcePercent;
            m_Type = other.m_Type;
            m_SwapMotors = other.m_SwapMotors;
            m_Axes = other.m_Axes;
            m_Caps = other.m_Caps;
        }
        return *this;
    }

    bool ForceFeedback::SetState(XINPUT_VIBRATION* pVibration);

    u32 m_LeftPeriod;
    u32 m_RightPeriod;
    float m_ForcePercent;
    u8 m_Type;
    bool m_SwapMotors;

private:
    static BOOL CALLBACK EnumFFAxesCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);
    static BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

    void SetCaps(const ForceFeedbackCaps& caps)
    {
        m_Caps = caps;
    }

    bool IsSupported();
    void StartEffects(DIEFFECT* effectType);
    bool SetDeviceForcesEjocys(XINPUT_VIBRATION* pVibration);
    bool SetDeviceForcesNew(XINPUT_VIBRATION* pVibration);
    bool SetDeviceForcesFailsafe(XINPUT_VIBRATION* pVibration);

    Controller* m_pController;
    std::vector<LPDIRECTINPUTEFFECT> m_effects;
    Mutex m_mutex;
    u8 m_Axes;
    ForceFeedbackCaps m_Caps;
};

class Controller
{
    friend class ForceFeedback;
public:
    Controller(u32 user);
    ~Controller();

    // required because mutex is not copyable
    Controller(const Controller &other)
    {
        m_pDevice = other.m_pDevice;
        m_state = other.m_state;
        m_mapping = other.m_mapping;
        m_productid = other.m_productid;
        m_instanceid = other.m_instanceid;
        m_user = other.m_user;
        m_axiscount = other.m_axiscount;
        m_gamepadtype = other.m_gamepadtype;
        m_passthrough = other.m_passthrough;
        m_useforce = other.m_useforce;

        if (other.m_pForceFeedback)
        {
            m_pForceFeedback = new ForceFeedback(this);
            *m_pForceFeedback = *other.m_pForceFeedback;
        }
    }

    Controller& operator=(const Controller& other)
    {
        if (this != &other)
        {
            m_pDevice = other.m_pDevice;
            m_state = other.m_state;
            m_mapping = other.m_mapping;
            m_productid = other.m_productid;
            m_instanceid = other.m_instanceid;
            m_user = other.m_user;
            m_axiscount = other.m_axiscount;
            m_gamepadtype = other.m_gamepadtype;
            m_passthrough = other.m_passthrough;
            m_useforce = other.m_useforce;

            if (other.m_pForceFeedback)
            {
                m_pForceFeedback = new ForceFeedback(this);
                *m_pForceFeedback = *other.m_pForceFeedback;
            }
        }
        return *this;
    }

    DWORD GetState(XINPUT_STATE* pState);
    DWORD CreateDevice();

    bool Initalized() const
    {
        return m_pDevice != nullptr;
    }

    Mapping m_mapping;
    ForceFeedback* m_pForceFeedback;
    GUID m_productid;
    GUID m_instanceid;
    u32 m_user;
    u32 m_axiscount;
    u8 m_gamepadtype;
    bool m_passthrough;
    bool m_useforce;

private:
    static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);
    bool ButtonPressed(u32 buttonidx);
    HRESULT UpdateState();

    Mutex m_mutex;

    LPDIRECTINPUTDEVICE8 m_pDevice;
    DIJOYSTATE2 m_state;
};

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

extern std::vector<Controller> g_Controllers;