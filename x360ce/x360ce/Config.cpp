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

void ParsePrefix(const std::string& input, MappingType* pMappingType, s8* pValue)
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

void InitLogger()
{
    SWIP ini;
    ini.Load("x360ce.ini", "x360ce");

    bool con;
    bool file;

    ini.Get("Options", "Console", &con);
    ini.Get("Options", "Log", &file);

    if (con)
        LogConsole("x360ce", legal_notice);

    if (file)
    {
        char logfilename[MAX_PATH];
        SYSTEMTIME systime;
        GetLocalTime(&systime);
        std::string processName;
        ModuleFileName(&processName);

        sprintf_s(logfilename, "x360ce_%s_%02u-%02u-%02u_%08u.log", processName.c_str(), systime.wYear,
            systime.wMonth, systime.wDay, GetTickCount());
        LogFile(logfilename, "x360ce");
    }
}

void ReadConfig()
{
    SWIP ini;
    ini.Load("x360ce.ini", "x360ce");

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
        if (!ini.Get("Mappings", key, &section))
            continue;

        u32 index = 0;
        if (!ini.Get(section, "UserIndex", &index))
            index = i;

        g_Controllers.push_back(Controller(index));

        if (ReadPadConfig(&g_Controllers.back(), section, &ini))
            ReadPadMapping(&g_Controllers.back(), section, &ini);
    }
}

bool ReadPadConfig(Controller* pController, const std::string& section, SWIP* pSWIP)
{
    std::string buffer;

    if (pSWIP->Get(section, "ProductGUID", &buffer))
        StringToGUID(&pController->m_productid, buffer);

    if (pSWIP->Get(section, "InstanceGUID", &buffer))
        StringToGUID(&pController->m_instanceid, buffer);

    if (IsEqualGUID(pController->m_productid, GUID_NULL) || IsEqualGUID(pController->m_instanceid, GUID_NULL))
    {
        std::string message = StringFormat("[PAD%u] Misconfigured device, check GUIDs", pController->m_dwUserIndex);
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
        pController->m_pForceFeedback = new ForceFeedback(pController);

        pSWIP->Get(section, "SwapMotor", &pController->m_pForceFeedback->m_SwapMotors);
        pSWIP->Get(section, "FFBType", &pController->m_pForceFeedback->m_Type);
        pSWIP->Get(section, "ForcePercent", &pController->m_pForceFeedback->m_ForcePercent, 100);
        pController->m_pForceFeedback->m_ForcePercent *= 0.01f;

        pSWIP->Get(section, "LeftMotorPeriod", &pController->m_pForceFeedback->m_LeftPeriod, 60);
        pSWIP->Get(section, "RightMotorPeriod", &pController->m_pForceFeedback->m_RightPeriod, 20);
    }

    return true;
}

void ReadPadMapping(Controller* pController, const std::string& section, SWIP* pSWIP)
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

