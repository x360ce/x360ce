#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"
#include "Timer.h"

class ForceFeedback
{
    friend class Controller;

    struct Caps
    {
        Caps()
        {
            ConstantForce = false;
            PeriodicForce = false;
        }

        bool ConstantForce;
        bool PeriodicForce;
    };

    struct Motor
    {
        Motor() {
            type = 0;
            period = 0;
            strength = 1.0f;
            actuator = -1;

            effect = nullptr;
            currentForce = 0;
            periodBufferMax = -1;
            periodBufferLast = -1;
        }

        u8 type;
        u32 period;
        float strength;
        int actuator;

        Timer timer;
        LPDIRECTINPUTEFFECT effect;
        LONG currentForce;
        LONG periodBufferMax;
        LONG periodBufferLast;
    };

public:
    ForceFeedback(Controller* pController);
    ~ForceFeedback();

    void Shutdown();
    DWORD SetState(XINPUT_VIBRATION* pVibration);

    float m_ForcePercent;
    Motor m_LeftMotor;
    Motor m_RightMotor;
    u32 m_UpdateInterval;

private:
    static BOOL CALLBACK EnumActuatorsCallback(LPCDIDEVICEOBJECTINSTANCE pdidoi, LPVOID pvRef);
    static BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO pdiei, LPVOID pvRef);
    static void CALLBACK ProcessRequests(PVOID lpParameter, BOOLEAN TimerOrWaitFired);

    bool IsSupported();
    bool SetEffects(Motor& motor, LONG speed);

    Controller* m_pController;
    Caps m_Caps;
    std::vector<DWORD> m_Actuators;
    HANDLE m_hTimer;
};
