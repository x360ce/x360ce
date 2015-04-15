#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#include "ControllerBase.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedbackCombiner.h"

class ControllerCombiner : public ControllerBase
{
public:
	ControllerCombiner(u32 user);
	~ControllerCombiner();

	virtual DWORD GetState(XINPUT_STATE* pState);
	virtual DWORD CreateDevice();
	virtual bool Initalized() const;

private:
	ForceFeedbackCombiner m_ForceFeedbackInst;
};