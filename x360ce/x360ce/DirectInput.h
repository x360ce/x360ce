/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
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

#if _MSC_VER < 1700
#include "mutex.h"
#else
#include <mutex>
#endif

// disable C4351 - new behavior: elements of array 'array' will be default initialized
#pragma warning( disable:4351 )

// FIXME
class DInputFFB
{
public:

    DInputFFB()
        :effect()
        ,eff()
        ,pf()
        ,cf()
        ,rf()
        ,xForce(0)
        ,yForce(0)
        ,oldXForce(0)
        ,oldYForce(0)
        ,axisffbcount(0)
        ,oldMagnitude(0)
        ,oldPeriod(0)
        ,leftPeriod(0)
        ,rightPeriod(0)
        ,forcepercent(100)
        ,type(0)
        ,is_created(false)
        ,ffbcaps()
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
    int32_t xForce;
    int32_t yForce;
    int32_t oldXForce;
    int32_t oldYForce;
    uint32_t oldMagnitude;
    uint32_t oldPeriod;
    uint32_t leftPeriod;
    uint32_t rightPeriod;
    float forcepercent;
    uint8_t axisffbcount;
    uint8_t type;
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
    DInputDevice()
        :device(NULL)
        ,state()
        ,ff()
        ,productid(GUID_NULL)
        ,instanceid(GUID_NULL)
        ,dwUserIndex((DWORD)-1)
        ,axiscount(0)
        ,triggerdz()
        ,a2ddeadzone(0)
        ,a2doffset(0)
        ,axisdeadzone()
        ,antideadzone()
        ,axislinear()
        ,gamepadtype(1)
        ,swapmotor(false)
        ,passthrough(true)
        ,axistodpad(false)
        ,useproduct(false)
        ,useforce(false)
    {
    }

    ~DInputDevice()
    {
		//lock_guard lock(m_mutex);

        //check for broken ffd
        bool brokenffd = false;
        if(GetModuleHandleA("tmffbdrv.dll")) brokenffd = true;
        //causes exception in tmffbdrv.dll (Thrustmaster FFB driver)
        //works fine with xiffd.dll (Mori's FFB driver for XInput)
        if(device && brokenffd == false)
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
    uint32_t dwUserIndex;
    uint32_t axiscount;
    int32_t a2ddeadzone;
    int32_t a2doffset;
    int16_t axisdeadzone[4];
    int16_t antideadzone[4];
    int16_t axislinear[4];
    uint8_t triggerdz[2];
    uint8_t gamepadtype;
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

    HRESULT Init()
    {
		return DirectInput8Create(CURRENT_MODULE, DIRECTINPUT_VERSION, IID_IDirectInput8, (VOID**)&m_pDI, NULL);;
    }
};

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
