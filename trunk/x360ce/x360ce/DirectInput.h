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

#define SAFE_DELETE(p)  { if(p) { delete (p);     (p)=NULL; } }
#define SAFE_RELEASE(p) { if(p) { (p)->Release(); (p)=NULL; } }

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

struct DINPUT_FF
{
	DIPERIODIC pf;
	INT xForce;
	INT yForce;
	INT oldXForce;
	INT oldYForce;
	DWORD oldMagnitude;
	DWORD oldPeriod;
	DIEFFECT eff;
	BOOL IsUpdateEffectCreated;
	DINPUT_FF()
	{
		ZeroMemory(this,sizeof(DINPUT_FF));
	}
};

struct DINPUT_GAMEPAD {
	DWORD id1;
	LPDIRECTINPUTDEVICE8 g_pGamepad;
	DWORD dwPadIndex;  //starting from 1
	BOOL connected;
	BOOL configured;
	TCHAR name[MAX_PATHW];
	WORD vid;
	WORD pid;
	GUID product;
	GUID instance;
	DIJOYSTATE2 state;
	LPDIRECTINPUTEFFECT g_pEffect[2];
	BOOL forceready;
	DWORD g_dwNumForceFeedbackAxis;
	BOOL native;
	BOOL swapmotor;
	WORD wLMotorDirection;
	WORD wRMotorDirection;
	DWORD tdeadzone;
	BOOL useforce;
	FLOAT forcepercent;
	BYTE gamepadtype;
	BOOL axistodpad;
	INT axistodpaddeadzone;
	INT axistodpadoffset;
	DWORD id2;
	DINPUT_FF ff;
};

//externs
extern struct DINPUT_GAMEPAD Gamepad[4];

// prototypes
HRESULT InitDirectInput(HWND, INT,INT);
BOOL ButtonPressed(DWORD, INT);
HRESULT UpdateState( INT );
HRESULT InitDirectInput( HWND hook, INT idx );
WORD EnumPadCount();
HRESULT Enumerate(DWORD idx);
void ReleaseDirectInput();
void Deactivate();
HRESULT SetDeviceForces(DWORD idx, WORD force, WORD effidx);
HRESULT PrepareForce(DWORD idx, WORD effidx);
BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);