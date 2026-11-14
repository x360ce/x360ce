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

	/// <summary>Whether the mapping comes from SDL's database, the section's own mapping keys winning over it.</summary>
	bool m_autoMap;
	/// <summary>Whether SDL's database had a row for the device when it was opened.</summary>
	bool m_sdlKnown;
	/// <summary>Whether the mapping was read over x360ce's database row for the device.</summary>
	bool m_rowFound;
	/// <summary>Whether the section or the row says whether to use force feedback.</summary>
	bool m_forceKey;

private:
	static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

	bool ButtonPressed(u32 buttonidx);
	HRESULT UpdateState();
	void ApplyAutoMapping(bool exclusive);
	s32 HalfReading(Config::MappingType type, s8 id, const s32* axis, const s32* slider, const bool* dPadButtons);

	std::unique_ptr<IDirectInputDevice8A, COMDeleter> m_pDevice;
	DIJOYSTATE2 m_state;
	bool m_stateChanged;
	bool m_emptyStateIsSet;
	DIJOYSTATE2 m_emptyState;
	ForceFeedback m_ForceFeedbackInst;

};

