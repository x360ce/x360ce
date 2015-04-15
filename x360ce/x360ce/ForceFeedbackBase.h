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
	ForceFeedbackBase(){}
	virtual ~ForceFeedbackBase(){}

	virtual bool IsSupported() = 0;

	virtual bool SetState(XINPUT_VIBRATION* pVibration) = 0;

	virtual void Shutdown() = 0;

	u32 m_LeftPeriod;
	u32 m_RightPeriod;
	float m_ForcePercent;
	u8 m_Type;
	bool m_SwapMotors;

};
