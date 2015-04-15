#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#include "ControllerBase.h"
#include "ForceFeedback.h"

class Controller : public ControllerBase
{
    friend class ForceFeedback;
public:
    Controller(u32 user);
    ~Controller();

    Controller(const Controller &other):
        m_ForceFeedback(this)
    {
        m_pDevice.reset(other.m_pDevice.get());
        m_state = other.m_state;
        m_mapping = other.m_mapping;
        m_productid = other.m_productid;
        m_instanceid = other.m_instanceid;
        m_user = other.m_user;
        m_axiscount = other.m_axiscount;
        m_gamepadtype = other.m_gamepadtype;
        m_passthrough = other.m_passthrough;
        m_useforce = other.m_useforce;
        m_failcount = other.m_failcount;
    }

    DWORD GetState(XINPUT_STATE* pState);
    DWORD CreateDevice();

    bool Initalized() const
    {
        return m_pDevice != nullptr;
    }

    ForceFeedback m_ForceFeedback;

private:
    static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

    bool ButtonPressed(u32 buttonidx);
    HRESULT UpdateState();

    std::unique_ptr<IDirectInputDevice8A, COMDeleter> m_pDevice;
    DIJOYSTATE2 m_state;
};

