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
bool g_bNative = false;
bool g_bDisable = false;
bool g_bContinue = false;

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

DWORD ReadGameDatabase()
{
    u32 out = 0;
    SWIP ini;
    if (ini.Load("x360ce.gdb"))
    {
        PrintLog("Using game database file:");
        PrintLog(ini.GetIniPath().c_str());

        ini.Get(exename, "HookMask", &out);
    }
    return out;
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

        ini.Get("Options", "Continue", &g_bContinue);
        ini.Get("Options", "UseInitBeep", &g_bInitBeep, true);

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
            sprintf_s(logfilename, "x360ce_%s_%02u-%02u-%02u_%08u.log", exename.c_str(), systime.wYear,
                systime.wMonth, systime.wDay, GetTickCount());
            LogFile(logfilename);
        }

        PrintLog("Using config file:");
        PrintLog(ini.GetIniPath().c_str());

        u32 ver = 0;
        ini.Get("Options", "Version", &ver);
        if (ver != VERSION_CONFIG)
            PrintLog("WARNING: Configuration file version mismatch detected");

        once_flag = true;
    }

    // Simple Game Database support
    // InputHook

    bool hook_override = false;
    ini.Get("InputHook", "Override", &hook_override);
    u32 hookMask = ReadGameDatabase();
    if (hookMask && hook_override == false)
    {
        g_iHook.SetMask(hookMask);
        g_iHook.Enable();
    }
    else
    {
        ini.Get("InputHook", "HookMask", &hookMask);
        if (hookMask)
        {
            g_iHook.SetMask(hookMask);
            g_iHook.Enable();
        }
        else
        {
            bool hookCheck = 0;

            ini.Get("InputHook", "HookLL", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_LL);

            ini.Get("InputHook", "HookCOM", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_COM);

            ini.Get("InputHook", "HookDI", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_DI);

            ini.Get("InputHook", "HookPIDVID", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_PIDVID);

            ini.Get("InputHook", "HookSA", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_SA);

            ini.Get("InputHook", "HookNAME", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_NAME);

            ini.Get("InputHook", "HookSTOP", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_STOP);

            ini.Get("InputHook", "HookWT", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_WT);

            ini.Get("InputHook", "HookNoTimeout", &hookCheck);
            if (hookCheck) g_iHook.EnableHook(InputHook::HOOK_NOTIMEOUT);

            if (g_iHook.GetMask()) g_iHook.Enable();
        }
    }
    if (g_iHook.GetState(InputHook::HOOK_PIDVID))
    {
        u32 vid = 0x045E;
        u32 pid = 0x028E;
        ini.Get("InputHook", "FakeVID", &vid, 0x045E);
        ini.Get("InputHook", "FakePID", &pid, 0x028E);

        if (vid != 0x045E || pid != 0x28E) g_iHook.SetFakePIDVID(MAKELONG(vid, pid));
    }

    u32 timeout = 60;
    ini.Get("InputHook", "Timeout", &timeout, 60);
    g_iHook.SetTimeout(timeout);

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

        ReadPadConfig(g_pControllers[index], section, &ini);
        ReadPadMapping(g_pControllers[index], section, &ini);
    }
}

void ReadPadMapping(Controller* pController, const std::string& section, SWIP *ini)
{
    Mapping* pMapping = &pController->mapping;

    // Guide button
    ini->Get(section, "GuideButton", &pMapping->guide);

    // Fire buttons
    for (s8 i = 0; i < 10; ++i)
    {
        s8 button = 0;
        ini->Get(section, buttonNames[i], &button);
        pMapping->Button[i] = button - 1;
    }

    // D-PAD
    ini->Get(section, "D-pad POV", &pMapping->DpadPOV);
    if (pMapping->DpadPOV == 0)
    {
        for (s8 i = 0; i < 4; ++i)
        {
            // D-PAD directions
            s16 val = 0;
            ini->Get(section, povNames[i], &val, -1);
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

    for (s8 i = 0; i < 4; ++i)
    {
        // Axes
        std::string axis;
        ini->Get(section, axisNames[i], &axis);
        ParsePrefix(axis, &pMapping->Axis[i].analogType, &pMapping->Axis[i].id);

        // DeadZones
        ini->Get(section, axisDZNames[i], &pMapping->Axis[i].axisdeadzone);

        // Anti DeadZones
        ini->Get(section, axisADZNames[i], &pMapping->Axis[i].antideadzone);

        // Linearity
        ini->Get(section, axisLNames[i], &pMapping->Axis[i].axislinear);

        // Axis to DPAD options
        ini->Get(section, "AxisToDPad", &pMapping->Axis[i].axistodpad);
        ini->Get(section, "AxisToDPadDeadZone", &pMapping->Axis[i].a2ddeadzone);
        ini->Get(section, "AxisToDPadOffset", &pMapping->Axis[i].a2doffset);

        // Axis to button mappings
        s8 ret = 0;
        ini->Get(section, axisBNames[i * 2], &ret);
        if (ret > 0)
        {
            pMapping->Axis[i].hasDigital = true;
            pMapping->Axis[i].positiveButtonID = ret - 1;
        }
        ini->Get(section, axisBNames[i * 2 + 1], &ret);
        if (ret > 0)
        {
            pMapping->Axis[i].hasDigital = true;
            pMapping->Axis[i].negativeButtonID = ret - 1;
        }
    }

    // Triggers
    std::string trigger_left;
    ini->Get(section, "Left Trigger", &trigger_left);

    std::string trigger_right;
    ini->Get(section, "Right Trigger", &trigger_right);

    ParsePrefix(trigger_left, &pMapping->Trigger[0].type, &pMapping->Trigger[0].id);
    ParsePrefix(trigger_right, &pMapping->Trigger[1].type, &pMapping->Trigger[1].id);

    ini->Get(section, "Left Trigger DZ", &pMapping->Trigger[0].triggerdz);
    ini->Get(section, "Right Trigger DZ", &pMapping->Trigger[0].triggerdz);

    // SeDoG mod
    ini->Get(section, "Left Trigger But", &pMapping->Trigger[0].but);
    ini->Get(section, "Right Trigger But", &pMapping->Trigger[1].but);
}

void ReadPadConfig(Controller* pController, const std::string& section, SWIP *ini)
{
    std::string buffer;

    ini->Get(section, "ProductGUID", &buffer);
    if (buffer.empty()) PrintLog("ProductGUID is empty");
    else StringToGUID(&pController->productid, buffer);

    ini->Get(section, "InstanceGUID", &buffer);
    if (buffer.empty()) PrintLog("InstanceGUID is empty");
    else StringToGUID(&pController->instanceid, buffer);

    ini->Get(section, "UseProductGUID", &pController->useproduct);
    ini->Get(section, "PassThrough", &pController->passthrough, true);

    if (pController->passthrough) return;

    // Device type
    ini->Get(section, "ControllerType", &pController->gamepadtype, 1);

    // FFB options
    ini->Get(section, "UseForceFeedback", &pController->useforce);
    ini->Get(section, "SwapMotor", &pController->ffb.swapmotor);
    ini->Get(section, "FFBType", &pController->ffb.type);
    ini->Get(section, "ForcePercent", &pController->ffb.forcepercent, 100);
    pController->ffb.forcepercent *= 0.01f;

    ini->Get(section, "LeftMotorPeriod", &pController->ffb.leftPeriod, 60);
    ini->Get(section, "RightMotorPeriod", &pController->ffb.rightPeriod, 20);
}
