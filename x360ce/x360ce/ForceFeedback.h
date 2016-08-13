#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"
#include "ForceFeedbackBase.h"

class ForceFeedback : public ForceFeedbackBase
{
	friend class Controller;
public:
	ForceFeedback(Controller* pController);
	~ForceFeedback();

	virtual bool IsSupported();

	virtual bool SetState(XINPUT_VIBRATION* pVibration);

	virtual void Shutdown();


private:
	static BOOL CALLBACK EnumFFAxesCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);
	static BOOL CALLBACK EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef);

	void SetCaps(const ForceFeedbackCaps& caps)
	{
		m_Caps = caps;
	}

	void StartEffects(DIEFFECT* diEffect, LPDIRECTINPUTEFFECT* effect, BOOL restartEffect);
	bool SetDeviceForces(XINPUT_VIBRATION* pVibration, u8 forceType);

	Controller* m_pController;
	std::vector<LPDIRECTINPUTEFFECT> m_effects;
	u8 m_Axes;
	ForceFeedbackCaps m_Caps;
};
