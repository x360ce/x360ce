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

LPDIRECTINPUT8	g_pDI = NULL ;   //only one DirectInput interface is needed
DIGAMEPAD Gamepad[4];	//but we need a 4 gamepads

INT init[4] = {NULL};
INT axiscount=0;

#if 0
WORD ConfigPadCount()
{
	WORD ret=0;
	for(WORD pad=0;pad<4;pad++)
	{
		if(Gamepad[pad].id > -1) ret++;
	}
	return ret;	//returns configured pad count
}
#endif

WORD EnumPadCount()
{
	WORD ret=0;
	for(WORD pad=0;pad<4;pad++)
	{
		if(Gamepad[pad].g_pGamepad != NULL) ret++;
	}
	return ret;	//returns enumerated (created) pad count
}

VOID FreeAll(VOID)
{

	for(WORD pad=0;pad<4;pad++)
	{
		FreeDirectInput(pad);
	}

}

VOID FreeDirectInput(INT idx)
{
	// Unacquire the device one last time just in case 
	// the app tried to exit while the device is still acquired.

	if( Gamepad[idx].g_pGamepad )
		Gamepad[idx].g_pGamepad->Unacquire();

	// Release any DirectInput objects.
	for(INT i=0;i<2;i++)
		SAFE_RELEASE( Gamepad[idx].g_pEffect[i] );

	SAFE_RELEASE( Gamepad[idx].g_pGamepad );
	SAFE_RELEASE( g_pDI );

	//Added by Racer_S 9/20/2009
	Gamepad[idx].g_pGamepad = NULL;
	g_pDI = NULL;

	delete[] &Gamepad[idx].name;
}

BOOL CALLBACK EnumGamepadsCallback( const DIDEVICEINSTANCE* pInst,
								   VOID* pContext )
{
	LPDIRECTINPUTDEVICE8 pDevice;
	DIGAMEPAD * gp = (DIGAMEPAD*) pContext;

	DWORD dwgpPIDVID = MAKELONG(gp->vid,gp->pid);

	if(dwgpPIDVID == pInst->guidProduct.Data1 && gp->instance == pInst->guidInstance )
	{
		g_pDI->CreateDevice( pInst->guidInstance, &pDevice, NULL );
		if(pDevice)
		{
			gp->g_pGamepad = pDevice;
			//gp->guid = pInst->guidProduct; 
			_tcscpy_s(gp->name,pInst->tszProductName);
			gp->connected = 1;
			WriteLog(_T("[PAD%d] Device \"%s\" initialized"),gp->dwPadIndex+1,gp->name);

			//LPOLESTR bbb;
			//StringFromIID(gp->guid,&bbb);
			//WriteLog(_T("GUID %s"),bbb);
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

	DIGAMEPAD * gp = (DIGAMEPAD*) pContext;

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
	if( FAILED( hr ) )
	{
		WriteLog(_T("Failed to poll pad %d, HR = %s"), idx, DXErrStr(hr));

		hr = Gamepad[idx].g_pGamepad->Acquire();
		WriteLog(_T("Trying to reacquire pad %d, HR = %s"), idx, DXErrStr(hr));
		while( (hr == DIERR_INPUTLOST) || (hr == DIERR_NOTACQUIRED) ) {
			hr = Gamepad[idx].g_pGamepad->Acquire();
		}

		// hr may be DIERR_OTHERAPPHASPRIO or other errors.  This
		// may occur when the app is minimized or in the process of 
		// switching, so just try again later 
		return S_OK;
	}

	// Get the input's device state
	if( FAILED( hr = Gamepad[idx].g_pGamepad->GetDeviceState( sizeof( DIJOYSTATE2 ), &Gamepad[idx].state ) ) )
		return hr; // The device should have been acquired during the Poll()

	return S_OK;
}

HRESULT Enumerate(DWORD idx)
{
	HRESULT hr;

	if( FAILED( hr = DirectInput8Create( hX360ceInstance, DIRECTINPUT_VERSION,		
		IID_IDirectInput8, ( VOID** )&g_pDI, NULL ) ) )
	{
		return hr;
	}

	WriteLog(_T("[PAD%d] Enumerating User ID %d"),idx+1,idx);
	if( FAILED( hr = g_pDI->EnumDevices( DI8DEVCLASS_GAMECTRL,
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

HRESULT SetDeviceForces(INT idx, WORD force, INT type)
{
	// Modifying an effect is basically the same as creating a new one, except
	// you need only specify the parameters you are modifying
	HRESULT hr= S_OK;
	LONG     rglDirection[2] = { 0, 0 };
	DWORD    rgdwAxes[2]     = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis

	DICONSTANTFORCE cf;

	LONG magnitude = (LONG)(force/256*256-1);

	if(type == 0)
	{
		rglDirection[0] =  1;
		cf.lMagnitude = magnitude;
	}
	if(type == 1)
	{
		rglDirection[1] =  1;
		cf.lMagnitude = magnitude;
	}

	DIEFFECT eff;
	ZeroMemory( &eff, sizeof( eff ) );
	eff.dwSize = sizeof( DIEFFECT );
	eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
	eff.cAxes = Gamepad[idx].g_dwNumForceFeedbackAxis;
	eff.rgdwAxes = rgdwAxes;
	eff.rglDirection = rglDirection;
	eff.lpEnvelope = 0;
	eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
	eff.lpvTypeSpecificParams = &cf;
	eff.dwStartDelay = 0;

	// Now set the new parameters and start the effect immediately.
	hr= Gamepad[idx].g_pEffect[type]->SetParameters( &eff, DIEP_DIRECTION |DIEP_TYPESPECIFICPARAMS |DIEP_START );
	return hr;
}

HRESULT PrepareForce(INT idx, INT effidx)
{
	HRESULT hr= S_FALSE;

	if(effidx == 0)
	{
		// wLeftMotorSpeed

		DWORD    rgdwAxes[2]     = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
		LONG rglDirection[2] = { 1, 0 };

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
			return hr;
		}
		if( NULL == Gamepad[idx].g_pEffect[effidx] )
		{
			WriteLog(_T("g_pEffect is NULL!!!!"));
			return E_FAIL;
		}
		return S_OK;
	}
	else if(effidx ==1)
	{
		DWORD    rgdwAxes[2]     = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
		LONG rglDirection[2] = { 1, 0 };

		DICONSTANTFORCE cf;
		DIEFFECT eff;

		// wRightMotorSpeed
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
			WriteLog(_T("[PAD%d] CreateEffect (2) failed with code HR = %s"), idx+1, DXErrStr(hr));
			return hr;
		}
		if( NULL == Gamepad[idx].g_pEffect[effidx] )
			{
				WriteLog(_T("[PAD%d] g_pEffect is NULL!!!!"),idx+1);
				return E_FAIL;
			}
		return S_OK;
	}
	return S_OK;
}

// return buttons state (1 pressed, 0 not pressed)
BOOL ButtonPressed(DWORD buttonidx, INT idx) {

	return (Gamepad[idx].state.rgbButtons[buttonidx] & 0x80) != 0;
}