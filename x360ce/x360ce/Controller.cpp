#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"
#include "InputHook.h"

#include "Controller.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

#include "mutex.h"

DInputManager dinput;
std::vector<Controller> g_Controllers;

Controller::Controller(u32 user)
{
    m_pDevice = nullptr;
    m_pForceFeedback = nullptr;
    m_productid = GUID_NULL;
    m_instanceid = GUID_NULL;
    m_axiscount = 0;

    m_gamepadtype = 1;
    m_passthrough = false;

    m_user = user;
}

Controller::~Controller()
{
    delete m_pForceFeedback;

    if (m_pDevice)
        m_pDevice->Release();
}

BOOL CALLBACK Controller::EnumObjectsCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef)
{
    Controller *gp = (Controller*)pvRef;

    if (lpddoi->dwType & DIDFT_AXIS)
    {
        DIPROPRANGE diprg;
        diprg.diph.dwSize = sizeof(DIPROPRANGE);
        diprg.diph.dwHeaderSize = sizeof(DIPROPHEADER);
        diprg.diph.dwHow = DIPH_BYID;
        diprg.diph.dwObj = lpddoi->dwType;
        diprg.lMin = -32768;
        diprg.lMax = +32767;

        if (FAILED(gp->m_pDevice->SetProperty(DIPROP_RANGE, &diprg.diph)))
            return DIENUM_STOP;

        gp->m_axiscount++;
    }

    return DIENUM_CONTINUE;
}

HRESULT Controller::UpdateState()
{
    HRESULT hr = E_FAIL;

    if ((!m_pDevice)) return E_FAIL;

    m_pDevice->Poll();
    hr = m_pDevice->GetDeviceState(sizeof(DIJOYSTATE2), &m_state);

    if (FAILED(hr))
    {
        PrintLog("[PAD%d] Device Reacquired", m_user + 1);
        hr = m_pDevice->Acquire();
    }

    return hr;
}

HRESULT Controller::InitDirectInput(HWND hWnd)
{
    DIPROPDWORD dipdw;
    HRESULT hr = E_FAIL;

    if (FAILED(dinput.Init()))
    {
        PrintLog("DirectInput cannot be initialized");
        MessageBox(NULL, "DirectInput cannot be initialized", "x360ce - Error", MB_ICONERROR);
        ExitProcess(hr);
    }

    LockGuard lock(m_mutex);

    PrintLog("[PAD%d] Creating device", m_user + 1);

    bool bHookDI = g_iHook.GetState(InputHook::HOOK_DI);
    bool bHookSA = g_iHook.GetState(InputHook::HOOK_SA);

    if (bHookDI) g_iHook.DisableHook(InputHook::HOOK_DI);
    if (bHookSA) g_iHook.DisableHook(InputHook::HOOK_SA);

    hr = dinput.Get()->CreateDevice(m_instanceid, &m_pDevice, NULL);
    if (FAILED(hr))
    {
        std::string strInstance;
        GUIDtoString(&strInstance, m_instanceid);
        PrintLog("InstanceGUID %s is incorrect trying ProductGUID", strInstance.c_str());
        hr = dinput.Get()->CreateDevice(m_productid, &m_pDevice, NULL);
    }

    if (!m_pDevice) return ERROR_DEVICE_NOT_CONNECTED;
    else PrintLog("[PAD%d] Device created", m_user + 1);

    hr = m_pDevice->SetDataFormat(&c_dfDIJoystick2);

    if (FAILED(hr)) PrintLog("[PAD%d] SetDataFormat failed with code HR = %X", m_user + 1, hr);

    HRESULT setCooperativeLevelResult = m_pDevice->SetCooperativeLevel(hWnd, DISCL_EXCLUSIVE | DISCL_BACKGROUND);
    if (FAILED(setCooperativeLevelResult))
    {
        m_useforce = false;
        PrintLog("Cannot get exclusive device access, disabling ForceFeedback");

        setCooperativeLevelResult = m_pDevice->SetCooperativeLevel(hWnd, DISCL_NONEXCLUSIVE | DISCL_BACKGROUND);
        if (FAILED(setCooperativeLevelResult)) PrintLog("[PAD%d] SetCooperativeLevel failed with code HR = %X", m_user + 1, setCooperativeLevelResult);
    }

    dipdw.diph.dwSize = sizeof(DIPROPDWORD);
    dipdw.diph.dwHeaderSize = sizeof(DIPROPHEADER);
    dipdw.diph.dwObj = 0;
    dipdw.diph.dwHow = DIPH_DEVICE;
    dipdw.dwData = DIPROPAUTOCENTER_OFF;
    m_pDevice->SetProperty(DIPROP_AUTOCENTER, &dipdw.diph);

    hr = m_pDevice->EnumObjects(EnumObjectsCallback, (VOID*)this, DIDFT_AXIS);
    if (FAILED(hr)) PrintLog("[PAD%d] EnumObjects failed with code HR = %X", m_user + 1, hr);
    else PrintLog("[PAD%d] Detected axis count: %d", m_user + 1, m_axiscount);

    if (m_pForceFeedback && m_useforce)
        m_useforce = m_pForceFeedback->IsSupported();

    if (!m_useforce)
        delete m_pForceFeedback;

    hr = m_pDevice->Acquire();

    if (bHookSA) g_iHook.EnableHook(InputHook::HOOK_SA);
    if (bHookDI) g_iHook.EnableHook(InputHook::HOOK_DI);

    return hr;
}

bool Controller::ButtonPressed(u32 buttonidx)
{
    return (buttonidx != INVALIDBUTTONINDEX) ? (m_state.rgbButtons[buttonidx] & 0x80) != 0 : 0;
}

ForceFeedback::ForceFeedback(Controller* pController) :
m_pController(pController)
{
    for (u32 i = 0; i < _countof(m_pEffectObject); ++i) m_pEffectObject[i] = nullptr;
    m_xForce = 0;
    m_yForce = 0;
    m_Axes = 0;
    m_LeftPeriod = 0;
    m_RightPeriod = 0;
    m_ForcePercent = 100;
    m_Type = 0;
    m_Caps.ConstantForce = false;
    m_Caps.PeriodicForce = false;
    m_Caps.RampForce = false;
};

ForceFeedback::~ForceFeedback()
{
    if (m_pEffectObject[FFB_LEFTMOTOR] || m_pEffectObject[FFB_RIGHTMOTOR])
    {
        bool brokenffd = false;
        if (GetModuleHandleA("tmffbdrv.dll")) brokenffd = true;

        if (m_pController->m_pDevice && !brokenffd)
            m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_RESET);    
    }

    if (m_pEffectObject[FFB_LEFTMOTOR])
        m_pEffectObject[FFB_LEFTMOTOR]->Release();

    if (m_pEffectObject[FFB_RIGHTMOTOR])
        m_pEffectObject[FFB_RIGHTMOTOR]->Release();
}

BOOL CALLBACK ForceFeedback::EnumFFAxesCallback(LPCDIDEVICEOBJECTINSTANCE lpddoi, LPVOID pvRef)
{
    DWORD* pdwNumForceFeedbackAxis = (DWORD*)pvRef;

    if (((lpddoi->dwFlags & DIDOI_FFACTUATOR) != 0))
        (*pdwNumForceFeedbackAxis)++;

    return DIENUM_CONTINUE;
}

BOOL CALLBACK ForceFeedback::EnumEffectsCallback(LPCDIEFFECTINFO di, LPVOID pvRef)
{
    ForceFeedback* ffb = (ForceFeedback*)pvRef;
    ForceFeedbackCaps caps;

    caps.ConstantForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_CONSTANTFORCE;
    caps.PeriodicForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_PERIODIC;
    caps.RampForce = DIEFT_GETTYPE(di->dwEffType) == DIEFT_RAMPFORCE;

    ffb->SetCaps(caps);
    PrintLog("ForceFeedback effect '%s'. IsConstant = %d, IsPeriodic = %d", di->tszName, caps.ConstantForce, caps.PeriodicForce);
    return DIENUM_CONTINUE;
}

bool ForceFeedback::IsSupported()
{
    DIDEVCAPS DeviceCaps;
    DeviceCaps.dwSize = sizeof(DeviceCaps);

    HRESULT hr = m_pController->m_pDevice->GetCapabilities(&DeviceCaps);
    if (hr != DI_OK)
    {
        PrintLog("[DINPUT] [PAD%d] IsForceSupported: GetCapabilities returned HR = %X", m_pController->m_user + 1, hr);
        return false;
    }
    bool ffSupported = ((DeviceCaps.dwFlags & DIDC_FORCEFEEDBACK) == DIDC_FORCEFEEDBACK);
    PrintLog("[DINPUT] [PAD%d] IsForceSupported: 0x%08X %s", m_pController->m_user + 1, DeviceCaps.dwFlags, ffSupported == true ? "YES" : "NO");

    if (ffSupported)
    {
        HRESULT hr = m_pController->m_pDevice->EnumObjects(EnumFFAxesCallback, (VOID*)&m_Axes, DIDFT_AXIS);
        if (FAILED(hr)) PrintLog("[PAD%d] EnumFFAxesCallback failed with code HR = %X", m_pController->m_user + 1, hr);

        if (m_Axes > 2)
            m_Axes = 2;

        if (!m_Axes)
        {
            PrintLog("ForceFeedback unsupported");
            m_pController->m_useforce = false;
            return false;
        }
        else
        {
            m_pController->m_pDevice->EnumEffects(EnumEffectsCallback, this, DIEFT_ALL);
            m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_RESET);
            m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_SETACTUATORSON);
            return true;
        }
    }
    return false;
}

void ForceFeedback::SetState(WORD rightMotor, WORD leftMotor)
{
    LockGuard lock(m_mutex);

    if (!rightMotor && !leftMotor)
        m_pController->m_pDevice->SendForceFeedbackCommand(DISFFC_RESET);
    if (!leftMotor && m_pEffectObject[FFB_LEFTMOTOR])

        m_pEffectObject[FFB_LEFTMOTOR]->Stop();

    if (!rightMotor && m_pEffectObject[FFB_RIGHTMOTOR])
        m_pEffectObject[FFB_RIGHTMOTOR]->Stop();

    for (u8 i = 0; i < _countof(m_pEffectObject); ++i)
    {
        WORD force = 0;

        if (i == FFB_LEFTMOTOR)
            force = rightMotor;
        else 
            force = leftMotor;

        switch (m_Type)
        {
            case 1:
                SetDeviceForcesEjocys(force, i);
                break;
            case 2:
                SetDeviceForcesNew(force, i);
                break;
            default:
                SetDeviceForcesFailsafe(force, i);
        }
    }
}

HRESULT ForceFeedback::SetDeviceForcesFailsafe(WORD force, u8 motor)
{
    DWORD    rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };

    DICONSTANTFORCE contantForce;
    contantForce.lMagnitude = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);

    DIEFFECT effectType;
    ZeroMemory(&effectType, sizeof(DIEFFECT));

    effectType.dwSize = sizeof(DIEFFECT);
    effectType.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effectType.dwDuration = INFINITE;
    effectType.dwSamplePeriod = 0;
    effectType.dwGain = DI_FFNOMINALMAX;
    effectType.dwTriggerButton = DIEB_NOTRIGGER;
    effectType.dwTriggerRepeatInterval = 0;
    effectType.cAxes = m_Axes;
    effectType.rgdwAxes = rgdwAxes;
    effectType.rglDirection = rglDirection;
    effectType.lpEnvelope = 0;
    effectType.cbTypeSpecificParams = sizeof(DICONSTANTFORCE);
    effectType.lpvTypeSpecificParams = &contantForce;
    effectType.dwStartDelay = 0;

    if (!m_pEffectObject[motor])
    {
        HRESULT hr = m_pController->m_pDevice->CreateEffect(GUID_ConstantForce, &effectType, &m_pEffectObject[motor], NULL);
        if (FAILED(hr))
        {
            PrintLog("[PAD%d] CreateEffect failed with code HR = %X", m_pController->m_user + 1, hr);
            return hr;
        }
    }

    return m_pEffectObject[motor]->SetParameters(&effectType, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
}

HRESULT ForceFeedback::SetDeviceForcesEjocys(WORD force, u8 motor)
{
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };

    DIPERIODIC periodicForce;
    ZeroMemory(&periodicForce, sizeof(DIPERIODIC));

    DIEFFECT effectType;
    ZeroMemory(&effectType, sizeof(DIEFFECT));

    effectType.dwSize = sizeof(DIEFFECT);
    effectType.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effectType.cAxes = m_Axes;
    effectType.lpEnvelope = 0;
    effectType.dwStartDelay = 0;
    effectType.cbTypeSpecificParams = sizeof(DIPERIODIC);
    effectType.dwDuration = INFINITE;
    effectType.dwSamplePeriod = 0;
    effectType.dwGain = DI_FFNOMINALMAX;
    effectType.dwTriggerButton = DIEB_NOTRIGGER;
    effectType.dwTriggerRepeatInterval = 0;
    effectType.rgdwAxes = rgdwAxes;
    effectType.rglDirection = rglDirection;
    effectType.lpvTypeSpecificParams = &periodicForce;

    INT nForce = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);

    if (motor == FFB_LEFTMOTOR)
    {
        m_xForce = nForce;
        periodicForce.dwPeriod = m_LeftPeriod * 1000;
    }
    else
    {
        m_yForce = nForce;
        periodicForce.dwPeriod = m_RightPeriod * 1000;
    }

    if (m_Axes == 1)
    {
        periodicForce.dwMagnitude = std::max(m_xForce, m_yForce);
        periodicForce.dwPeriod = (m_LeftPeriod * 1000 * m_xForce + m_RightPeriod * 1000 * m_yForce) / (m_xForce + m_yForce);
        effectType.rglDirection[0] = 0;
        effectType.rglDirection[1] = 0;
    }
    else
    {
        periodicForce.dwMagnitude = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);
        effectType.rglDirection[0] = m_xForce;
        effectType.rglDirection[1] = m_yForce;
    }

    HRESULT hr = m_pController->m_pDevice->CreateEffect(GUID_Sine, &effectType, &m_pEffectObject[motor], NULL);
    if (FAILED(hr))
    {
        PrintLog("[DINPUT] [PAD%d] PrepareForce (%d) failed with code HR = %X", m_pController->m_user + 1, motor, hr);
        return hr;
    }

    return m_pEffectObject[motor]->SetParameters(&effectType, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
}

HRESULT ForceFeedback::SetDeviceForcesNew(WORD force, u8 motor)
{
    DWORD rgdwAxes[2] = { DIJOFS_X, DIJOFS_Y };
    LONG rglDirection[2] = { 0, 0 };

    DIPERIODIC periodicForce;
    ZeroMemory(&periodicForce, sizeof(DIPERIODIC));

    DIEFFECT effectType;
    ZeroMemory(&effectType, sizeof(DIEFFECT));

    effectType.dwSize = sizeof(DIEFFECT);
    effectType.dwFlags = DIEFF_CARTESIAN | DIEFF_OBJECTOFFSETS;
    effectType.cAxes = m_Axes;
    effectType.lpEnvelope = 0;
    effectType.dwStartDelay = 0;
    effectType.cbTypeSpecificParams = sizeof(DIPERIODIC);
    effectType.dwDuration = INFINITE;
    effectType.dwSamplePeriod = 0;
    effectType.dwGain = DI_FFNOMINALMAX; // no scaling
    effectType.dwTriggerButton = DIEB_NOTRIGGER;
    effectType.dwTriggerRepeatInterval = 0;
    effectType.rgdwAxes = rgdwAxes;
    effectType.rglDirection = rglDirection;
    effectType.lpvTypeSpecificParams = &periodicForce;

    periodicForce.dwMagnitude = MulDiv(force, DI_FFNOMINALMAX, USHRT_MAX);

    GUID effectGUID;
    if (motor == FFB_LEFTMOTOR)
    {
        effectGUID = GUID_SawtoothDown;
        effectType.dwDuration = m_LeftPeriod * 1000;
        periodicForce.dwPeriod = m_LeftPeriod * 1000;
    }

    if (motor == FFB_RIGHTMOTOR)
    {
        effectGUID = GUID_SawtoothUp;
        effectType.dwDuration = m_LeftPeriod * 1000;
        periodicForce.dwPeriod = m_RightPeriod * 1000;
    }

    HRESULT hr = m_pController->m_pDevice->CreateEffect(effectGUID, &effectType, &m_pEffectObject[motor], NULL);
    if (FAILED(hr))
    {
        PrintLog("[DINPUT] [PAD%d] PrepareForce (%d) failed with code HR = %X", m_pController->m_user + 1, motor, hr);
        return hr;
    }

    return m_pEffectObject[motor]->SetParameters(&effectType, DIEP_DIRECTION | DIEP_TYPESPECIFICPARAMS | DIEP_START);
}
