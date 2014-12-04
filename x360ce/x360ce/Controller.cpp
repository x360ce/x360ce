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

std::vector<Controller> g_Controllers;

Controller::Controller(u32 user)
{
    m_pDevice = nullptr;
    m_pForceFeedback = nullptr;
    m_productid = GUID_NULL;
    m_instanceid = GUID_NULL;
    m_axiscount = 0;

    m_gamepadtype = 1;
    m_passthrough = false;

    m_user = user;
}

Controller::~Controller()
{
    delete m_pForceFeedback;

    if (m_pDevice)
    {
        m_pDevice->Unacquire();
        m_pDevice->Release();
    }
}

BOOL CALLBACK Controller::EnumObjectsCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef)
{
    Controller *gp = (Controller*)pvRef;

    if (lpddoi->dwType & DIDFT_AXIS)
    {
        DIPROPRANGE diprg;
        diprg.diph.dwSize = sizeof(DIPROPRANGE);
        diprg.diph.dwHeaderSize = sizeof(DIPROPHEADER);
        diprg.diph.dwHow = DIPH_BYID;
        diprg.diph.dwObj = lpddoi->dwType;
        diprg.lMin = -32768;
        diprg.lMax = +32767;

        if (FAILED(gp->m_pDevice->SetProperty(DIPROP_RANGE, &diprg.diph)))
            return DIENUM_STOP;

        gp->m_axiscount++;
    }

    return DIENUM_CONTINUE;
}

DWORD Controller::GetState(XINPUT_STATE* pState)
{
    HRESULT hr = E_FAIL;
    hr = UpdateState();

#if 0
    PrintLog("UpdateState %u %u", dwUserIndex, hr);
#endif

    if (FAILED(hr)) return
        ERROR_DEVICE_NOT_CONNECTED;

    pState->Gamepad.wButtons = 0;
    pState->Gamepad.bLeftTrigger = 0;
    pState->Gamepad.bRightTrigger = 0;
    pState->Gamepad.sThumbLX = 0;
    pState->Gamepad.sThumbLY = 0;
    pState->Gamepad.sThumbRX = 0;
    pState->Gamepad.sThumbRY = 0;

    // timestamp packet
    pState->dwPacketNumber = GetTickCount();

    // --- Map buttons ---
    if (ButtonPressed(m_mapping.guide))
        pState->Gamepad.wButtons |= 0x400;

    for (u32 i = 0; i < _countof(m_mapping.Button); ++i)
    {
        if (ButtonPressed(m_mapping.Button[i]))
            pState->Gamepad.wButtons |= buttonIDs[i];
    }

    // --- Map POV to the D-pad ---
    if (m_mapping.DpadPOV > 0 && m_mapping.PovIsButton == false)
    {
        //INT pov = POVState(m_mapping.DpadPOV,dwUserIndex,Gamepad[dwUserIndex].povrotation);

        int povdeg = m_state.rgdwPOV[m_mapping.DpadPOV - 1];
        if (povdeg >= 0)
        {
            // Up-left, up, up-right, up (at 360 degrees)
            if (IN_RANGE2(povdeg, m_mapping.pov[GAMEPAD_DPAD_LEFT] + 1, m_mapping.pov[GAMEPAD_DPAD_UP]) || IN_RANGE2(povdeg, 0, m_mapping.pov[GAMEPAD_DPAD_RIGHT] - 1))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

            // Up-right, right, down-right
            if (IN_RANGE(povdeg, 0, m_mapping.pov[GAMEPAD_DPAD_DOWN]))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

            // Down-right, down, down-left
            if (IN_RANGE(povdeg, m_mapping.pov[GAMEPAD_DPAD_RIGHT], m_mapping.pov[GAMEPAD_DPAD_LEFT]))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;

            // Down-left, left, up-left
            if (IN_RANGE(povdeg, m_mapping.pov[GAMEPAD_DPAD_DOWN], m_mapping.pov[GAMEPAD_DPAD_UP]))
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
        }
    }
    else if (m_mapping.PovIsButton == true)
    {
        for (int i = 0; i < _countof(m_mapping.pov); ++i)
        {
            if (ButtonPressed(m_mapping.pov[i]))
            {
                pState->Gamepad.wButtons |= povIDs[i];
            }
        }
    }

    // Created so we can refer to each axis with an ID
    s32 axis[] =
    {
        m_state.lX,
        m_state.lY,
        m_state.lZ,
        m_state.lRx,
        m_state.lRy,
        m_state.lRz
    };

    s32 slider[] =
    {
        m_state.rglSlider[0],
        m_state.rglSlider[1]
    };

    // --- Map triggers ---
    u8 *targetTrigger[] =
    {
        &pState->Gamepad.bLeftTrigger,
        &pState->Gamepad.bRightTrigger
    };

    for (u32 i = 0; i < _countof(m_mapping.Trigger); ++i)
    {
        MappingType triggerType = m_mapping.Trigger[i].type;

        if (triggerType == DIGITAL)
        {
            if (ButtonPressed(m_mapping.Trigger[i].id - 1))*(targetTrigger[i]) = 255;
        }
        else
        {
            s32 *values;

            switch (triggerType)
            {
                case AXIS:
                case HAXIS:
                case CBUT:
                    values = axis;
                    break;

                case SLIDER:
                case HSLIDER:
                    values = slider;
                    break;

                default:
                    values = axis;
                    break;
            }

            s32 v = 0;

            if (m_mapping.Trigger[i].id > 0) v = values[m_mapping.Trigger[i].id - 1];
            else if (m_mapping.Trigger[i].id < 0) v = -values[-m_mapping.Trigger[i].id - 1] - 1;

            /* FIXME: axis negative max should be -32768
            --- v is the full range (-32768 .. +32767) that should be projected to 0...255

            --- Full ranges
            AXIS:	(	0 to 255 from -32768 to 32767) using axis
            SLIDER:	(	0 to 255 from -32768 to 32767) using slider
            ------------------------------------------------------------------------------
            --- Half ranges
            HAXIS:	(	0 to 255 from 0 to 32767) using axis
            HSLIDER:	(	0 to 255 from 0 to 32767) using slider
            */

            s32 v2 = 0;
            s32 offset = 0;
            s32 scaling = 1;

            switch (triggerType)
            {
                // Full range
                case AXIS:
                case SLIDER:
                    scaling = 255;
                    offset = 32767;
                    break;

                    // Half range
                case HAXIS:
                case HSLIDER:
                case CBUT: // add /////////////////////////////////////////////////////////
                    scaling = 127;
                    offset = 0;
                    break;

                default:
                    scaling = 1;
                    offset = 0;
                    break;
            }

            //v2 = (v + offset) / scaling;
            // Add deadzones
            //*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, pController->triggerdz, 255);

            /////////////////////////////////////////////////////////////////////////////////////////
            if (triggerType == CBUT)
            {

                if (ButtonPressed(m_mapping.Trigger[0].but)
                    && ButtonPressed(m_mapping.Trigger[1].but))
                {
                    *(targetTrigger[0]) = 255;
                    *(targetTrigger[1]) = 255;
                }

                if (ButtonPressed(m_mapping.Trigger[0].but)
                    && !ButtonPressed(m_mapping.Trigger[1].but))
                {
                    v2 = (offset - v) / scaling;
                    *(targetTrigger[0]) = 255;
                    *(targetTrigger[1]) = 255 - (u8)deadzone(v2, 0, 255, m_mapping.Trigger[1].triggerdz, 255);
                }

                if (!ButtonPressed(m_mapping.Trigger[0].but)
                    && ButtonPressed(m_mapping.Trigger[1].but))
                {
                    v2 = (offset + v) / scaling;
                    *(targetTrigger[0]) = 255 - (u8)deadzone(v2, 0, 255, m_mapping.Trigger[0].triggerdz, 255);
                    *(targetTrigger[1]) = 255;
                }

                if (!ButtonPressed(m_mapping.Trigger[0].but)
                    && !ButtonPressed(m_mapping.Trigger[1].but))
                {
                    v2 = (offset + v) / scaling;
                    *(targetTrigger[i]) = (u8)deadzone(v2, 0, 255, m_mapping.Trigger[0].triggerdz, 255);
                }

            }
            else
            {
                v2 = (offset + v) / scaling;
                *(targetTrigger[i]) = (u8)deadzone(v2, 0, 255, m_mapping.Trigger[0].triggerdz, 255);
            }

            /////////////////////////////////////////////////////////////////////////////////////////
        }
    }

    // --- Map thumbsticks ---

    // Created so we can refer to each axis with an ID
    SHORT *targetAxis[4] =
    {
        &pState->Gamepad.sThumbLX,
        &pState->Gamepad.sThumbLY,
        &pState->Gamepad.sThumbRX,
        &pState->Gamepad.sThumbRY
    };

    for (u32 i = 0; i < _countof(m_mapping.Axis); ++i)
    {
        if (m_mapping.Axis[i].axistodpad == 0)
        {
            s32 *values = axis;

            // Analog input
            if (m_mapping.Axis[i].analogType == AXIS) values = axis;

            if (m_mapping.Axis[i].analogType == SLIDER) values = slider;

            if (m_mapping.Axis[i].analogType != NONE)
            {

                if (m_mapping.Axis[i].id > 0)
                {
                    s32 val = (s32)values[m_mapping.Axis[i].id - 1];
                    *(targetAxis[i]) = (s16)clamp(val, -32768, 32767);
                }
                else if (m_mapping.Axis[i].id < 0)
                {
                    s32 val = (s32)(-1 - values[-m_mapping.Axis[i].id - 1]);
                    *(targetAxis[i]) = (s16)clamp(val, -32768, 32767);
                }
            }

            // Digital input, positive direction
            if (m_mapping.Axis[i].hasDigital && m_mapping.Axis[i].positiveButtonID >= 0)
            {
                if (ButtonPressed(m_mapping.Axis[i].positiveButtonID))
                    *(targetAxis[i]) = 32767;
            }

            // Digital input, negative direction
            if (m_mapping.Axis[i].hasDigital && m_mapping.Axis[i].negativeButtonID >= 0)
            {
                if (ButtonPressed(m_mapping.Axis[i].negativeButtonID))
                    *(targetAxis[i]) = -32768;
            }
        }
        else
        {
            //PrintLog("x: %d, y: %d, z: %d",Gamepad[dwUserIndex].state.lX,Gamepad[dwUserIndex].state.lY,Gamepad[dwUserIndex].state.lZ);

            if (m_state.lX - m_mapping.Axis[i].a2doffset > m_mapping.Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;

            if (m_state.lX - m_mapping.Axis[i].a2doffset < -m_mapping.Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

            if (m_state.lY - m_mapping.Axis[i].a2doffset < -m_mapping.Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

            if (m_state.lY - m_mapping.Axis[i].a2doffset > m_mapping.Axis[i].a2ddeadzone)
                pState->Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
        }

        //        [ 32768 steps | 32768 steps ]
        // DInput [ 0     32767 | 32768 65535 ] 
        // XInput [ 32768    -1 | 0     32767 ]
        //
        //int xInput = dInputValue;
        s32 xInput = *(targetAxis[i]);
        s32 deadZone = (s32)m_mapping.Axis[i].axisdeadzone;
        s32 antiDeadZone = (s32)m_mapping.Axis[i].antideadzone;
        s32 linear = (s32)m_mapping.Axis[i].axislinear;
        s32 min = -32768;
        s32 max = 32767;
        // If deadzone value is set then...
        bool invert = xInput < 0;
        // Convert [-32768;-1] -> [32767;0]
        if (invert) xInput = -1 - xInput;
        //if  invert 
        if (deadZone > 0)
        {
            if (xInput > deadZone)
            {
                // [deadZone;32767] => [0;32767];
                xInput = (s32)((float)(xInput - deadZone) / (float)(max - deadZone) * (float)max);
            }
            else
            {
                xInput = 0;
            }
        }
        // If anti-deadzone value is set then...
        if (antiDeadZone > 0)
        {
            if (xInput > 0)
            {
                // [0;32767] => [antiDeadZone;32767];
                xInput = (s32)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
            }
        }
        // If linear value is set then...
        if (linear != 0 && xInput > 0)
        {
            // [antiDeadZone;32767] => [0;32767];
            float xInputF = (float)(xInput - antiDeadZone) / (float)(max - antiDeadZone) * (float)max;
            float linearF = (float)linear / 100.f;
            xInputF = ConvertToFloat((short)xInputF);
            float x = -xInputF;
            if (linearF < 0.f) x = 1.f + x;
            float v = ((float)sqrt(1.f - x * x));
            if (linearF < 0.f) v = 1.f - v;
            xInputF = xInputF + (2.f - v - xInputF - 1.f) * abs(linearF);
            xInput = ConvertToShort(xInputF);
            // [0;32767] => [antiDeadZone;32767];
            xInput = (s32)((float)(xInput) / (float)max * (float)(max - antiDeadZone) + antiDeadZone);
        }
        // Convert [32767;0] -> [-32768;-1]
        if (invert) xInput = -1 - xInput;
        *(targetAxis[i]) = (s16)clamp(xInput, min, max);
        //return (short)xInput;
    }

    return ERROR_SUCCESS;
}

HRESULT Controller::UpdateState()
{
    HRESULT hr = E_FAIL;

    if ((!m_pDevice)) return E_FAIL;

    m_pDevice->Poll();
    hr = m_pDevice->GetDeviceState(sizeof(DIJOYSTATE2), &m_state);

    if (FAILED(hr))
    {
        PrintLog("[PAD%d] Device Reacquired", m_user + 1);
        hr = m_pDevice->Acquire();
    }

    return hr;
}

DWORD Controller::CreateDevice()
{
    DIPROPDWORD dipdw;
    HRESULT hr = E_FAIL;

    LockGuard lock(m_mutex);

    bool bHookDI = g_iHook.GetState(InputHook::HOOK_DI);
    bool bHookSA = g_iHook.GetState(InputHook::HOOK_SA);

    if (bHookDI) g_iHook.DisableHook(InputHook::HOOK_DI);
    if (bHookSA) g_iHook.DisableHook(InputHook::HOOK_SA);

    hr = DirectInputManager::Get().GetDirectInput()->CreateDevice(m_instanceid, &m_pDevice, NULL);
    if (FAILED(hr))
    {
        std::string strInstance;
        if (GUIDtoString(&strInstance, m_instanceid))
            PrintLog("[PAD%d] InstanceGUID %s is incorrect trying ProductGUID", m_user + 1, strInstance.c_str());

        hr = DirectInputManager::Get().GetDirectInput()->CreateDevice(m_productid, &m_pDevice, NULL);
        if (FAILED(hr))
            return ERROR_DEVICE_NOT_CONNECTED;
    }

    if (!m_pDevice)
        return ERROR_DEVICE_NOT_CONNECTED;
    else
        PrintLog("[PAD%d] Device created", m_user + 1);

    hr = m_pDevice->SetDataFormat(&c_dfDIJoystick2);
    if (FAILED(hr)) PrintLog("[PAD%d] SetDataFormat failed with code HR = %X", m_user + 1, hr);

    HRESULT setCooperativeLevelResult = m_pDevice->SetCooperativeLevel(DirectInputManager::Get().GetWindow(), DISCL_EXCLUSIVE | DISCL_BACKGROUND);
    if (FAILED(setCooperativeLevelResult))
    {
        m_useforce = false;
        PrintLog("Cannot get exclusive device access, disabling ForceFeedback");

        setCooperativeLevelResult = m_pDevice->SetCooperativeLevel(DirectInputManager::Get().GetWindow(), DISCL_NONEXCLUSIVE | DISCL_BACKGROUND);
        if (FAILED(setCooperativeLevelResult)) PrintLog("[PAD%d] SetCooperativeLevel failed with code HR = %X", m_user + 1, setCooperativeLevelResult);
    }

    dipdw.diph.dwSize = sizeof(DIPROPDWORD);
    dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    dipdw.diph.dwObj = 0;
    dipdw.diph.dwHow = DIPH_DEVICE;
    dipdw.dwData = DIPROPAUTOCENTER_OFF;
    m_pDevice->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);

    hr = m_pDevice->EnumObjects(EnumObjectsCallback, (VOID*)this, DIDFT_AXIS);
    if (FAILED(hr))
        PrintLog("[PAD%d] EnumObjects failed with code HR = %X", m_user + 1, hr);
    else
        PrintLog("[PAD%d] Detected axis count: %d", m_user + 1, m_axiscount);

    if (m_pForceFeedback && m_useforce)
        m_useforce = m_pForceFeedback->IsSupported();

    if (!m_useforce)
        delete m_pForceFeedback;

    hr = m_pDevice->Acquire();

    if (bHookSA) g_iHook.EnableHook(InputHook::HOOK_SA);
    if (bHookDI) g_iHook.EnableHook(InputHook::HOOK_DI);

    if (SUCCEEDED(hr))
        return ERROR_SUCCESS;

    return ERROR_DEVICE_NOT_CONNECTED;
}

bool Controller::ButtonPressed(u32 buttonidx)
{
    return (buttonidx != INVALIDBUTTONINDEX) ? (m_state.rgbButtons[buttonidx] & 0x80) != 0 : 0;
}

ForceFeedback::ForceFeedback(Controller* pController) :
m_pController(pController)
{
    m_Axes = 0;
    m_LeftPeriod = 0;
    m_RightPeriod = 0;
    m_ForcePercent = 100;
    m_Type = 0;
    m_Caps.ConstantForce = false;
    m_Caps.PeriodicForce = false;
    m_Caps.RampForce = false;
};

ForceFeedback::~ForceFeedback()
{
    if (m_pController->m_pDevice)
        m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_RESET);

    for (auto it = m_effects.begin(); it != m_effects.end(); ++it)
    {
        (*it)->Release();
    }
}

BOOL CALLBACK ForceFeedback::EnumFFAxesCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef)
{
    DWORD* pdwNumForceFeedbackAxis = (DWORD*)pvRef;

    if (((lpddoi->dwFlags & DIDOI_FFACTUATOR) != 0))
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

bool ForceFeedback::IsSupported()
{
    DIDEVCAPS deviceCaps;
    deviceCaps.dwSize = sizeof(DIDEVCAPS);

    HRESULT hr = m_pController->m_pDevice->GetCapabilities(&deviceCaps);
    if (hr != DI_OK)
    {
        PrintLog("[DINPUT] [PAD%d] IsForceSupported: GetCapabilities returned HR = %X", m_pController->m_user + 1, hr);
        return false;
    }
    bool ffSupported = ((deviceCaps.dwFlags & DIDC_FORCEFEEDBACK) == DIDC_FORCEFEEDBACK);
    PrintLog("[DINPUT] [PAD%d] IsForceSupported: 0x%08X %s", m_pController->m_user + 1, deviceCaps.dwFlags, ffSupported == true ? "YES" : "NO");

    if (ffSupported)
    {
        HRESULT hr = m_pController->m_pDevice->EnumObjects(EnumFFAxesCallback, (VOID*)&m_Axes, DIDFT_AXIS);
        if (FAILED(hr)) PrintLog("[PAD%d] EnumFFAxesCallback failed with code HR = %X", m_pController->m_user + 1, hr);

        if (m_Axes > 2)
            m_Axes = 2;

        if (!m_Axes)
        {
            PrintLog("ForceFeedback unsupported");
            m_pController->m_useforce = false;
            return false;
        }
        else
        {
            m_pController->m_pDevice->EnumEffects(EnumEffectsCallback, this, DIEFT_ALL);
            m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_RESET);
            m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_SETACTUATORSON);
            return true;
        }
    }
    return false;
}

bool ForceFeedback::SetState(XINPUT_VIBRATION* pVibration)
{
    switch (m_Type)
    {
        case 1:
            return SetDeviceForcesEjocys(pVibration);
        case 2:
            return SetDeviceForcesNew(pVibration);
        default:
            return SetDeviceForcesFailsafe(pVibration);
    }
}

void ForceFeedback::StartEffects(DIEFFECT* effectType)
{
    for (auto it = m_effects.begin(); it != m_effects.end(); ++it)
    {
        u32 flags = DIEP_START;
        if (effectType->cAxes && effectType->rgdwAxes && !effectType->rglDirection) flags |= DIEP_AXES;
        if (effectType->cAxes && effectType->rglDirection) flags |= DIEP_DIRECTION;
        if (effectType->dwDuration) flags |= DIEP_DURATION;
        if (effectType->lpEnvelope) flags |= DIEP_ENVELOPE;
        if (effectType->dwGain) flags |= DIEP_GAIN;
        if (effectType->dwSamplePeriod) flags |= DIEP_SAMPLEPERIOD;
        if (effectType->dwStartDelay) flags |= DIEP_STARTDELAY;
        if (effectType->dwTriggerButton != DIEB_NOTRIGGER) flags |= DIEP_TRIGGERBUTTON;
        if (effectType->dwTriggerRepeatInterval) flags |= DIEP_TRIGGERREPEATINTERVAL;
        if (effectType->lpvTypeSpecificParams) flags |= DIEP_TYPESPECIFICPARAMS;

        (*it)->SetParameters(effectType, flags);
    }
}

bool ForceFeedback::SetDeviceForcesFailsafe(XINPUT_VIBRATION* pVibration)
{
    DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };

    //As we cannot properly emulate 2 separate motor force in DirectInput we should try to combine forces;
    u32 force = (int)((pVibration->wLeftMotorSpeed + pVibration->wRightMotorSpeed) * m_ForcePercent);
    force = MulDiv(force, DI_FFNOMINALMAX, UINT16_MAX * 2);
    force = clamp(force, 0, DI_FFNOMINALMAX);

    DICONSTANTFORCE contantForce;
    contantForce.lMagnitude = force;

    // PrintLog("=========== %u", contantForce.lMagnitude);

    DIEFFECT effectType;
    ZeroMemory(&effectType, sizeof(DIEFFECT));

    effectType.dwSize = sizeof(DIEFFECT);
    effectType.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effectType.dwDuration = INFINITE;
    effectType.dwSamplePeriod = 0;
    effectType.dwGain = DI_FFNOMINALMAX;
    effectType.dwTriggerButton = DIEB_NOTRIGGER;
    effectType.dwTriggerRepeatInterval = 0;
    effectType.cAxes = m_Axes;
    effectType.rgdwAxes = rgdwAxes;
    effectType.rglDirection = rglDirection;
    effectType.lpEnvelope = 0;
    effectType.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
    effectType.lpvTypeSpecificParams = &contantForce;
    effectType.dwStartDelay = 0;

    // we want only one effect for now
    if (m_effects.size() < 1)
    {
        LPDIRECTINPUTEFFECT effect;
        HRESULT hr = m_pController->m_pDevice->CreateEffect(GUID_ConstantForce, &effectType, &effect, NULL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] CreateEffect failed with code HR = %X, FFBType = %u", m_pController->m_user + 1, hr, m_Type);
            return false;
        }
        else
        {
            m_effects.push_back(effect);
        }
    }

    StartEffects(&effectType);
    return true;
}

bool ForceFeedback::SetDeviceForcesEjocys(XINPUT_VIBRATION* pVibration)
{
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };

    DIPERIODIC periodicForce;
    ZeroMemory(&periodicForce, sizeof(DIPERIODIC));

    DIEFFECT effectType;
    ZeroMemory(&effectType, sizeof(DIEFFECT));

    effectType.dwSize = sizeof(DIEFFECT);
    effectType.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effectType.cAxes = m_Axes;
    effectType.lpEnvelope = 0;
    effectType.dwStartDelay = 0;
    effectType.cbTypeSpecificParams = sizeof(DIPERIODIC);
    effectType.dwDuration = INFINITE;
    effectType.dwSamplePeriod = 0;
    effectType.dwGain = DI_FFNOMINALMAX;
    effectType.dwTriggerButton = DIEB_NOTRIGGER;
    effectType.dwTriggerRepeatInterval = 0;
    effectType.rgdwAxes = rgdwAxes;
    effectType.rglDirection = rglDirection;
    effectType.lpvTypeSpecificParams = &periodicForce;

    // NOTE: This will not work as expected, because both motors can have speeds at once.
    if (pVibration->wLeftMotorSpeed)
        periodicForce.dwPeriod = m_LeftPeriod * 1000;
    else
        periodicForce.dwPeriod = m_RightPeriod * 1000;

    //As we cannot properly emulate 2 separate motor force in DirectInput we should try to combine forces;
    u32 force = (int)((pVibration->wLeftMotorSpeed + pVibration->wRightMotorSpeed) * m_ForcePercent);
    force = MulDiv(force, DI_FFNOMINALMAX, UINT16_MAX * 2);
    force = clamp(force, 0, DI_FFNOMINALMAX);

    // PrintLog("=========== %u", force);

    if (m_Axes == 1)
    {
        periodicForce.dwMagnitude = std::max(pVibration->wLeftMotorSpeed, pVibration->wRightMotorSpeed);
        periodicForce.dwPeriod = (m_LeftPeriod * 1000 * pVibration->wLeftMotorSpeed + m_RightPeriod * 1000 * pVibration->wRightMotorSpeed) /
            (pVibration->wLeftMotorSpeed + pVibration->wRightMotorSpeed);
        effectType.rglDirection[0] = 0;
        effectType.rglDirection[1] = 0;
    }
    else
    {
        periodicForce.dwMagnitude = force;
        effectType.rglDirection[0] = pVibration->wLeftMotorSpeed;
        effectType.rglDirection[1] = pVibration->wRightMotorSpeed;
    }

    // we want only one effect for now
    if (m_effects.size() < 1)
    {
        LPDIRECTINPUTEFFECT effect;
        HRESULT hr = m_pController->m_pDevice->CreateEffect(GUID_Sine, &effectType, &effect, NULL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] CreateEffect failed with code HR = %X, FFBType = %u", m_pController->m_user + 1, hr, m_Type);
            return false;
        }
        else
        {
            m_effects.push_back(effect);
        }
    }

    StartEffects(&effectType);
    return true;
}

bool ForceFeedback::SetDeviceForcesNew(XINPUT_VIBRATION* pVibration)
{
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };

    DIPERIODIC periodicForce;
    ZeroMemory(&periodicForce, sizeof(DIPERIODIC));

    DIEFFECT effectType;
    ZeroMemory(&effectType, sizeof(DIEFFECT));

    effectType.dwSize = sizeof(DIEFFECT);
    effectType.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effectType.cAxes = m_Axes;
    effectType.lpEnvelope = 0;
    effectType.dwStartDelay = 0;
    effectType.cbTypeSpecificParams = sizeof(DIPERIODIC);
    effectType.dwDuration = 0;
    effectType.dwSamplePeriod = 0;
    effectType.dwGain = DI_FFNOMINALMAX; // no scaling
    effectType.dwTriggerButton = DIEB_NOTRIGGER;
    effectType.dwTriggerRepeatInterval = 0;
    effectType.rgdwAxes = rgdwAxes;
    effectType.rglDirection = rglDirection;
    effectType.lpvTypeSpecificParams = &periodicForce;

    //As we cannot properly emulate 2 separate motor force in DirectInput we should try to combine forces;
    u32 force = (int)((pVibration->wLeftMotorSpeed + pVibration->wRightMotorSpeed) * m_ForcePercent);
    force = MulDiv(force, DI_FFNOMINALMAX, UINT16_MAX * 2);
    force = clamp(force, 0, DI_FFNOMINALMAX);

    periodicForce.dwMagnitude = force;

    GUID effectGUID;
    if (pVibration->wLeftMotorSpeed)
    {
        effectGUID = GUID_SawtoothDown;
        effectType.dwDuration = m_LeftPeriod * 1000;
        periodicForce.dwPeriod = m_LeftPeriod * 1000;
    }
    else
    {
        effectGUID = GUID_SawtoothUp;
        effectType.dwDuration = m_LeftPeriod * 1000;
        periodicForce.dwPeriod = m_RightPeriod * 1000;
    }

    // we want only one effect for now
    if (m_effects.size() < 1)
    {
        LPDIRECTINPUTEFFECT effect;
        HRESULT hr = m_pController->m_pDevice->CreateEffect(effectGUID, &effectType, &effect, NULL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] CreateEffect failed with code HR = %X, FFBType = %u", m_pController->m_user + 1, hr, m_Type);
            return false;
        }
        else
        {
            m_effects.push_back(effect);
        }
    }

    StartEffects(&effectType);
    return true;
}
