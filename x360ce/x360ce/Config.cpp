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

static const char* const padNames[] =
{
    "PAD1",
    "PAD2",
    "PAD3",
    "PAD4",
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
    return ini.GetDword(exename.c_str(), "HookMask",0);
}

void ReadConfig(bool skip)
{
    Ini ini("x360ce.ini");

    // Read global options
    g_bDisable = ini.GetBool("Options", "Disable",0);
    if(g_bDisable) return;

    DWORD ver = ini.GetDword("Options", "Version",0);
    if(ver != VERSION_CONFIG && skip == false)
        MessageBoxA(NULL,"Configuration file version does not match x360ce version.\n"
                    "Some options may not work until configuration file will be updated.\n"
                    ,"x360ce - Warning", MB_ICONWARNING);

    g_bInitBeep = ini.GetBool("Options", "UseInitBeep",1);

    bool local = ini.GetBool("Options", "LocalLog",0);
    bool log = ini.GetBool("Options", "Log",0);
    bool con = ini.GetBool("Options", "Console",0);
    InitLog(log,con,local);

    // Simple Game Database support
    // InputHook
    if(pHooks)
    {
        bool overrride = ini.GetBool("InputHook", "Override",0);
        DWORD hookMask = ReadGameDatabase();
        if(hookMask && overrride == false)
        {
            pHooks->SetMask(hookMask);
            pHooks->Enable();
        }
        else
        {
            hookMask = ini.GetDword("InputHook", "HookMask",0);
            if(hookMask)
            {
                pHooks->SetMask(hookMask);
                pHooks->Enable();
            }
            else
            {
                bool hookCheck = ini.GetBool("InputHook", "HookLL",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_LL);

                hookCheck = ini.GetBool("InputHook", "HookCOM",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_COM);

                hookCheck = ini.GetBool("InputHook", "HookDI",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_DI);

                hookCheck = ini.GetBool("InputHook", "HookPIDVID",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_PIDVID);

                hookCheck = ini.GetBool("InputHook", "HookSA",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_SA);

                hookCheck = ini.GetBool("InputHook", "HookNAME",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_NAME);

                hookCheck = ini.GetBool("InputHook", "HookSTOP",0);
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_STOP);

                hookCheck = ini.GetBool("InputHook", "HookWT",0);
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


void ReadPadConfig(DWORD idx, Ini &ini)
{
    DWORD ret;
    char section[MAX_PATH] = "Mappings";
    char key[MAX_PATH];
    sprintf_s(key,"PAD%u",idx+1);

    char buffer[MAX_PATH];

    ret = ini.GetString(section, key, buffer);
    if(!ret) return;

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
    strcpy_s(section,buffer);

    device.dwUserIndex = idx;

    ini.GetString(section, "ProductGUID", buffer, 0);
    StringToGUID(buffer,device.productid);

    ini.GetString(section, "InstanceGUID", buffer, 0);
    StringToGUID(buffer,device.instanceid);

    device.useproduct = ini.GetBool(section, "UseProductGUID",0);

    device.passthrough = ini.GetBool(section, "PassThrough",1);

    if(device.passthrough)
    {
        mapping.enabled = false;
        g_Mappings.push_back(mapping);
        g_Devices.push_back(device);
        return;
    }

    if (!(IsEqualGUID(device.productid,GUID_NULL))
            && !(IsEqualGUID(device.instanceid,GUID_NULL)))
    {
        mapping.enabled = true;
    }
    else return;


    device.axistodpad = ini.GetBool(section, "AxisToDPad",0);
    device.triggerdeadzone = ini.GetLong(section, "TriggerDeadzone",0);
    device.gamepadtype = static_cast<BYTE>(ini.GetLong(section, "ControllerType",1));;
    device.a2ddeadzone = static_cast<INT>(ini.GetLong(section, "AxisToDPadDeadZone",0));
    device.a2doffset = static_cast<INT>(ini.GetLong(section, "AxisToDPadOffset",0));


    // FFB
    device.useforce = ini.GetBool(section, "UseForceFeedback",0);
    device.swapmotor = ini.GetBool(section, "SwapMotor",0);
    device.ff.type = (BYTE) ini.GetLong(section, "FFBType",0);
    device.ff.forcepercent = static_cast<FLOAT>(ini.GetLong(section, "ForcePercent",100) * 0.01);
    device.ff.leftPeriod = ini.GetLong(section, "LeftMotorPeriod",60);
    device.ff.rightPeriod = ini.GetLong(section, "RightMotorPeriod",20);

    // Mappings start

    // Guide button
    mapping.guide = static_cast<WORD>(ini.GetLong(section, "GuideButton",0));

    // Fire buttons
    for (INT i=0; i<10; ++i)
    {
        if (ini.GetLong(section,buttonNames[i],0) > 0)
        {
            mapping.Button[i] = static_cast<WORD>(ini.GetLong(section,buttonNames[i],0)) - 1;
        }
    }

    for (INT i=0; i<4; ++i)
    {
        // D-PAD directions
        int val = ini.GetLong(section, povNames[i], -1);
        if(val > 0 && val < 100)
        {
            mapping.pov[i] = static_cast<WORD>(val - 1);
            mapping.PovIsButton = true;
        }
        else if(val > 0)
        {
            mapping.pov[i] = static_cast<WORD>(val);
            mapping.PovIsButton = false;
        }

        // Axes
        mapping.Axis[i].id = ini.GetLong(section, axisNames[i]);
        mapping.Axis[i].analogType = getMappingType(ini.GetLastPrefix());

        // DeadZones
        device.axisdeadzone[i] =  static_cast<SHORT>(ini.GetLong(section, axisDZNames[i], 0));

        // Anti DeadZones
        SHORT tmp = static_cast<SHORT>(ini.GetLong(section, axisADZNames[i], 0));
        device.antideadzone[i] =  static_cast<SHORT>(((clamp))(tmp,0,32767));

        // Linears
        device.axislinear[i] = static_cast<SHORT>(ini.GetLong(section, axisLNames[i], 0));

        // Axis to button mappings
        INT ret = ini.GetLong(section, axisBNames[i*2]);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].positiveButtonID = ret - 1;
        }
        ret = ini.GetLong(section, axisBNames[i*2+1]);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].negativeButtonID = ret - 1;
        }
    }

    // Triggers
    mapping.Trigger[0].id = ini.GetLong(section, "Left Trigger");
    mapping.Trigger[0].type = getMappingType(ini.GetLastPrefix());

    mapping.Trigger[1].id = ini.GetLong(section, "Right Trigger");
    mapping.Trigger[1].type = getMappingType(ini.GetLastPrefix());

    // SeDoG mod
    mapping.Trigger[0].but = ini.GetLong(section, "Left Trigger But");
    mapping.Trigger[1].but = ini.GetLong(section, "Right Trigger But");

    // D-PAD
    if (ini.GetLong(section, "D-pad POV") > 0)
    {
        mapping.DpadPOV = static_cast<WORD>(ini.GetLong(section, "D-pad POV",0)) - 1;
    }
    else mapping.DpadPOV = (WORD) -1;
}