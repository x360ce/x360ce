#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

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
