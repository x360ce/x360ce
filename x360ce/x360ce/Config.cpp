/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#include "stdafx.h"
#include "globals.h"
#include "Utilities\Ini.h"
#include "Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"
#include "version.h"

extern iHook* pHooks;
extern std::string exename;
extern std::vector<DInputDevice> g_Devices;

bool g_bInitBeep = false;
bool g_bNative = false;
bool g_bDisable = false;
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
MappingType getMappingType(const char s)
{
    if (s == 'a') return AXIS;      // Axis
    if (s == 's') return SLIDER;	// Slider
    if (s == 'x') return HAXIS;     // Half range axis
    if (s == 'h') return HSLIDER;	// Half range slider
    if (s == 'z') return CBUT;      //
    return DIGITAL;                 // Digital
}

DWORD ReadGameDatabase()
{
    Ini ini("x360ce.gdb");

    if(ini.is_open())
    {
        PrintLog(LOG_CORE,"Using game database file:");
        PrintLog(LOG_CORE,"%s", ini.GetFilename());
    }
    return ini.GetDword(exename.c_str(), "HookMask");
}

void ReadConfig(bool skip)
{
    Ini ini("x360ce.ini");

    // Read global options
    g_bDisable = ini.GetBool("Options", "Disable");
    if(g_bDisable) return;

    DWORD ver = ini.GetDword("Options", "Version");
    if(ver != VERSION_CONFIG && skip == false)
        MessageBoxA(NULL,"Configuration file version does not match x360ce version.\n"
                    "Some options may not work until configuration file will be updated.\n"
                    ,"x360ce - Warning", MB_ICONWARNING);

    g_bInitBeep = ini.GetBool("Options", "UseInitBeep",1);

    bool local = ini.GetBool("Options", "LocalLog");
    bool log = ini.GetBool("Options", "Log");
    bool con = ini.GetBool("Options", "Console");
    InitLog(log,con,local);

    // Simple Game Database support
    // InputHook
    if(pHooks)
    {
        bool override = ini.GetBool("InputHook", "Override");
        DWORD hookMask = ReadGameDatabase();
        if(hookMask && override == false)
        {
            pHooks->SetMask(hookMask);
            pHooks->Enable();
        }
        else
        {
            hookMask = ini.GetDword("InputHook", "HookMask");
            if(hookMask)
            {
                pHooks->SetMask(hookMask);
                pHooks->Enable();
            }
            else
            {
                bool hookCheck = ini.GetBool("InputHook", "HookLL");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_LL);

                hookCheck = ini.GetBool("InputHook", "HookCOM");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_COM);

                hookCheck = ini.GetBool("InputHook", "HookDI");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_DI);

                hookCheck = ini.GetBool("InputHook", "HookPIDVID");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_PIDVID);

                hookCheck = ini.GetBool("InputHook", "HookSA");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_SA);

                hookCheck = ini.GetBool("InputHook", "HookNAME");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_NAME);

                hookCheck = ini.GetBool("InputHook", "HookSTOP");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_STOP);

                hookCheck = ini.GetBool("InputHook", "HookWT");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_WT);

                if(pHooks->GetMask()) pHooks->Enable();
            }
        }
        if(pHooks->GetState(iHook::HOOK_PIDVID))
        {
            DWORD vid = ini.GetDword("InputHook", "FakeVID",0x045E);
            DWORD pid = ini.GetDword("InputHook", "FakePID",0x028E);
            if(vid != 0x045E || pid != 0x28E) pHooks->SetFakePIDVID(MAKELONG(vid,pid));
        }
    }
    // Read pad mappings
    for (DWORD i = 0; i < 4; ++i)
        ReadPadConfig(i, ini);
}


void ReadPadConfig(DWORD dwUserIndex, Ini &ini)
{
    char section[MAX_PATH] = "Mappings";
    char key[MAX_PATH];
    sprintf_s(key,"PAD%u",dwUserIndex+1);
    std::string strBuf = ini.GetString(section, key);
    if(strBuf.empty()) return;

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
    strcpy_s(section,strBuf.c_str());

    device.dwUserIndex = ini.GetDword(section, "UserIndex", (uint32_t)-1);
    if(device.dwUserIndex == (uint32_t)-1) device.dwUserIndex = dwUserIndex; //fallback to old indexing

    strBuf = ini.GetString(section, "ProductGUID");
    StringToGUID(device.productid,strBuf.c_str());

    strBuf = ini.GetString(section, "InstanceGUID");
    StringToGUID(device.instanceid,strBuf.c_str());

    device.useproduct = ini.GetBool(section, "UseProductGUID");
    device.passthrough = ini.GetBool(section, "PassThrough",1);

    if(device.passthrough) return;

    // Device type
    device.gamepadtype = ini.GetUByte(section, "ControllerType",1);

    // Axis to DPAD options
    device.axistodpad = ini.GetBool(section, "AxisToDPad");
    device.a2ddeadzone = ini.GetLong(section, "AxisToDPadDeadZone");
    device.a2doffset = ini.GetLong(section, "AxisToDPadOffset");

    // FFB options
    device.useforce = ini.GetBool(section, "UseForceFeedback");
    device.swapmotor = ini.GetBool(section, "SwapMotor");
    device.ff.type = ini.GetUByte(section, "FFBType");
    device.ff.forcepercent = static_cast<float>(ini.GetLong(section, "ForcePercent",100) * 0.01);
    device.ff.leftPeriod = ini.GetLong(section, "LeftMotorPeriod",60);
    device.ff.rightPeriod = ini.GetLong(section, "RightMotorPeriod",20);

    /* ==================================== Mapping start ============================================*/

    // Guide button
    mapping.guide = ini.GetSByte(section, "GuideButton",0);

    // Fire buttons
    for (int8_t i=0; i<10; ++i)
        mapping.Button[i] = ini.GetSByte(section,buttonNames[i]) - 1;

    // D-PAD
    mapping.DpadPOV = ini.GetUByte(section, "D-pad POV");
    if(mapping.DpadPOV == 0)
    {
        for (int8_t i=0; i<4; ++i)
        {
            // D-PAD directions
            int16_t val = ini.GetShort(section, povNames[i], -1);
            if(val > 0 && val < 128)
            {
                mapping.pov[i] = val - 1;
                mapping.PovIsButton = true;
            }
            else if(val > -1)
            {
                mapping.pov[i] = val;
                mapping.PovIsButton = false;
            }
        }
    }

    for (int8_t i=0; i<4; ++i)
    {
        // Axes
        mapping.Axis[i].id = ini.GetSByte(section, axisNames[i]);
        mapping.Axis[i].analogType = getMappingType(ini.GetLastPrefix());

        // DeadZones
        device.axisdeadzone[i] = ini.GetShort(section, axisDZNames[i]);

        // Anti DeadZones
        device.antideadzone[i] = ini.GetShort(section, axisADZNames[i]);

        // Linearity
        device.axislinear[i] = ini.GetShort(section, axisLNames[i]);

        // Axis to button mappings
        char ret = ini.GetSByte(section, axisBNames[i*2]);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].positiveButtonID = ret - 1;
        }
        ret = ini.GetSByte(section, axisBNames[i*2+1]);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].negativeButtonID = ret - 1;
        }
    }

    // Triggers
    mapping.Trigger[0].id = ini.GetSByte(section, "Left Trigger");
    mapping.Trigger[0].type = getMappingType(ini.GetLastPrefix());
    if(mapping.Trigger[0].type == DIGITAL) mapping.Trigger[0].id--;

    mapping.Trigger[1].id = ini.GetSByte(section, "Right Trigger");
    mapping.Trigger[1].type = getMappingType(ini.GetLastPrefix());
    if(mapping.Trigger[1].type == DIGITAL) mapping.Trigger[1].id--;

    device.triggerdz[0] = ini.GetUByte(section, "Left Trigger DZ");
    device.triggerdz[1] = ini.GetUByte(section, "Right Trigger DZ");

    // SeDoG mod
    mapping.Trigger[0].but = ini.GetSByte(section, "Left Trigger But");
    mapping.Trigger[1].but = ini.GetSByte(section, "Right Trigger But");
}