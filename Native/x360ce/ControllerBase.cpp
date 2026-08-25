#include "stdafx.h"

#include "Common.h"
#include "InputHook.h"

#include "Config.h"
#include "InputHookManager.h"
#include "ControllerManager.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"
#include "ControllerBase.h"

#include "XInputModuleManager.h"

ControllerBase::ControllerBase(u32 user)
{
	m_axiscount = 0;
	m_failcount = 0;
	m_gamepadtype = 1;
	m_passthrough = false;
	m_user = user;
}

ControllerBase::~ControllerBase()
{
}

DWORD ControllerBase::CancelGuideButtonWait()
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputCancelGuideButtonWait(m_passthroughindex);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetAudioDeviceIds(LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputGetAudioDeviceIds(m_passthroughindex, pRenderDeviceId, pRenderCount, pCaptureDeviceId, pCaptureCount);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetBatteryInformation(BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputGetBatteryInformation(m_passthroughindex, devType, pBatteryInformation);

	// Report a wired controller
	XINPUT_BATTERY_INFORMATION &xBatInfo = *pBatteryInformation;
	xBatInfo.BatteryLevel = BATTERY_LEVEL_FULL;
	xBatInfo.BatteryType = BATTERY_TYPE_WIRED;

	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetBaseBusInformation(struct XINPUT_BUSINFO* pBusinfo)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputGetBaseBusInformation(m_passthroughindex, pBusinfo);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetCapabilities(DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	// If full passthrough mode just pass through
	if (m_passthrough)
	{
		return XInputModuleManager::Get().XInputGetCapabilities(m_passthroughindex, dwFlags, pCapabilities);
	}

	// Set some defaults for the virtual device
	pCapabilities->Type = 0;
	pCapabilities->SubType = m_gamepadtype; //customizable subtype
	pCapabilities->Flags = 0; // we do not support sound
	pCapabilities->Vibration.wLeftMotorSpeed = pCapabilities->Vibration.wRightMotorSpeed = 0xFF;
	pCapabilities->Gamepad.bLeftTrigger = pCapabilities->Gamepad.bRightTrigger = 0xFF;

	pCapabilities->Gamepad.sThumbLX = (SHORT)-64;
	pCapabilities->Gamepad.sThumbLY = (SHORT)-64;
	pCapabilities->Gamepad.sThumbRX = (SHORT)-64;
	pCapabilities->Gamepad.sThumbRY = (SHORT)-64;
	pCapabilities->Gamepad.wButtons = (WORD)0xF3FF;

	// If we are doing force passthrough, use force capabilities of actual device
	if (m_forcespassthrough)
	{
		// Allocate and clear
		XINPUT_CAPABILITIES passThroughCaps;
		ZeroMemory(&passThroughCaps, sizeof(XINPUT_CAPABILITIES));

		// Get capabilities from actual device
		XInputModuleManager::Get().XInputGetCapabilities(m_passthroughindex, dwFlags, &passThroughCaps);

		// Update vibration capabilities
		pCapabilities->Vibration.wLeftMotorSpeed = passThroughCaps.Vibration.wLeftMotorSpeed;
		pCapabilities->Vibration.wRightMotorSpeed = passThroughCaps.Vibration.wRightMotorSpeed;
	}

	// Done
	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetCapabilitiesEx(DWORD unk1 /*seems that only 1 is valid*/, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx)
{
	if (m_passthrough || m_forcespassthrough)
		return XInputModuleManager::Get().XInputGetCapabilitiesEx(unk1, m_passthroughindex, dwFlags, pCapabilitiesEx);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetDSoundAudioDeviceGuids(GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputGetDSoundAudioDeviceGuids(m_passthroughindex, pDSoundRenderGuid, pDSoundCaptureGuid);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	*pDSoundRenderGuid = GUID_NULL;
	*pDSoundCaptureGuid = GUID_NULL;

	return ERROR_SUCCESS;
}

DWORD ControllerBase::GetKeystroke(DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputGetKeystroke(m_passthroughindex, dwReserved, pKeystroke);

	XINPUT_STATE xstate;
	ZeroMemory(&xstate, sizeof(XINPUT_STATE));
	XInputGetState(m_user, &xstate);

	static WORD repeat[14];
	static WORD flags[14];

	ZeroMemory(pKeystroke, sizeof(XINPUT_KEYSTROKE));
	pKeystroke->UserIndex = (BYTE)m_user;

	static const WORD allButtonIDs[14] =
	{
		XINPUT_GAMEPAD_A,
		XINPUT_GAMEPAD_B,
		XINPUT_GAMEPAD_X,
		XINPUT_GAMEPAD_Y,
		XINPUT_GAMEPAD_LEFT_SHOULDER,
		XINPUT_GAMEPAD_RIGHT_SHOULDER,
		XINPUT_GAMEPAD_BACK,
		XINPUT_GAMEPAD_START,
		XINPUT_GAMEPAD_LEFT_THUMB,
		XINPUT_GAMEPAD_RIGHT_THUMB,
		XINPUT_GAMEPAD_DPAD_UP,
		XINPUT_GAMEPAD_DPAD_DOWN,
		XINPUT_GAMEPAD_DPAD_LEFT,
		XINPUT_GAMEPAD_DPAD_RIGHT
	};

	static const u16 keyIDs[14] =
	{
		VK_PAD_A,
		VK_PAD_B,
		VK_PAD_X,
		VK_PAD_Y,
		VK_PAD_LSHOULDER,
		VK_PAD_RSHOULDER,
		VK_PAD_BACK,
		VK_PAD_START,
		VK_PAD_LTHUMB_PRESS,
		VK_PAD_RTHUMB_PRESS,
		VK_PAD_DPAD_UP,
		VK_PAD_DPAD_DOWN,
		VK_PAD_DPAD_LEFT,
		VK_PAD_DPAD_RIGHT
	};


	for (int i = 0; i < 14; i++)
	{
		if (xstate.Gamepad.wButtons & allButtonIDs[i])
		{
			if (flags[i] == NULL)
			{
				pKeystroke->VirtualKey = keyIDs[i];
				pKeystroke->Flags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN;
				break;
			}
			if ((flags[i] & XINPUT_KEYSTROKE_KEYDOWN))
			{
				if (repeat[i] <= 0)
				{
					repeat[i] = 5;
					pKeystroke->VirtualKey = keyIDs[i];
					pKeystroke->Flags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN | XINPUT_KEYSTROKE_REPEAT;
					break;
				}
				else
				{
					repeat[i]--;
					continue;
				}
			}
		}
		if (!(xstate.Gamepad.wButtons & allButtonIDs[i]))
		{
			if (flags[i] & XINPUT_KEYSTROKE_KEYDOWN)
			{
				repeat[i] = 5 * 4;
				pKeystroke->VirtualKey = keyIDs[i];
				pKeystroke->Flags = flags[i] = XINPUT_KEYSTROKE_KEYUP;
				break;
			}
			if (flags[i] & XINPUT_KEYSTROKE_KEYUP)
			{
				pKeystroke->Flags = flags[i] = NULL;
				break;
			}
		}
	}

	//PrintLog("ret: %u, flags: %u, hid: %u, unicode: %c, user: %u, vk: 0x%X",ret,pKeystroke->Flags,pKeystroke->HidCode,pKeystroke->Unicode,pKeystroke->UserIndex,pKeystroke->VirtualKey);

	if (pKeystroke->VirtualKey)
		return ERROR_SUCCESS;
	else
		return ERROR_EMPTY;

}

DWORD ControllerBase::GetStateEx(XINPUT_STATE *pState)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputGetStateEx(m_passthroughindex, pState);

	//PrintLog("XInputGetStateEx %u",xstate.Gamepad.wButtons);

	return GetState(pState);
}

DWORD ControllerBase::PowerOffController()
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputPowerOffController(m_passthroughindex);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	return ERROR_SUCCESS;
}

DWORD ControllerBase::SetState(XINPUT_VIBRATION* pVibration)
{
	if (m_ForceFeedback->m_SwapMotors)
	{
		std::swap(pVibration->wLeftMotorSpeed, pVibration->wRightMotorSpeed);
	}
	if (m_passthrough)
	{
		return XInputModuleManager::Get().XInputSetState(m_passthroughindex, pVibration);
	}
	if (m_useforce)
	{
		m_ForceFeedback->SetState(pVibration);
	}
	return ERROR_SUCCESS;
}

DWORD ControllerBase::WaitForGuideButton(DWORD dwFlag, LPVOID pVoid)
{
	if (m_passthrough)
		return XInputModuleManager::Get().XInputWaitForGuideButton(m_passthroughindex, dwFlag, pVoid);

	PrintLog("Call to unimplemented function " __FUNCTION__);

	return ERROR_SUCCESS;
}
