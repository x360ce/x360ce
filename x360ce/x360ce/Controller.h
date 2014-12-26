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

