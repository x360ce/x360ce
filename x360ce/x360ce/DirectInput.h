#pragma once

#include <dinput.h>
#include "mutex.h"

// disable C4351 - new behavior: elements of array 'array' will be default initialized
#pragma warning( disable:4351 )

// FIXME
class DInputFFB
{
public:

    DInputFFB()
        :effect()
        , eff()
        , pf()
        , cf()
        , rf()
        , xForce(0)
        , yForce(0)
        , oldXForce(0)
        , oldYForce(0)
        , axisffbcount(0)
        , oldMagnitude(0)
        , oldPeriod(0)
        , leftPeriod(0)
        , rightPeriod(0)
        , forcepercent(100)
        , type(0)
        , is_created()
        , IsMotorInitialized()
        , IsSupported(false)
        , IsSupportChecked(false)
        , ffbcaps()
    {};

    virtual ~DInputFFB()
    {
        if (effect[FFB_LEFTMOTOR]) effect[FFB_LEFTMOTOR]->Release();
        if (effect[FFB_RIGHTMOTOR]) effect[FFB_RIGHTMOTOR]->Release();
    }

    LPDIRECTINPUTEFFECT effect[2];
    DIEFFECT eff[2];
    DIPERIODIC pf;
    DICONSTANTFORCE cf;
    DIRAMPFORCE rf;
    s32 xForce;
    s32 yForce;
    s32 oldXForce;
    s32 oldYForce;
    u32 oldMagnitude;
    u32 oldPeriod;
    u32 leftPeriod;
    u32 rightPeriod;
    float forcepercent;
    u8 axisffbcount;
    u8 type;
    BOOL is_created[2];
    BOOL IsMotorInitialized[2];
    BOOL IsSupported;
    BOOL IsSupportChecked;
    struct Caps
    {
        bool ConstantForce;
        bool PeriodicForce;
    } ffbcaps;
};

// FIXME
class DInputDevice
{
public:
    DInputDevice()
        :device(NULL)
        , state()
        , ff()
        , productid(GUID_NULL)
        , instanceid(GUID_NULL)
        , dwUserIndex((DWORD)-1)
        , axiscount(0)
        , triggerdz()
        , a2ddeadzone(0)
        , a2doffset(0)
        , axisdeadzone()
        , antideadzone()
        , axislinear()
        , gamepadtype(1)
        , swapmotor(false)
        , passthrough(true)
        , axistodpad(false)
        , useproduct(false)
        , useforce(false)
    {
    }

    ~DInputDevice()
    {
        //lock_guard lock(m_mutex);

        //check for broken ffd
        bool brokenffd = false;
        if (GetModuleHandleA("tmffbdrv.dll")) brokenffd = true;
        //causes exception in tmffbdrv.dll (Thrustmaster FFB driver)
        //works fine with xiffd.dll (Mori's FFB driver for XInput)
        if (device && brokenffd == false)
        {
            device->SendForceFeedbackCommand(DISFFC_RESET);
            device->Release();
        }
    }

    // FIXME
    //recursive_mutex m_mutex;

    LPDIRECTINPUTDEVICE8 device;
    DIJOYSTATE2 state;
    DInputFFB ff;
    GUID productid;
    GUID instanceid;
    u32 dwUserIndex;
    u32 axiscount;
    s32 a2ddeadzone;
    s32 a2doffset;
    s16 axisdeadzone[4];
    s16 antideadzone[4];
    s16 axislinear[4];
    u8 triggerdz[2];
    u8 gamepadtype;
    bool swapmotor;
    bool passthrough;
    bool axistodpad;
    bool useproduct;
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
        return DirectInput8Create(CURRENT_MODULE, DIRECTINPUT_VERSION, IID_IDirectInput8, (VOID**)&m_pDI, NULL);;
    }
};

HRESULT InitDirectInput(HWND hDlg, DInputDevice& device);
BOOL ButtonPressed(DWORD buttonidx, DInputDevice& device);
HRESULT UpdateState(DInputDevice& device);
WORD EnumPadCount();
BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

BOOL IsForceSupported(DInputDevice& device);

HRESULT DInputSetState(DInputDevice& device, WORD force, bool motor);

HRESULT SetDeviceForces(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForce(DInputDevice& device, bool motor);

HRESULT SetDeviceForcesFailsafe(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForceFailsafe(DInputDevice& device, bool motor);

HRESULT SetDeviceForcesEjocys(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForceEjocys(DInputDevice& device, bool motor);

HRESULT SetDeviceForcesNew(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForceNew(DInputDevice& device, bool motor);

