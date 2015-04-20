#pragma once

#include <dinput.h>
#include "Config.h"
#include "Mutex.h"

#include "ForceFeedback.h"

class Controller
{
    friend class ForceFeedback;
public:
    Controller(u32 user);
    ~Controller();

    Controller(const Controller &other):
        m_ForceFeedback(this)
    {
        m_pDevice = other.m_pDevice;
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

    Config::Mapping m_mapping;
    ForceFeedback m_ForceFeedback;
    GUID m_productid;
    GUID m_instanceid;
    u32 m_user;
    u32 m_axiscount;
    u8 m_gamepadtype;
    bool m_passthrough;
    bool m_useforce;

    u32 m_failcount;

private:
    static BOOL CALLBACK EnumObjectsCallback(const DIDEVICEOBJECTINSTANCE* pdidoi, VOID* pContext);

    bool ButtonPressed(u32 buttonidx);
    HRESULT UpdateState();

    LPDIRECTINPUTDEVICE8 m_pDevice;
    DIJOYSTATE2 m_state;
};

