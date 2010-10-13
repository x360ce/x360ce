/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 ToCA Edit
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
 
#include <dinput.h>

struct DINPUT_DATA 
{
	LPDIRECTINPUT8 g_pDI;
	UINT refCount;
	UINT deviceCount;
	DINPUT_DATA()
	{
		ZeroMemory(this,sizeof(DINPUT_DATA));
	}
};
struct FFB_CAPS
{
	BOOL ConstantForce;
	BOOL PeriodicForce;
};

struct DINPUT_FF
{
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
	LPDIRECTINPUTEFFECT g_pEffect[2];
	DWORD g_dwNumForceFeedbackAxis;
	DOUBLE forcepercent;
	FFB_CAPS ffbcaps;
	DINPUT_FF()
	{
		ZeroMemory(this,sizeof(DINPUT_FF));
		forcepercent = 100;
	}
};

struct DINPUT_GAMEPAD {
	LPDIRECTINPUTDEVICE8 g_pGamepad;
	DWORD dwPadIndex;
	BOOL connected;
	BOOL configured;
	BOOL native;
	BOOL axistodpad;
	UINT dwAxisCount;
	UINT swapmotor;
	UINT tdeadzone;
	WORD vid;
	WORD pid;
	GUID productGUID;
	GUID instanceGUID;
	DIJOYSTATE2 state;
	SHORT adeadzone[4];
	SHORT antidz[4];
	INT axistodpaddeadzone;
	INT axistodpadoffset;
	SHORT axislinear[4];
	BYTE gamepadtype;
	DINPUT_FF ff;
	DINPUT_GAMEPAD()
	{
		ZeroMemory(this,sizeof(DINPUT_GAMEPAD));
		gamepadtype = 1;
	}
};

extern struct DINPUT_GAMEPAD Gamepad[4];

HRESULT InitDirectInput( HWND hDlg, DWORD idx );
BOOL ButtonPressed(DWORD, INT);
HRESULT UpdateState( DWORD );
WORD EnumPadCount();
HRESULT Enumerate(DWORD idx);
void ReleaseDirectInput();
void Deactivate();
BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

HRESULT SetDeviceForces(DWORD idx, WORD force, WORD effidx);
HRESULT PrepareForce(DWORD idx, WORD effidx);

HRESULT SetDeviceForcesFailsafe(DWORD idx, WORD force, WORD effidx);
HRESULT PrepareForceFailsafe(DWORD idx, WORD effidx);

HRESULT SetDeviceForcesEjocys(DWORD idx, WORD force, WORD effidx);
HRESULT PrepareForceEjocys(DWORD idx, WORD effidx);

HRESULT SetDeviceForcesNew(DWORD idx, WORD force, WORD effidx);
HRESULT PrepareForceNew(DWORD idx, WORD effidx);



