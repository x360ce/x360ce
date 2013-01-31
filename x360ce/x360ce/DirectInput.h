/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Found-
 *  ation, either version 3 of the License, or (at your option) any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#ifndef _DIRECTINPUT_H_
#define _DIRECTINPUT_H_

#include <dinput.h>
#include "Config.h"

#include "Utilities\CriticalSection.h"

// disable C4351 - new behavior: elements of array 'array' will be default initialized
#pragma warning( disable:4351 )

// FIXME
class DInputFFB
{
public:

    DInputFFB():
        effect(),
        eff(),
        pf(),
        cf(),
        rf(),
        xForce(0),
        yForce(0),
        oldXForce(0),
        oldYForce(0),
        axisffbcount(0),
        oldMagnitude(0),
        oldPeriod(0),
        leftPeriod(0),
        rightPeriod(0),
        forcepercent(100),
        type(0),
        is_created(false),
        ffbcaps()
    {};

    virtual ~DInputFFB()
    {
        if(effect[FFB_LEFTMOTOR]) effect[FFB_LEFTMOTOR]->Release();
        if(effect[FFB_RIGHTMOTOR]) effect[FFB_RIGHTMOTOR]->Release();
    }

    LPDIRECTINPUTEFFECT effect[2];
    DIEFFECT eff[2];
    DIPERIODIC pf;
    DICONSTANTFORCE cf;
    DIRAMPFORCE rf;
    LONG xForce;
    LONG yForce;
    LONG oldXForce;
    LONG oldYForce;
    DWORD axisffbcount;
    DWORD oldMagnitude;
    DWORD oldPeriod;
    DWORD leftPeriod;
    DWORD rightPeriod;
    FLOAT forcepercent;
    BYTE type;
    bool is_created;

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

    static CriticalSection& Mutex()
    {
        static CriticalSection mutex;
        return mutex;
    }

    DInputDevice():
        device(NULL),
        state(),
        ff(),
        productid(GUID_NULL),
        instanceid(GUID_NULL),
        dwUserIndex((DWORD)-1),
        axiscount(0),
        triggerdeadzone(0),
        a2ddeadzone(0),
        a2doffset(0),
        axisdeadzone(),
        antideadzone(),
        axislinear(),
        gamepadtype(1),
        swapmotor(false),
        connected(false),
        initialized(false),
        passthrough(true),
        fail(false),
        axistodpad(false),
        useproduct(false),
        useforce(false)
    {
        Mutex();
    }

    ~DInputDevice()
    {
        Mutex().Lock();

        //check for broken ffd
        bool brokenffd = false;
        if(GetModuleHandle(L"tmffbdrv.dll")) brokenffd = true;
        //causes exception in tmffbdrv.dll (Thrustmaster FFB driver)
        //works fine with xiffd.dll (Mori's FFB driver for XInput)
        if(device && !brokenffd)
        {
            device->SendForceFeedbackCommand(DISFFC_RESET);
            device->Release();
        }
        Mutex().Unlock();
    }

    LPDIRECTINPUTDEVICE8 device;
    DIJOYSTATE2 state;
    DInputFFB ff;
    GUID productid;
    GUID instanceid;
    DWORD dwUserIndex;
    DWORD axiscount;
    DWORD triggerdeadzone;
    LONG a2ddeadzone;
    LONG a2doffset;
    SHORT axisdeadzone[4];
    SHORT antideadzone[4];
    SHORT axislinear[4];
    BYTE gamepadtype;
    bool swapmotor;
    bool connected;
    bool initialized;
    bool passthrough;
    bool fail;
    bool axistodpad;
    bool useproduct;
    bool useforce;
};

class DInputManager
{
private:
    LPDIRECTINPUT8 m_pDI;
public:

    DInputManager():
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

    HRESULT Init(HMODULE hMod)
    {
        return DirectInput8Create( hMod, DIRECTINPUT_VERSION,IID_IDirectInput8, ( VOID** )&m_pDI, NULL );;
    }
};

extern std::vector<DInputDevice> g_Devices;

HRESULT InitDirectInput( HWND hDlg, DInputDevice& device );
BOOL ButtonPressed(DWORD buttonidx, DInputDevice& device);
HRESULT UpdateState(DInputDevice& device);
WORD EnumPadCount();
BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

HRESULT SetDeviceForces(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForce(DInputDevice& device, bool motor);

HRESULT SetDeviceForcesFailsafe(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForceFailsafe(DInputDevice& device, bool motor);

HRESULT SetDeviceForcesEjocys(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForceEjocys(DInputDevice& device, bool motor);

HRESULT SetDeviceForcesNew(DInputDevice& device, WORD force, bool motor);
HRESULT PrepareForceNew(DInputDevice& device, bool motor);

#endif
