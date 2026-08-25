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

#include "XInputModuleManager.h"

ControllerCombiner::ControllerCombiner(u32 user) : ControllerBase(user),
m_ForceFeedbackInst(this)
{
	m_ForceFeedback = &m_ForceFeedbackInst;
	m_combined = true;
}

ControllerCombiner::~ControllerCombiner()
{
	if (m_useforce)
	{
		m_ForceFeedback->Shutdown();
	}
}

DWORD ControllerCombiner::CancelGuideButtonWait()
{
	if (m_PassthroughController)
		return m_PassthroughController->CancelGuideButtonWait();

	return ControllerBase::CancelGuideButtonWait();
}

SHORT ControllerCombiner::CombineAxis(SHORT min, SHORT max) const
{
	// If max isn't a positive number, just go with the smaller number
	if (max < 0)
	{
		return min;
	}
	// If min isn't a negative number, just go with the larger number
	else if (min >= 0)
	{
		return max;
	}
	// Max is positive and min is negative, add the two together
	else
	{
		return max + min; // effectively subracting min from max
	}
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

		// Check for first full passthrough
		if (!m_PassthroughController)
		{
			if ((*it)->m_passthrough)
			{
				m_PassthroughController = (*it);
			}
		}

		// Check for first forces passthrough
		if (!m_ForcesPassthroughController)
		{
			if (((*it)->m_passthrough) || ((*it)->m_forcespassthrough))
			{
				m_ForcesPassthroughController = (*it);
				m_useforce = true;
			}
		}

		// Enable force feedback?
		if (!m_useforce)
		{
			if ((*it)->m_useforce)
			{
				m_useforce = true;
			}
		}

		// Use the maximum axis count found on any child
		m_axiscount = std::max(m_axiscount, (*it)->m_axiscount);
	}

	// All initialized
	return ERROR_SUCCESS;
}

DWORD ControllerCombiner::GetAudioDeviceIds(LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetAudioDeviceIds(pRenderDeviceId, pRenderCount, pCaptureDeviceId, pCaptureCount);

	return ControllerBase::GetAudioDeviceIds(pRenderDeviceId, pRenderCount, pCaptureDeviceId, pCaptureCount);
}

DWORD ControllerCombiner::GetBatteryInformation(BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetBatteryInformation(devType, pBatteryInformation);

	return ControllerBase::GetBatteryInformation(devType, pBatteryInformation);
}

DWORD ControllerCombiner::GetBaseBusInformation(struct XINPUT_BUSINFO* pBusinfo)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetBaseBusInformation(pBusinfo);

	return ControllerBase::GetBaseBusInformation(pBusinfo);
}

DWORD ControllerCombiner::GetCapabilities(DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetCapabilities(dwFlags, pCapabilities);

	return ControllerBase::GetCapabilities(dwFlags, pCapabilities);
}

DWORD ControllerCombiner::GetCapabilitiesEx(DWORD unk1, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetCapabilitiesEx(unk1, dwFlags, pCapabilitiesEx);

	return ControllerBase::GetCapabilitiesEx(unk1, dwFlags, pCapabilitiesEx);
}

std::vector<std::shared_ptr<ControllerBase>>& ControllerCombiner::GetControllers()
{
	return m_controllers;
}

DWORD ControllerCombiner::GetDSoundAudioDeviceGuids(GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetDSoundAudioDeviceGuids(pDSoundRenderGuid, pDSoundCaptureGuid);

	return ControllerBase::GetDSoundAudioDeviceGuids(pDSoundRenderGuid, pDSoundCaptureGuid);
}

DWORD ControllerCombiner::GetKeystroke(DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetKeystroke(dwReserved, pKeystroke);

	return ControllerBase::GetKeystroke(dwReserved, pKeystroke);
}

DWORD ControllerCombiner::GetState(XINPUT_STATE* pState)
{
	// Start with pass through state or clear state?
	if (m_PassthroughController)
	{
		m_PassthroughController->GetState(pState);
	}
	else
	{
		if (pState) ZeroMemory(pState, sizeof(XINPUT_STATE));
	}

	// If not enabled, nothing to do
	if (!ControllerManager::Get().XInputEnabled())
	{
		return ERROR_SUCCESS;
	}

	// Timestamp packet
	pState->dwPacketNumber = GetTickCount();

	// Temp variable for calling each child
	XINPUT_STATE childState;
	SHORT minLX = pState->Gamepad.sThumbLX;
	SHORT maxLX = pState->Gamepad.sThumbLX;
	SHORT minLY = pState->Gamepad.sThumbLY;
	SHORT maxLY = pState->Gamepad.sThumbLY;
	SHORT minRX = pState->Gamepad.sThumbRX;
	SHORT maxRX = pState->Gamepad.sThumbRX;
	SHORT minRY = pState->Gamepad.sThumbRY;
	SHORT maxRY = pState->Gamepad.sThumbRY;

	// Make sure all children are initialized
	for (auto it = m_controllers.begin(); it != m_controllers.end(); ++it)
	{
		// If this is the pass through controller, skip it since we already have its state
		if ((m_PassthroughController) && (m_PassthroughController == (*it)))
			continue;

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
		pState->Gamepad.bRightTrigger = std::max(pState->Gamepad.bRightTrigger, childState.Gamepad.bRightTrigger);

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
	pState->Gamepad.sThumbLX = CombineAxis(minLX, maxLX);
	pState->Gamepad.sThumbLY = CombineAxis(minLY, maxLY);
	pState->Gamepad.sThumbRX = CombineAxis(minRX, maxRX);
	pState->Gamepad.sThumbRY = CombineAxis(minRY, maxRY);

	// Done evaluating
	return ERROR_SUCCESS;
}

DWORD ControllerCombiner::GetStateEx(XINPUT_STATE *pState)
{
	if (m_PassthroughController)
		return m_PassthroughController->GetStateEx(pState);

	return ControllerBase::GetStateEx(pState);
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

DWORD ControllerCombiner::PowerOffController()
{
	if (m_PassthroughController)
		return m_PassthroughController->PowerOffController();

	return ControllerBase::PowerOffController();
}

DWORD ControllerCombiner::SetState(XINPUT_VIBRATION* pVibration)
{
	if (m_ForcesPassthroughController)
		return m_ForcesPassthroughController->SetState(pVibration);

	return ControllerBase::SetState(pVibration);
}

DWORD ControllerCombiner::WaitForGuideButton(DWORD dwFlag, LPVOID pVoid)
{
	if (m_PassthroughController)
		return m_PassthroughController->WaitForGuideButton(dwFlag, pVoid);

	return ControllerBase::WaitForGuideButton(dwFlag, pVoid);
}
