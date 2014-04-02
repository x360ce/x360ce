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
#include "SWIP.h"
#include "Log.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"
#include "version.h"
#include "Misc.h"

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

void ParsePrefix(const std::string& input, MappingType* mapping_type, int8_t* value)
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
			*value = strtol(input.c_str() + 1, NULL, 0);
		else
			*value = strtol(input.c_str(), NULL, 0);
	}
}

DWORD ReadGameDatabase()
{
    SWIP ini("x360ce.gdb");

    if(ini.is_open())
    {
        PrintLog(LOG_CORE,"Using game database file:");
        PrintLog(LOG_CORE,"%s", ini.get_inipath().c_str());
    }
    return ini.get_uint(exename, "HookMask");
}

void ReadConfig()
{
	SWIP ini("x360ce.ini");

    // Read global options
    g_bDisable = ini.get_bool("Options", "Disable");
    if(g_bDisable) return;

	g_bInitBeep = ini.get_bool("Options", "UseInitBeep", 1);

	bool file = ini.get_bool("Options", "Log");
	bool con = ini.get_bool("Options", "Console");
	char* logfilename = NULL;
	if (file)
	{
		logfilename = new char[MAX_PATH];
		sprintf_s(logfilename, MAX_PATH, "x360ce_%s_%u.log", exename.c_str(), GetTickCount());
	}
	InitLog(logfilename, con);
	delete[] logfilename;

	DWORD ver = ini.get_uint("Options", "Version");
	if (ver != VERSION_CONFIG)
		PrintLog(LOG_CORE, "WARNING: Configuration file version mismatch detected");

    // Simple Game Database support
    // InputHook
    if(pHooks)
    {
		bool override = ini.get_bool("InputHook", "Override");
        DWORD hookMask = ReadGameDatabase();
        if(hookMask && override == false)
        {
            pHooks->SetMask(hookMask);
            pHooks->Enable();
        }
        else
        {
			hookMask = ini.get_uint("InputHook", "HookMask");
            if(hookMask)
            {
                pHooks->SetMask(hookMask);
                pHooks->Enable();
            }
            else
            {
                bool hookCheck = ini.get_bool("InputHook", "HookLL");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_LL);

				hookCheck = ini.get_bool("InputHook", "HookCOM");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_COM);

				hookCheck = ini.get_bool("InputHook", "HookDI");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_DI);

				hookCheck = ini.get_bool("InputHook", "HookPIDVID");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_PIDVID);

				hookCheck = ini.get_bool("InputHook", "HookSA");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_SA);

				hookCheck = ini.get_bool("InputHook", "HookNAME");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_NAME);

				hookCheck = ini.get_bool("InputHook", "HookSTOP");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_STOP);

				hookCheck = ini.get_bool("InputHook", "HookWT");
                if(hookCheck) pHooks->EnableHook(iHook::HOOK_WT);

                if(pHooks->GetMask()) pHooks->Enable();
            }
        }
        if(pHooks->GetState(iHook::HOOK_PIDVID))
        {
            DWORD vid = ini.get_uint("InputHook", "FakeVID",0x045E);
			DWORD pid = ini.get_uint("InputHook", "FakePID", 0x028E);
            if(vid != 0x045E || pid != 0x28E) pHooks->SetFakePIDVID(MAKELONG(vid,pid));
        }
    }
    // Read pad mappings
    for (DWORD i = 0; i < 4; ++i)
        ReadPadConfig(i, ini);
}


void ReadPadConfig(DWORD dwUserIndex, const SWIP &ini)
{
    char section[MAX_PATH] = "Mappings";
    char key[MAX_PATH];
    sprintf_s(key,"PAD%u",dwUserIndex+1);
    std::string strBuf = ini.get_string(section, key);
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

    device.dwUserIndex = ini.get_uint(section, "UserIndex", (uint32_t)-1);
    if(device.dwUserIndex == (uint32_t)-1) device.dwUserIndex = dwUserIndex; //fallback to old indexing

    strBuf = ini.get_string(section, "ProductGUID");
    StringToGUID(device.productid,strBuf.c_str());

	strBuf = ini.get_string(section, "InstanceGUID");
    StringToGUID(device.instanceid,strBuf.c_str());

    device.useproduct = ini.get_bool(section, "UseProductGUID");
	device.passthrough = ini.get_bool(section, "PassThrough", 1);

    if(device.passthrough) return;

    // Device type
    device.gamepadtype = ini.get_uint(section, "ControllerType",1);

    // Axis to DPAD options
	device.axistodpad = ini.get_bool(section, "AxisToDPad");
    device.a2ddeadzone = ini.get_int(section, "AxisToDPadDeadZone");
	device.a2doffset = ini.get_int(section, "AxisToDPadOffset");

    // FFB options
	device.useforce = ini.get_bool(section, "UseForceFeedback");
	device.swapmotor = ini.get_bool(section, "SwapMotor");
    device.ff.type = ini.get_uint(section, "FFBType");
    device.ff.forcepercent = static_cast<float>(ini.get_uint(section, "ForcePercent",100) * 0.01);
	device.ff.leftPeriod = ini.get_uint(section, "LeftMotorPeriod", 60);
	device.ff.rightPeriod = ini.get_uint(section, "RightMotorPeriod", 20);

    /* ==================================== Mapping start ============================================*/

    // Guide button
	mapping.guide = ini.get_int(section, "GuideButton", 0);

    // Fire buttons
    for (int8_t i=0; i<10; ++i)
		mapping.Button[i] = ini.get_int(section, buttonNames[i]) - 1;

    // D-PAD
	mapping.DpadPOV = ini.get_int(section, "D-pad POV");
    if(mapping.DpadPOV == 0)
    {
        for (int8_t i=0; i<4; ++i)
        {
            // D-PAD directions
			int16_t val = ini.get_int(section, povNames[i], -1);
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
		std::string axis = ini.get_string(section, axisNames[i]);
		ParsePrefix(axis, &mapping.Axis[i].analogType, &mapping.Axis[i].id);
        
        // DeadZones
		device.axisdeadzone[i] = ini.get_int(section, axisDZNames[i]);

        // Anti DeadZones
		device.antideadzone[i] = ini.get_int(section, axisADZNames[i]);

        // Linearity
		device.axislinear[i] = ini.get_int(section, axisLNames[i]);

        // Axis to button mappings
		char ret = ini.get_int(section, axisBNames[i * 2]);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].positiveButtonID = ret - 1;
        }
		ret = ini.get_int(section, axisBNames[i * 2 + 1]);
        if (ret > 0)
        {
            mapping.Axis[i].hasDigital = true;
            mapping.Axis[i].negativeButtonID = ret - 1;
        }
    }

    // Triggers
	std::string trigger_left = ini.get_string(section, "Left Trigger");
	std::string trigger_right = ini.get_string(section, "Right Trigger");

	ParsePrefix(trigger_left, &mapping.Trigger[0].type, &mapping.Trigger[0].id);
	ParsePrefix(trigger_right, &mapping.Trigger[1].type, &mapping.Trigger[1].id);

	device.triggerdz[0] = ini.get_uint(section, "Left Trigger DZ");
	device.triggerdz[1] = ini.get_uint(section, "Right Trigger DZ");

    // SeDoG mod
	mapping.Trigger[0].but = ini.get_int(section, "Left Trigger But");
	mapping.Trigger[1].but = ini.get_int(section, "Right Trigger But");
}
