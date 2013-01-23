/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2011 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Found-
 *  ation, either version 3 of the License, or (at your option) any later version.
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
#include "Utilities\Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"

BOOL bInitBeep=0;
WORD wNativeMode=0;

BOOL g_Disable;

extern iHook g_iHook;

static LPCWSTR buttonNames[] =
{
    L"A",
    L"B",
    L"X",
    L"Y",
    L"Left Shoulder",
    L"Right Shoulder",
    L"Back",
    L"Start",
    L"Left Thumb",
    L"Right Thumb",
};

static LPCWSTR povNames[] =
{
    L"D-pad Up",
    L"D-pad Down",
    L"D-pad Left",
    L"D-pad Right"
};

static LPCWSTR axisNames[] =
{
    L"Left Analog X",
    L"Left Analog Y",
    L"Right Analog X",
    L"Right Analog Y"
};

static LPCWSTR axisDZNames[] =
{
    L"Left Analog X DeadZone",
    L"Left Analog Y DeadZone",
    L"Right Analog X DeadZone",
    L"Right Analog Y DeadZone",
};

static LPCWSTR axisADZNames[] =
{
    L"Left Analog X AntiDeadZone",
    L"Left Analog Y AntiDeadZone",
    L"Right Analog X AntiDeadZone",
    L"Right Analog Y AntiDeadZone",
};

static LPCWSTR axisLNames[] =
{
    L"Left Analog X Linear",
    L"Left Analog Y Linear",
    L"Right Analog X Linear",
    L"Right Analog Y Linear"
};

static LPCWSTR axisBNames[] =
{
    L"Left Analog X+ Button",
    L"Left Analog X- Button",
    L"Left Analog Y+ Button",
    L"Left Analog Y- Button",
    L"Right Analog X+ Button",
    L"Right Analog X- Button",
    L"Right Analog Y+ Button",
    L"Right Analog Y- Button"
};

static LPCWSTR padNames[] =
{
    L"PAD1",
    L"PAD2",
    L"PAD3",
    L"PAD4",
};

//GamepadMap GamepadMapping[4];
std::vector<GamepadMap> GamepadMapping;

void ReadConfig(InI &ini)
{

    // Read global options
	g_Disable = ini.ReadLongFromFile(L"Options", L"Disable",0);
    bInitBeep = static_cast<BOOL>(ini.ReadLongFromFile(L"Options", L"UseInitBeep",1));
    LogEnable(static_cast<BOOL>(ini.ReadLongFromFile(L"Options", L"Log",0)));
    enableconsole = static_cast<BOOL>(ini.ReadLongFromFile(L"Options", L"Console",0));
	if(g_Disable) return;

    //InputHook
	DWORD tmp;
    tmp = ini.ReadLongFromFile(L"InputHook", L"HookMode",0);

	//TODO: make this nicer
	if(tmp == 1) g_iHook.SetMode(iHook::HOOK_WMI);
	if(tmp == 2) g_iHook.SetMode(iHook::HOOK_WMI | iHook::HOOK_VIDPID | iHook::HOOK_DI);
	if(tmp == 3) g_iHook.SetMode(iHook::HOOK_WMI | iHook::HOOK_VIDPID | iHook::HOOK_DI | iHook::HOOK_NAME);
	if(tmp >  3) g_iHook.SetMode(iHook::HOOK_WMI | iHook::HOOK_VIDPID | iHook::HOOK_DI | iHook::HOOK_NAME | iHook::HOOK_STOP);

	if(tmp > 0) g_iHook.Enable();
	
	tmp  = ini.ReadLongFromFile(L"InputHook", L"HookUseANSI",0);
	if(tmp == 1) g_iHook.SetMode(iHook::HOOK_WMIA);

    tmp = ini.ReadLongFromFile(L"InputHook", L"HookWinTrust",0);
	if(tmp == 1) g_iHook.SetMode(iHook::HOOK_TRUST);

	if(g_iHook.GetState())
	{
		DWORD vid = ini.ReadLongFromFile(L"InputHook", L"HookVID",0x045E);
		DWORD pid = ini.ReadLongFromFile(L"InputHook", L"HookPID",0x028E);
		if(vid != 0x045E || pid != 0x28E) g_iHook.SetFakeVIDPID(MAKELONG(vid,pid));
	}

    // Read pad mappings
    for (DWORD i = 0; i < 4; ++i)
        ReadPadConfig(i, ini);
}


void ReadPadConfig(DWORD idx, InI &ini)
{
    DWORD ret;
    WCHAR section[MAX_PATH] = L"Mappings";
    WCHAR key[MAX_PATH];
    swprintf_s(key,L"PAD%u",idx+1);

    WCHAR buffer[MAX_PATH];

    ret = ini.ReadStringFromFile(section, key, buffer);
    if(!ret) return;

	DINPUT_GAMEPAD gamepad;
	GamepadMap PadMap;

    //store value as section name
    wcscpy_s(section,buffer);

    ini.ReadStringFromFile(section, L"ProductGUID", buffer, 0);
    StringToGUID(&gamepad.productGUID,buffer);

    ini.ReadStringFromFile(section, L"InstanceGUID", buffer, 0);
    StringToGUID(&gamepad.instanceGUID,buffer);

	//TODO rework pass-trough handling code
	for (int i=0; i<4; ++i)
	{
		SHORT tmp = static_cast<SHORT>(ini.ReadLongFromFile(section, axisADZNames[i], 0));
		gamepad.antidz[i] =  static_cast<SHORT>(clamp(tmp,0,32767));
	}

    gamepad.passthrough = (ini.ReadLongFromFile(section, L"PassThrough",1) !=0);

    if(gamepad.passthrough)
    {
        wNativeMode = 1;
        //gamepad.configured = true;
        PadMap.enabled = false;
		GamepadMapping.push_back(PadMap);
		g_Gamepads.push_back(gamepad);
        return;
    }

    if (!(IsEqualGUID(gamepad.productGUID,GUID_NULL)) && !(IsEqualGUID(gamepad.instanceGUID,GUID_NULL)))
    {
        //gamepad.configured = true;
        PadMap.enabled = true;

    }
    else return;


    gamepad.dwUserIndex = idx;

	gamepad.useProduct = ini.ReadLongFromFile(section, L"UseProductGUID",0)!=0;
    gamepad.ff.type = (BYTE) ini.ReadLongFromFile(section, L"FFBType",0);
    gamepad.swapmotor = ini.ReadLongFromFile(section, L"SwapMotor",0);
    gamepad.tdeadzone = ini.ReadLongFromFile(section, L"TriggerDeadzone",0);
    gamepad.ff.useforce = static_cast<BOOL>(ini.ReadLongFromFile(section, L"UseForceFeedback",0));
    gamepad.gamepadtype = static_cast<BYTE>(ini.ReadLongFromFile(section, L"ControllerType",1));
    gamepad.axistodpad = (ini.ReadLongFromFile(section, L"AxisToDPad",0) !=0);
    gamepad.axistodpaddeadzone = static_cast<INT>(ini.ReadLongFromFile(section, L"AxisToDPadDeadZone",0));
    gamepad.axistodpadoffset = static_cast<INT>(ini.ReadLongFromFile(section, L"AxisToDPadOffset",0));
    gamepad.ff.forcepercent = static_cast<FLOAT>(ini.ReadLongFromFile(section, L"ForcePercent",100) * 0.01);
    gamepad.ff.leftPeriod = ini.ReadLongFromFile(section, L"LeftMotorPeriod",60);
    gamepad.ff.rightPeriod = ini.ReadLongFromFile(section, L"RightMotorPeriod",20);

	PadMap.guide = static_cast<WORD>(ini.ReadLongFromFile(section, L"GuideButton",0));

	//memset(PadMap.Button,-1,sizeof(PadMap.Button));

    for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].type = NONE;

    ///////////////////////////////////////////////////////////////////////////////////////
    for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].but = -1;

    ///////////////////////////////////////////////////////////////////////////////////////

    PadMap.DpadPOV = (WORD) -1;

    for (INT i=0; i<10; ++i)
    {
        if (ini.ReadLongFromFile(section,buttonNames[i],0) > 0)
        {
            PadMap.Button[i] = static_cast<WORD>(ini.ReadLongFromFile(section,buttonNames[i],0)) - 1;
        }
    }

    for (INT i=0; i<4; ++i)
    {
        if (ini.ReadStringFromFile(section, povNames[i], buffer) > 0)
        {
			int val = _wtoi(buffer);
			if(val == 0)
			{
				//for compatibility with x360ce.App
				if(wcsstr(buffer,L"UP")) PadMap.pov[i] = 36000;
				if(wcsstr(buffer,L"DOWN")) PadMap.pov[i] = 18000;
				if(wcsstr(buffer,L"LEFT")) PadMap.pov[i] = 27000;
				if(wcsstr(buffer,L"RIGHT")) PadMap.pov[i] = 9000;
				PadMap.PovIsButton = false;
			}
			else if(val < 100)
			{
				PadMap.pov[i] = static_cast<WORD>(val - 1);
				PadMap.PovIsButton = true;
			}
			else 
			{
				PadMap.pov[i] = static_cast<WORD>(val);
				PadMap.PovIsButton = false;
			}
        }

        if (ini.ReadStringFromFile(section, axisNames[i], buffer) > 0)
        {
            LPWSTR a = buffer;

            if (towlower(*a) == L's')   // Slider
            {
                PadMap.Axis[i].analogType = SLIDER;
                ++a;
                PadMap.Axis[i].id = _wtoi(a);
            }
            else
            {
                // Axis
                PadMap.Axis[i].analogType = AXIS;
                PadMap.Axis[i].id = _wtoi(a);
            }
        }

        gamepad.adeadzone[i] =  static_cast<SHORT>(ini.ReadLongFromFile(section, axisDZNames[i], 0));
        gamepad.axislinear[i] = static_cast<SHORT>(ini.ReadLongFromFile(section, axisLNames[i], 0));

		INT ret = ini.ReadLongFromFile(section, axisBNames[i*2]);
        if (ret > 0)
        {
            PadMap.Axis[i].hasDigital = true;
            PadMap.Axis[i].positiveButtonID = ret - 1;
        }
		ret = ini.ReadLongFromFile(section, axisBNames[i*2+1]);
        if (ret > 0)
        {
            PadMap.Axis[i].hasDigital = true;
            PadMap.Axis[i].negativeButtonID = ret - 1;
        }
    }

    if (ini.ReadStringFromFile(section, L"Left Trigger", buffer) > 0)
    {
        LPWSTR a = buffer;

        if ((PadMap.Trigger[0].type = getTriggerType(a)) == DIGITAL)
        {
            PadMap.Trigger[0].id = _wtoi(a) - 1;
        }
        else
        {
            ++a;
            PadMap.Trigger[0].id = _wtoi(a);
        }
    }

    if (ini.ReadStringFromFile(section, L"Right Trigger", buffer) > 0)
    {
        LPWSTR a = buffer;

        if ((PadMap.Trigger[1].type = getTriggerType(a)) == DIGITAL)
        {
            PadMap.Trigger[1].id = _wtoi(a) - 1;
        }
        else
        {
            ++a;
            PadMap.Trigger[1].id = _wtoi(a);
        }
    }

///////////////////////////////////////////////////////////////////////////////////////
    if (ini.ReadStringFromFile(section, L"Left Trigger But", buffer) > 0)
    {
        LPWSTR a = buffer;
        PadMap.Trigger[0].but = _wtoi(a) - 1;
    }

    if (ini.ReadStringFromFile(section, L"Right Trigger But", buffer) > 0)
    {
        LPWSTR a = buffer;
        PadMap.Trigger[1].but = _wtoi(a) - 1;
    }

///////////////////////////////////////////////////////////////////////////////////////

    if (ini.ReadLongFromFile(section, L"D-pad POV") > 0)
    {
        PadMap.DpadPOV = static_cast<WORD>(ini.ReadLongFromFile(section, L"D-pad POV",0)) - 1;
    }

	GamepadMapping.push_back(PadMap);
	g_Gamepads.push_back(gamepad);
}

// NOTE: Letters corresponding to mapping types changed. Include in update notes.
MappingType getTriggerType(LPCWSTR s)
{
    if (towlower(*s) == L'a') return AXIS;	// Axis

    if (towlower(*s) == L's') return SLIDER;	// Slider

    if (towlower(*s) == L'x') return HAXIS;	// Half range axis

    if (towlower(*s) == L'h') return HSLIDER;	// Half range slider

    ////////////////////////////////////////////////////////////////////
    if (towlower(*s) == L'z') return CBUT;

    ////////////////////////////////////////////////////////////////////
    return DIGITAL;							// Digital
}
