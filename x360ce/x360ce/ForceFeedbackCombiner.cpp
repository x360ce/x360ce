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
    // TODO: Make sure at least one child device supports
	return true;
}

bool ForceFeedbackCombiner::SetState(XINPUT_VIBRATION* pVibration)
{
    if (!ControllerManager::Get().XInputEnabled())
    {
        // Clear state
        if (pVibration) ZeroMemory(pVibration, sizeof(XINPUT_VIBRATION));
        return ERROR_SUCCESS;
    }

	// TODO: Update child devices
	return ERROR_SUCCESS;
}