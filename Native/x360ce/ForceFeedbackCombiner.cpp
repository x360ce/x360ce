#include "stdafx.h"

#include "Common.h"
#include "InputHook.h"
#include "Config.h"

#include "ControllerManager.h"
#include "ControllerBase.h"
#include "ControllerCombiner.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedbackCombiner.h"

ForceFeedbackCombiner::ForceFeedbackCombiner(ControllerCombiner* pController)
{
	m_Controller = pController;
};

ForceFeedbackCombiner::~ForceFeedbackCombiner()
{
}

void ForceFeedbackCombiner::Shutdown()
{
}

bool ForceFeedbackCombiner::IsSupported()
{
	// Make sure at least one child device supports feedback
	for (auto it = m_Controller->GetControllers().begin(); it != m_Controller->GetControllers().end(); ++it)
	{
		// If child supports it, combined supports it
		if ((*it)->m_ForceFeedback->IsSupported())
		{
			return true;
		}
	}

	// No child devices found that support feedback
	return false;
}

bool ForceFeedbackCombiner::SetState(XINPUT_VIBRATION* pVibration)
{
	if (!ControllerManager::Get().XInputEnabled())
	{
		// Clear state
		if (pVibration) ZeroMemory(pVibration, sizeof(XINPUT_VIBRATION));
		return ERROR_SUCCESS;
	}

	// Update all child devices
	for (auto it = m_Controller->GetControllers().begin(); it != m_Controller->GetControllers().end(); ++it)
	{
		// Update child?
		if ((*it)->m_useforce)
		{
			// (ignore failure for now, maybe keep track of failed devices at some point?)
			(*it)->m_ForceFeedback->SetState(pVibration);
		}
	}

	return ERROR_SUCCESS;
}
