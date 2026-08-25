#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#include "ControllerBase.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"

class Controller : public ControllerBase
{
	friend class ForceFeedback;
public:
	Controller(u32 user);
	~Controller();

	virtual DWORD GetState(XINPUT_STATE* pState);
	virtual DWORD CreateDevice();

	virtual bool Initalized();

	GUID m_productid;
	GUID m_instanceid;

private:
	static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

	bool ButtonPressed(u32 buttonidx);
	HRESULT UpdateState();

	std::unique_ptr<IDirectInputDevice8A, COMDeleter> m_pDevice;
	DIJOYSTATE2 m_state;
	bool m_stateChanged;
	bool m_emptyStateIsSet;
	DIJOYSTATE2 m_emptyState;
	ForceFeedback m_ForceFeedbackInst;

};

