#include "stdafx.h"
#include "Common.h"
#include "SWIP.h"
#include "Logger.h"
#include "version.h"
#include "Utils.h"
#include "InputHook.h"

#include "SWIP.h"
#include "Config.h"

#include "Controller.h"
#include "ForceFeedback.h"

#include "ControllerManager.h"

const u16 Config::buttonIDs[10] =
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
};

const u16 Config::povIDs[4] =
{
    XINPUT_GAMEPAD_DPAD_UP,
    XINPUT_GAMEPAD_DPAD_DOWN,
    XINPUT_GAMEPAD_DPAD_LEFT,
    XINPUT_GAMEPAD_DPAD_RIGHT
};

 const char* const Config::buttonNames[] =
{
    "A",
    "B",
    "X",
    "Y",
    "Left Shoulder",
    "Right Shoulder",
    "Back",
    "Start",
    "Left Thumb",
    "Right Thumb",
};

 const char* const Config::povNames[] =
{
    "D-pad Up",
    "D-pad Down",
    "D-pad Left",
    "D-pad Right"
};

 const char* const Config::axisNames[] =
{
    "Left Analog X",
    "Left Analog Y",
    "Right Analog X",
    "Right Analog Y"
};

 const char* const Config::axisDZNames[] =
{
    "Left Analog X DeadZone",
    "Left Analog Y DeadZone",
    "Right Analog X DeadZone",
    "Right Analog Y DeadZone",
};

 const char* const Config::axisADZNames[] =
{
    "Left Analog X AntiDeadZone",
    "Left Analog Y AntiDeadZone",
    "Right Analog X AntiDeadZone",
    "Right Analog Y AntiDeadZone",
};

 const char* const Config::axisLNames[] =
{
    "Left Analog X Linear",
    "Left Analog Y Linear",
    "Right Analog X Linear",
    "Right Analog Y Linear"
};

 const char* const Config::axisBNames[] =
{
    "Left Analog X+ Button",
    "Left Analog X- Button",
    "Left Analog Y+ Button",
    "Left Analog Y- Button",
    "Right Analog X+ Button",
    "Right Analog X- Button",
    "Right Analog Y+ Button",
    "Right Analog Y- Button"
};

 const char* const Config::triggerNames[] =
{
    "Left Trigger",
    "Right Trigger"
};

 const char* const Config::triggerDZNames[] =
{
    "Left Trigger DZ",
    "Right Trigger DZ"
};

 const char* const Config::triggerBNames[] =
{
    "Left Trigger But",
    "Right Trigger But"
};

 void Config::ParsePrefix(const std::string& input, MappingType* pMappingType, s8* pValue)
{
    if (pMappingType)
    {
        switch (input[0])
        {
            case 'a': // Axis
                *pMappingType = AXIS;
                break;
            case 's': // Slider
                *pMappingType = SLIDER;
                break;
            case 'x': // Half range axis
                *pMappingType = HAXIS;
                break;
            case 'h': // Half range slider
                *pMappingType = HSLIDER;
                break;
            case 'z':
                *pMappingType = CBUT;
                break;
            default: // Digital
                *pMappingType = DIGITAL;
        }
    }

    if (pValue && pMappingType)
    {
        if (*pMappingType != DIGITAL)
            Convert(input.c_str() + 1, pValue);
        else
            Convert(input, pValue);
    }
}

void Config::ReadConfig()
{
    SWIP ini;
    std::string inipath("x360ce.ini");
    if (!ini.Load(inipath))
        CheckCommonDirectory(&inipath, "x360ce");
    if (!ini.Load(inipath)) return;

    static bool once_flag = false;
    if (!once_flag)
    {
        // Read global options
        ini.Get("Options", "Disable", &m_globalDisable);
        if (m_globalDisable) return;

        ini.Get("Options", "UseInitBeep", &m_initBeep, true);

        PrintLog("Using config file:");
        PrintLog(ini.GetIniPath().c_str());

        u32 ver = 0;
        ini.Get("Options", "Version", &ver);
        if (ver != VERSION_CONFIG)
            PrintLog("WARNING: Configuration file version mismatch detected");

        once_flag = true;
    }

    // Read pad mappings
    for (u32 i = 0; i < XUSER_MAX_COUNT; ++i)
    {
        std::string section;
        std::string key = StringFormat("PAD%u", i + 1);
        if (!ini.Get("Mappings", key, &section))
            continue;

        u32 index = 0;
        if (!ini.Get(section, "UserIndex", &index))
            index = i;

        // Require Controller copy constructor
        ControllerManager::Get().GetControllers().push_back(Controller(index));
        Controller* pController = &ControllerManager::Get().GetControllers().back();

        if (ReadPadConfig(pController, section, &ini))
            ReadPadMapping(pController, section, &ini);
    }
}

bool Config::ReadPadConfig(Controller* pController, const std::string& section, SWIP* pSWIP)
{
    std::string buffer;

    if (pSWIP->Get(section, "ProductGUID", &buffer))
        StringToGUID(&pController->m_productid, buffer);

    if (pSWIP->Get(section, "InstanceGUID", &buffer))
        StringToGUID(&pController->m_instanceid, buffer);

    if (IsEqualGUID(pController->m_productid, GUID_NULL) || IsEqualGUID(pController->m_instanceid, GUID_NULL))
    {
        std::string message = StringFormat("[PAD%u] Misconfigured device, check GUIDs", pController->m_user);
        PrintLog(message.c_str());
        MessageBoxA(NULL, message.c_str(), "x360ce", MB_ICONERROR);
        return false;
    }
    pSWIP->Get(section, "PassThrough", &pController->m_passthrough);
    if (pController->m_passthrough)
        return false;

    // Device type
    pSWIP->Get(section, "ControllerType", &pController->m_gamepadtype, 1);

    // FFB options
    pSWIP->Get(section, "UseForceFeedback", &pController->m_useforce);
    if (pController->m_useforce)
    {
        pSWIP->Get(section, "ForcePercent", &pController->m_ForceFeedback.m_ForcePercent, 100);
        pController->m_ForceFeedback.m_ForcePercent *= 0.01f;

        pSWIP->Get(section, "LeftMotorType", &pController->m_ForceFeedback.m_LeftMotor.type, 0);
        pSWIP->Get(section, "LeftMotorPeriod", &pController->m_ForceFeedback.m_LeftMotor.period, 100);
        pSWIP->Get(section, "LeftMotorActuator", &pController->m_ForceFeedback.m_LeftMotor.actuator, 0);
        pSWIP->Get(section, "LeftMotorStrength", &pController->m_ForceFeedback.m_LeftMotor.strength, 100);
        pController->m_ForceFeedback.m_LeftMotor.strength *= 0.01f;

        pSWIP->Get(section, "RightMotorType", &pController->m_ForceFeedback.m_RightMotor.type, 0);
        pSWIP->Get(section, "RightMotorPeriod", &pController->m_ForceFeedback.m_RightMotor.period, 100);
        pSWIP->Get(section, "RightMotorActuator", &pController->m_ForceFeedback.m_RightMotor.actuator, 1);
        pSWIP->Get(section, "RightMotorStrength", &pController->m_ForceFeedback.m_RightMotor.strength, 100);
        pController->m_ForceFeedback.m_RightMotor.strength *= 0.01f;

        pSWIP->Get(section, "FFUpdateInterval", &pController->m_ForceFeedback.m_UpdateInterval, 20);
    }

    return true;
}

void Config::ReadPadMapping(Controller* pController, const std::string& section, SWIP* pSWIP)
{
    Mapping* pMapping = &pController->m_mapping;

    // Guide button
    pSWIP->Get(section, "GuideButton", &pMapping->guide, -1);

    // Fire buttons
    for (u32 i = 0; i < _countof(pMapping->Button); ++i)
    {
        s8 button;
        pSWIP->Get(section, buttonNames[i], &button);
        pMapping->Button[i] = button - 1;
    }

    // D-PAD
    pSWIP->Get(section, "D-pad POV", &pMapping->DpadPOV);
    if (pMapping->DpadPOV >= 0)
    {
        for (u32 i = 0; i < _countof(pMapping->pov); ++i)
        {
            // D-PAD directions
            s16 val = 0;
            pSWIP->Get(section, povNames[i], &val, -1);
            if (val > 0 && val < 128)
            {
                pMapping->pov[i] = val - 1;
                pMapping->PovIsButton = true;
            }
            else if (val > -1)
            {
                pMapping->pov[i] = val;
                pMapping->PovIsButton = false;
            }
        }
    }

    for (u32 i = 0; i < _countof(pMapping->Axis); ++i)
    {
        // Axes
        std::string axis;
        if (pSWIP->Get(section, axisNames[i], &axis))
            ParsePrefix(axis, &pMapping->Axis[i].analogType, &pMapping->Axis[i].id);

        // DeadZones
        pSWIP->Get(section, axisDZNames[i], &pMapping->Axis[i].axisdeadzone);

        // Anti DeadZones
        pSWIP->Get(section, axisADZNames[i], &pMapping->Axis[i].antideadzone);

        // Linearity
        pSWIP->Get(section, axisLNames[i], &pMapping->Axis[i].axislinear);

        // Axis to DPAD options
        pSWIP->Get(section, "AxisToDPad", &pMapping->Axis[i].axistodpad);
        pSWIP->Get(section, "AxisToDPadDeadZone", &pMapping->Axis[i].a2ddeadzone);
        pSWIP->Get(section, "AxisToDPadOffset", &pMapping->Axis[i].a2doffset);

        // Axis to button mappings
        s8 ret;
        pSWIP->Get(section, axisBNames[i * 2], &ret);
        if (ret > 0)
        {
            pMapping->Axis[i].hasDigital = true;
            pMapping->Axis[i].positiveButtonID = ret - 1;
        }
        pSWIP->Get(section, axisBNames[i * 2 + 1], &ret);
        if (ret > 0)
        {
            pMapping->Axis[i].hasDigital = true;
            pMapping->Axis[i].negativeButtonID = ret - 1;
        }
    }

    // Triggers
    for (u32 i = 0; i < _countof(pMapping->Trigger); ++i)
    {
        std::string trigger;
        if (pSWIP->Get(section, triggerNames[i], &trigger))
            ParsePrefix(trigger, &pMapping->Trigger[i].type, &pMapping->Trigger[i].id);

        pSWIP->Get(section, triggerDZNames[i], &pMapping->Trigger[i].triggerdz);

        // SeDoG mod
        pSWIP->Get(section, triggerBNames[i], &pMapping->Trigger[i].but);
    }
}

