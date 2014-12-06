#include "stdafx.h"

#include "Common.h"
#include "InputHook.h"
#include "Config.h"

#include "ControllerManager.h"
#include "Controller.h"
#include "ForceFeedback.h"

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
    if (m_pController->IsBrokenFFD()) return;

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
    if (!ControllerManager::Get().XInputEnabled())
    {
        // Clear state
        if (pVibration) ZeroMemory(pVibration, sizeof(XINPUT_VIBRATION));
        return ERROR_SUCCESS;
    }

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
