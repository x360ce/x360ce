#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"
#include "InputHook.h"

#include "Controller.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

#include "mutex.h"

DInputManager dinput;
Controller* g_pControllers[XUSER_MAX_COUNT] = { nullptr, nullptr, nullptr, nullptr };

Controller::Controller()
{
    Init();
}

Controller::~Controller()
{
    if (device)
        device->Release();
}

void Controller::Init()
{
    device = nullptr;
    productid = GUID_NULL;
    instanceid = GUID_NULL;
    dwUserIndex = INVALIDUSERINDEX;
    axiscount = 0;

    gamepadtype = 1;
    passthrough = true;
}

void Controller::Reset()
{
    if (device)
        device->Release();
    Init();
}

BOOL CALLBACK Controller::EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext)
{
    Controller *gp = (Controller*)pContext;

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

        gp->axiscount++;
    }

    return DIENUM_CONTINUE;
}

HRESULT Controller::UpdateState()
{
    HRESULT hr = E_FAIL;

    if ((!device)) return E_FAIL;

    device->Poll();
    hr = device->GetDeviceState(sizeof(DIJOYSTATE2), &state);

    if (FAILED(hr))
    {
        PrintLog("[PAD%d] Device Reacquired", dwUserIndex + 1);
        hr = device->Acquire();
    }

    return hr;
}

HRESULT Controller::InitDirectInput(HWND hWnd)
{
    DIPROPDWORD dipdw;
    HRESULT hr = E_FAIL;

    if (FAILED(dinput.Init()))
    {
        PrintLog("DirectInput cannot be initialized");
        MessageBox(NULL, "DirectInput cannot be initialized", "x360ce - Error", MB_ICONERROR);
        ExitProcess(hr);
    }

    LockGuard lock(mutex);

    PrintLog("[PAD%d] Creating device", dwUserIndex + 1);

    bool bHookDI = g_iHook.GetState(InputHook::HOOK_DI);
    bool bHookSA = g_iHook.GetState(InputHook::HOOK_SA);

    if (bHookDI) g_iHook.DisableHook(InputHook::HOOK_DI);
    if (bHookSA) g_iHook.DisableHook(InputHook::HOOK_SA);

    hr = dinput.Get()->CreateDevice(instanceid, &device, NULL);
    if (FAILED(hr))
    {
        std::string strInstance;
        GUIDtoStringA(&strInstance, instanceid);
        PrintLog("InstanceGUID %s is incorrect trying ProductGUID", strInstance.c_str());
        hr = dinput.Get()->CreateDevice(productid, &device, NULL);
    }

    if (!device) return ERROR_DEVICE_NOT_CONNECTED;
    else PrintLog("[PAD%d] Device created", dwUserIndex + 1);

    hr = device->SetDataFormat(&c_dfDIJoystick2);

    if (FAILED(hr)) PrintLog("[PAD%d] SetDataFormat failed with code HR = %X", dwUserIndex + 1, hr);

    HRESULT setCooperativeLevelResult = device->SetCooperativeLevel(hWnd, DISCL_EXCLUSIVE | DISCL_BACKGROUND);
    if (FAILED(setCooperativeLevelResult))
    {
        useforce = false;
        PrintLog("Cannot get exclusive device access, disabling ForceFeedback");

        setCooperativeLevelResult = device->SetCooperativeLevel(hWnd, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND);
        if (FAILED(setCooperativeLevelResult)) PrintLog("[PAD%d] SetCooperativeLevel failed with code HR = %X", dwUserIndex + 1, setCooperativeLevelResult);
    }

    dipdw.diph.dwSize = sizeof(DIPROPDWORD);
    dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    dipdw.diph.dwObj = 0;
    dipdw.diph.dwHow = DIPH_DEVICE;
    dipdw.dwData = FALSE;
    device->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);

    hr = device->EnumObjects(EnumObjectsCallback, (VOID*)this, DIDFT_AXIS);
    if (FAILED(hr)) PrintLog("[PAD%d] EnumObjects failed with code HR = %X", dwUserIndex + 1, hr);
    else PrintLog("[PAD%d] Detected axis count: %d", dwUserIndex + 1, axiscount);

    if (useforce)
    {
        hr = device->EnumObjects(ForceFeedback::EnumFFAxesCallback, (VOID*)&ffb.axisffbcount, DIDFT_AXIS);
        if (FAILED(hr)) PrintLog("[PAD%d] EnumFFAxesCallback failed with code HR = %X", dwUserIndex + 1, hr);

        if (ffb.axisffbcount > 2)
            ffb.axisffbcount = 2;

        if (ffb.axisffbcount > 0)
            ffb.controller = this;
        else
            useforce = false;
    }

    hr = device->Acquire();

    if (bHookSA) g_iHook.EnableHook(InputHook::HOOK_SA);
    if (bHookDI) g_iHook.EnableHook(InputHook::HOOK_DI);

    return hr;
}

BOOL Controller::ButtonPressed(DWORD buttonidx)
{
    return (buttonidx != INVALIDBUTTONINDEX) ? (state.rgbButtons[buttonidx] & 0x80) != 0 : 0;
}

void ForceFeedback::Init()
{
    for (u32 i = 0; i < _countof(effect); ++i) effect[i] = 0;
    for (u32 i = 0; i < _countof(eff); ++i) eff[i] = nullptr;
    pf = nullptr;
    cf = nullptr;
    rf = nullptr;
    xForce = 0;
    yForce = 0;
    oldXForce = 0;
    oldYForce = 0;
    oldMagnitude = 0;
    oldPeriod = 0;
    for (u32 i = 0; i < _countof(is_created); ++i) is_created[i] = 0;
    for (u32 i = 0; i < _countof(IsMotorInitialized); ++i) IsMotorInitialized[i] = 0;
    IsSupported = false;
    IsSupportChecked = false;
    axisffbcount = 0;
    leftPeriod = 0;
    rightPeriod = 0;
    forcepercent = 100;
    type = 0;
    Caps.ConstantForce = false;
    Caps.PeriodicForce = false;
    Caps.RampForce = false;
}

void ForceFeedback::Reset()
{
    if (effect[FFB_LEFTMOTOR] || effect[FFB_RIGHTMOTOR])
    {
        bool brokenffd = false;
        if (GetModuleHandleA("tmffbdrv.dll")) brokenffd = true;
        if (controller->device && brokenffd == false)
        {
            controller->device->SendForceFeedbackCommand(DISFFC_RESET);
        }
    }

    if (effect[FFB_LEFTMOTOR])
        effect[FFB_LEFTMOTOR]->Release();

    if (effect[FFB_RIGHTMOTOR])
        effect[FFB_RIGHTMOTOR]->Release();

    delete eff[0];
    delete eff[1];

    delete cf;
    delete pf;
    delete rf;

    Init();
}

ForceFeedback::ForceFeedback()
{
    Init();
};

ForceFeedback::~ForceFeedback()
{
    if (effect[FFB_LEFTMOTOR] || effect[FFB_RIGHTMOTOR])
    {
        bool brokenffd = false;
        if (GetModuleHandleA("tmffbdrv.dll")) brokenffd = true;
        if (controller->device && brokenffd == false)
        {
            controller->device->SendForceFeedbackCommand(DISFFC_RESET);
        }
    }

    if (effect[FFB_LEFTMOTOR])
        effect[FFB_LEFTMOTOR]->Release();

    if (effect[FFB_RIGHTMOTOR])
        effect[FFB_RIGHTMOTOR]->Release();

    delete eff[0];
    delete eff[1];

    delete cf;
    delete pf;
    delete rf;
}

BOOL CALLBACK ForceFeedback::EnumFFAxesCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext)
{
    DWORD* pdwNumForceFeedbackAxis = (DWORD*)pContext;

    if (((pdidoi->dwFlags & DIDOI_FFACTUATOR) != 0))
        (*pdwNumForceFeedbackAxis)++;

    return DIENUM_CONTINUE;
}

BOOL CALLBACK ForceFeedback::EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef)
{
    ForceFeedback* ffb = (ForceFeedback*)pvRef;
    ForceFeedbackCaps caps;

    caps.ConstantForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_CONSTANTFORCE;
    caps.PeriodicForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_PERIODIC;
    caps.RampForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_RAMPFORCE;

    ffb->SetCaps(caps);
    PrintLog("ForceFeedback effect '%s'. IsConstant = %d, IsPeriodic = %d", di->tszName, caps.ConstantForce, caps.PeriodicForce);
    return DIENUM_CONTINUE;
}

HRESULT ForceFeedback::SetState(WORD force, u8 motor)
{
    // If device was not initialized previously.
    if (!IsMotorInitialized[motor]){
        // Mark as initialized.
        IsMotorInitialized[motor] = true;
        // Prepare force feedback effect.
        HRESULT prepareResult = PrepareForce(motor);
        if (FAILED(prepareResult))
            PrintLog("[DINPUT] [PAD%d] [M%d] DInputSetState: PrepareForce returned HR = %X", controller->dwUserIndex + 1, motor, prepareResult);
        else {
            if (effect[motor] != NULL){
                // Initialize will stop motor and aquire device.
                HRESULT initializeResult = SetDeviceForces(0, motor);
                if (FAILED(initializeResult))
                    PrintLog("[DINPUT] [PAD%d] [M%d] DInputSetState: SetDeviceForces returned HR = %X // Initializing", controller->dwUserIndex + 1, motor, initializeResult);
            }
        }
    }
    if (effect[motor] != NULL){
        HRESULT setForceResult = SetDeviceForces(force, motor);
        if (FAILED(setForceResult))
            PrintLog("[DINPUT] [PAD%d] [M%d] DInputSetState: returned HR = %X", controller->dwUserIndex + 1, motor, setForceResult);
    }
    return ERROR_SUCCESS;
}

HRESULT ForceFeedback::SetDeviceForces(WORD force, u8 motor)
{
    LPDIRECTINPUTDEVICE8 device = controller->device;
#ifdef _DEBUG
    PrintLog("[DINPUT] [PAD%d] [M%d] SetDeviceForces: force = %d", controller->dwUserIndex + 1, motor, force);
#endif
    if (!effect[motor]) return E_FAIL;

    if (force == 0)
    {
        if (FAILED(effect[motor]->Stop()))
        {
            device->Acquire();
            if (FAILED(effect[motor]->Stop()))
            {
                device->Unacquire();
                device->Acquire();
                effect[motor]->Stop();
                return E_FAIL;
            }
        }
        return S_OK;
    }

    LockGuard lock(controller->mutex);

    if (type == 1) SetDeviceForcesEjocys(force, motor);
    else if (type == 2) SetDeviceForcesNew(force, motor);
    else SetDeviceForcesFailsafe(force, motor);

    return S_OK;
}

HRESULT ForceFeedback::PrepareForce(u8 motor)
{
    delete eff[motor];

    if (type == 1) return PrepareForceEjocys(motor);
    if (type == 2) return PrepareForceNew(motor);
    return PrepareForceFailsafe(motor);
}

HRESULT ForceFeedback::PrepareForceFailsafe(u8 motor)
{
    HRESULT hr = E_FAIL;

    DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis
    LONG rglDirection[2] = { 0, 0 };

    cf = new DICONSTANTFORCE;
    cf->lMagnitude = 0;

    eff[motor] = new DIEFFECT;
    ZeroMemory(eff[motor], sizeof(DIEFFECT));

    eff[motor]->dwSize = sizeof(DIEFFECT);
    eff[motor]->dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff[motor]->dwDuration = INFINITE;
    eff[motor]->dwSamplePeriod = 0;
    eff[motor]->dwGain = DI_FFNOMINALMAX;
    eff[motor]->dwTriggerButton = DIEB_NOTRIGGER;
    eff[motor]->dwTriggerRepeatInterval = 0;
    eff[motor]->cAxes = axisffbcount;
    eff[motor]->rgdwAxes = rgdwAxes;
    eff[motor]->rglDirection = rglDirection;
    eff[motor]->lpEnvelope = 0;
    eff[motor]->cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
    eff[motor]->lpvTypeSpecificParams = cf;
    eff[motor]->dwStartDelay = 0;

    // Create the prepared effect
    LPDIRECTINPUTDEVICE8 device = controller->device;
    hr = device->CreateEffect(GUID_ConstantForce, eff[motor], &effect[motor], NULL);
    if (FAILED(hr))
    {
        PrintLog("[PAD%d] CreateEffect failed with code HR = %X", controller->dwUserIndex + 1, hr);
        return hr;
    }

    if (!effect[motor])
    {
        PrintLog("Effect is nullptr, disabling FFB");
        controller->useforce = false;
    }

    return hr;
}

HRESULT ForceFeedback::SetDeviceForcesFailsafe(WORD force, u8 motor)
{
    // Modifying an effect is basically the same as creating a new one, except
    // you need only specify the parameters you are modifying
    HRESULT hr = S_OK;
    LONG     rglDirection[2] = { 0, 0 };
    DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };  // X- and y-axis

    LONG magnitude = (LONG)(force / 256 * 256 - 1);

    rglDirection[0] = motor;
    cf->lMagnitude = magnitude;

    eff[motor]->dwSize = sizeof(DIEFFECT);
    eff[motor]->dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff[motor]->cAxes = axisffbcount;
    eff[motor]->rgdwAxes = rgdwAxes;
    eff[motor]->rglDirection = rglDirection;
    eff[motor]->lpEnvelope = 0;
    eff[motor]->cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
    eff[motor]->lpvTypeSpecificParams = &cf;
    eff[motor]->dwStartDelay = 0;

    // Now set the new parameters and start the effect immediately.
    hr = effect[motor]->SetParameters(eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
    return hr;
}

BOOL ForceFeedback::IsForceSupported()
{
    // Check if force feedback is available.
    DIDEVCAPS didcaps;
    didcaps.dwSize = sizeof(didcaps);
    // Get device capabilites.
    LPDIRECTINPUTDEVICE8 device = controller->device;
    HRESULT hr = device->GetCapabilities(&didcaps);
    if (hr != DI_OK){
        PrintLog("[DINPUT] [PAD%d] IsForceSupported: GetCapabilities returned HR = %X", controller->dwUserIndex + 1, hr);
        return false;
    }
    bool ffSupported = ((didcaps.dwFlags & DIDC_FORCEFEEDBACK) == DIDC_FORCEFEEDBACK);
    PrintLog("[DINPUT] [PAD%d] IsForceSupported: 0x%08X %s", controller->dwUserIndex + 1, didcaps.dwFlags, ffSupported == true ? "YES" : "NO");
    return ffSupported;
}

//-----------------------------------------------------------------------------
// Name: PrepareDeviceForces()
// Desc: Prepare force feedback effect.
//-----------------------------------------------------------------------------
HRESULT ForceFeedback::PrepareForceEjocys(u8 motor)
{
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    GUID effGuid = GUID_Sine;
    // Clear original effect values.
    pf = new DIPERIODIC;
    xForce = 0;
    yForce = 0;
    oldXForce = 0;
    oldYForce = 0;
    oldMagnitude = 0;
    oldPeriod = 0;
    is_created[motor] = false;

    // Enumerate effects.
    LPDIRECTINPUTDEVICE8 device = controller->device;
    device->EnumEffects(&EnumEffectsCallback, this, DIEFT_ALL);
    // Create structure that provides parameters for the effect.
    eff[motor] = new DIEFFECT;
    // Fills a block of memory with zeros.
    ZeroMemory(eff[motor], sizeof(DIEFFECT));
    eff[motor]->dwSize = sizeof(DIEFFECT);
    eff[motor]->dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    eff[motor]->cAxes = axisffbcount;
    eff[motor]->lpEnvelope = 0;
    eff[motor]->dwStartDelay = 0;
    eff[motor]->cbTypeSpecificParams = sizeof(DIPERIODIC);
    eff[motor]->dwDuration = INFINITE;
    eff[motor]->dwSamplePeriod = 0;
    eff[motor]->dwGain = DI_FFNOMINALMAX; // no scaling
    eff[motor]->dwTriggerButton = DIEB_NOTRIGGER;
    eff[motor]->dwTriggerRepeatInterval = 0;
    eff[motor]->rgdwAxes = rgdwAxes;
    eff[motor]->rglDirection = rglDirection;
    eff[motor]->lpvTypeSpecificParams = pf;

    // Create prepared effect.
    HRESULT hr = device->CreateEffect(effGuid, eff[motor], &effect[motor], NULL);
    if (FAILED(hr))
    {
        //DIERR_DEVICEFULL, DIERR_DEVICENOTREG, DIERR_INVALIDPARAM, DIERR_NOTINITIALIZED.
        PrintLog("[DINPUT] [PAD%d] PrepareForce (%d) failed with code HR = %X", controller->dwUserIndex + 1, motor, hr);
        return hr;
    }
    // If pointer to the IDirectInputEffect Interface is empty then return faield state.
    if (effect[motor] == NULL) return E_FAIL;
    //PrintLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,motor, DXErrStr(hr));
    return S_OK;
}

//-----------------------------------------------------------------------------
// Name: SetDeviceForces()
// Desc: Modify previously created force feedback effect.
//-----------------------------------------------------------------------------
HRESULT ForceFeedback::SetDeviceForcesEjocys(WORD force, u8 motor)
{
    PrintLog("[DINPUT] [PAD%d] [M%d] SetDeviceForcesEJocys: force = %d", controller->dwUserIndex + 1, motor, force);
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
        xForce = nForce;
        period = leftPeriod * 1000;
    }
    else
    {
        // Update right parameters.
        yForce = nForce;
        period = rightPeriod * 1000;
    }
    // Sine Wave: Duration, Gain, TriggerButton, Axes, Direction, Envelope, TypeSpecificParams, StartDelay, SamplePeriod
    HRESULT hr = S_OK;
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !1! HR = %s"), idx+1,motor, DXErrStr(hr));
    DWORD magnitude;
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !2! HR = %s"), idx+1,motor, DXErrStr(hr));
    // When modifying an effect you need only specify the parameters you are modifying
    // If only one force feedback actuator is available.
    if (axisffbcount == 1)
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3a! HR = %s"), idx+1,motor, DXErrStr(hr));
        magnitude = max(xForce, yForce);
        period = (leftPeriod * 1000 * xForce + rightPeriod * 1000 * yForce) / (xForce + yForce);
        // Apply only one direction and keep the direction at zero
        eff[motor]->rglDirection[0] = 0;
        eff[motor]->rglDirection[1] = 0;
    }
    else
    {
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! HR = %s"), idx+1,motor, DXErrStr(hr));
        magnitude = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);
        // Apply magnitude from both directions
        eff[motor]->rglDirection[0] = xForce;
        eff[motor]->rglDirection[1] = yForce;
        // lOffset - Offset of the effect. The range of forces generated by the effect is lOffset minus dwMagnitude to lOffset plus dwMagnitude. The value of the lOffset member is also the baseline for any envelope that is applied to the effect.
        // dwPhase - Position in the cycle of the periodic effect at which playback begins, in the range from 0 through 35,999. See Remarks.
    }
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !3b! axis = %d, x = %d, y = %d, m = %d"), idx+1,motor, Gamepad[idx].g_dwNumForceFeedbackAxis, Gamepad[idx].xForce, Gamepad[idx].yForce, magnitude);
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !6! HR = %s"), idx+1,motor, DXErrStr(hr));
    // dwMagnitude - Magnitude of the effect, in the range from 0 through 10,000. If an envelope is applied to this effect, the value represents the magnitude of the sustain. If no envelope is applied, the value represents the amplitude of the entire effect.
    pf->dwMagnitude = magnitude;
    // dwPeriod - Period of the effect, in microseconds.
    pf->dwPeriod = period;
    eff[motor]->lpvTypeSpecificParams = pf;

    if (oldMagnitude != pf->dwMagnitude ||
        oldPeriod != pf->dwPeriod ||
        oldXForce != xForce ||
        oldYForce != yForce)
    {
        oldMagnitude = pf->dwMagnitude;
        oldPeriod = pf->dwPeriod;
        oldXForce = xForce;
        oldYForce = yForce;
        //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) !7! HR = %s"), idx+1,motor, DXErrStr(hr));
        // Set the new parameters and start the effect immediately.
        hr = effect[motor]->SetParameters(eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
        if (FAILED(hr))
        {
            PrintLog("[DINPUT] [PAD%d] [M%d] SetDeviceForcesEJocys: failed with code HR = %X", controller->dwUserIndex + 1, motor, hr);
            return hr;
        };
    }
    //PrintLog(_T("[DINPUT]  [PAD%d] SetDeviceForces (%d) return HR = %s"), idx+1,motor, DXErrStr(hr));
    return hr;
}

BOOL ForceFeedback::EffectIsPlaying()
{
    DWORD state;
    LPDIRECTINPUTDEVICE8 device = controller->device;
    device->GetForceFeedbackState(&state);

    if (state & DIGFFS_STOPPED) return FALSE;

    return TRUE;
}

HRESULT ForceFeedback::PrepareForceNew(u8 motor)
{
    LONG rglDirection[2] = { 0, 0 };
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };

    pf = new DIPERIODIC;

    eff[motor] = new DIEFFECT;
    eff[motor]->dwSize = sizeof(DIEFFECT);
    eff[motor]->dwFlags = DIEFF_POLAR | DIEFF_OBJECTOFFSETS;
    eff[motor]->cAxes = axisffbcount;
    eff[motor]->lpEnvelope = 0;
    eff[motor]->dwStartDelay = 0;
    eff[motor]->cbTypeSpecificParams = sizeof(DIPERIODIC);

    // Enumerate effects
    LPDIRECTINPUTDEVICE8 device = controller->device;
    HRESULT hr = device->EnumEffects(&EnumEffectsCallback, this, DIEFT_ALL);
    if (FAILED(hr))
    {
        PrintLog("[PAD%d] EnumEffectsCallback failed");
    }

    eff[motor]->dwDuration = INFINITE;
    eff[motor]->dwSamplePeriod = 0;
    eff[motor]->dwGain = DI_FFNOMINALMAX;
    eff[motor]->dwTriggerButton = DIEB_NOTRIGGER;
    eff[motor]->dwTriggerRepeatInterval = 0;
    eff[motor]->rgdwAxes = rgdwAxes;
    eff[motor]->rglDirection = rglDirection;
    eff[motor]->lpvTypeSpecificParams = pf;

    // Create the prepared effect
    if (motor == FFB_LEFTMOTOR)
        hr = device->CreateEffect(GUID_SawtoothDown, eff[motor], &effect[motor], NULL);
    if (motor == FFB_RIGHTMOTOR)
        hr = device->CreateEffect(GUID_SawtoothUp, eff[motor], &effect[motor], NULL);

    if (FAILED(hr))
    {
        PrintLog("[PAD%d] PrepareForce (%d) failed with code HR = %X", controller->dwUserIndex + 1, motor, hr);
        return hr;
    }

    if (!effect[motor]) return E_FAIL;

    //PrintLog(_T("[DINPUT]  [PAD%d] PrepareForce (%d) HR = %s"), idx+1,motor, DXErrStr(hr));

    return S_OK;
}

HRESULT ForceFeedback::SetDeviceForcesNew(WORD force, u8 motor)
{
    HRESULT hr;

    if (!force) return effect[motor]->Stop();

    if (EffectIsPlaying()) effect[motor]->Stop();

    LONG nForce = MulDiv(force, DI_FFNOMINALMAX, 65535);
    nForce = clamp(nForce, -DI_FFNOMINALMAX, DI_FFNOMINALMAX);

    bool bMotor = swapmotor ? !motor != 0 : motor != 0;

    if (bMotor == FFB_LEFTMOTOR)
    {
        eff[motor]->dwDuration = leftPeriod * 1000;
        pf->dwMagnitude = (DWORD)nForce;
        pf->dwPeriod = leftPeriod * 1000;
    }

    if (bMotor == FFB_RIGHTMOTOR)
    {
        eff[motor]->dwDuration = rightPeriod * 1000;
        pf->dwMagnitude = (DWORD)nForce;
        pf->dwPeriod = rightPeriod * 1000;
    }

    eff[motor]->lpvTypeSpecificParams = pf;

    hr = effect[motor]->SetParameters(eff[motor], DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_DURATION | DIEP_SAMPLEPERIOD);
    if (FAILED(hr)) return hr;

    hr = effect[motor]->Start(INFINITE, DIES_SOLO);

    if (FAILED(hr)) return hr;

    return S_OK;
}
