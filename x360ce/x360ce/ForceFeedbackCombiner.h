#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"
#include "ForceFeedbackBase.h"

class ForceFeedbackCombiner : public ForceFeedbackBase
{
	friend class ControllerCombiner;
public:
	ForceFeedbackCombiner(ControllerCombiner* pController);
	~ForceFeedbackCombiner();

	virtual bool IsSupported();

	virtual bool SetState(XINPUT_VIBRATION* pVibration);

	virtual void Shutdown();

private:
	ControllerCombiner* m_Controller;
};
