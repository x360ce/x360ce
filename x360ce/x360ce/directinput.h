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

HRESULT InitDirectInput(HWND, INT,INT);
BOOL ButtonPressed(DWORD, INT);

VOID Initialize(DWORD);
HRESULT Update(HWND, INT);
HRESULT SetDeviceForces(INT idx, WORD force, INT type);
HRESULT PrepareForce(INT idx, INT effidx);

extern LPDIRECTINPUT8 g_pDI;

struct DIGAMEPAD {
	TCHAR ident[9];
	LPDIRECTINPUTDEVICE8 g_pGamepad;
	DWORD dwPadIndex;
	BOOL connected;
	TCHAR name[MAX_PATH];
	WORD vid;
	WORD pid;
	GUID instance;
	DIJOYSTATE2 state;
	LPDIRECTINPUTEFFECT g_pEffect[2];
	DWORD g_dwNumForceFeedbackAxis;
	BOOL native;
	BOOL swapmotor;
	WORD wLMotorDirection;
	WORD wRMotorDirection;
	DWORD tdeadzone;
	BOOL useforce;
	FLOAT forcepercent;
	INT gamepadtype;
	BOOL axistodpad;
	INT axistodpaddeadzone;
	INT axistodpadoffset;
	DIGAMEPAD()
	{
		_tcscpy_s(ident,_T("[x360ce]"));
		dwPadIndex = (DWORD) -1;
		connected = 0;
		for (INT i = 0; i < 2; ++i) g_pEffect[i] = NULL;
		g_dwNumForceFeedbackAxis = NULL;
		native = 0;
		swapmotor = 0;
		wLMotorDirection = 0;
		wRMotorDirection = 1;
		tdeadzone  = 0;
		useforce = 0;
		forcepercent = 100;
		gamepadtype = 1;

	}
};

// externs

extern struct DIGAMEPAD Gamepad[4];
extern VOID Initialize(DWORD idx);
extern HRESULT UpdateState( INT );
extern HRESULT InitDirectInput( HWND hook, INT idx );
extern VOID FreeDirectInput( INT idx );
//extern WORD ConfigPadCount();
extern WORD EnumPadCount();
extern HRESULT Enumerate(DWORD idx);
extern VOID FreeAll( VOID );
