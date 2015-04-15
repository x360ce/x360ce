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

	std::vector<std::shared_ptr<Controller>>& GetControllers();
	virtual DWORD GetState(XINPUT_STATE* pState);
	virtual DWORD CreateDevice();
	virtual bool Initalized();

private:
	std::vector<std::shared_ptr<Controller>> m_controllers;
	ForceFeedbackCombiner m_ForceFeedbackInst;
};