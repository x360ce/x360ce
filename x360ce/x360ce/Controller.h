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
    ForceFeedback();
    virtual ~ForceFeedback();

    void Init();
    void Reset();

    HRESULT SetState(WORD force, u8 motor);
    HRESULT SetDeviceForces(WORD force, u8 motor);

    BOOL IsForceSupported();
    BOOL EffectIsPlaying();

    void ClearEffects();

    HRESULT PrepareForce(u8 motor);
    HRESULT PrepareForceEjocys(u8 motor);
    HRESULT PrepareForceNew(u8 motor);
    HRESULT PrepareForceFailsafe(u8 motor);

    HRESULT SetDeviceForcesEjocys(WORD force, u8 motor);
    HRESULT SetDeviceForcesNew(WORD force, u8 motor);
    HRESULT SetDeviceForcesFailsafe(WORD force, u8 motor);

    static BOOL CALLBACK EnumFFAxesCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);
    static BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

    void SetCaps(const ForceFeedbackCaps& caps)
    {
        Caps = caps;
    }

    Controller* controller;

    LPDIRECTINPUTEFFECT effect[2];
    DIEFFECT* eff[2];
    DICONSTANTFORCE* cf;
    DIPERIODIC* pf;
    DIRAMPFORCE* rf;
    s32 xForce;
    s32 yForce;
    s32 oldXForce;
    s32 oldYForce;
    u32 oldMagnitude;
    u32 oldPeriod;
    BOOL is_created[2];
    BOOL IsMotorInitialized[2];
    BOOL IsSupported;
    BOOL IsSupportChecked;
    ForceFeedbackCaps Caps;

    u32 leftPeriod;
    u32 rightPeriod;
    float forcepercent;
    u8 axisffbcount;
    u8 type;
    bool swapmotor;
};

class Controller
{
public:
    Controller();
    ~Controller();

    void Init();
    void Reset();

    HRESULT UpdateState();
    HRESULT InitDirectInput(HWND hWnd);
    BOOL ButtonPressed(DWORD buttonidx);

    static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

    Mutex mutex;
    LPDIRECTINPUTDEVICE8 device;
    DIJOYSTATE2 state;
    Mapping mapping;
    ForceFeedback ffb;
    GUID productid;
    GUID instanceid;
    u32 dwUserIndex;
    u32 axiscount;
    u8 gamepadtype;
    bool passthrough;
    bool useforce;
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

extern Controller* g_pControllers[XUSER_MAX_COUNT];