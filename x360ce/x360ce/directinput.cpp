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
 
#include "stdafx.h"
#include "globals.h"
#include "directinput.h"
#include "utils.h"
#include "config.h"

DINPUT_DATA DDATA;
DINPUT_GAMEPAD Gamepad[4];	//but we need a 4 gamepads
LPDIEFFECT lpDIeff;

INT init[4] = {NULL};
INT axiscount=0;

LPDIRECTINPUT8 GetDirectInput() {
	if (!DDATA.g_pDI) {
		if (FAILED(DirectInput8Create( hX360ceInstance, DIRECTINPUT_VERSION,IID_IDirectInput8, ( VOID** )&DDATA.g_pDI, NULL ) ) )
			return 0;
	}
	DDATA.refCount++;
	return DDATA.g_pDI;
}

void ReleaseDirectInput() 
{
	if (DDATA.refCount) 
	{
		DDATA.refCount--;
		if (!DDATA.refCount) 
		{
			DDATA.g_pDI->Release();
			DDATA.g_pDI = 0;
		}
	}
}

void Deactivate(DWORD idx) 
{
	if (Gamepad[idx].g_pEffect) 
	{
		for (int i=0; i<2; i++) 
		{
			if (Gamepad[idx].g_pEffect[i]) 
			{
				Gamepad[idx].g_pEffect[i]->Stop();
				Gamepad[idx].g_pEffect[i]->Release();
			}
			free(Gamepad[idx].g_pEffect[i]);
			Gamepad[idx].g_pEffect[i] = 0;
		}
	}
	if (Gamepad[idx].connected) 
	{
		Gamepad[idx].g_pGamepad->Unacquire();
		Gamepad[idx].g_pGamepad->Release();
		Gamepad[idx].g_pGamepad = 0;
		Gamepad[idx].connected = 0;
	}
}

void Deactivate() 
{
	for (int i=0; i<4; i++) Deactivate(i);
}

BOOL CALLBACK EnumGamepadsCallback( const DIDEVICEINSTANCE* pInst,
								   VOID* pContext )
{
	LPDIRECTINPUT8 lpDI8 = GetDirectInput();
	LPDIRECTINPUTDEVICE8 pDevice;
	DINPUT_GAMEPAD * gp = (DINPUT_GAMEPAD*) pContext;

	if(gp->product == pInst->guidProduct && gp->instance == pInst->guidInstance )
	{
		lpDI8->CreateDevice( pInst->guidInstance, &pDevice, NULL );
		if(pDevice)
		{
			if(bInitBeep) MessageBeep(MB_OK);
			gp->g_pGamepad = pDevice;
			_tcscpy_s(gp->name,pInst->tszProductName);
			gp->connected = 1;
			WriteLog(_T("[PAD%d] Device \"%s\" initialized"),gp->dwPadIndex,gp->name);

		}
		return DIENUM_STOP;
	}
	else
	{
		return DIENUM_CONTINUE;
	}
}

BOOL CALLBACK EnumObjectsCallback( const DIDEVICEOBJECTINSTANCE* pdidoi,VOID* pContext )
{

	DINPUT_GAMEPAD * gp = (DINPUT_GAMEPAD*) pContext;

	// For axes that are returned, set the DIPROP_RANGE property for the
	// enumerated axis in order to scale min/max values.
	if( pdidoi->dwType & DIDFT_AXIS )
	{
		DIPROPRANGE diprg; 
		diprg.diph.dwSize       = sizeof(DIPROPRANGE); 
		diprg.diph.dwHeaderSize = sizeof(DIPROPHEADER); 
		diprg.diph.dwHow        = DIPH_BYID; 
		diprg.diph.dwObj        = pdidoi->dwType; // Specify the enumerated axis
		diprg.lMin              = -32768;
		diprg.lMax              = +32767; 

		// Set the range for the axis
		if( FAILED( gp->g_pGamepad->SetProperty( DIPROP_RANGE, &diprg.diph ) ) ) 
			return DIENUM_STOP;

	}
	axiscount++;
	return DIENUM_CONTINUE;
}

BOOL CALLBACK EnumFFAxesCallback( const DIDEVICEOBJECTINSTANCE* pdidoi,VOID* pContext )
{
	DWORD* pdwNumForceFeedbackAxis = (DWORD*) pContext;

	if( ((pdidoi->dwFlags && DIDOI_FFACTUATOR) != 0) )
		(*pdwNumForceFeedbackAxis)++;

	return DIENUM_CONTINUE;
}

HRESULT UpdateState(INT idx )
{
	HRESULT hr;

	if( (NULL == Gamepad[idx].g_pGamepad) || !(Gamepad[idx].connected))
		return S_FALSE;

	// Poll the device to read the current state
	hr = Gamepad[idx].g_pGamepad->Poll();

	if ((hr != DI_OK && hr != DI_NOEFFECT) || DI_OK != Gamepad[idx].g_pGamepad->GetDeviceState( sizeof( DIJOYSTATE2 ), &Gamepad[idx].state ) )
	{
			Deactivate(idx);
			return S_OK;
	}

	return S_OK;
}

HRESULT Enumerate(DWORD idx)
{
	HRESULT hr;

	Deactivate(idx);
	LPDIRECTINPUT8 lpDI8 = GetDirectInput();

	WriteLog(_T("[PAD%d] Enumerating User ID %d"),idx+1,idx);
	if( FAILED( hr = lpDI8->EnumDevices( DI8DEVCLASS_GAMECTRL,
		EnumGamepadsCallback, &Gamepad[idx],
		DIEDFL_ATTACHEDONLY ) ) )
	{
		WriteLog(_T("[PAD%d] Enumeration FAILED !!!"),idx+1);
		return hr;
	}
	if(Gamepad[idx].g_pGamepad == NULL) WriteLog(_T("[PAD%d] Enumeration FAILED !!!"),idx+1);
	return ERROR_SUCCESS;
}

HRESULT InitDirectInput( HWND hDlg, INT idx )
{

	if(Gamepad[idx].g_pGamepad == NULL) return ERROR_DEVICE_NOT_CONNECTED;

	DIPROPDWORD dipdw;
	HRESULT hr=S_OK;
	HRESULT coophr=S_OK;

	if( FAILED( hr = Gamepad[idx].g_pGamepad->SetDataFormat( &c_dfDIJoystick2 ) ) )
	{
		WriteLog(_T("[PAD%d] SetDataFormat failed with code HR = %s"), idx+1, DXErrStr(hr));
		return hr;
	}

	// Set the cooperative level to let DInput know how this device should
	// interact with the system and with other DInput applications.
	// Exclusive access is required in order to perform force feedback.

	if( FAILED( coophr = Gamepad[idx].g_pGamepad->SetCooperativeLevel( hDlg,
		DISCL_EXCLUSIVE |
		DISCL_BACKGROUND ) ) )
	{
		WriteLog(_T("[PAD%d] SetCooperativeLevel (1) failed with code HR = %s"), idx+1, DXErrStr(coophr));
		//return coophr;
	}
	if(coophr!=S_OK) 
	{
		WriteLog(_T("Device not exclusive acquired, disabling ForceFeedback"));
		Gamepad[idx].useforce = 0;
		if( FAILED( coophr = Gamepad[idx].g_pGamepad->SetCooperativeLevel( hDlg,
			DISCL_NONEXCLUSIVE |
			DISCL_BACKGROUND ) ) )
		{
			WriteLog(_T("[PAD%d] SetCooperativeLevel (2) failed with code HR = %s"), idx+1, DXErrStr(coophr));
			//return coophr;
		}
	}

	// Since we will be playing force feedback effects, we should disable the
	// auto-centering spring.
	dipdw.diph.dwSize = sizeof( DIPROPDWORD );
	dipdw.diph.dwHeaderSize = sizeof( DIPROPHEADER );
	dipdw.diph.dwObj = 0;
	dipdw.diph.dwHow = DIPH_DEVICE;
	dipdw.dwData = FALSE;

	if( FAILED( hr = Gamepad[idx].g_pGamepad->SetProperty( DIPROP_AUTOCENTER, &dipdw.diph ) ) )
	{
		WriteLog(_T("[PAD%d] SetProperty failed with code HR = %s"), idx+1, DXErrStr(hr));
		//return hr;
	}

	if( FAILED( hr = Gamepad[idx].g_pGamepad->EnumObjects( EnumObjectsCallback,
		( VOID* )&Gamepad[idx], DIDFT_AXIS ) ) )
	{
		WriteLog(_T("[PAD%d] EnumObjects failed with code HR = %s"), idx+1, DXErrStr(hr));
		//return hr;
	}
	else
	{
		WriteLog(_T("[PAD%d] Device with %d axes"),idx+1,axiscount);
	}
	axiscount=0;

	if( FAILED( hr = Gamepad[idx].g_pGamepad->EnumObjects( EnumFFAxesCallback,
		( VOID* )&Gamepad[idx].g_dwNumForceFeedbackAxis, DIDFT_AXIS ) ) )
	{
		WriteLog(_T("[PAD%d] EnumFFAxesCallback failed with code HR = %s"), idx+1, DXErrStr(hr));
		//return hr;
	}

	if( Gamepad[idx].g_dwNumForceFeedbackAxis > 2 )
		Gamepad[idx].g_dwNumForceFeedbackAxis = 2;

	if( FAILED( hr = Gamepad[idx].g_pGamepad->Acquire() ) )
	{
		WriteLog(_T("[PAD%d] Acquire failed with code HR = %s"), idx+1, DXErrStr(hr));
		//return hr;
	}

	return coophr;
}

HRESULT SetDeviceForces(DWORD idx, WORD force, WORD effidx)
{
	// Modifying an effect is basically the same as creating a new one, except
	// you need only specify the parameters you are modifying
	HRESULT hr= S_OK;
	LONG     rglDirection[2] = { effidx, 0 };
	DWORD    rgdwAxes[2]     = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis

	LONG magnitude = (LONG)(force/256*256-1);

	DICONSTANTFORCE cf;
	DIEFFECT eff;

	cf.lMagnitude = magnitude;
	ZeroMemory( &eff, sizeof( eff ) );
	eff.dwSize = sizeof( DIEFFECT );
	eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
	eff.dwDuration =INFINITE;
	eff.dwSamplePeriod = 0;
	eff.dwGain = DI_FFNOMINALMAX;
	eff.dwTriggerButton = DIEB_NOTRIGGER;
	eff.dwTriggerRepeatInterval = 0;
	eff.cAxes = Gamepad[idx].g_dwNumForceFeedbackAxis;
	eff.rgdwAxes = rgdwAxes;
	eff.rglDirection = rglDirection;
	eff.lpEnvelope = 0;
	eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
	eff.lpvTypeSpecificParams = &cf;
	eff.dwStartDelay = 0;

	// Now set the new parameters and start the effect immediately.
	hr= Gamepad[idx].g_pEffect[effidx]->SetParameters( &eff, DIEP_DIRECTION |DIEP_TYPESPECIFICPARAMS |DIEP_START |DIES_SOLO );
	return hr;
}

HRESULT PrepareForce(DWORD idx, WORD effidx)
{
	HRESULT hr= S_FALSE;
	if( NULL == Gamepad[idx].g_pEffect[effidx] )
	{

		DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
		LONG rglDirection[2] = { effidx, 0 };

		DICONSTANTFORCE cf;
		DIEFFECT eff;

		ZeroMemory( &eff, sizeof( eff ) );
		eff.dwSize = sizeof( DIEFFECT );
		eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
		eff.dwDuration = INFINITE;
		eff.dwSamplePeriod = 0;
		eff.dwGain = DI_FFNOMINALMAX;
		eff.dwTriggerButton = DIEB_NOTRIGGER;
		eff.dwTriggerRepeatInterval = 0;
		eff.cAxes = Gamepad[idx].g_dwNumForceFeedbackAxis;
		eff.rgdwAxes = rgdwAxes;
		eff.rglDirection = rglDirection;
		eff.lpEnvelope = 0;
		eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
		eff.lpvTypeSpecificParams = &cf;
		eff.dwStartDelay = 0;

		// Create the prepared effect
		if( FAILED( hr = Gamepad[idx].g_pGamepad->CreateEffect( GUID_ConstantForce  ,
			&eff, &Gamepad[idx].g_pEffect[effidx] , NULL ) ) )
		{
			WriteLog(_T("[PAD%d] CreateEffect (1) failed with code HR = %s"), idx+1, DXErrStr(hr));
		}
	}
	return hr;
}

// return buttons state (1 pressed, 0 not pressed)
BOOL ButtonPressed(DWORD buttonidx, INT idx) 
{

	return (Gamepad[idx].state.rgbButtons[buttonidx] & 0x80) != 0;
}