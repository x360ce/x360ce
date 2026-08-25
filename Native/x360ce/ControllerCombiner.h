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

	virtual DWORD CreateDevice();
	virtual bool Initalized();

	virtual DWORD CancelGuideButtonWait();
	virtual DWORD GetAudioDeviceIds(LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount);
	virtual DWORD GetBatteryInformation(BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
	virtual DWORD GetBaseBusInformation(struct XINPUT_BUSINFO* pBusinfo);
	virtual DWORD GetCapabilities(DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
	virtual DWORD GetCapabilitiesEx(DWORD unk1, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx);
	virtual DWORD GetDSoundAudioDeviceGuids(GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
	virtual DWORD GetKeystroke(DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke);
	virtual DWORD GetState(XINPUT_STATE* pState);
	virtual DWORD GetStateEx(XINPUT_STATE *pState);
	virtual DWORD PowerOffController();
	virtual DWORD SetState(XINPUT_VIBRATION* pVibration);
	virtual DWORD WaitForGuideButton(DWORD dwFlag, LPVOID pVoid);

	std::vector<std::shared_ptr<ControllerBase>>& GetControllers();

private:
	std::vector<std::shared_ptr<ControllerBase>> m_controllers;
	ForceFeedbackCombiner m_ForceFeedbackInst;
	std::shared_ptr<ControllerBase> m_ForcesPassthroughController;
	std::shared_ptr<ControllerBase> m_PassthroughController;

	SHORT CombineAxis(SHORT min, SHORT max) const;
};