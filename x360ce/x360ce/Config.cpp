#include "stdafx.h"
#include "Common.h"
#include "IniFile.h"
#include "Logger.h"
#include "version.h"
#include "Utils.h"
#include "InputHook.h"

#include "IniFile.h"
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
    IniFile ini;
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

bool Config::ReadPadConfig(Controller* pController, const std::string& section, IniFile* pIniFile)
{
    std::string buffer;

    if (pIniFile->Get(section, "ProductGUID", &buffer))
        StringToGUID(&pController->m_productid, buffer);

    if (pIniFile->Get(section, "InstanceGUID", &buffer))
        StringToGUID(&pController->m_instanceid, buffer);

    if (IsEqualGUID(pController->m_productid, GUID_NULL) || IsEqualGUID(pController->m_instanceid, GUID_NULL))
    {
        std::string message = StringFormat("[PAD%u] Misconfigured device, check GUIDs", pController->m_user);
        PrintLog(message.c_str());
        MessageBoxA(NULL, message.c_str(), "x360ce", MB_ICONERROR);
        return false;
    }
    pIniFile->Get(section, "PassThrough", &pController->m_passthrough);
    if (pController->m_passthrough)
        return false;

    // Device type
    pIniFile->Get<u8>(section, "ControllerType", &pController->m_gamepadtype, 1);

    // FFB options
    pIniFile->Get(section, "UseForceFeedback", &pController->m_useforce);
    if (pController->m_useforce)
    {
        pIniFile->Get(section, "SwapMotor", &pController->m_ForceFeedback.m_SwapMotors);
        pIniFile->Get(section, "FFBType", &pController->m_ForceFeedback.m_Type);
        pIniFile->Get<float>(section, "ForcePercent", &pController->m_ForceFeedback.m_ForcePercent, 100);
        pController->m_ForceFeedback.m_ForcePercent *= 0.01f;

        pIniFile->Get<u32>(section, "LeftMotorPeriod", &pController->m_ForceFeedback.m_LeftPeriod, 60);
        pIniFile->Get<u32>(section, "RightMotorPeriod", &pController->m_ForceFeedback.m_RightPeriod, 20);
    }

    return true;
}

void Config::ReadPadMapping(Controller* pController, const std::string& section, IniFile* pIniFile)
{
    Mapping* pMapping = &pController->m_mapping;

    // Guide button
    pIniFile->Get<s8>(section, "GuideButton", &pMapping->guide, -1);

    // Fire buttons
    for (u32 i = 0; i < _countof(pMapping->Button); ++i)
    {
        s8 button;
        pIniFile->Get(section, buttonNames[i], &button);
        pMapping->Button[i] = button - 1;
    }

    // D-PAD
    pIniFile->Get(section, "D-pad POV", &pMapping->DpadPOV);
    if (pMapping->DpadPOV >= 0)
    {
        for (u32 i = 0; i < _countof(pMapping->pov); ++i)
        {
            // D-PAD directions
            s16 val = 0;
            pIniFile->Get<s16>(section, povNames[i], &val, -1);
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
        if (pIniFile->Get(section, axisNames[i], &axis))
            ParsePrefix(axis, &pMapping->Axis[i].analogType, &pMapping->Axis[i].id);

        // DeadZones
        pIniFile->Get(section, axisDZNames[i], &pMapping->Axis[i].axisdeadzone);

        // Anti DeadZones
        pIniFile->Get(section, axisADZNames[i], &pMapping->Axis[i].antideadzone);

        // Linearity
        pIniFile->Get(section, axisLNames[i], &pMapping->Axis[i].axislinear);

        // Axis to DPAD options
        pIniFile->Get(section, "AxisToDPad", &pMapping->Axis[i].axistodpad);
        pIniFile->Get(section, "AxisToDPadDeadZone", &pMapping->Axis[i].a2ddeadzone);
        pIniFile->Get(section, "AxisToDPadOffset", &pMapping->Axis[i].a2doffset);

        // Axis to button mappings
        s8 ret;
        pIniFile->Get(section, axisBNames[i * 2], &ret);
        if (ret > 0)
        {
            pMapping->Axis[i].hasDigital = true;
            pMapping->Axis[i].positiveButtonID = ret - 1;
        }
        pIniFile->Get(section, axisBNames[i * 2 + 1], &ret);
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
        if (pIniFile->Get(section, triggerNames[i], &trigger))
            ParsePrefix(trigger, &pMapping->Trigger[i].type, &pMapping->Trigger[i].id);

        pIniFile->Get(section, triggerDZNames[i], &pMapping->Trigger[i].triggerdz);

        // SeDoG mod
        pIniFile->Get(section, triggerBNames[i], &pMapping->Trigger[i].but);
    }
}

