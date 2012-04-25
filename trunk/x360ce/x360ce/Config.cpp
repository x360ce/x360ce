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

BOOL bInitBeep=0;
WORD wNativeMode=0;

IHOOK_CONIFG x360ce_InputHookConfig;
IHOOK_GAMEPAD_CONIFG x360ce_InputHookGamepadConfig[4];

BOOL g_Disable;

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

GamepadMap GamepadMapping[4];

void ReadConfig()
{

    // Read global options

	g_Disable = ReadLongFromFile(L"Options", L"Disable",0);
    bInitBeep = static_cast<BOOL>(ReadLongFromFile(L"Options", L"UseInitBeep",1));
    LogEnable(static_cast<BOOL>(ReadLongFromFile(L"Options", L"Log",0)));
    enableconsole = static_cast<BOOL>(ReadLongFromFile(L"Options", L"Console",0));
	if(g_Disable) return;

    //InputHook
    x360ce_InputHookConfig.dwHookMode = ReadLongFromFile(L"InputHook", L"HookMode",0);
    x360ce_InputHookConfig.dwHookWMIANSI = ReadLongFromFile(L"InputHook", L"HookUseANSI",0);
    x360ce_InputHookConfig.dwHookWinTrust = ReadLongFromFile(L"InputHook", L"HookWinTrust",0);

    if(x360ce_InputHookConfig.dwHookMode)
    {
        x360ce_InputHookConfig.bEnabled = 1;
        x360ce_InputHookConfig.dwHookVID = ReadLongFromFile(L"InputHook", L"HookVID",0x045E);
        x360ce_InputHookConfig.dwHookPID = ReadLongFromFile(L"InputHook", L"HookPID",0x028E);
    }

    // Read pad mappings
    for (DWORD i = 0; i < 4; ++i)
        ReadPadConfig(i);

    IniCleanup();
}


void ReadPadConfig(DWORD idx)
{

    DWORD ret;
    WCHAR section[MAX_PATH] = L"Mappings";
    WCHAR key[MAX_PATH];
    swprintf_s(key,L"PAD%u",idx+1);

    WCHAR buffer[MAX_PATH];

    ret = ReadStringFromFile(section, key, buffer);

    if(!ret) return;

    //store value as section name
    wcscpy_s(section,buffer);

    WCHAR NullGUIDStr[50];
    GamepadMap &PadMap = GamepadMapping[idx];

    GUIDtoString(GUID_NULL,NullGUIDStr,50);

    ret = ReadStringFromFile(section, L"ProductGUID", buffer, NullGUIDStr);

    if(!ret) return;

    StringToGUID(buffer,&g_Gamepad[idx].productGUID);

    if(IsEqualGUID(g_Gamepad[idx].productGUID,GUID_NULL))
    {
        ret = ReadStringFromFile(section, L"Product", buffer, NullGUIDStr);

        if(!ret) return;

        StringToGUID(buffer,&g_Gamepad[idx].productGUID);
    }

    ret = ReadStringFromFile(section, L"InstanceGUID", buffer, NullGUIDStr);

    if(!ret) return;

    StringToGUID(buffer,&g_Gamepad[idx].instanceGUID);

    if(IsEqualGUID(g_Gamepad[idx].instanceGUID,GUID_NULL))
    {
        ret = ReadStringFromFile(section, L"Instance", buffer, NullGUIDStr);

        if(!ret) return;

        StringToGUID(buffer,&g_Gamepad[idx].instanceGUID);
    }

    g_Gamepad[idx].passthrough = (ReadLongFromFile(section, L"PassThrough",1) !=0);

    if(g_Gamepad[idx].passthrough)
    {
        wNativeMode = 1;
        g_Gamepad[idx].configured = true;
        PadMap.enabled = false;
        return;
    }

    if (!(IsEqualGUID(g_Gamepad[idx].productGUID,GUID_NULL)) && !(IsEqualGUID(g_Gamepad[idx].instanceGUID,GUID_NULL)))
    {
        g_Gamepad[idx].configured = true;
        PadMap.enabled = true;

    }
    else
    {
        return;
    }

    g_Gamepad[idx].dwPadIndex = idx;

    g_Gamepad[idx].ff.type = (BYTE) ReadLongFromFile(section, L"FFBType",0);
    g_Gamepad[idx].swapmotor = ReadLongFromFile(section, L"SwapMotor",0);
    g_Gamepad[idx].tdeadzone = ReadLongFromFile(section, L"TriggerDeadzone",XINPUT_GAMEPAD_TRIGGER_THRESHOLD);
    g_Gamepad[idx].ff.useforce = static_cast<BOOL>(ReadLongFromFile(section, L"UseForceFeedback",0));
    g_Gamepad[idx].gamepadtype = static_cast<BYTE>(ReadLongFromFile(section, L"ControllerType",1));
    g_Gamepad[idx].axistodpad = (ReadLongFromFile(section, L"AxisToDPad",0) !=0);
    g_Gamepad[idx].axistodpaddeadzone = static_cast<INT>(ReadLongFromFile(section, L"AxisToDPadDeadZone",0));
    g_Gamepad[idx].axistodpadoffset = static_cast<INT>(ReadLongFromFile(section, L"AxisToDPadOffset",0));
    g_Gamepad[idx].ff.forcepercent = static_cast<FLOAT>(ReadLongFromFile(section, L"ForcePercent",100) * 0.01);
    g_Gamepad[idx].ff.leftPeriod = ReadLongFromFile(section, L"LeftMotorPeriod",60);
    g_Gamepad[idx].ff.rightPeriod = ReadLongFromFile(section, L"RightMotorPeriod",20);

    for (INT i = 0; i < 10; ++i) PadMap.Button[i] = (WORD) -1;

    for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].type = NONE;

    ///////////////////////////////////////////////////////////////////////////////////////
    for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].but = -1;

    ///////////////////////////////////////////////////////////////////////////////////////

    PadMap.DpadPOV = (WORD) -1;

    for (INT i=0; i<10; ++i)
    {
        if (ReadLongFromFile(section,buttonNames[i],0) > 0)
        {
            PadMap.Button[i] = static_cast<INT>(ReadLongFromFile(section,buttonNames[i],0)) - 1;
        }
    }

    for (INT i=0; i<4; ++i)
    {
        if (ReadStringFromFile(section, povNames[i], buffer) > 0)
        {
            INT val = _wtoi(buffer);

            if(val > 0)
            {
                PadMap.pov[i] = val - 1;
                PadMap.PovIsButton = true;
            }
            else
            {
                if(wcsstr(buffer,L"UP")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_UP;

                if(wcsstr(buffer,L"DOWN")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_DOWN;

                if(wcsstr(buffer,L"LEFT")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_LEFT;

                if(wcsstr(buffer,L"RIGHT")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_RIGHT;

                PadMap.PovIsButton = false;
            }
        }

        if (ReadStringFromFile(section, axisNames[i], buffer) > 0)
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

        g_Gamepad[idx].adeadzone[i] =  static_cast<SHORT>(ReadLongFromFile(section, axisDZNames[i], 0));
        g_Gamepad[idx].antidz[i] = static_cast<SHORT>(ReadLongFromFile(section, axisADZNames[i], 0));
        g_Gamepad[idx].axislinear[i] = static_cast<SHORT>(ReadLongFromFile(section, axisLNames[i], 0));

        if (INT ret = ReadLongFromFile(section, axisBNames[i*2]) > 0)
        {
            PadMap.Axis[i].hasDigital = true;
            PadMap.Axis[i].positiveButtonID = ret - 1;
        }

        if (INT ret = ReadLongFromFile(section, axisBNames[i*2+1]) > 0)
        {
            PadMap.Axis[i].hasDigital = true;
            PadMap.Axis[i].negativeButtonID = ret - 1;
        }
    }

    if (ReadStringFromFile(section, L"Left Trigger", buffer) > 0)
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

    if (ReadStringFromFile(section, L"Right Trigger", buffer) > 0)
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
    if (ReadStringFromFile(section, L"Left Trigger But", buffer) > 0)
    {
        LPWSTR a = buffer;
        PadMap.Trigger[0].but = _wtoi(a) - 1;
    }

    if (ReadStringFromFile(section, L"Right Trigger But", buffer) > 0)
    {
        LPWSTR a = buffer;
        PadMap.Trigger[1].but = _wtoi(a) - 1;
    }

///////////////////////////////////////////////////////////////////////////////////////

    if (ReadLongFromFile(section, L"D-pad POV") > 0)
    {
        PadMap.DpadPOV = static_cast<INT>(ReadLongFromFile(section, L"D-pad POV",0)) - 1;
    }
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
