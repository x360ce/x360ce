#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#define FFB_LEFTMOTOR 0
#define FFB_RIGHTMOTOR 1

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
public:
    ForceFeedback(Controller* pController);
    virtual ~ForceFeedback();

    // required because mutex is not copyable
    ForceFeedback(const ForceFeedback &other)
    {
        m_pEffectObject[0] = other.m_pEffectObject[0];
        m_pEffectObject[1] = other.m_pEffectObject[1];

        m_LeftPeriod = other.m_LeftPeriod;
        m_RightPeriod = other.m_RightPeriod;
        m_ForcePercent = other.m_ForcePercent;
        m_Type = other.m_Type;
        m_SwapMotors = other.m_SwapMotors;

        m_xForce = other.m_xForce;
        m_yForce = other.m_yForce;
        m_Axes = other.m_Axes;
        m_Caps = other.m_Caps;
    }

    ForceFeedback& operator=(const ForceFeedback& other)
    {
        if (this != &other)
        {
            m_pEffectObject[0] = other.m_pEffectObject[0];
            m_pEffectObject[1] = other.m_pEffectObject[1];

            m_LeftPeriod = other.m_LeftPeriod;
            m_RightPeriod = other.m_RightPeriod;
            m_ForcePercent = other.m_ForcePercent;
            m_Type = other.m_Type;
            m_SwapMotors = other.m_SwapMotors;

            m_xForce = other.m_xForce;
            m_yForce = other.m_yForce;
            m_Axes = other.m_Axes;
            m_Caps = other.m_Caps;
        }
        return *this;
    }

    void SetState(WORD rightMotor, WORD leftMotor);

    bool IsSupported();
    void ClearEffects();

    HRESULT SetDeviceForcesEjocys(WORD force, u8 motor);
    HRESULT SetDeviceForcesNew(WORD force, u8 motor);
    HRESULT SetDeviceForcesFailsafe(WORD force, u8 motor);

    static BOOL CALLBACK EnumFFAxesCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);
    static BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

    void SetCaps(const ForceFeedbackCaps& caps)
    {
        m_Caps = caps;
    }

    LPDIRECTINPUTEFFECT m_pEffectObject[2];
    u32 m_LeftPeriod;
    u32 m_RightPeriod;
    float m_ForcePercent;
    u8 m_Type;
    bool m_SwapMotors;

private:
    Controller* m_pController;
    Mutex m_mutex;
    s32 m_xForce;
    s32 m_yForce;
    u8 m_Axes;
    ForceFeedbackCaps m_Caps;
};

class Controller
{
    friend class ForceFeedback;
public:
    Controller(DWORD index);
    ~Controller();

    // required because mutex is not copyable
    Controller(const Controller &other)
    {
        m_pDevice = other.m_pDevice;
        m_state = other.m_state;
        m_mapping = other.m_mapping;
        m_productid = other.m_productid;
        m_instanceid = other.m_instanceid;
        m_dwUserIndex = other.m_dwUserIndex;
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
            m_dwUserIndex = other.m_dwUserIndex;
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

    HRESULT UpdateState();
    HRESULT InitDirectInput(HWND hWnd);
    BOOL ButtonPressed(DWORD buttonidx);

    bool Initalized() const
    {
        return m_pDevice != nullptr;
    }

    const DIJOYSTATE2& GetState() const
    {
        return m_state;
    }

    static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

    Mapping m_mapping;
    ForceFeedback* m_pForceFeedback;
    GUID m_productid;
    GUID m_instanceid;
    u32 m_dwUserIndex;
    u32 m_axiscount;
    u8 m_gamepadtype;
    bool m_passthrough;
    bool m_useforce;

private:
    Mutex m_mutex;

    LPDIRECTINPUTDEVICE8 m_pDevice;
    DIJOYSTATE2 m_state;
};

class DInputManager
{
private:
    LPDIRECTINPUT8 m_pDI;
public:

    DInputManager() :
        m_pDI(NULL)
    {}
    virtual ~DInputManager()
    {
        if (m_pDI)
        {
            m_pDI->Release();
        }
    }

    LPDIRECTINPUT8& Get()
    {
        return m_pDI;
    }

    HRESULT Init()
    {
        return DirectInput8Create(CurrentModule(), DIRECTINPUT_VERSION, IID_IDirectInput8, (VOID**)&m_pDI, NULL);;
    }
};

extern std::vector<Controller> g_Controllers;