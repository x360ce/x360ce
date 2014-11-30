#include "stdafx.h"
#include "Common.h"
#include "SWIP.h"
#include "Logger.h"
#include "version.h"
#include "Utils.h"
#include "InputHook.h"

#include "Controller.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

bool g_bInitBeep = true;
bool g_bDisable = false;

static const char* const buttonNames[] =
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

static const char* const povNames[] =
{
    "D-pad Up",
    "D-pad Down",
    "D-pad Left",
    "D-pad Right"
};

static const char* const axisNames[] =
{
    "Left Analog X",
    "Left Analog Y",
    "Right Analog X",
    "Right Analog Y"
};

static const char* const axisDZNames[] =
{
    "Left Analog X DeadZone",
    "Left Analog Y DeadZone",
    "Right Analog X DeadZone",
    "Right Analog Y DeadZone",
};

static const char* const axisADZNames[] =
{
    "Left Analog X AntiDeadZone",
    "Left Analog Y AntiDeadZone",
    "Right Analog X AntiDeadZone",
    "Right Analog Y AntiDeadZone",
};

static const char* const axisLNames[] =
{
    "Left Analog X Linear",
    "Left Analog Y Linear",
    "Right Analog X Linear",
    "Right Analog Y Linear"
};

static const char* const axisBNames[] =
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

static const char* const triggerNames[] =
{
    "Left Trigger",
    "Right Trigger"
};

static const char* const triggerDZNames[] =
{
    "Left Trigger DZ",
    "Right Trigger DZ"
};

static const char* const triggerBNames[] =
{
    "Left Trigger But",
    "Right Trigger But"
};

void ParsePrefix(const std::string& input, MappingType* mapping_type, s8* value)
{
    if (mapping_type)
    {
        switch (input[0])
        {
        case 'a': // Axis
            *mapping_type = AXIS;
            break;
        case 's': // Slider
            *mapping_type = SLIDER;
            break;
        case 'x': // Half range axis
            *mapping_type = HAXIS;
            break;
        case 'h': // Half range slider
            *mapping_type = HSLIDER;
            break;
        case 'z':
            *mapping_type = CBUT;
            break;
        default: // Digital
            *mapping_type = DIGITAL;
        }
    }

    if (value)
    {
        if (mapping_type && *mapping_type != DIGITAL)
            *value = (s8)strtol(input.c_str() + 1, NULL, 0);
        else
            *value = (s8)strtol(input.c_str(), NULL, 0);
    }
}

void InitLogger()
{
    SWIP ini;
    ini.Load("x360ce.ini");

    bool file = false;
    bool con = false;

    ini.Get("Options", "Log", &file);
    ini.Get("Options", "Console", &con);

    if (con) LogConsole("x360ce", legal_notice);
    if (file)
    {
        char logfilename[MAX_PATH];
        SYSTEMTIME systime;
        GetLocalTime(&systime);
        std::string processName;
        ModuleFileName(&processName);

        sprintf_s(logfilename, "x360ce_%s_%02u-%02u-%02u_%08u.log", processName.c_str(), systime.wYear,
            systime.wMonth, systime.wDay, GetTickCount());
        LogFile(logfilename);
    }
}

void ReadConfig()
{
    SWIP ini;
    ini.Load("x360ce.ini");

    static bool once_flag = false;
    if (!once_flag)
    {
        // Read global options
        ini.Get("Options", "Disable", &g_bDisable);
        if (g_bDisable) return;

        ini.Get("Options", "UseInitBeep", &g_bInitBeep, true);

        PrintLog("Using config file:");
        PrintLog(ini.GetIniPath().c_str());

        u32 ver = 0;
        ini.Get("Options", "Version", &ver);
        if (ver != VERSION_CONFIG)
            PrintLog("WARNING: Configuration file version mismatch detected");

        once_flag = true;
    }

    // Read pad mappings
    for (DWORD i = 0; i < XUSER_MAX_COUNT; ++i)
    {
        std::string section;
        std::string key = StringFormat("PAD%u", i + 1);
        ini.Get("Mappings", key, &section);
        if (section.empty()) continue;

        u32 index = 0;
        ini.Get(section, "UserIndex", &index, INVALIDUSERINDEX);
        if (index == INVALIDUSERINDEX) index = i;

        g_pControllers[index] = new Controller;
        g_pControllers[index]->dwUserIndex = index;

        if (ReadPadConfig(g_pControllers[index], section, &ini))
            ReadPadMapping(g_pControllers[index], section, &ini);
    }
}

bool ReadPadConfig(Controller* pController, const std::string& section, SWIP* pSWIP)
{
    std::string buffer;

    pSWIP->Get(section, "ProductGUID", &buffer);
    StringToGUID(&pController->productid, buffer);

    pSWIP->Get(section, "InstanceGUID", &buffer);
    StringToGUID(&pController->instanceid, buffer);

    if (IsEqualGUID(pController->productid, GUID_NULL) ||
        IsEqualGUID(pController->instanceid, GUID_NULL))
    {
        std::string message = StringFormat("[PAD%u] Misconfigured device, check GUIDs", pController->dwUserIndex);
        PrintLog(message.c_str());
        MessageBoxA(NULL, message.c_str(), "x360ce", MB_ICONERROR);
        return false;
    }

    pSWIP->Get(section, "PassThrough", &pController->passthrough, true);
    if (pController->passthrough) return false;

    // Device type
    pSWIP->Get(section, "ControllerType", &pController->gamepadtype, 1);

    // FFB options
    pSWIP->Get(section, "UseForceFeedback", &pController->useforce);
    pSWIP->Get(section, "SwapMotor", &pController->ffb.swapmotor);
    pSWIP->Get(section, "FFBType", &pController->ffb.type);
    pSWIP->Get(section, "ForcePercent", &pController->ffb.forcepercent, 100);
    pController->ffb.forcepercent *= 0.01f;

    pSWIP->Get(section, "LeftMotorPeriod", &pController->ffb.leftPeriod, 60);
    pSWIP->Get(section, "RightMotorPeriod", &pController->ffb.rightPeriod, 20);

    return true;
}

void ReadPadMapping(Controller* pController, const std::string& section, SWIP* pSWIP)
{
    Mapping* pMapping = &pController->mapping;

    // Guide button
    pSWIP->Get(section, "GuideButton", &pMapping->guide);

    // Fire buttons
    for (u32 i = 0; i < 10; ++i)
    {
        s8 button = 0;
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
        pSWIP->Get(section, axisNames[i], &axis);
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
        s8 ret = 0;
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
        pSWIP->Get(section, triggerNames[i], &trigger);  
        ParsePrefix(trigger, &pMapping->Trigger[i].type, &pMapping->Trigger[i].id);

        pSWIP->Get(section, triggerDZNames[i], &pMapping->Trigger[i].triggerdz);

        // SeDoG mod
        pSWIP->Get(section, triggerBNames[i], &pMapping->Trigger[i].but);
    }
}

