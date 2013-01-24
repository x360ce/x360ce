/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2011 Robert Krawczyk
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

struct FFB_CAPS
{
    BOOL ConstantForce;
    BOOL PeriodicForce;
};

struct DINPUT_FF
{
    BYTE type;
    INT xForce;
    INT yForce;
    INT oldXForce;
    INT oldYForce;
    DWORD oldMagnitude;
    DWORD oldPeriod;
    DWORD leftPeriod;
    DWORD rightPeriod;
    BOOL IsUpdateEffectCreated;
    BOOL useforce;
    DIPERIODIC pf;
    DICONSTANTFORCE cf;
    DIRAMPFORCE rf;
    DIEFFECT eff[2];
    LPDIRECTINPUTEFFECT pEffect[2];
    DWORD dwNumForceFeedbackAxis;
    FLOAT forcepercent;
    FFB_CAPS ffbcaps;
    DINPUT_FF()
    {
        ZeroMemory(this,sizeof(DINPUT_FF));
        forcepercent = 100;
    }
};

struct DINPUT_GAMEPAD
{
    LPDIRECTINPUTDEVICE8 pGamepad;
    DIJOYSTATE2 state;
    DINPUT_FF ff;
    DWORD dwUserIndex;
    BOOL connected;
    BOOL initialized;
    BOOL passthrough;
    BOOL enumfail;
    BOOL axistodpad;
    BOOL useProduct;
    UINT dwAxisCount;
    UINT swapmotor;
    UINT tdeadzone;
    GUID productGUID;
    GUID instanceGUID;
    SHORT adeadzone[4];
    SHORT antidz[4];
    INT axistodpaddeadzone;
    INT axistodpadoffset;
    SHORT axislinear[4];
    BYTE gamepadtype;

    DINPUT_GAMEPAD()
    {
        ZeroMemory(this,sizeof(DINPUT_GAMEPAD));
        gamepadtype = 1;
        passthrough = 1;
    }
};

extern std::vector<DINPUT_GAMEPAD> g_Gamepads;

HRESULT InitDirectInput( HWND hDlg, DINPUT_GAMEPAD &gamepad );
BOOL ButtonPressed(DWORD buttonidx, DINPUT_GAMEPAD &gamepad);
HRESULT UpdateState(DINPUT_GAMEPAD &gamepad);
WORD EnumPadCount();
void FreeDinput();
void Deactivate();
BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

HRESULT SetDeviceForces(DINPUT_GAMEPAD &gamepad, WORD force, WORD effidx);
HRESULT PrepareForce(DINPUT_GAMEPAD &gamepad, WORD effidx);

HRESULT SetDeviceForcesFailsafe(DINPUT_GAMEPAD &gamepad, WORD force, WORD effidx);
HRESULT PrepareForceFailsafe(DINPUT_GAMEPAD &gamepad, WORD effidx);

HRESULT SetDeviceForcesEjocys(DINPUT_GAMEPAD &gamepad, WORD force, WORD effidx);
HRESULT PrepareForceEjocys(DINPUT_GAMEPAD &gamepad, WORD effidx);

HRESULT SetDeviceForcesNew(DINPUT_GAMEPAD &gamepad, WORD force, WORD effidx);
HRESULT PrepareForceNew(DINPUT_GAMEPAD &gamepad, WORD effidx);

#endif
