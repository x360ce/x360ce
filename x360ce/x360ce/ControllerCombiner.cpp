#include "stdafx.h"

#include "Common.h"
#include "InputHook.h"

#include "Config.h"
#include "InputHookManager.h"
#include "ControllerManager.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"
#include "ControllerBase.h"
#include "ControllerCombiner.h"

ControllerCombiner::ControllerCombiner(u32 user) :
m_ForceFeedbackInst(this)
{
	m_ForceFeedback = &m_ForceFeedbackInst;
	m_axiscount = 0;

	m_gamepadtype = 1;
	m_passthrough = false;

	m_user = user;

	m_failcount = 0;
}

ControllerCombiner::~ControllerCombiner()
{
	if (m_useforce)
	{
		m_ForceFeedback->Shutdown();
	}
}

bool ControllerCombiner::Initalized() const
{
	// TODO: Check devices
	return true;
}

DWORD ControllerCombiner::GetState(XINPUT_STATE* pState)
{
	// If not enabled, clear and bail
	if (!ControllerManager::Get().XInputEnabled())
	{
		// Clear state
		if (pState) ZeroMemory(pState, sizeof(XINPUT_STATE));
		return ERROR_SUCCESS;
	}

	// TODO: Get state from each device
	HRESULT hr = ERROR_SUCCESS;

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

	// TODO: Read child devices

	return ERROR_SUCCESS;
}

DWORD ControllerCombiner::CreateDevice()
{
	// TODO: Make sure all child devices are created
	return ERROR_SUCCESS;
}
