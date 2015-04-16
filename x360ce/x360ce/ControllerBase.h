#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"

class ControllerBase
{
public:
	ControllerBase() { }
	virtual ~ControllerBase() {}

	virtual DWORD GetState(XINPUT_STATE* pState) = 0;
	virtual DWORD CreateDevice() = 0;

	virtual bool Initalized() = 0;

	Config::Mapping m_mapping;
	u32 m_user;
	u32 m_axiscount;
	u8 m_gamepadtype;
	bool m_passthrough;
	bool m_forcespassthrough;
	bool m_useforce;

	bool m_combined = false;
	u32 m_combinedIndex = 0;

	ForceFeedbackBase* m_ForceFeedback;

	u32 m_failcount;
};