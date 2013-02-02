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

#include "stdafx.h"
#include "globals.h"
#include "x360ce.h"
#include "Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"
#include "Utilities\CriticalSection.h"

std::vector<DInputDevice> g_Devices;

INT init[4] = {NULL};
WORD lastforce = 0;

extern iHook* pHooks;

BOOL CALLBACK EnumObjectsCallback( const DIDEVICEOBJECTINSTANCE* pdidoi,VOID* pContext )
{
    DInputDevice *gp = (DInputDevice*) pContext;

    if( pdidoi->dwType & DIDFT_AXIS )
    {
        DIPROPRANGE diprg;
        diprg.diph.dwSize       = sizeof(DIPROPRANGE);
        diprg.diph.dwHeaderSize = sizeof(DIPROPHEADER);
        diprg.diph.dwHow        = DIPH_BYID;
        diprg.diph.dwObj        = pdidoi->dwType;
        diprg.lMin              = -32767;
        diprg.lMax              = +32767;

        if( FAILED( gp->device->SetProperty( DIPROP_RANGE, &diprg.diph ) ) )
            return DIENUM_STOP;
    }

    gp->axiscount++;
    return DIENUM_CONTINUE;
}

BOOL CALLBACK EnumFFAxesCallback( const DIDEVICEOBJECTINSTANCE* pdidoi,VOID* pContext )
{
    DWORD* pdwNumForceFeedbackAxis = (DWORD*) pContext;

    if( ((pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0) )
        (*pdwNumForceFeedbackAxis)++;

    return DIENUM_CONTINUE;
}

HRESULT UpdateState(DInputDevice& device)
{
    HRESULT hr=E_FAIL;

    if( (!device.device))
        return E_FAIL;

    device.device->Poll();
    hr = device.device->GetDeviceState( sizeof( DIJOYSTATE2 ),&device.state );

    if(FAILED(hr))
    {
        PrintLog(LOG_DINPUT,"[PAD%d] Device Reacquired",device.dwUserIndex+1);
        hr = device.device->Acquire();
    }

    return hr;
}

HRESULT InitDirectInput( HWND hDlg, DInputDevice& device )
{
    DIPROPDWORD dipdw;
    HRESULT hr=E_FAIL;
    HRESULT coophr=E_FAIL;

    static DInputManager dinput;

    if(FAILED(dinput.Init(hThis)))
    {
        PrintLog(LOG_CORE,"DirectInput cannot be initialized");
        MessageBox(NULL,L"DirectInput cannot be initialized",L"Error",MB_ICONERROR);
        ExitProcess(hr);
    }

    static CriticalSection mutex;
    mutex.Lock();

    PrintLog(LOG_DINPUT,"[PAD%d] Creating device",device.dwUserIndex+1);
    bool bHookDI = pHooks->CheckHook(iHook::HOOK_DI);
    bool bHookSA = pHooks->CheckHook(iHook::HOOK_SA);

    if(bHookDI) pHooks->DisableHook(iHook::HOOK_DI);
    if(bHookSA) pHooks->DisableHook(iHook::HOOK_SA);

    if(!device.useproduct)
    {
        hr = dinput.Get()->CreateDevice( device.instanceid,& device.device, NULL );
        if(FAILED(hr))
        {
            PrintLog(LOG_CORE,"%s","InstanceGuid is incorrect trying ProductGuid");
            hr = dinput.Get()->CreateDevice( device.productid,& device.device, NULL );
        }
    }
    else hr = dinput.Get()->CreateDevice( device.productid,& device.device, NULL );

    if(bHookSA) pHooks->EnableHook(iHook::HOOK_SA);
    if(bHookDI) pHooks->EnableHook(iHook::HOOK_DI);

    if(FAILED(hr))
    {
        PrintLog(LOG_CORE,"x360ce is misconfigured or device is disconnected");
        MessageBox(NULL,L"x360ce is misconfigured or device is disconnected",L"Error",MB_ICONERROR);
        ExitProcess(hr);
    }

    if(!device.device)
    {
        device.connected = 0;
        device.fail = 1;
        return ERROR_DEVICE_NOT_CONNECTED;
    }
    else
    {
        device.connected = 1;
        PrintLog(LOG_DINPUT,"[PAD%d] Device created",device.dwUserIndex+1);
    }

    if( FAILED( hr = device.device->SetDataFormat( &c_dfDIJoystick2 ) ) )
        PrintLog(LOG_DINPUT,"[PAD%d] SetDataFormat failed with code HR = %X", device.dwUserIndex+1, hr);


    if( FAILED( coophr = device.device->SetCooperativeLevel( hDlg, DISCL_EXCLUSIVE |DISCL_BACKGROUND ) ) )
        PrintLog(LOG_DINPUT,"[PAD%d] SetCooperativeLevel (1) failed with code HR = %X", device.dwUserIndex+1, coophr);

    if(coophr!=S_OK)
    {
        PrintLog(LOG_DINPUT,"[Device not exclusive acquired, disabling ForceFeedback");
        device.useforce = 0;

        if( FAILED( coophr = device.device->SetCooperativeLevel( hDlg, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND ) ) )
            PrintLog(LOG_DINPUT,"[PAD%d] SetCooperativeLevel (2) failed with code HR = %X", device.dwUserIndex+1, coophr);

    }

    dipdw.diph.dwSize = sizeof( DIPROPDWORD );
    dipdw.diph.dwHeaderSize = sizeof( DIPROPHEADER );
    dipdw.diph.dwObj = 0;
    dipdw.diph.dwHow = DIPH_DEVICE;
    dipdw.dwData = FALSE;
    device.device->SetProperty( DIPROP_AUTOCENTER, &dipdw.diph );

    if( FAILED( hr = device.device->EnumObjects( EnumObjectsCallback, ( VOID* )&device, DIDFT_AXIS ) ) )
        PrintLog(LOG_DINPUT,"[PAD%d] EnumObjects failed with code HR = %X", device.dwUserIndex+1, hr);
    else PrintLog(LOG_DINPUT,"[PAD%d] Detected axis count: %d",device.dwUserIndex+1,device.axiscount);


    if( FAILED( hr = device.device->EnumObjects( EnumFFAxesCallback, ( VOID* )&device.ff.axisffbcount, DIDFT_AXIS ) ) )
        PrintLog(LOG_DINPUT,"[PAD%d] EnumFFAxesCallback failed with code HR = %X", device.dwUserIndex+1, hr);


    if( device.ff.axisffbcount > 2 )
        device.ff.axisffbcount = 2;

    if( device.ff.axisffbcount <= 0 )
        device.useforce = 0;

    hr = device.device->Acquire();

    mutex.Unlock();

    return hr;
}

BOOL ButtonPressed(DWORD buttonidx, DInputDevice& device)
{
    return (device.state.rgbButtons[buttonidx] & 0x80) != 0;
}

BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef)
{
    DInputFFB * ffb = (DInputFFB*) pvRef;

    // Pointer to calling device
    ffb->ffbcaps.ConstantForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_CONSTANTFORCE;
    ffb->ffbcaps.PeriodicForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_PERIODIC;
    PrintLog(LOG_DINPUT,"   Effect '%s'. IsConstant = %d, IsPeriodic = %d", di->tszName, ffb->ffbcaps.ConstantForce, ffb->ffbcaps.PeriodicForce);
    return DIENUM_CONTINUE;
}

HRESULT SetDeviceForces(DInputDevice& device, WORD force, bool motor)
{
    if(!device.ff.effect[motor]) return E_FAIL;

    if ( force == 0)
    {
        if (FAILED (device.ff.effect[motor]->Stop()))
        {
            device.device->Acquire();
            if (FAILED (device.ff.effect[motor]->Stop()))
            {
                device.device->Unacquire();
                device.device->Acquire();
                device.ff.effect[motor]->Stop();
                return E_FAIL;
            }
        }
        return S_OK;
    }

    static CriticalSection mutex;

    mutex.Lock();
    if(device.ff.type == 1) SetDeviceForcesEjocys(device,force,motor);
    else if(device.ff.type == 2) SetDeviceForcesNew(device,force,motor);
    else SetDeviceForcesFailsafe(device,force,motor);
    mutex.Unlock();

    return S_OK;
}

HRESULT PrepareForce(DInputDevice& device, bool motor)
{
    if(device.ff.effect[motor]) return E_FAIL;
    if(device.ff.type == 1) return PrepareForceEjocys(device,motor);
    if(device.ff.type == 2) return PrepareForceNew(device,motor);
    return PrepareForceFailsafe(device,motor);
}

HRESULT SetDeviceForcesFailsafe(DInputDevice& device, WORD force, bool motor)
{
    // Modifying an effect is basically the same as creating a new one, except
    // you need only specify the parameters you are modifying
    HRESULT hr= S_OK;
    LONG     rglDirection[2] = { 0, 0 };
    DWORD    rgdwAxes[2]     = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis

    DICONSTANTFORCE cf;

    LONG magnitude = (LONG)(force/256*256-1);

    rglDirection[0] = motor;
    cf.lMagnitude = magnitude;

    DIEFFECT eff;
    ZeroMemory( &eff, sizeof( eff ) );
    eff.dwSize = sizeof( DIEFFECT );
    eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff.cAxes = device.ff.axisffbcount;
    eff.rgdwAxes = rgdwAxes;
    eff.rglDirection = rglDirection;
    eff.lpEnvelope = 0;
    eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
    eff.lpvTypeSpecificParams = &cf;
    eff.dwStartDelay = 0;

    // Now set the new parameters and start the effect immediately.
    hr= device.ff.effect[motor]->SetParameters( &eff, DIEP_DIRECTION |DIEP_TYPESPECIFICPARAMS |DIEP_START );
    return hr;
}

HRESULT PrepareForceFailsafe(DInputDevice& device, bool motor)
{

    HRESULT hr= E_FAIL;

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
        eff.cAxes = device.ff.axisffbcount;
        eff.rgdwAxes = rgdwAxes;
        eff.rglDirection = rglDirection;
        eff.lpEnvelope = 0;
        eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
        eff.lpvTypeSpecificParams = &cf;
        eff.dwStartDelay = 0;

        // Create the prepared effect
        if( FAILED( hr = device.device->CreateEffect( GUID_ConstantForce, &eff, &device.ff.effect[motor] , NULL ) ) )
        {
            PrintLog(LOG_DINPUT,"[PAD%d] CreateEffect (1) failed with code HR = %X", device.dwUserIndex+1, hr);
            return hr;
        }

        if( NULL == device.ff.effect[motor] )
        {
            PrintLog(LOG_DINPUT,"g_pEffect is NULL!!!!");
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
        eff.cAxes = device.ff.axisffbcount;
        eff.rgdwAxes = rgdwAxes;
        eff.rglDirection = rglDirection;
        eff.lpEnvelope = 0;
        eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
        eff.lpvTypeSpecificParams = &cf;
        eff.dwStartDelay = 0;

        // Create the prepared effect
        if( FAILED( hr = device.device->CreateEffect( GUID_ConstantForce  ,
                         &eff,& device.ff.effect[motor] , NULL ) ) )
        {
            PrintLog(LOG_DINPUT,"[PAD%d] CreateEffect (2) failed with code HR = %X", device.dwUserIndex+1, hr);
            return hr;
        }

        if( NULL == device.ff.effect[motor] )
        {
            PrintLog(LOG_DINPUT,"[PAD%d] g_pEffect is NULL!!!!",device.dwUserIndex+1);
            return E_FAIL;
        }

        return S_OK;
    }

    return E_FAIL;
}

HRESULT SetDeviceForcesEjocys(DInputDevice& device, WORD force, bool motor)
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
    //hr= Gamepad[idx].g_pEffect[motor]->SetParameters( &eff, DIEP_DIRECTION |DIEP_TYPESPECIFICPARAMS |DIEP_START |DIES_SOLO );
    //return hr;
    //return S_OK;

    PrintLog(LOG_DINPUT,"[PAD%d] SetDeviceForces (%d) %d", device.dwUserIndex+1,motor, force);
    //[-10000:10000]
    //INT nForce = MulDiv(force, 2 * DI_FFNOMINALMAX, 65535) - DI_FFNOMINALMAX;
    //[0:10000]
    INT nForce = MulDiv(force, DI_FFNOMINALMAX, 65535);
    DWORD period;

    // Keep force within bounds
    if( nForce < -DI_FFNOMINALMAX ) nForce = -DI_FFNOMINALMAX;

    if( nForce > +DI_FFNOMINALMAX ) nForce = +DI_FFNOMINALMAX;

    if (motor == 0)
    {
        device.ff.xForce = nForce;
        period = 60000;
    }
    else
    {
        device.ff.yForce = nForce;
        period = 120000;
    }

    DWORD magnitude = 0;
    // Constant:  Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    HRESULT hr = S_OK;
    LONG rglDirection[2] = { 0, 0 };
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !1! HR = %s"), idx+1,motor, DXErrStr(hr));
    DIEFFECT eff;

    if ( device.ff.is_created == false)
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !1a! HR = %s"), idx+1,motor, DXErrStr(hr));
        ZeroMemory( &eff, sizeof( eff ) );
        eff.dwSize = sizeof( DIEFFECT );
        eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
        eff.cAxes =  device.ff.axisffbcount;
        eff.lpEnvelope = 0;
        eff.dwStartDelay = 0;
        //eff.cbTypeSpecificParams = sizeof( DICONSTANTFORCE );
        eff.cbTypeSpecificParams = sizeof( DIPERIODIC );
        device.ff.eff[0] = eff;
        device.ff.is_created = true;
    }

    eff =  device.ff.eff[0];

    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !2! HR = %s"), idx+1,motor, DXErrStr(hr));
    // When modifying an effect you need only specify the parameters you are modifying
    if(  device.ff.axisffbcount == 1 )
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3a! HR = %s"), idx+1,motor, DXErrStr(hr));
        // Apply only one direction and keep the direction at zero
        magnitude = ( DWORD )sqrt( ( double )device.ff.xForce * ( double )device.ff.xForce + ( double )device.ff.yForce * ( double )device.ff.yForce );
        rglDirection[0] = 0;
        device.ff.pf.dwMagnitude = device.ff.xForce;
        device.ff.pf.dwPeriod = period;
    }
    else
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! HR = %s"), idx+1,motor, DXErrStr(hr));
        magnitude = MulDiv(force, DI_FFNOMINALMAX, 65535);
        // Apply magnitude from both directions
        rglDirection[0] = device.ff.xForce;
        rglDirection[1] = device.ff.yForce;
        device.ff.pf.dwMagnitude = magnitude;
        device.ff.pf.dwPeriod = period;
        //LeftForceMagnitude
        //LeftForcePeriod
        // dwMagnitude - Magnitude of the effect, in the range from 0 through 10,000. If an envelope is applied to this effect, the value represents the magnitude of the sustain. If no envelope is applied, the value represents the amplitude of the entire effect.
        // lOffset - Offset of the effect. The range of forces generated by the effect is lOffset minus dwMagnitude to lOffset plus dwMagnitude. The value of the lOffset member is also the baseline for any envelope that is applied to the effect.
        // dwPhase - Position in the cycle of the periodic effect at which playback begins, in the range from 0 through 35,999. See Remarks.
        // dwPeriod - Period of the effect, in microseconds.
    }

    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! axis = %d, x = %d, y = %d, m = %d"), idx+1,motor, Gamepad[idx].g_dwNumForceFeedbackAxis, Gamepad[idx].xForce, Gamepad[idx].yForce, magnitude);
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !6! HR = %s"), idx+1,motor, DXErrStr(hr));
    device.ff.eff[0].rglDirection = rglDirection;
    device.ff.eff[0].lpvTypeSpecificParams =& device.ff.pf;

    if ( device.ff.oldMagnitude != device.ff.pf.dwMagnitude ||
            device.ff.oldPeriod !=  device.ff.pf.dwPeriod ||
            device.ff.oldXForce != device.ff.xForce ||
            device.ff.oldYForce !=  device.ff.yForce)
    {
        device.ff.oldMagnitude =  device.ff.pf.dwMagnitude;
        device.ff.oldPeriod =  device.ff.pf.dwPeriod;
        device.ff.oldXForce = device.ff.xForce;
        device.ff.oldYForce = device.ff.yForce;

        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !7! HR = %s"), idx+1,motor, DXErrStr(hr));
        // Set the new parameters and start the effect immediately.
        if( FAILED( hr = device.ff.effect[motor]->SetParameters(& device.ff.eff[0], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START )))
        {
            PrintLog(LOG_DINPUT,"[PAD%d] SetDeviceForces (%d) failed with code HR = %X", device.dwUserIndex+1,motor, hr);
            return hr;
        };
    }

    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) return HR = %s"), idx+1,motor, DXErrStr(hr));
    return hr;
}

//-----------------------------------------------------------------------------
// Name: PrepareDeviceForces()
// Desc: Prepare force feedback effect.
//-----------------------------------------------------------------------------
HRESULT PrepareForceEjocys(DInputDevice& device, bool motor)
{
    //HRESULT hr= E_FAIL;
    //if( NULL == Gamepad[idx].g_pEffect[motor] )
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
    //              &eff,& device[idx].g_pEffect[motor] , NULL ) ) )
    //      {
    //              PrintLog(_T("[DINPUT]  [PAD%d] CreateEffect (%d) failed with code HR = %s"), idx+1,motor, DXErrStr(hr));
    //              return hr;
    //      }
    //}
    //return S_OK;

    DIEFFECT eff;
    DIPERIODIC pf = { 0 };
    device.ff.pf = pf;
    device.ff.xForce = 0;
    device.ff.yForce = 0;
    device.ff.oldXForce = 0;
    device.ff.oldYForce = 0;
    device.ff.oldMagnitude = 0;
    device.ff.oldPeriod = 0;
    device.ff.is_created = false;
    // Constant:  Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    HRESULT hr = E_FAIL;
    LONG rglDirection[2] = { 0, 0 };
    // Create effect
    ZeroMemory( &eff, sizeof( eff ) );
    eff.dwSize = sizeof( DIEFFECT );
    eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff.cAxes = device.ff.axisffbcount;
    eff.lpEnvelope = 0;
    eff.dwStartDelay = 0;
    eff.cbTypeSpecificParams = sizeof( DIPERIODIC );
    GUID effGuid = GUID_Sine;
    // Force feedback
    DIDEVCAPS didcaps;
    didcaps.dwSize = sizeof didcaps;

    if (SUCCEEDED(device.device->GetCapabilities(&didcaps)) && (didcaps.dwFlags & DIDC_FORCEFEEDBACK))
    {
        PrintLog(LOG_DINPUT,"[PAD%d] PrepareForce (%d) Force Feedback is available", device.dwUserIndex+1,motor);
    }
    else
    {
        PrintLog(LOG_DINPUT,"[PAD%d] PrepareForce (%d) Force Feedback is NOT available", device.dwUserIndex+1,motor);
    }

    // Enumerate effects
    device.device->EnumEffects(&EnumEffectsCallback, &device.ff, DIEFT_ALL);

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
    eff.lpvTypeSpecificParams =& device.ff.pf;

    // Create the prepared effect
    if( FAILED( hr = device.device->CreateEffect(
                         effGuid,  // GUID from enumeration
                         &eff, // where the data is
                         & device.ff.effect[motor],  // where to put interface pointer
                         NULL)))
    {
        PrintLog(LOG_DINPUT,"[DINPUT]  [PAD%d] PrepareForce (%d) failed with code HR = %X", device.dwUserIndex+1,motor, hr);
        return hr;
    }

    if(device.ff.effect[motor] == NULL) return E_FAIL;

    //PrintLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,motor, DXErrStr(hr));
    return S_OK;
}

BOOL EffectIsPlaying(DInputDevice& device)
{
    DWORD state;
    device.device->GetForceFeedbackState(&state);

    if(state & DIGFFS_STOPPED) return FALSE;

    return TRUE;
}

HRESULT SetDeviceForcesNew(DInputDevice& device, WORD force, bool motor)
{
    HRESULT hr;

    if(!force) return device.ff.effect[motor]->Stop();

    if(EffectIsPlaying(device)) device.ff.effect[motor]->Stop();

    LONG nForce = MulDiv(force, DI_FFNOMINALMAX, 65535);
    nForce = Misc::clamp(nForce,-DI_FFNOMINALMAX,DI_FFNOMINALMAX);

    if(motor == FFB_LEFTMOTOR)
    {
        device.ff.eff[motor].dwDuration = device.ff.leftPeriod*1000;
        device.ff.pf.dwMagnitude = (DWORD) nForce;
        device.ff.pf.dwPeriod = device.ff.leftPeriod*1000;
    }

    if(motor == FFB_RIGHTMOTOR)
    {
        device.ff.eff[motor].dwDuration = device.ff.rightPeriod*1000;
        device.ff.pf.dwMagnitude = (DWORD) nForce;
        device.ff.pf.dwPeriod = device.ff.rightPeriod*1000;
    }

    device.ff.eff[motor].lpvTypeSpecificParams =& device.ff.pf;

    hr = device.ff.effect[motor]->SetParameters(& device.ff.eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_DURATION | DIEP_SAMPLEPERIOD);

    if(FAILED(hr)) return hr;

    hr = device.ff.effect[motor]->Start(INFINITE,DIES_SOLO);

    if(FAILED(hr)) return hr;

    return S_OK;
}

HRESULT PrepareForceNew(DInputDevice& device, bool motor)
{
    if(!device.ff.effect[motor])
    {
        HRESULT hr = E_FAIL;
        LONG rglDirection[2] = { 0, 0 };

        // Create effect
        ZeroMemory(&device.ff.pf,sizeof(device.ff.pf));
        ZeroMemory(&device.ff.eff[motor], sizeof( device.ff.eff[motor] ) );

        device.ff.eff[motor].dwSize = sizeof( DIEFFECT );
        device.ff.eff[motor].dwFlags = DIEFF_POLAR | DIEFF_OBJECTOFFSETS;
        device.ff.eff[motor].cAxes = device.ff.axisffbcount;
        device.ff.eff[motor].lpEnvelope = 0;
        device.ff.eff[motor].dwStartDelay = 0;
        device.ff.eff[motor].cbTypeSpecificParams = sizeof( DIPERIODIC );

        // Force feedback
        DIDEVCAPS didcaps;
        didcaps.dwSize = sizeof didcaps;

        if (SUCCEEDED(device.device->GetCapabilities(&didcaps)) && (didcaps.dwFlags & DIDC_FORCEFEEDBACK))
        {
            PrintLog(LOG_DINPUT,"[[PAD%d] PrepareForce (%d) Force Feedback is available", device.dwUserIndex+1,motor);
        }
        else
        {
            PrintLog(LOG_DINPUT,"[PAD%d] PrepareForce (%d) Force Feedback is NOT available", device.dwUserIndex+1,motor);
        }

        // Enumerate effects
        if (SUCCEEDED(hr = device.device->EnumEffects(&EnumEffectsCallback,& device, DIEFT_ALL)))
        {
        }

        DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
        device.ff.eff[motor].dwDuration = INFINITE;
        device.ff.eff[motor].dwSamplePeriod = 0;
        device.ff.eff[motor].dwGain = DI_FFNOMINALMAX;
        device.ff.eff[motor].dwTriggerButton = DIEB_NOTRIGGER;
        device.ff.eff[motor].dwTriggerRepeatInterval = 0;
        device.ff.eff[motor].rgdwAxes = rgdwAxes;
        device.ff.eff[motor].rglDirection = rglDirection;
        device.ff.eff[motor].lpvTypeSpecificParams =& device.ff.rf;

        GUID geff;

        if ( motor == FFB_LEFTMOTOR ) geff = GUID_SawtoothDown;

        if ( motor == FFB_RIGHTMOTOR ) geff = GUID_SawtoothUp;

        // Create the prepared effect
        hr = device.device->CreateEffect(geff,& device.ff.eff[motor],& device.ff.effect[motor], NULL);

        if(FAILED(hr))
        {
            PrintLog(LOG_DINPUT,"[PAD%d] PrepareForce (%d) failed with code HR = %X", device.dwUserIndex+1,motor, hr);
            return hr;
        }

        if(!device.ff.effect[motor]) return E_FAIL;

        //PrintLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,motor, DXErrStr(hr));
    }

    return S_OK;
}


