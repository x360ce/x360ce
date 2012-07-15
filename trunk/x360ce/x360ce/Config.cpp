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
#include "Utilities\IniReader.h"
#include "Utilities\Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"

BOOL bInitBeep=true;
WORD wNativeMode=0;

extern iHook g_iHook;

//IHOOK_CONIFG x360ce_InputHookConfig;
//IHOOK_GAMEPAD_CONIFG x360ce_InputHookGamepadConfig[4];

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

static LPCWSTR triggerNames[] =
{
	L"Right Trigger",
	L"Left Trigger"
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

void ReadConfig(wchar_t* ininame)
{
	// Read global options
	IniReader* ini = new IniReader(ininame);

	ini->setSection(L"Options");
	g_Disable = ini->getValueAsBoolean(L"Disable",L"0");
	bInitBeep = ini->getValueAsBoolean(L"UseInitBeep",L"1"); 
	LogEnable(ini->getValueAsBoolean(L"Log",L"0"));
	enableconsole = ini->getValueAsBoolean(L"Console",L"0");
	if(g_Disable) return;

	//InputHook
	ini->setSection(L"InputHook");
	g_iHook.SetHookMode(ini->getValueAsLong(L"HookMode",L"0"));
	g_iHook.EnableANSIMode(ini->getValueAsBoolean(L"HookUseANSI",L"0"));
	g_iHook.EnableTrustHook(ini->getValueAsBoolean(L"HookWinTrust",L"0"));

	if(g_iHook.GetHookMode())
	{
		g_iHook.Enable();
		DWORD vid =  ini->getValueAsLong(L"HookVID",L"0x045E");
		DWORD pid = ini->getValueAsLong(L"HookPID",L"0x028E");
		if(vid != 0x045E || pid != 0x28E) g_iHook.SetHookVIDPID(MAKELONG(pid,vid));
	}

	WCHAR* pBuffer = new WCHAR[1024];

	// Read pad mappings
	for (DWORD i = 0; i < 4; ++i)
		ReadPadConfig(i,pBuffer, ini);

	delete [] pBuffer;
	delete ini;
}


void ReadPadConfig(DWORD idx, LPWSTR pBuffer, IniReader* ini)
{
	if(!ini)
		return;

	WCHAR section[MAX_PATH] = L"Mappings";
	WCHAR key[MAX_PATH];
	swprintf_s(key,L"PAD%u",idx+1);

	ini->setSection(section);
	wcscpy_s(section,ini->getValue(key,L"").c_str());
	ini->setSection(section);

	WCHAR NullGUIDStr[50];
	GamepadMap &PadMap = GamepadMapping[idx];

	GUIDtoString(GUID_NULL,NullGUIDStr);
	wcscpy_s(pBuffer,1024,ini->getValue( L"ProductGuid",NullGUIDStr).c_str());
	StringToGUID(pBuffer,&g_Gamepad[idx].productGUID);
	wcscpy_s(pBuffer,1024,ini->getValue( L"InstanceGuid",NullGUIDStr).c_str());
	StringToGUID(pBuffer,&g_Gamepad[idx].instanceGUID);

	for (int i=0; i<4; ++i)
		g_Gamepad[idx].antidz[i] = clamp(ini->getValueAsLong(axisADZNames[i],L"0"),0,32767);

	//FIXME
	g_Gamepad[idx].passthrough = ini->getValueAsBoolean(L"PassThrough",L"0");

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

	g_Gamepad[idx].ff.type = (BYTE) ini->getValueAsLong(L"FFBType",L"0");
	g_Gamepad[idx].swapmotor = ini->getValueAsLong(L"SwapMotor",L"0");
	g_Gamepad[idx].tdeadzone = ini->getValueAsLong(L"TriggerDeadzone",L"XINPUT_GAMEPAD_TRIGGER_THRESHOLD");
	g_Gamepad[idx].ff.useforce = ini->getValueAsLong(L"UseForceFeedback",L"0"); 
	g_Gamepad[idx].gamepadtype = ini->getValueAsLong(L"ControllerType",L"1");
	g_Gamepad[idx].axistodpad = ini->getValueAsBoolean(L"AxisToDPad",L"0");
	g_Gamepad[idx].axistodpaddeadzone = ini->getValueAsLong(L"AxisToDPadDeadZone",L"0"); 
	g_Gamepad[idx].axistodpadoffset = ini->getValueAsLong(L"AxisToDPadOffset",L"0");
	g_Gamepad[idx].ff.forcepercent = ini->getValueAsLong(L"ForcePercent",L"100") * 0.01; 
	g_Gamepad[idx].ff.leftPeriod = ini->getValueAsLong(L"LeftMotorPeriod",L"60");
	g_Gamepad[idx].ff.rightPeriod = ini->getValueAsLong(L"RightMotorPeriod",L"20");

	memset(PadMap.Button,-1,sizeof(PadMap.Button));

	for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].type = NONE;

	///////////////////////////////////////////////////////////////////////////////////////
	for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].but = -1;

	///////////////////////////////////////////////////////////////////////////////////////

	PadMap.DpadPOV = (WORD) -1;

	for (INT i=0; i<10; ++i)
	{	
		if (ini->getValueAsLong(buttonNames[i],L"0") > 0)
			PadMap.Button[i] = ini->getValueAsLong(buttonNames[i],L"0") - 1;
	}

	for (INT i=0; i<4; ++i)
	{
		WORD val = NULL;
		if (ini->getValue(povNames[i],L"0").length() > 0)
		{
			wcscpy_s(pBuffer,1024,ini->getValue(povNames[i],L"0").c_str());
			val = _wtoi(pBuffer);

			//for compatibility with x360ce.App
			if(wcsstr(pBuffer,L"UP")) PadMap.pov[i] = 36000;
			if(wcsstr(pBuffer,L"DOWN")) PadMap.pov[i] = 18000;
			if(wcsstr(pBuffer,L"LEFT")) PadMap.pov[i] = 27000;
			if(wcsstr(pBuffer,L"RIGHT")) PadMap.pov[i] = 9000;
			PadMap.PovIsButton = false;
		}
		else if(val < 100)
		{
			PadMap.pov[i] = val - 1;
			PadMap.PovIsButton = true;
		}
		else 
		{
			PadMap.pov[i] = val;
			PadMap.PovIsButton = false;
		}

		if (ini->getValue(axisNames[i],L"0").length() > 0)
		{
			wcscpy_s(pBuffer,1024,ini->getValue(axisNames[i],L"0").c_str());
			LPWSTR a = pBuffer;

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

		g_Gamepad[idx].adeadzone[i] =  ini->getValueAsLong(axisDZNames[i],L"0");
		g_Gamepad[idx].axislinear[i] = ini->getValueAsLong(axisLNames[i],L"0"); 

		if (INT ret = ini->getValueAsLong(axisBNames[i*2],L"0") > 0)
		{
			PadMap.Axis[i].hasDigital = true;
			PadMap.Axis[i].positiveButtonID = ret - 1;
		}

		if (INT ret = ini->getValueAsLong( axisBNames[i*2+1],L"0") > 0)
		{
			PadMap.Axis[i].hasDigital = true;
			PadMap.Axis[i].negativeButtonID = ret - 1;
		}
	}

	for (INT i=0; i<2; ++i)
	{
		if (ini->getValue( triggerNames[i],L"0").length() > 0)
		{
			wcscpy_s(pBuffer,1024,ini->getValue(triggerNames[i],L"0").c_str());
			LPWSTR a = pBuffer;

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
	}

	///////////////////////////////////////////////////////////////////////////////////////
	if (ini->getValueAsLong(L"Left Trigger But",L"0") > 0)
	{
		PadMap.Trigger[0].but = ini->getValueAsLong(L"Left Trigger But",L"0") - 1;
	}

	if (ini->getValueAsLong(L"Right Trigger But",L"0") > 0)
	{
		PadMap.Trigger[1].but = ini->getValueAsLong(L"Left Trigger But",L"0") - 1;
	}

	///////////////////////////////////////////////////////////////////////////////////////

	if (ini->getValueAsLong(L"D-pad POV",L"0") > 0)
	{
		PadMap.DpadPOV = ini->getValueAsLong(L"D-pad POV",L"0") - 1;
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
