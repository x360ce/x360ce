#include "stdafx.h"
#include "Common.h"
#include "SWIP.h"
#include "Logger.h"
#include "version.h"
#include "Utils.h"
#include "InputHook.h"

#include "DirectInput.h"
#include "SWIP.h"
#include "Config.h"
#include "x360ce.h"

bool g_bInitBeep = true;
bool g_bNative = false;
bool g_bDisable = false;
bool g_bContinue = false;
std::vector<Mapping> g_Mappings;

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
        PrintLog("%s", ini.GetIniPath().c_str());

        ini.Get(exename, "HookMask", &out);
    }
    return out;
}

void ReadConfig(bool reset)
{
    SWIP ini;
    ini.Load("x360ce.ini");

    if (!reset)
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
            sprintf_s(logfilename, "x360ce_%s_%u.log", exename.c_str(), GetTickCount());
            LogFile(logfilename);
        }

        PrintLog("Using config file:");
        PrintLog("%s", ini.GetIniPath().c_str());

        u32 ver = 0;
        ini.Get("Options", "Version", &ver);
        if (ver != VERSION_CONFIG)
            PrintLog("WARNING: Configuration file version mismatch detected");
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
    for (DWORD i = 0; i < 4; ++i)
        ReadPadConfig(i, &ini);
}

void ReadPadConfig(DWORD dwUserIndex, SWIP *ini)
{
    char section[MAX_PATH] = "Mappings";
    char key[MAX_PATH];
    sprintf_s(key, "PAD%u", dwUserIndex + 1);
    std::string strBuf;
    ini->Get(section, key, &strBuf);
    if (strBuf.empty()) return;

#if _MSC_VER < 1700
    g_Devices.push_back(DInputDevice());
    DInputDevice& device = g_Devices.back();

    g_Mappings.push_back(Mapping());
    Mapping& mapping = g_Mappings.back();
#else
    g_Devices.emplace_back();
    DInputDevice& device = g_Devices.back();

    g_Mappings.emplace_back();
    Mapping& mapping = g_Mappings.back();
#endif

    //store value as section name
    strcpy_s(section, strBuf.c_str());

    device.dwUserIndex = 0;
    ini->Get(section, "UserIndex", &device.dwUserIndex, (u32)-1);
    if (device.dwUserIndex == (u32)-1) device.dwUserIndex = dwUserIndex; //fallback to old indexing

    ini->Get(section, "ProductGUID", &strBuf);
    if (strBuf.empty()) PrintLog("ProductGUID is empty");
    else StringToGUID(&device.productid, strBuf.c_str());

    ini->Get(section, "InstanceGUID", &strBuf);
    if (strBuf.empty()) PrintLog("InstanceGUID is empty");
    else StringToGUID(&device.instanceid, strBuf.c_str());

    ini->Get(section, "UseProductGUID", &device.useproduct);
    ini->Get(section, "PassThrough", &device.passthrough, true);

    if (device.passthrough) return;

    // Device type
    ini->Get(section, "ControllerType", &device.gamepadtype, 1);

    // Axis to DPAD options
    ini->Get(section, "AxisToDPad", &device.axistodpad);
    ini->Get(section, "AxisToDPadDeadZone", &device.a2ddeadzone);
    ini->Get(section, "AxisToDPadOffset", &device.a2doffset);

    // FFB options
    ini->Get(section, "UseForceFeedback", &device.useforce);
    ini->Get(section, "SwapMotor", &device.swapmotor);
    ini->Get(section, "FFBType", &device.ff.type);
    ini->Get(section, "ForcePercent", &device.ff.forcepercent, 100);
    device.ff.forcepercent *= 0.01f;

    ini->Get(section, "LeftMotorPeriod", &device.ff.leftPeriod, 60);
    ini->Get(section, "RightMotorPeriod", &device.ff.rightPeriod, 20);

    /* ==================================== Mapping start ============================================*/

    // Guide button
    ini->Get(section, "GuideButton", &mapping.guide);

    // Fire buttons
    for (s8 i = 0; i < 10; ++i)
    {
        s8 button = 0;
        ini->Get(section, buttonNames[i], &button);
        mapping.Button[i] = button - 1;
    }

    // D-PAD
    ini->Get(section, "D-pad POV", &mapping.DpadPOV);
    if (mapping.DpadPOV == 0)
    {
        for (s8 i = 0; i < 4; ++i)
        {
            // D-PAD directions
            s16 val = 0;
            ini->Get(section, povNames[i], &val, - 1);
            if (val > 0 && val < 128)
            {
                mapping.pov[i] = val - 1;
                mapping.PovIsButton = true;
            }
            else if (val > -1)
            {
                mapping.pov[i] = val;
                mapping.PovIsButton = false;
            }
        }
    }

    for (s8 i = 0; i < 4; ++i)
    {
        // Axes
        std::string axis;
        ini->Get(section, axisNames[i], &axis);
        ParsePrefix(axis, &mapping.Axis[i].analogType, &mapping.Axis[i].id);

        // DeadZones
        ini->Get(section, axisDZNames[i], &device.axisdeadzone[i]);

        // Anti DeadZones
        ini->Get(section, axisADZNames[i], &device.antideadzone[i]);

        // Linearity
        ini->Get(section, axisLNames[i], &device.axislinear[i]);

        // Axis to button mappings
        s8 ret = 0;
        ini->Get(section, axisBNames[i * 2], &ret);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].positiveButtonID = ret - 1;
        }
        ini->Get(section, axisBNames[i * 2 + 1], &ret);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].negativeButtonID = ret - 1;
        }
    }

    // Triggers
    std::string trigger_left;
    ini->Get(section, "Left Trigger", &trigger_left);

    std::string trigger_right;
    ini->Get(section, "Right Trigger", &trigger_right);

    ParsePrefix(trigger_left, &mapping.Trigger[0].type, &mapping.Trigger[0].id);
    ParsePrefix(trigger_right, &mapping.Trigger[1].type, &mapping.Trigger[1].id);

    ini->Get(section, "Left Trigger DZ", &device.triggerdz[0]);
    ini->Get(section, "Right Trigger DZ", &device.triggerdz[1]);

    // SeDoG mod
    ini->Get(section, "Left Trigger But", &mapping.Trigger[0].but);
    ini->Get(section, "Right Trigger But", &mapping.Trigger[1].but);
}
