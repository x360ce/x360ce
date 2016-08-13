#include "stdafx.h"

#include "Common.h"
#include "InputHook.h"
#include "Config.h"

#include "ControllerManager.h"
#include "ControllerBase.h"
#include "Controller.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"

#include "XInputModuleManager.h"

ForceFeedback::ForceFeedback(Controller* pController)
{
	m_pController = pController;
	m_Axes = 0;
	m_Type = 0;
	m_Caps.ConstantForce = false;
	m_Caps.PeriodicForce = false;
	m_Caps.RampForce = false;
	m_OveralStrength = DI_FFNOMINALMAX;
	// Left Motor.
	m_LeftPeriod = 60;
	m_LeftStrength = DI_FFNOMINALMAX;
	m_LeftDirection = 0;
	// Right Motor.
	m_RightPeriod = 120;
	m_RightStrength = DI_FFNOMINALMAX;
	m_RightDirection = 0;
};

ForceFeedback::~ForceFeedback()
{
}

void ForceFeedback::Shutdown()
{
	if ((m_pController->m_useforce) && (!m_pController->m_forcespassthrough) && (m_pController->m_pDevice))
	{
		m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_SETACTUATORSOFF);
		m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_STOPALL); // DISFFC_RESET or DISFFC_STOPALL
		m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_RESET); // DISFFC_RESET or DISFFC_STOPALL
	}

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
	if (m_pController->m_forcespassthrough)
		return true;

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
		HRESULT hr2 = m_pController->m_pDevice->EnumObjects(EnumFFAxesCallback, (VOID*)&m_Axes, DIDFT_AXIS);
		if (FAILED(hr2)) PrintLog("[PAD%d] EnumFFAxesCallback failed with code HR = %X", m_pController->m_user + 1, hr2);

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

	// Forces pass through mode?
	if (m_pController->m_forcespassthrough)
	{
		DWORD result = XInputModuleManager::Get().XInputSetState(m_pController->m_passthroughindex, pVibration);
		return !FAILED(result);
	}
	else
	{
		return SetDeviceForces(pVibration, m_Type);
	}
}

void ForceFeedback::StartEffects(DIEFFECT* diEffect, LPDIRECTINPUTEFFECT* effect, BOOL restartEffect)
{
	u32 flags = restartEffect ? DIEP_START : DIEP_NORESTART;
	if (diEffect->cAxes && diEffect->rgdwAxes && !diEffect->rglDirection) flags |= DIEP_AXES;
	if (diEffect->cAxes && diEffect->rglDirection) flags |= DIEP_DIRECTION;
	if (diEffect->dwDuration) flags |= DIEP_DURATION;
	if (diEffect->lpEnvelope) flags |= DIEP_ENVELOPE;
	if (diEffect->dwGain) flags |= DIEP_GAIN;
	if (diEffect->dwSamplePeriod) flags |= DIEP_SAMPLEPERIOD;
	if (diEffect->dwStartDelay) flags |= DIEP_STARTDELAY;
	if (diEffect->dwTriggerButton != DIEB_NOTRIGGER) flags |= DIEP_TRIGGERBUTTON;
	if (diEffect->dwTriggerRepeatInterval) flags |= DIEP_TRIGGERREPEATINTERVAL;
	if (diEffect->lpvTypeSpecificParams) flags |= DIEP_TYPESPECIFICPARAMS;
	(*effect)->SetParameters(diEffect, flags);
}

bool ForceFeedback::SetDeviceForces(XINPUT_VIBRATION* pVibration, u8 forceType)
{
	u32 leftSpeed = pVibration->wLeftMotorSpeed;
	u32 rightSpeed = pVibration->wRightMotorSpeed;

	u32 leftPeriod = m_LeftPeriod * 1000;
	u32 rightPeriod = m_RightPeriod * 1000;

	// Combine strengths into magnitude.
	u32 leftMagnitude = MulDiv(leftSpeed, DI_FFNOMINALMAX, UINT16_MAX);
	u32 rightMagnitude = MulDiv(rightSpeed, DI_FFNOMINALMAX, UINT16_MAX);

	u32 leftMagnitudeAdjusted = MulDiv(leftMagnitude, m_LeftStrength, DI_FFNOMINALMAX);
	u32 rightMagnitudeAdjusted = MulDiv(rightMagnitude, m_RightStrength, DI_FFNOMINALMAX);

	// Parameters for created effect.
	DIEFFECT diEffectX;
	ZeroMemory(&diEffectX, sizeof(DIEFFECT));

	// Parameters for created effect.
	DIEFFECT diEffectY;
	ZeroMemory(&diEffectY, sizeof(DIEFFECT));

	DIPERIODIC periodicForceX;
	ZeroMemory(&periodicForceX, sizeof(DIPERIODIC));

	DIPERIODIC periodicForceY;
	ZeroMemory(&periodicForceY, sizeof(DIPERIODIC));

	DICONSTANTFORCE constantForceX;
	ZeroMemory(&constantForceX, sizeof(DICONSTANTFORCE));

	DICONSTANTFORCE constantForceY;
	ZeroMemory(&constantForceY, sizeof(DICONSTANTFORCE));

	// Right-handed Cartesian direction:
	// x: -1 = left,     1 = right,   0 - no direction
	// y: -1 = backward, 1 = forward, 0 - no direction
	// z: -1 = down,     1 = up,      0 - no direction

	LONG     lZeroX = m_LeftDirection;
	LONG     lZeroY = m_RightDirection;
	DWORD    dwAxisX = DIJOFS_X;
	DWORD    dwAxisY = DIJOFS_Y;

	// Left motor.
	diEffectX.dwSize = sizeof(DIEFFECT);
	diEffectX.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
	diEffectX.lpEnvelope = 0;
	diEffectX.dwStartDelay = 0;
	diEffectX.dwDuration = INFINITE;
	diEffectX.dwSamplePeriod = 0;
	diEffectX.dwGain = m_OveralStrength;
	diEffectX.dwTriggerButton = DIEB_NOTRIGGER;
	diEffectX.dwTriggerRepeatInterval = 0;
	diEffectX.rgdwAxes = &dwAxisX;
	diEffectX.rglDirection = &lZeroX;
	diEffectX.cAxes = 1;
	if (m_Axes > 1)
	{
		// Right motor.
		diEffectY.rgdwAxes = &dwAxisY;
		diEffectY.rglDirection = &lZeroY;
		diEffectY.cAxes = 1;
		diEffectY.dwSize = sizeof(DIEFFECT);
		diEffectY.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
		diEffectY.lpEnvelope = 0;
		diEffectY.dwStartDelay = 0;
		diEffectY.dwDuration = INFINITE;
		diEffectY.dwSamplePeriod = 0;
		diEffectY.dwGain = m_OveralStrength;
		diEffectY.dwTriggerButton = DIEB_NOTRIGGER;
		diEffectY.dwTriggerRepeatInterval = 0;
	}
	GUID GUID_Force;

	// If device have only one force fedback actuator (probably wheel).
	if (m_Axes == 1)
	{
		// Forces must be combined.
		u32 combinedMagnitudeAdjusted = std::max(leftMagnitudeAdjusted, rightMagnitudeAdjusted);
		u32 combinedPeriod = 0;
		// If at least one speed is specified then...
		if (leftMagnitudeAdjusted > 0 || rightMagnitudeAdjusted > 0)
		{
			combinedPeriod = 1000 *
				((m_LeftPeriod * leftMagnitudeAdjusted) + (m_RightPeriod * rightMagnitudeAdjusted))
				/ (leftMagnitudeAdjusted + rightMagnitudeAdjusted);
		}
		leftMagnitudeAdjusted = combinedMagnitudeAdjusted;
		leftPeriod = combinedPeriod;
	}

	// 1 - Periodic 'Sine Wave'.
	// 2 - Periodic 'Sawtooth Down Wave'.
	if (forceType == 1 || forceType == 2)
	{
		GUID_Force = forceType == 1 ? GUID_Sine : GUID_SawtoothDown;
		// Left motor.
		periodicForceX.dwMagnitude = leftMagnitudeAdjusted;
		periodicForceX.dwPeriod = leftPeriod;
		diEffectX.cbTypeSpecificParams = sizeof(DIPERIODIC);
		diEffectX.lpvTypeSpecificParams = &periodicForceX;
		if (m_Axes > 1)
		{
			// Right motor.
			periodicForceY.dwMagnitude = rightMagnitudeAdjusted;
			periodicForceY.dwPeriod = rightPeriod;
			diEffectY.cbTypeSpecificParams = sizeof(DIPERIODIC);
			diEffectY.lpvTypeSpecificParams = &periodicForceY;
		}
	}
	// Constant.
	else
	{
		GUID_Force = GUID_ConstantForce;
		// Left motor.
		constantForceX.lMagnitude = leftMagnitudeAdjusted;
		diEffectX.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
		diEffectX.lpvTypeSpecificParams = &constantForceX;
		if (m_Axes > 1)
		{
			// Right motor.
			constantForceY.lMagnitude = rightMagnitudeAdjusted;
			diEffectY.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
			diEffectY.lpvTypeSpecificParams = &constantForceY;
		}
	}

	PrintLog("Type %d Axes %d OMag %d LSpeed %d RSpeed %d LMag %d RMag %d LPeriod %d RPeriod", forceType, m_Axes, m_OveralStrength, leftSpeed, rightSpeed, leftMagnitudeAdjusted, rightMagnitudeAdjusted, leftPeriod, rightPeriod);

	// If no effects exists then...
	if (m_effects.size() == 0)
	{
		m_LeftRestartEffect = true;
		// Left motor.
		HRESULT hrX = m_pController->m_pDevice->CreateEffect(GUID_Force, &diEffectX, &effectX, NULL);
		if (FAILED(hrX))
		{
			PrintLog("[PAD%d] CreateEffect failed with code HR = %X, FFBType = %u", m_pController->m_user + 1, hrX, m_Type);
			return false;
		}
		else
		{
			PrintLog("[PAD%d] CreateEffect succeded effectIndex %d", m_pController->m_user + 1, 1);
			m_effects.push_back(effectX);
		}
		if (m_Axes > 1)
		{
			m_RightRestartEffect = true;
			// Right motor.
			HRESULT hrY = m_pController->m_pDevice->CreateEffect(GUID_Force, &diEffectY, &effectY, NULL);
			if (FAILED(hrY))
			{
				PrintLog("[PAD%d] CreateEffect failed with code HR = %X, FFBType = %u", m_pController->m_user + 1, hrY, m_Type);
				return false;
			}
			else
			{
				PrintLog("[PAD%d] CreateEffect succeded effectIndex %d", m_pController->m_user + 1, 1);
				m_effects.push_back(effectY);
			}
		}
	}
	StartEffects(&diEffectX, &effectX, m_LeftRestartEffect);
	if (m_Axes > 1)
	{
		StartEffects(&diEffectY, &effectY, m_RightRestartEffect);
		// Restart left motorr effect next time if it was stopped.
		m_LeftRestartEffect = (leftSpeed == 0);
		// Restart right motor effect next time if it was stopped.
		m_RightRestartEffect = (rightSpeed == 0);
	}
	else
	{
		// Restart combined effect if it was stopped.
		m_LeftRestartEffect = (leftMagnitudeAdjusted == 0);
	}

	return true;
}

