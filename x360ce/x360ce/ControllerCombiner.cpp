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

bool ControllerCombiner::Initalized()
{
	// Check child devices
	for (auto it = m_controllers.begin(); it != m_controllers.end(); ++it)
	{
		// If any child is not initialized, combinator is not initialized
		if (!(*it)->Initalized())
		{
			return false;
		}
	}

	// All children initialized
	return true;
}

std::vector<std::shared_ptr<Controller>>& ControllerCombiner::GetControllers()
{
	return m_controllers;
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

	// Timestamp packet
	pState->dwPacketNumber = GetTickCount();

	// Temp variable for calling each child
	XINPUT_STATE childState;
	SHORT minLX = 0;
	SHORT maxLX = 0;
	SHORT minLY = 0;
	SHORT maxLY = 0;
	SHORT minRX = 0;
	SHORT maxRX = 0;
	SHORT minRY = 0;
	SHORT maxRY = 0;

	// Make sure all children are initialized
	for (auto it = m_controllers.begin(); it != m_controllers.end(); ++it)
	{
		// Get state from child
		DWORD result = (*it)->GetState(&childState);

		// If child failed to respond, skip it
		if (FAILED(result))
		{
			continue;
		}

		// Buttons are additive
		pState->Gamepad.wButtons |= childState.Gamepad.wButtons;

		// Triggers use whichever is pressed most
		pState->Gamepad.bLeftTrigger = std::max(pState->Gamepad.bLeftTrigger, childState.Gamepad.bLeftTrigger);
		pState->Gamepad.bRightTrigger = std::max(pState->Gamepad.bLeftTrigger, childState.Gamepad.bRightTrigger);

		// Axis are computed based on highest and lowest
		minLX = std::min(minLX, childState.Gamepad.sThumbLX);
		maxLX = std::max(maxLX, childState.Gamepad.sThumbLX);
		minLY = std::min(minLY, childState.Gamepad.sThumbLY);
		maxLY = std::max(maxLY, childState.Gamepad.sThumbLY);
		minRX = std::min(minRX, childState.Gamepad.sThumbRX);
		maxRX = std::max(maxRX, childState.Gamepad.sThumbRX);
		minRY = std::min(minRY, childState.Gamepad.sThumbRY);
		maxRY = std::max(maxRY, childState.Gamepad.sThumbRY);
	}

	// Final calc of axis
	pState->Gamepad.sThumbLX = maxLX - minLX;
	pState->Gamepad.sThumbLY = maxLY - minLY;
	pState->Gamepad.sThumbRX = maxRX - minRX;
	pState->Gamepad.sThumbRY = maxRY - minRY;

	// Done evaluating
	return ERROR_SUCCESS;
}

DWORD ControllerCombiner::CreateDevice()
{
	// Make sure all children are initialized
	for (auto it = m_controllers.begin(); it != m_controllers.end(); ++it)
	{
		if (!(*it)->Initalized())
		{
			DWORD result = (*it)->CreateDevice();
			if (FAILED(result))
			{
				PrintLog("WARNING: Unable to initialize child device for ControllerCombiner");
				return result;
			}
		}

		// If this is the first child, make the combined gamepad type match the child type
		if (it == m_controllers.begin())
		{
			m_gamepadtype = (*it)->m_gamepadtype;
		}

		// Use the maximum axis count found on any child
		m_axiscount = std::max(m_axiscount, (*it)->m_axiscount);
	}

	// All initialized
	return ERROR_SUCCESS;
}
