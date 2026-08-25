#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

struct ForceFeedbackCaps
{
	bool ConstantForce;
	bool PeriodicForce;
	bool RampForce;
};

class ForceFeedbackBase
{
public:
	ForceFeedbackBase() {}
	virtual ~ForceFeedbackBase() {}

	virtual bool IsSupported() = 0;

	virtual bool SetState(XINPUT_VIBRATION* pVibration) = 0;

	virtual void Shutdown() = 0;

	u32 m_OveralStrength;
	u8 m_Type;
	// Left Motor.
	u32 m_LeftPeriod;
	u32 m_LeftStrength;
	LONG m_LeftDirection;
	BOOL m_LeftRestartEffect = true;
	// Right Motor.
	u32 m_RightPeriod;
	u32 m_RightStrength;
	LONG m_RightDirection;
	BOOL m_RightRestartEffect = true;
	// Create X effect.
	LPDIRECTINPUTEFFECT effectX;
	// Create Y effect.
	LPDIRECTINPUTEFFECT effectY;

	bool m_SwapMotors;

};
