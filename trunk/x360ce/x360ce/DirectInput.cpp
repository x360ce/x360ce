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

#include "stdafx.h"
#include "globals.h"
#include "x360ce.h"
#include "Utilities\Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"

extern HINSTANCE hDinput8;
extern void LoadSystemDInput8DLL();

//-----------------------------------------------------------------------------
// Defines, constants, and global variables
//-----------------------------------------------------------------------------
DINPUT_DATA DDATA;
DINPUT_GAMEPAD g_Gamepad[4];	//but we need a 4 gamepads

INT init[4] = {NULL};
WORD lastforce = 0;

//-----------------------------------------------------------------------------

LPDIRECTINPUT8 GetDirectInput()
{
    if (!DDATA.pDI)
    {
		if(!hDinput8) LoadSystemDInput8DLL();
		typedef HRESULT (WINAPI *tDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
		tDirectInput8Create oDirectInput8Create = (tDirectInput8Create) GetProcAddress( hDinput8, "DirectInput8Create");
        HRESULT hr = oDirectInput8Create( hThis, DIRECTINPUT_VERSION,IID_IDirectInput8, ( VOID** )&DDATA.pDI, NULL );

        if (FAILED(hr))
            return 0;
    }

    DDATA.refCount++;
    return DDATA.pDI;
}

void ReleaseDirectInput()
{
    if (DDATA.refCount)
    {
        DDATA.refCount--;

        if (!DDATA.refCount)
        {
            DDATA.pDI->Release();
            DDATA.pDI = 0;
        }
    }
}

void Deactivate(DWORD idx)
{
    if (*g_Gamepad[idx].ff.pEffect)
    {
        for (int i=0; i<2; i++)
        {
            if (g_Gamepad[idx].ff.pEffect[i])
            {
                g_Gamepad[idx].ff.pEffect[i]->Stop();
                g_Gamepad[idx].ff.pEffect[i]->Release();
            }

            free(g_Gamepad[idx].ff.pEffect[i]);
            g_Gamepad[idx].ff.pEffect[i] = 0;
        }
    }

    if (g_Gamepad[idx].connected)
    {
        g_Gamepad[idx].pGamepad->Unacquire();
        g_Gamepad[idx].pGamepad->Release();
        g_Gamepad[idx].pGamepad = 0;
        g_Gamepad[idx].connected = 0;
    }
}

void Deactivate()
{
    for (DWORD i=0; i<4; i++) Deactivate(i);
}

static bool AcquireDevice(LPDIRECTINPUTDEVICE8 lpDirectInputDevice)
{
	if (FAILED (lpDirectInputDevice->Acquire()))
	{
		HRESULT result = lpDirectInputDevice->Acquire();
		if (result == DIERR_OTHERAPPHASPRIO)
			return FALSE;
		if (FAILED (result))
		{
			Deactivate();
			return FALSE;
		}

	}
	return TRUE;
}

BOOL CALLBACK EnumGamepadsCallback( const DIDEVICEINSTANCE* pInst, VOID* pContext )
{
    LPDIRECTINPUT8 lpDI8 = GetDirectInput();
    LPDIRECTINPUTDEVICE8 pDevice;
    DINPUT_GAMEPAD * gp = (DINPUT_GAMEPAD*) pContext;

    if(IsEqualGUID(gp->productGUID, pInst->guidProduct) && IsEqualGUID(gp->instanceGUID, pInst->guidInstance) )
    {
        lpDI8->CreateDevice( pInst->guidInstance, &pDevice, NULL );

        if(pDevice)
        {
            gp->pGamepad = pDevice;
            gp->connected = 1;
            WriteLog(LOG_DINPUT,L"[PAD%d] Device \"%s\" created",gp->dwPadIndex+1,pInst->tszProductName);
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
        diprg.lMin              = -32767;
        diprg.lMax              = +32767;

        // Set the range for the axis
        if( FAILED( gp->pGamepad->SetProperty( DIPROP_RANGE, &diprg.diph ) ) )
            return DIENUM_STOP;
    }

    gp->dwAxisCount++;
    return DIENUM_CONTINUE;
}

//-----------------------------------------------------------------------------
// Name: EnumFFAxesCallback()
// Desc: Callback function for enumerating the axes on a joystick and counting
//       each force feedback enabled axis
//-----------------------------------------------------------------------------
BOOL CALLBACK EnumFFAxesCallback( const DIDEVICEOBJECTINSTANCE* pdidoi,VOID* pContext )
{
    DWORD* pdwNumForceFeedbackAxis = (DWORD*) pContext;

    if( ((pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0) )
        (*pdwNumForceFeedbackAxis)++;

    return DIENUM_CONTINUE;
}

HRESULT UpdateState(DWORD idx )
{
    HRESULT hr=E_FAIL;

    if( (!g_Gamepad[idx].pGamepad))
        return E_FAIL;

    // Poll the device to read the current state
    // not all devices must be polled so checking result code is useless
    g_Gamepad[idx].pGamepad->Poll();

    //But GetDeviceState must be succesed
    hr = g_Gamepad[idx].pGamepad->GetDeviceState( sizeof( DIJOYSTATE2 ), &g_Gamepad[idx].state );

    if(FAILED(hr))
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] Device Reacquired",idx+1);
        hr = g_Gamepad[idx].pGamepad->Acquire();
    }

    return hr;
}

HRESULT Enumerate(DWORD idx)
{
    HRESULT hr;

    Deactivate(idx);
    LPDIRECTINPUT8 lpDI8 = GetDirectInput();

    WriteLog(LOG_DINPUT,L"[PAD%d] Enumerating UserIndex %d",idx+1,idx);
    hr = lpDI8->EnumDevices( DI8DEVCLASS_GAMECTRL, EnumGamepadsCallback, &g_Gamepad[idx], DIEDFL_ATTACHEDONLY );

    if FAILED(hr)
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] Enumeration FAILED !!!",idx+1);
        return hr;
    }

    if(!g_Gamepad[idx].pGamepad) WriteLog(LOG_DINPUT,L"[PAD%d] Enumeration FAILED !!!",idx+1);

    return ERROR_SUCCESS;
}

HRESULT InitDirectInput( HWND hDlg, DWORD idx )
{

    if(!g_Gamepad[idx].pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

    DIPROPDWORD dipdw;
    HRESULT hr=S_OK;
    HRESULT coophr=S_OK;

    // Set the data format to "simple joystick" - a predefined data format. A
    // data format specifies which controls on a device we are interested in,
    // and how they should be reported.
    //
    // This tells DirectInput that we will be passing a DIJOYSTATE structure to
    // IDirectInputDevice8::GetDeviceState(). Even though we won't actually do
    // it in this sample. But setting the data format is important so that the
    // DIJOFS_* values work properly.
    if( FAILED( hr = g_Gamepad[idx].pGamepad->SetDataFormat( &c_dfDIJoystick2 ) ) )
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] SetDataFormat failed with code HR = %X", idx+1, hr);
        return hr;
    }

    // Set the cooperative level to let DInput know how this device should
    // interact with the system and with other DInput applications.
    // Exclusive access is required in order to perform force feedback.
    if( FAILED( coophr = g_Gamepad[idx].pGamepad->SetCooperativeLevel( hDlg,
                         DISCL_EXCLUSIVE |
                         DISCL_BACKGROUND ) ) )
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] SetCooperativeLevel (1) failed with code HR = %X", idx+1, coophr);
        //return coophr;
    }

    if(coophr!=S_OK)
    {
        WriteLog(LOG_DINPUT,L"[Device not exclusive acquired, disabling ForceFeedback");
        g_Gamepad[idx].ff.useforce = 0;

        if( FAILED( coophr = g_Gamepad[idx].pGamepad->SetCooperativeLevel( hDlg,
                             DISCL_NONEXCLUSIVE |
                             DISCL_BACKGROUND ) ) )
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] SetCooperativeLevel (2) failed with code HR = %X", idx+1, coophr);
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
    // not all gamepad drivers need this (like PS3), so do not check result code
    g_Gamepad[idx].pGamepad->SetProperty( DIPROP_AUTOCENTER, &dipdw.diph );

    if( FAILED( hr = g_Gamepad[idx].pGamepad->EnumObjects( EnumObjectsCallback,
                     ( VOID* )&g_Gamepad[idx], DIDFT_AXIS ) ) )
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] EnumObjects failed with code HR = %X", idx+1, hr);
        //return hr;
    }
    else
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] Detected axis count: %d",idx+1,g_Gamepad[idx].dwAxisCount);
    }

    if( FAILED( hr = g_Gamepad[idx].pGamepad->EnumObjects( EnumFFAxesCallback,
                     ( VOID* )&g_Gamepad[idx].ff.dwNumForceFeedbackAxis, DIDFT_AXIS ) ) )
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] EnumFFAxesCallback failed with code HR = %X", idx+1, hr);
        //return hr;
    }

    if( g_Gamepad[idx].ff.dwNumForceFeedbackAxis > 2 )
        g_Gamepad[idx].ff.dwNumForceFeedbackAxis = 2;

    if( g_Gamepad[idx].ff.dwNumForceFeedbackAxis <= 0 )
        g_Gamepad[idx].ff.useforce = 0;

    return g_Gamepad[idx].pGamepad->Acquire();
}

BOOL ButtonPressed(DWORD buttonidx, INT idx)
{
    return (g_Gamepad[idx].state.rgbButtons[buttonidx] & 0x80) != 0;
}

BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef)
{
    DINPUT_GAMEPAD * gamepad = (DINPUT_GAMEPAD*) pvRef;

    // Pointer to calling device
    gamepad->ff.ffbcaps.ConstantForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_CONSTANTFORCE;
    gamepad->ff.ffbcaps.PeriodicForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_PERIODIC;
    WriteLog(LOG_DINPUT,L"   Effect '%s'. IsConstant = %d, IsPeriodic = %d", di->tszName, gamepad->ff.ffbcaps.ConstantForce, gamepad->ff.ffbcaps.PeriodicForce);
    return DIENUM_CONTINUE;
}

HRESULT SetDeviceForces(DWORD idx, WORD force, WORD motor)
{
    if(!g_Gamepad[idx].ff.pEffect[motor]) return S_FALSE;

	if ( force == 0) {
		if (FAILED (g_Gamepad[idx].ff.pEffect[motor]->Stop())) {
			AcquireDevice (g_Gamepad[idx].pGamepad);
			if (FAILED (g_Gamepad[idx].ff.pEffect[motor]->Stop()))
			{
				ReleaseDirectInput();
				return S_FALSE;
			}
		}
		return S_OK;
	}

    if(g_Gamepad[idx].ff.type == 1) return SetDeviceForcesEjocys(idx,force,motor);

    if(g_Gamepad[idx].ff.type == 2) return SetDeviceForcesNew(idx,force,motor);

    return SetDeviceForcesFailsafe(idx,force,motor);
}

HRESULT PrepareForce(DWORD idx, WORD motor)
{
    if(g_Gamepad[idx].ff.pEffect[motor]) return S_FALSE;


    if(g_Gamepad[idx].ff.type == 1) return PrepareForceEjocys(idx,motor);

    if(g_Gamepad[idx].ff.type == 2) return PrepareForceNew(idx,motor);

    return PrepareForceFailsafe(idx,motor);
}

HRESULT SetDeviceForcesFailsafe(DWORD idx, WORD force, WORD motor)
{
    // Modifying an effect is basically the same as creating a new one, except
    // you need only specify the parameters you are modifying
    HRESULT hr= S_OK;
    LONG     rglDirection[2] = { 1, 0 };
    DWORD    rgdwAxes[2]     = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis

    DICONSTANTFORCE cf;

    LONG magnitude = (LONG)(force/256*256-1);

    if(motor == 0)
    {
        cf.lMagnitude = magnitude;
    }

    if(motor == 1)
    {
        cf.lMagnitude = magnitude;
    }

    DIEFFECT eff;
    ZeroMemory( &eff, sizeof( eff ) );
    eff.dwSize = sizeof( DIEFFECT );
    eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff.cAxes = g_Gamepad[idx].ff.dwNumForceFeedbackAxis;
    eff.rgdwAxes = rgdwAxes;
    eff.rglDirection = rglDirection;
    eff.lpEnvelope = 0;
    eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
    eff.lpvTypeSpecificParams = &cf;
    eff.dwStartDelay = 0;

    // Now set the new parameters and start the effect immediately.
    hr= g_Gamepad[idx].ff.pEffect[motor]->SetParameters( &eff, DIEP_DIRECTION |DIEP_TYPESPECIFICPARAMS |DIEP_START );
    return hr;
}

HRESULT PrepareForceFailsafe(DWORD idx, WORD motor)
{

    HRESULT hr= S_FALSE;

    if(motor == 0)
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
        eff.cAxes = g_Gamepad[idx].ff.dwNumForceFeedbackAxis;
        eff.rgdwAxes = rgdwAxes;
        eff.rglDirection = rglDirection;
        eff.lpEnvelope = 0;
        eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
        eff.lpvTypeSpecificParams = &cf;
        eff.dwStartDelay = 0;

        // Create the prepared effect
        if( FAILED( hr = g_Gamepad[idx].pGamepad->CreateEffect( GUID_ConstantForce  ,
                         &eff, &g_Gamepad[idx].ff.pEffect[motor] , NULL ) ) )
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] CreateEffect (1) failed with code HR = %X", idx+1, hr);
            return hr;
        }

        if( NULL == g_Gamepad[idx].ff.pEffect[motor] )
        {
            WriteLog(LOG_DINPUT,L"g_pEffect is NULL!!!!");
            return E_FAIL;
        }

        return S_OK;
    }
    else if(motor ==1)
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
        eff.cAxes = g_Gamepad[idx].ff.dwNumForceFeedbackAxis;
        eff.rgdwAxes = rgdwAxes;
        eff.rglDirection = rglDirection;
        eff.lpEnvelope = 0;
        eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
        eff.lpvTypeSpecificParams = &cf;
        eff.dwStartDelay = 0;

        // Create the prepared effect
        if( FAILED( hr = g_Gamepad[idx].pGamepad->CreateEffect( GUID_ConstantForce  ,
                         &eff, &g_Gamepad[idx].ff.pEffect[motor] , NULL ) ) )
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] CreateEffect (2) failed with code HR = %X", idx+1, hr);
            return hr;
        }

        if( NULL == g_Gamepad[idx].ff.pEffect[motor] )
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] g_pEffect is NULL!!!!",idx+1);
            return E_FAIL;
        }

        return S_OK;
    }

    return E_FAIL;
}

HRESULT SetDeviceForcesEjocys(DWORD idx, WORD force, WORD effidx)
{
    //// Modifying an effect is basically the same as creating a new one, except
    //// you need only specify the parameters you are modifying
    //HRESULT hr= S_OK;
    //LONG     rglDirection[2] = { 0, 0 };
    //DOUBLE        correction = 6.5535;
    //if( Gamepad[idx].g_dwNumForceFeedbackAxis == 1 )
    //{
    //      rglDirection[0] = 0;
    //}
    //else
    //{
    //      rglDirection[0] = force;
    //}
    //LONG magnitude = (LONG)((DOUBLE)force / (DOUBLE)correction);
    //DICONSTANTFORCE cf;
    //DIEFFECT eff;
    //cf.lMagnitude = magnitude;
    //ZeroMemory( &eff, sizeof( eff ) );
    //eff.dwSize = sizeof( DIEFFECT );
    //eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    //eff.dwDuration =INFINITE;
    //eff.dwSamplePeriod = 0;
    //eff.dwGain = DI_FFNOMINALMAX;
    //eff.dwTriggerButton = DIEB_NOTRIGGER;
    //eff.dwTriggerRepeatInterval = 0;
    //eff.cAxes = Gamepad[idx].g_dwNumForceFeedbackAxis;
    //eff.rglDirection = rglDirection;
    //eff.lpEnvelope = 0;
    //eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
    //eff.lpvTypeSpecificParams = &cf;
    //eff.dwStartDelay = 0;
    //// Now set the new parameters and start the effect immediately.
    //hr= Gamepad[idx].g_pEffect[effidx]->SetParameters( &eff, DIEP_DIRECTION |DIEP_TYPESPECIFICPARAMS |DIEP_START |DIES_SOLO );
    //return hr;
    //return S_OK;

    WriteLog(LOG_DINPUT,L"[PAD%d] SetDeviceForces (%d) %d", idx+1,effidx, force);
    //[-10000:10000]
    //INT nForce = MulDiv(force, 2 * DI_FFNOMINALMAX, 65535) - DI_FFNOMINALMAX;
    //[0:10000]
    INT nForce = MulDiv(force, DI_FFNOMINALMAX, 65535);
    DWORD period;

    // Keep force within bounds
    if( nForce < -DI_FFNOMINALMAX ) nForce = -DI_FFNOMINALMAX;

    if( nForce > +DI_FFNOMINALMAX ) nForce = +DI_FFNOMINALMAX;

    if (effidx == 0)
    {
        g_Gamepad[idx].ff.xForce = nForce;
        period = 60000;
    }
    else
    {
        g_Gamepad[idx].ff.yForce = nForce;
        period = 120000;
    }

    DWORD magnitude = 0;
    // Constant:  Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    HRESULT hr = S_OK;
    LONG rglDirection[2] = { 0, 0 };
    //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !1! HR = %s"), idx+1,effidx, DXErrStr(hr));
    DIEFFECT eff;

    if ( g_Gamepad[idx].ff.IsUpdateEffectCreated == false)
    {
        //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !1a! HR = %s"), idx+1,effidx, DXErrStr(hr));
        ZeroMemory( &eff, sizeof( eff ) );
        eff.dwSize = sizeof( DIEFFECT );
        eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
        eff.cAxes =  g_Gamepad[idx].ff.dwNumForceFeedbackAxis;
        eff.lpEnvelope = 0;
        eff.dwStartDelay = 0;
        //eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
        eff.cbTypeSpecificParams = sizeof( DIPERIODIC );
        g_Gamepad[idx].ff.eff[0] = eff;
        g_Gamepad[idx].ff.IsUpdateEffectCreated = true;
    }

    eff =  g_Gamepad[idx].ff.eff[0];

    //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !2! HR = %s"), idx+1,effidx, DXErrStr(hr));
    // When modifying an effect you need only specify the parameters you are modifying
    if(  g_Gamepad[idx].ff.dwNumForceFeedbackAxis == 1 )
    {
        //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3a! HR = %s"), idx+1,effidx, DXErrStr(hr));
        // Apply only one direction and keep the direction at zero
        magnitude = ( DWORD )sqrt( ( double )g_Gamepad[idx].ff.xForce * ( double )g_Gamepad[idx].ff.xForce + ( double )g_Gamepad[idx].ff.yForce * ( double )g_Gamepad[idx].ff.yForce );
        rglDirection[0] = 0;
        g_Gamepad[idx].ff.pf.dwMagnitude = g_Gamepad[idx].ff.xForce;
        g_Gamepad[idx].ff.pf.dwPeriod = period;
    }
    else
    {
        //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! HR = %s"), idx+1,effidx, DXErrStr(hr));
        magnitude = MulDiv(force, DI_FFNOMINALMAX, 65535);
        // Apply magnitude from both directions
        rglDirection[0] = g_Gamepad[idx].ff.xForce;
        rglDirection[1] = g_Gamepad[idx].ff.yForce;
        g_Gamepad[idx].ff.pf.dwMagnitude = magnitude;
        g_Gamepad[idx].ff.pf.dwPeriod = period;
        //LeftForceMagnitude
        //LeftForcePeriod
        // dwMagnitude - Magnitude of the effect, in the range from 0 through 10,000. If an envelope is applied to this effect, the value represents the magnitude of the sustain. If no envelope is applied, the value represents the amplitude of the entire effect.
        // lOffset - Offset of the effect. The range of forces generated by the effect is lOffset minus dwMagnitude to lOffset plus dwMagnitude. The value of the lOffset member is also the baseline for any envelope that is applied to the effect.
        // dwPhase - Position in the cycle of the periodic effect at which playback begins, in the range from 0 through 35,999. See Remarks.
        // dwPeriod - Period of the effect, in microseconds.
    }

    //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! axis = %d, x = %d, y = %d, m = %d"), idx+1,effidx, Gamepad[idx].g_dwNumForceFeedbackAxis, Gamepad[idx].xForce, Gamepad[idx].yForce, magnitude);
    //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !6! HR = %s"), idx+1,effidx, DXErrStr(hr));
    g_Gamepad[idx].ff.eff[0].rglDirection = rglDirection;
    g_Gamepad[idx].ff.eff[0].lpvTypeSpecificParams = &g_Gamepad[idx].ff.pf;

    if ( g_Gamepad[idx].ff.oldMagnitude != g_Gamepad[idx].ff.pf.dwMagnitude ||
            g_Gamepad[idx].ff.oldPeriod !=  g_Gamepad[idx].ff.pf.dwPeriod ||
            g_Gamepad[idx].ff.oldXForce != g_Gamepad[idx].ff.xForce ||
            g_Gamepad[idx].ff.oldYForce !=  g_Gamepad[idx].ff.yForce)
    {
        g_Gamepad[idx].ff.oldMagnitude =  g_Gamepad[idx].ff.pf.dwMagnitude;
        g_Gamepad[idx].ff.oldPeriod =  g_Gamepad[idx].ff.pf.dwPeriod;
        g_Gamepad[idx].ff.oldXForce = g_Gamepad[idx].ff.xForce;
        g_Gamepad[idx].ff.oldYForce = g_Gamepad[idx].ff.yForce;

        //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !7! HR = %s"), idx+1,effidx, DXErrStr(hr));
        // Set the new parameters and start the effect immediately.
        if( FAILED( hr = g_Gamepad[idx].ff.pEffect[effidx]->SetParameters( &g_Gamepad[idx].ff.eff[0], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START )))
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] SetDeviceForces (%d) failed with code HR = %X", idx+1,effidx, hr);
            return hr;
        };
    }

    //WriteLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) return HR = %s"), idx+1,effidx, DXErrStr(hr));
    return hr;
}

//-----------------------------------------------------------------------------
// Name: PrepareDeviceForces()
// Desc: Prepare force feedback effect.
//-----------------------------------------------------------------------------
HRESULT PrepareForceEjocys(DWORD idx, WORD effidx)
{
    //HRESULT hr= E_FAIL;
    //if( NULL == Gamepad[idx].g_pEffect[effidx] )
    //{
    //      DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
    //      LONG rglDirection[2] = { 0, 0 };
    //      DICONSTANTFORCE cf = { 0 };
    //      DIEFFECT eff;
    //      ZeroMemory( &eff, sizeof( eff ) );
    //      eff.dwSize = sizeof( DIEFFECT );
    //      eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    //      eff.dwDuration = INFINITE;
    //      eff.dwSamplePeriod = 0;
    //      eff.dwGain = DI_FFNOMINALMAX;
    //      eff.dwTriggerButton = DIEB_NOTRIGGER;
    //      eff.dwTriggerRepeatInterval = 0;
    //      eff.cAxes = Gamepad[idx].g_dwNumForceFeedbackAxis;
    //      eff.rgdwAxes = rgdwAxes;
    //      eff.rglDirection = rglDirection;
    //      eff.lpEnvelope = 0;
    //      eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
    //      eff.lpvTypeSpecificParams = &cf;
    //      eff.dwStartDelay = 0;
    //      // Create the prepared effect
    //      if( FAILED( hr = Gamepad[idx].g_pGamepad->CreateEffect( GUID_ConstantForce  ,
    //              &eff, &Gamepad[idx].g_pEffect[effidx] , NULL ) ) )
    //      {
    //              WriteLog(_T("[DINPUT]  [PAD%d] CreateEffect (%d) failed with code HR = %s"), idx+1,effidx, DXErrStr(hr));
    //              return hr;
    //      }
    //}
    //return S_OK;

    DIEFFECT eff;
    DIPERIODIC pf = { 0 };
    g_Gamepad[idx].ff.pf = pf;
    g_Gamepad[idx].ff.xForce = 0;
    g_Gamepad[idx].ff.yForce = 0;
    g_Gamepad[idx].ff.oldXForce = 0;
    g_Gamepad[idx].ff.oldYForce = 0;
    g_Gamepad[idx].ff.oldMagnitude = 0;
    g_Gamepad[idx].ff.oldPeriod = 0;
    g_Gamepad[idx].ff.IsUpdateEffectCreated = false;
    // Constant:  Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    HRESULT hr = E_FAIL;
    LONG rglDirection[2] = { 0, 0 };
    // Create effect
    ZeroMemory( &eff, sizeof( eff ) );
    eff.dwSize = sizeof( DIEFFECT );
    eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff.cAxes = g_Gamepad[idx].ff.dwNumForceFeedbackAxis;
    eff.lpEnvelope = 0;
    eff.dwStartDelay = 0;
    eff.cbTypeSpecificParams = sizeof( DIPERIODIC );
    GUID effGuid = GUID_Sine;
    // Force feedback
    DIDEVCAPS didcaps;
    didcaps.dwSize = sizeof didcaps;

    if (SUCCEEDED(g_Gamepad[idx].pGamepad->GetCapabilities(&didcaps)) && (didcaps.dwFlags & DIDC_FORCEFEEDBACK))
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] PrepareForce (%d) Force Feedback is available", idx+1,effidx);
    }
    else
    {
        WriteLog(LOG_DINPUT,L"[PAD%d] PrepareForce (%d) Force Feedback is NOT available", idx+1,effidx);
    }

    // Enumerate effects
    if (SUCCEEDED(hr = g_Gamepad[idx].pGamepad->EnumEffects(&EnumEffectsCallback, g_Gamepad[idx].pGamepad, DIEFT_ALL)))
    {
    }
    else
    {
    }

    // This application needs only one effect: Applying raw forces.
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    eff.dwDuration = INFINITE;
    eff.dwSamplePeriod = 0;
    eff.dwGain = DI_FFNOMINALMAX; // no scaling
    eff.dwTriggerButton = DIEB_NOTRIGGER;
    eff.dwTriggerRepeatInterval = 0;
    eff.rgdwAxes = rgdwAxes;
    eff.rglDirection = rglDirection;
    //eff.lpvTypeSpecificParams = &cf;
    eff.lpvTypeSpecificParams = &g_Gamepad[idx].ff.pf;

    // Create the prepared effect
    if( FAILED( hr = g_Gamepad[idx].pGamepad->CreateEffect(
                         effGuid,  // GUID from enumeration
                         &eff, // where the data is
                         &g_Gamepad[idx].ff.pEffect[effidx],  // where to put interface pointer
                         NULL)))
    {
        WriteLog(LOG_DINPUT,L"[DINPUT]  [PAD%d] PrepareForce (%d) failed with code HR = %X", idx+1,effidx, hr);
        return hr;
    }

    if(g_Gamepad[idx].ff.pEffect[effidx] == NULL) return E_FAIL;

    //WriteLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,effidx, DXErrStr(hr));
    return S_OK;
}

BOOL EffectIsPlaying(DWORD idx)
{
    DWORD state;
    g_Gamepad[idx].pGamepad->GetForceFeedbackState(&state);

    if(state & DIGFFS_STOPPED) return FALSE;

    return TRUE;
}

HRESULT SetDeviceForcesNew(DWORD idx, WORD force, WORD motor)
{
    HRESULT hr;

    if(!force) return g_Gamepad[idx].ff.pEffect[motor]->Stop();

    if(EffectIsPlaying(idx)) g_Gamepad[idx].ff.pEffect[motor]->Stop();

    LONG nForce = MulDiv(force, DI_FFNOMINALMAX, 65535);
    nForce = clamp(nForce,-DI_FFNOMINALMAX,DI_FFNOMINALMAX);

    if(motor == FFB_LEFTMOTOR)
    {
        g_Gamepad[idx].ff.eff[motor].dwDuration = g_Gamepad[idx].ff.leftPeriod*1000;
        g_Gamepad[idx].ff.pf.dwMagnitude = (DWORD) nForce;
        g_Gamepad[idx].ff.pf.dwPeriod = g_Gamepad[idx].ff.leftPeriod*1000;
    }

    if(motor == FFB_RIGHTMOTOR)
    {
        g_Gamepad[idx].ff.eff[motor].dwDuration = g_Gamepad[idx].ff.rightPeriod*1000;
        g_Gamepad[idx].ff.pf.dwMagnitude = (DWORD) nForce;
        g_Gamepad[idx].ff.pf.dwPeriod = g_Gamepad[idx].ff.rightPeriod*1000;
    }

    g_Gamepad[idx].ff.eff[motor].lpvTypeSpecificParams = &g_Gamepad[idx].ff.pf;

    hr = g_Gamepad[idx].ff.pEffect[motor]->SetParameters( &g_Gamepad[idx].ff.eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_DURATION | DIEP_SAMPLEPERIOD);

    if(FAILED(hr)) return hr;

    hr = g_Gamepad[idx].ff.pEffect[motor]->Start(INFINITE,DIES_SOLO);

    if(FAILED(hr)) return hr;

    return S_OK;
}

HRESULT PrepareForceNew(DWORD idx, WORD motor)
{
    if(!g_Gamepad[idx].ff.pEffect[motor])
    {
        HRESULT hr = E_FAIL;
        LONG rglDirection[2] = { 0, 0 };

        // Create effect
        ZeroMemory(&g_Gamepad[idx].ff.pf,sizeof(g_Gamepad[idx].ff.pf));
        ZeroMemory(&g_Gamepad[idx].ff.eff[motor], sizeof( g_Gamepad[idx].ff.eff[motor] ) );

        g_Gamepad[idx].ff.eff[motor].dwSize = sizeof( DIEFFECT );
        g_Gamepad[idx].ff.eff[motor].dwFlags = DIEFF_POLAR | DIEFF_OBJECTOFFSETS;
        g_Gamepad[idx].ff.eff[motor].cAxes = g_Gamepad[idx].ff.dwNumForceFeedbackAxis;
        g_Gamepad[idx].ff.eff[motor].lpEnvelope = 0;
        g_Gamepad[idx].ff.eff[motor].dwStartDelay = 0;
        g_Gamepad[idx].ff.eff[motor].cbTypeSpecificParams = sizeof( DIPERIODIC );

        // Force feedback
        DIDEVCAPS didcaps;
        didcaps.dwSize = sizeof didcaps;

        if (SUCCEEDED(g_Gamepad[idx].pGamepad->GetCapabilities(&didcaps)) && (didcaps.dwFlags & DIDC_FORCEFEEDBACK))
        {
            WriteLog(LOG_DINPUT,L"[[PAD%d] PrepareForce (%d) Force Feedback is available", idx+1,motor);
        }
        else
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] PrepareForce (%d) Force Feedback is NOT available", idx+1,motor);
        }

        // Enumerate effects
        if (SUCCEEDED(hr = g_Gamepad[idx].pGamepad->EnumEffects(&EnumEffectsCallback, &g_Gamepad[idx], DIEFT_ALL)))
        {
        }

        DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
        g_Gamepad[idx].ff.eff[motor].dwDuration = INFINITE;
        g_Gamepad[idx].ff.eff[motor].dwSamplePeriod = 0;
        g_Gamepad[idx].ff.eff[motor].dwGain = DI_FFNOMINALMAX;
        g_Gamepad[idx].ff.eff[motor].dwTriggerButton = DIEB_NOTRIGGER;
        g_Gamepad[idx].ff.eff[motor].dwTriggerRepeatInterval = 0;
        g_Gamepad[idx].ff.eff[motor].rgdwAxes = rgdwAxes;
        g_Gamepad[idx].ff.eff[motor].rglDirection = rglDirection;
        g_Gamepad[idx].ff.eff[motor].lpvTypeSpecificParams = &g_Gamepad[idx].ff.rf;

        GUID geff;

        if ( motor == FFB_LEFTMOTOR ) geff = GUID_SawtoothDown;

        if ( motor == FFB_RIGHTMOTOR ) geff = GUID_SawtoothUp;

        // Create the prepared effect
        hr = g_Gamepad[idx].pGamepad->CreateEffect(geff, &g_Gamepad[idx].ff.eff[motor], &g_Gamepad[idx].ff.pEffect[motor], NULL);

        if(FAILED(hr))
        {
            WriteLog(LOG_DINPUT,L"[PAD%d] PrepareForce (%d) failed with code HR = %X", idx+1,motor, hr);
            return hr;
        }

        if(!g_Gamepad[idx].ff.pEffect[motor]) return E_FAIL;

        //WriteLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,effidx, DXErrStr(hr));


    }

    return S_OK;
}


