#include "stdafx.h"
#include "globals.h"
#include "Logger.h"
#include "Misc.h"
#include "InputHook.h"

#include "DirectInput.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

#include "mutex.h"

DInputManager dinput;
std::vector<DInputDevice> g_Devices;

BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext)
{
    DInputDevice *gp = (DInputDevice*)pContext;

    if (pdidoi->dwType & DIDFT_AXIS)
    {
        DIPROPRANGE diprg;
        diprg.diph.dwSize = sizeof(DIPROPRANGE);
        diprg.diph.dwHeaderSize = sizeof(DIPROPHEADER);
        diprg.diph.dwHow = DIPH_BYID;
        diprg.diph.dwObj = pdidoi->dwType;
        diprg.lMin = -32768;
        diprg.lMax = +32767;

        if (FAILED(gp->device->SetProperty(DIPROP_RANGE, &diprg.diph)))
            return DIENUM_STOP;
    }

    gp->axiscount++;
    return DIENUM_CONTINUE;
}

BOOL CALLBACK EnumFFAxesCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext)
{
    DWORD* pdwNumForceFeedbackAxis = (DWORD*)pContext;

    if (((pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0))
        (*pdwNumForceFeedbackAxis)++;

    return DIENUM_CONTINUE;
}

HRESULT UpdateState(DInputDevice& device)
{
    HRESULT hr = E_FAIL;

    if ((!device.device))
        return E_FAIL;

    device.device->Poll();
    hr = device.device->GetDeviceState(sizeof(DIJOYSTATE2), &device.state);

    if (FAILED(hr))
    {
        PrintLog("[PAD%d] Device Reacquired", device.dwUserIndex + 1);
        hr = device.device->Acquire();
    }

    return hr;
}

HRESULT InitDirectInput(HWND hDlg, DInputDevice& device)
{
    DIPROPDWORD dipdw;
    HRESULT hr = E_FAIL;
    HRESULT coophr = E_FAIL;

    if (FAILED(dinput.Init()))
    {
        PrintLog("DirectInput cannot be initialized");
        MessageBox(NULL, "DirectInput cannot be initialized", "x360ce - Error", MB_ICONERROR);
        ExitProcess(hr);
    }

    static recursive_mutex mutex;
    lock_guard lock(mutex);

    PrintLog("[PAD%d] Creating device", device.dwUserIndex + 1);

    bool bHookDI = false;
    bool bHookSA = false;

    bHookDI = g_iHook.GetState(InputHook::HOOK_DI);
    bHookSA = g_iHook.GetState(InputHook::HOOK_SA);

    if (bHookDI) g_iHook.DisableHook(InputHook::HOOK_DI);
    if (bHookSA) g_iHook.DisableHook(InputHook::HOOK_SA);

    if (!device.useproduct)
    {
        hr = dinput.Get()->CreateDevice(device.instanceid, &device.device, NULL);
        if (FAILED(hr))
        {
            std::string instanceid;
            GUIDtoStringA(&instanceid, device.instanceid);
            PrintLog("InstanceGUID %s is incorrect trying ProductGUID", instanceid.c_str());
            hr = dinput.Get()->CreateDevice(device.productid, &device.device, NULL);
        }
    }
    else hr = dinput.Get()->CreateDevice(device.productid, &device.device, NULL);

    if (FAILED(hr))
    {
        SWIP ini("x360ce.ini");
        if (ini.get_bool("Options", "Continue"))
        {
            device.passthrough = true;
            return S_OK;
        }
        PrintLog("x360ce is misconfigured or device is disconnected");
        int response = MessageBoxA(NULL, "x360ce is misconfigured or device is disconnected", "x360ce - Error", MB_CANCELTRYCONTINUE | MB_ICONWARNING | MB_SYSTEMMODAL);
        switch (response)
        {
        case IDCANCEL:
            ExitProcess(1);
        case IDTRYAGAIN:
            return InitDirectInput(hDlg, device);
        case IDCONTINUE:
            device.passthrough = true;
            return S_OK;
        }
    }

    if (!device.device) return ERROR_DEVICE_NOT_CONNECTED;
    else PrintLog("[PAD%d] Device created", device.dwUserIndex + 1);

    hr = device.device->SetDataFormat(&c_dfDIJoystick2);

    if (FAILED(hr)) PrintLog("[PAD%d] SetDataFormat failed with code HR = %X", device.dwUserIndex + 1, hr);

    coophr = device.device->SetCooperativeLevel(hDlg, DISCL_EXCLUSIVE | DISCL_BACKGROUND);
    if (FAILED(coophr)) PrintLog("[PAD%d] SetCooperativeLevel (1) failed with code HR = %X", device.dwUserIndex + 1, coophr);

    if (coophr != DI_OK)
    {
        PrintLog("Device not exclusive acquired, disabling ForceFeedback");
        device.useforce = 0;

        coophr = device.device->SetCooperativeLevel(hDlg, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND);
        if (FAILED(coophr)) PrintLog("[PAD%d] SetCooperativeLevel (2) failed with code HR = %X", device.dwUserIndex + 1, coophr);
    }

    dipdw.diph.dwSize = sizeof(DIPROPDWORD);
    dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    dipdw.diph.dwObj = 0;
    dipdw.diph.dwHow = DIPH_DEVICE;
    dipdw.dwData = FALSE;
    device.device->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);

    hr = device.device->EnumObjects(EnumObjectsCallback, (VOID*)&device, DIDFT_AXIS);
    if (FAILED(hr)) PrintLog("[PAD%d] EnumObjects failed with code HR = %X", device.dwUserIndex + 1, hr);
    else PrintLog("[PAD%d] Detected axis count: %d", device.dwUserIndex + 1, device.axiscount);

    hr = device.device->EnumObjects(EnumFFAxesCallback, (VOID*)&device.ff.axisffbcount, DIDFT_AXIS);
    if (FAILED(hr)) PrintLog("[PAD%d] EnumFFAxesCallback failed with code HR = %X", device.dwUserIndex + 1, hr);


    if (device.ff.axisffbcount > 2)
        device.ff.axisffbcount = 2;

    if (device.ff.axisffbcount <= 0)
        device.useforce = 0;

    hr = device.device->Acquire();

    if (bHookSA) g_iHook.EnableHook(InputHook::HOOK_SA);
    if (bHookDI) g_iHook.EnableHook(InputHook::HOOK_DI);

    return hr;
}

BOOL ButtonPressed(DWORD buttonidx, DInputDevice& device)
{
    return (device.state.rgbButtons[buttonidx] & 0x80) != 0;
}

BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef)
{
    DInputFFB * ffb = (DInputFFB*)pvRef;

    // Pointer to calling device
    ffb->ffbcaps.ConstantForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_CONSTANTFORCE;
    ffb->ffbcaps.PeriodicForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_PERIODIC;
    PrintLog("   Effect '%s'. IsConstant = %d, IsPeriodic = %d", di->tszName, ffb->ffbcaps.ConstantForce, ffb->ffbcaps.PeriodicForce);
    return DIENUM_CONTINUE;
}

HRESULT DInputSetState(DInputDevice& device, WORD force, bool motor){
    // If device was not initialized previously.
    if (!device.ff.IsMotorInitialized[motor]){
        // Mark as initialized.
        device.ff.IsMotorInitialized[motor] = true;
        // Prepare force feedback effect.
        HRESULT prepareResult = PrepareForce(device, motor);
        PrintLog("[DINPUT] [PAD%d] [M%d] DInputSetState: PrepareForce returned HR = %X", device.dwUserIndex + 1, motor, prepareResult);
        if (SUCCEEDED(prepareResult)){
            if (device.ff.effect[motor] != NULL){
                // Initialize will stop motor and aquire device.
                HRESULT initializeResult = SetDeviceForces(device, 0, motor);
                PrintLog("[DINPUT] [PAD%d] [M%d] DInputSetState: SetDeviceForces returned HR = %X // Initializing", device.dwUserIndex + 1, motor, initializeResult);
            }
        }
    }
    if (device.ff.effect[motor] != NULL){
        HRESULT setForceResult = SetDeviceForces(device, force, motor);
        PrintLog("[DINPUT] [PAD%d] [M%d] DInputSetState: returned HR = %X", device.dwUserIndex + 1, motor, setForceResult);
    }
    return ERROR_SUCCESS;
}

HRESULT SetDeviceForces(DInputDevice& device, WORD force, bool motor)
{
    PrintLog("[DINPUT] [PAD%d] [M%d] SetDeviceForces: force = %d", device.dwUserIndex + 1, motor, force);
    if (!device.ff.effect[motor]) return E_FAIL;
    if (force == 0)
    {
        if (FAILED(device.ff.effect[motor]->Stop()))
        {
            device.device->Acquire();
            if (FAILED(device.ff.effect[motor]->Stop()))
            {
                device.device->Unacquire();
                device.device->Acquire();
                device.ff.effect[motor]->Stop();
                return E_FAIL;
            }
        }
        return S_OK;
    }

    static recursive_mutex mutex;
    lock_guard lock(mutex);

    if (device.ff.type == 1) SetDeviceForcesEjocys(device, force, motor);
    else if (device.ff.type == 2) SetDeviceForcesNew(device, force, motor);
    else SetDeviceForcesFailsafe(device, force, motor);

    return S_OK;
}

HRESULT PrepareForce(DInputDevice& device, bool motor)
{
    //if (device.ff.effect[motor]) return E_FAIL;
    if (device.ff.type == 1) return PrepareForceEjocys(device, motor);
    if (device.ff.type == 2) return PrepareForceNew(device, motor);
    return PrepareForceFailsafe(device, motor);
}

HRESULT SetDeviceForcesFailsafe(DInputDevice& device, WORD force, bool motor)
{
    // Modifying an effect is basically the same as creating a new one, except
    // you need only specify the parameters you are modifying
    HRESULT hr = S_OK;
    LONG     rglDirection[2] = { 0, 0 };
    DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis

    DICONSTANTFORCE cf;

    LONG magnitude = (LONG)(force / 256 * 256 - 1);

    rglDirection[0] = motor;
    cf.lMagnitude = magnitude;

    DIEFFECT eff;
    ZeroMemory(&eff, sizeof(eff));
    eff.dwSize = sizeof(DIEFFECT);
    eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff.cAxes = device.ff.axisffbcount;
    eff.rgdwAxes = rgdwAxes;
    eff.rglDirection = rglDirection;
    eff.lpEnvelope = 0;
    eff.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
    eff.lpvTypeSpecificParams = &cf;
    eff.dwStartDelay = 0;

    // Now set the new parameters and start the effect immediately.
    hr = device.ff.effect[motor]->SetParameters(&eff, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
    return hr;
}

HRESULT PrepareForceFailsafe(DInputDevice& device, bool motor)
{

    HRESULT hr = E_FAIL;

    if (motor == 0)
    {
        // wLeftMotorSpeed

        DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
        LONG rglDirection[2] = { 0, 0 };

        DICONSTANTFORCE cf;
        DIEFFECT eff;

        ZeroMemory(&eff, sizeof(eff));
        eff.dwSize = sizeof(DIEFFECT);
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
        eff.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
        eff.lpvTypeSpecificParams = &cf;
        eff.dwStartDelay = 0;

        // Create the prepared effect
        hr = device.device->CreateEffect(GUID_ConstantForce, &eff, &device.ff.effect[motor], NULL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] CreateEffect (1) failed with code HR = %X", device.dwUserIndex + 1, hr);
            return hr;
        }

        if (NULL == device.ff.effect[motor])
        {
            PrintLog("g_pEffect is NULL!!!!");
            return E_FAIL;
        }

        return S_OK;
    }
    else if (motor == 1)
    {
        DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
        LONG rglDirection[2] = { 0, 0 };

        DICONSTANTFORCE cf;
        DIEFFECT eff;

        // wRightMotorSpeed
        ZeroMemory(&eff, sizeof(eff));
        eff.dwSize = sizeof(DIEFFECT);
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
        eff.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
        eff.lpvTypeSpecificParams = &cf;
        eff.dwStartDelay = 0;

        // Create the prepared effect
        hr = device.device->CreateEffect(GUID_ConstantForce, &eff, &device.ff.effect[motor], NULL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] CreateEffect (2) failed with code HR = %X", device.dwUserIndex + 1, hr);
            return hr;
        }

        if (NULL == device.ff.effect[motor])
        {
            PrintLog("[PAD%d] g_pEffect is NULL!!!!", device.dwUserIndex + 1);
            return E_FAIL;
        }

        return S_OK;
    }

    return E_FAIL;
}

BOOL IsForceSupported(DInputDevice& device)
{
    // Check if force feedback is available.
    DIDEVCAPS didcaps;
    didcaps.dwSize = sizeof(didcaps);
    // Get device capabilites.
    HRESULT hr = device.device->GetCapabilities(&didcaps);
    if (hr != DI_OK){
        PrintLog("[DINPUT] [PAD%d] IsForceSupported: GetCapabilities returned HR = %X", device.dwUserIndex + 1, hr);
        return false;
    }
    bool ffSupported = ((didcaps.dwFlags & DIDC_FORCEFEEDBACK) == DIDC_FORCEFEEDBACK);
    PrintLog("[DINPUT] [PAD%d] IsForceSupported: %d %s", device.dwUserIndex + 1, didcaps.dwFlags, ffSupported == true ? "YES" : "NO");
    return ffSupported;
}

//-----------------------------------------------------------------------------
// Name: PrepareDeviceForces()
// Desc: Prepare force feedback effect.
//-----------------------------------------------------------------------------
HRESULT PrepareForceEjocys(DInputDevice& device, bool motor)
{
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    GUID effGuid = GUID_Sine;
    // Clear original effect values.
    DIPERIODIC pf = { 0 };
    device.ff.pf = pf;
    device.ff.xForce = 0;
    device.ff.yForce = 0;
    device.ff.oldXForce = 0;
    device.ff.oldYForce = 0;
    device.ff.oldMagnitude = 0;
    device.ff.oldPeriod = 0;
    device.ff.is_created[motor] = false;
    // Enumerate effects.
    device.device->EnumEffects(&EnumEffectsCallback, &device.ff, DIEFT_ALL);
    // Create structure that provides parameters for the effect.
    DIEFFECT eff;
    // Fills a block of memory with zeros.
    ZeroMemory(&eff, sizeof(eff));
    eff.dwSize = sizeof(DIEFFECT);
    eff.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff.cAxes = device.ff.axisffbcount;
    eff.lpEnvelope = 0;
    eff.dwStartDelay = 0;
    eff.cbTypeSpecificParams = sizeof(DIPERIODIC);
    eff.dwDuration = INFINITE;
    eff.dwSamplePeriod = 0;
    eff.dwGain = DI_FFNOMINALMAX; // no scaling
    eff.dwTriggerButton = DIEB_NOTRIGGER;
    eff.dwTriggerRepeatInterval = 0;
    eff.rgdwAxes = rgdwAxes;
    eff.rglDirection = rglDirection;
    eff.lpvTypeSpecificParams = &device.ff.pf;
    // Store motor effect.
    device.ff.eff[motor] = eff;
    // Create prepared effect.
    HRESULT hr = device.device->CreateEffect(effGuid, &device.ff.eff[motor], &device.ff.effect[motor], NULL);
    if (FAILED(hr))
    {
        //DIERR_DEVICEFULL, DIERR_DEVICENOTREG, DIERR_INVALIDPARAM, DIERR_NOTINITIALIZED.
        PrintLog("[DINPUT] [PAD%d] PrepareForce (%d) failed with code HR = %X", device.dwUserIndex + 1, motor, hr);
        return hr;
    }
    // If pointer to the IDirectInputEffect Interface is empty then return faield state.
    if (device.ff.effect[motor] == NULL) return E_FAIL;
    //PrintLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,motor, DXErrStr(hr));
    return S_OK;
}

//-----------------------------------------------------------------------------
// Name: SetDeviceForces()
// Desc: Modify previously created force feedback effect.
//-----------------------------------------------------------------------------
HRESULT SetDeviceForcesEjocys(DInputDevice& device, WORD force, bool motor)
{
    PrintLog("[DINPUT] [PAD%d] [M%d] SetDeviceForcesEJocys: force = %d", device.dwUserIndex + 1, motor, force);
    // Convert [0;65535] range to [0:10000] range.
    INT nForce = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);
    DWORD period;
    // Keep force within bounds.
    if (nForce < -DI_FFNOMINALMAX) nForce = -DI_FFNOMINALMAX;
    if (nForce > +DI_FFNOMINALMAX) nForce = +DI_FFNOMINALMAX;
    // If this is left motor then...
    if (motor == 0)
    {
        // Update left parameters.
        device.ff.xForce = nForce;
        period = device.ff.leftPeriod * 1000;
    }
    else
    {
        // Update right parameters.
        device.ff.yForce = nForce;
        period = device.ff.rightPeriod * 1000;
    }
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    HRESULT hr = S_OK;
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !1! HR = %s"), idx+1,motor, DXErrStr(hr));
    DWORD magnitude;
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !2! HR = %s"), idx+1,motor, DXErrStr(hr));
    // When modifying an effect you need only specify the parameters you are modifying
    // If only one force feedback actuator is available.
    if (device.ff.axisffbcount == 1)
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3a! HR = %s"), idx+1,motor, DXErrStr(hr));
        magnitude = max(device.ff.xForce, device.ff.yForce);
        period = (device.ff.leftPeriod * 1000 * device.ff.xForce + device.ff.rightPeriod * 1000 * device.ff.yForce) / (device.ff.xForce + device.ff.yForce);
        // Apply only one direction and keep the direction at zero
        device.ff.eff[motor].rglDirection[0] = 0;
        device.ff.eff[motor].rglDirection[1] = 0;
    }
    else
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! HR = %s"), idx+1,motor, DXErrStr(hr));
        magnitude = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);
        // Apply magnitude from both directions
        device.ff.eff[motor].rglDirection[0] = device.ff.xForce;
        device.ff.eff[motor].rglDirection[1] = device.ff.yForce;
        // lOffset - Offset of the effect. The range of forces generated by the effect is lOffset minus dwMagnitude to lOffset plus dwMagnitude. The value of the lOffset member is also the baseline for any envelope that is applied to the effect.
        // dwPhase - Position in the cycle of the periodic effect at which playback begins, in the range from 0 through 35,999. See Remarks.
    }
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! axis = %d, x = %d, y = %d, m = %d"), idx+1,motor, Gamepad[idx].g_dwNumForceFeedbackAxis, Gamepad[idx].xForce, Gamepad[idx].yForce, magnitude);
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !6! HR = %s"), idx+1,motor, DXErrStr(hr));
    // dwMagnitude - Magnitude of the effect, in the range from 0 through 10,000. If an envelope is applied to this effect, the value represents the magnitude of the sustain. If no envelope is applied, the value represents the amplitude of the entire effect.
    device.ff.pf.dwMagnitude = magnitude;
    // dwPeriod - Period of the effect, in microseconds.
    device.ff.pf.dwPeriod = period;
    device.ff.eff[motor].lpvTypeSpecificParams = &device.ff.pf;

    if (device.ff.oldMagnitude != device.ff.pf.dwMagnitude ||
        device.ff.oldPeriod != device.ff.pf.dwPeriod ||
        device.ff.oldXForce != device.ff.xForce ||
        device.ff.oldYForce != device.ff.yForce)
    {
        device.ff.oldMagnitude = device.ff.pf.dwMagnitude;
        device.ff.oldPeriod = device.ff.pf.dwPeriod;
        device.ff.oldXForce = device.ff.xForce;
        device.ff.oldYForce = device.ff.yForce;
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !7! HR = %s"), idx+1,motor, DXErrStr(hr));
        // Set the new parameters and start the effect immediately.
        hr = device.ff.effect[motor]->SetParameters(&device.ff.eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
        if (FAILED(hr))
        {
            PrintLog("[DINPUT] [PAD%d] [M%d] SetDeviceForcesEJocys: failed with code HR = %X", device.dwUserIndex + 1, motor, hr);
            return hr;
        };
    }
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) return HR = %s"), idx+1,motor, DXErrStr(hr));
    return hr;
}

BOOL EffectIsPlaying(DInputDevice& device)
{
    DWORD state;
    device.device->GetForceFeedbackState(&state);

    if (state & DIGFFS_STOPPED) return FALSE;

    return TRUE;
}

HRESULT SetDeviceForcesNew(DInputDevice& device, WORD force, bool motor)
{
    HRESULT hr;

    if (!force) return device.ff.effect[motor]->Stop();

    if (EffectIsPlaying(device)) device.ff.effect[motor]->Stop();

    LONG nForce = MulDiv(force, DI_FFNOMINALMAX, 65535);
    nForce = clamp(nForce, -DI_FFNOMINALMAX, DI_FFNOMINALMAX);

    bool bMotor = device.swapmotor ? !motor : motor;

    if (bMotor == FFB_LEFTMOTOR)
    {
        device.ff.eff[motor].dwDuration = device.ff.leftPeriod * 1000;
        device.ff.pf.dwMagnitude = (DWORD)nForce;
        device.ff.pf.dwPeriod = device.ff.leftPeriod * 1000;
    }

    if (bMotor == FFB_RIGHTMOTOR)
    {
        device.ff.eff[motor].dwDuration = device.ff.rightPeriod * 1000;
        device.ff.pf.dwMagnitude = (DWORD)nForce;
        device.ff.pf.dwPeriod = device.ff.rightPeriod * 1000;
    }

    device.ff.eff[motor].lpvTypeSpecificParams = &device.ff.pf;

    hr = device.ff.effect[motor]->SetParameters(&device.ff.eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_DURATION | DIEP_SAMPLEPERIOD);
    if (FAILED(hr)) return hr;

    hr = device.ff.effect[motor]->Start(INFINITE, DIES_SOLO);

    if (FAILED(hr)) return hr;

    return S_OK;
}

HRESULT PrepareForceNew(DInputDevice& device, bool motor)
{
    if (!device.ff.effect[motor])
    {

        LONG rglDirection[2] = { 0, 0 };

        // Create effect
        ZeroMemory(&device.ff.pf, sizeof(device.ff.pf));
        ZeroMemory(&device.ff.eff[motor], sizeof(device.ff.eff[motor]));

        device.ff.eff[motor].dwSize = sizeof(DIEFFECT);
        device.ff.eff[motor].dwFlags = DIEFF_POLAR | DIEFF_OBJECTOFFSETS;
        device.ff.eff[motor].cAxes = device.ff.axisffbcount;
        device.ff.eff[motor].lpEnvelope = 0;
        device.ff.eff[motor].dwStartDelay = 0;
        device.ff.eff[motor].cbTypeSpecificParams = sizeof(DIPERIODIC);

        // Enumerate effects
        HRESULT hr = device.device->EnumEffects(&EnumEffectsCallback, &device, DIEFT_ALL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] EnumEffectsCallback failed");
        }

        DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
        device.ff.eff[motor].dwDuration = INFINITE;
        device.ff.eff[motor].dwSamplePeriod = 0;
        device.ff.eff[motor].dwGain = DI_FFNOMINALMAX;
        device.ff.eff[motor].dwTriggerButton = DIEB_NOTRIGGER;
        device.ff.eff[motor].dwTriggerRepeatInterval = 0;
        device.ff.eff[motor].rgdwAxes = rgdwAxes;
        device.ff.eff[motor].rglDirection = rglDirection;
        device.ff.eff[motor].lpvTypeSpecificParams = &device.ff.rf;

        GUID geff;

        if (motor == FFB_LEFTMOTOR) geff = GUID_SawtoothDown;

        if (motor == FFB_RIGHTMOTOR) geff = GUID_SawtoothUp;

        // Create the prepared effect
        hr = device.device->CreateEffect(geff, &device.ff.eff[motor], &device.ff.effect[motor], NULL);

        if (FAILED(hr))
        {
            PrintLog("[PAD%d] PrepareForce (%d) failed with code HR = %X", device.dwUserIndex + 1, motor, hr);
            return hr;
        }

        if (!device.ff.effect[motor]) return E_FAIL;

        //PrintLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,motor, DXErrStr(hr));
    }

    return S_OK;
}


