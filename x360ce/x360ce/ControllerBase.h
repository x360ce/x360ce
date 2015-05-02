#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"

class ControllerBase
{
public:
	ControllerBase(u32 user);
	virtual ~ControllerBase();

	virtual DWORD CreateDevice() = 0;
	virtual bool Initalized() = 0;

	virtual DWORD CancelGuideButtonWait();
	virtual DWORD GetAudioDeviceIds(LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount);
	virtual DWORD GetBatteryInformation(BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
	virtual DWORD GetBaseBusInformation(struct XINPUT_BUSINFO* pBusinfo);
	virtual DWORD GetCapabilities(DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
	virtual DWORD GetCapabilitiesEx(DWORD unk1 /*seems that only 1 is valid*/, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx);
	virtual DWORD GetDSoundAudioDeviceGuids(GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
	virtual DWORD GetKeystroke(DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke);
	virtual DWORD GetState(XINPUT_STATE* pState) = 0;
	virtual DWORD GetStateEx(XINPUT_STATE *pState);
	virtual DWORD PowerOffController();
	virtual DWORD SetState(XINPUT_VIBRATION* pVibration);
	virtual DWORD WaitForGuideButton(DWORD dwFlag, LPVOID pVoid);


	Config::Mapping m_mapping;
	u32 m_user;
	u32 m_axiscount;
	u8 m_gamepadtype;
	bool m_passthrough;
	u32 m_passthroughindex;
	bool m_forcespassthrough;
	bool m_useforce;

	bool m_combined = false;
	u32 m_combinedIndex = 0;

	ForceFeedbackBase* m_ForceFeedback;

	u32 m_failcount;
};