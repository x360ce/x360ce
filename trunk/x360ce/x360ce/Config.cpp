/*  x360ce - XBOX360 Controler Emulator
*  Copyright (C) 2002-2010 ToCA Edit
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
#include "Utils.h"
#include "Config.h"
#include "DirectInput.h"

BOOL bInitBeep=0;
WORD wNativeMode=0;

WORD wFakeMode=0;
WORD wFakeWinTrust=0;
WORD wFakeVID=0;
WORD wFakePID=0;

LPWSTR lpConfigFile = NULL;

static LPCWSTR buttonNames[10] = {
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

static LPCWSTR povNames[4] = {
	L"D-pad Up",
	L"D-pad Down",
	L"D-pad Left",
	L"D-pad Right"
};

static LPCWSTR axisNames[4] = {
	L"Left Analog X",
	L"Left Analog Y",
	L"Right Analog X",
	L"Right Analog Y"
};

static LPCWSTR axisDZNames[4] = {
	L"Left Analog X DeadZone",
	L"Left Analog Y DeadZone",
	L"Right Analog X DeadZone",
	L"Right Analog Y DeadZone",
};

static LPCWSTR axisADZNames[4] = {
	L"Left Analog X AntiDeadZone",
	L"Left Analog Y AntiDeadZone",
	L"Right Analog X AntiDeadZone",
	L"Right Analog Y AntiDeadZone",
};

static LPCWSTR axisLNames[4] = {
	L"Left Analog X Linear",
	L"Left Analog Y Linear",
	L"Right Analog X Linear",
	L"Right Analog Y Linear"
};

static LPCWSTR axisBNames[8] = {
	L"Left Analog X+ Button",
	L"Left Analog X- Button",
	L"Left Analog Y+ Button",
	L"Left Analog Y- Button",
	L"Right Analog X+ Button",
	L"Right Analog X- Button",
	L"Right Analog Y+ Button",
	L"Right Analog Y- Button"
};

static LPCWSTR padNames[4] = {
	L"PAD1",
	L"PAD2",
	L"PAD3",
	L"PAD4",
};

GamepadMap GamepadMapping[4];

void InitConfig(LPCWSTR ininame) 
{
	LPWSTR pStr;
	static WCHAR strPath[MAX_PATH];
	lpConfigFile = new WCHAR[MAX_PATH];

	GetModuleFileName (NULL, strPath, MAX_PATH);
	pStr = wcsrchr(strPath, L'\\');
	if (pStr != NULL)
		*(++pStr)=L'\0'; 

	swprintf_s(lpConfigFile,MAX_PATH,L"%s%s",strPath, ininame);
}

void ReadConfig() 
{

	// Read global options
	bInitBeep = ReadUINTFromFile(L"Options", L"UseInitBeep",1);
	writelog = ReadUINTFromFile(L"Options", L"Log",0);

	//FakeAPI
	wFakeMode = (WORD) ReadUINTFromFile(L"FakeAPI", L"FakeMode",0);
	wFakeWinTrust = (WORD) ReadUINTFromFile(L"FakeAPI", L"FakeWinTrust",0);

	if(wFakeMode)
	{
		wFakeVID = (WORD) ReadUINTFromFile(L"FakeAPI", L"FakeVID",0x045E);
		wFakePID = (WORD) ReadUINTFromFile(L"FakeAPI", L"FakePID",0x028E);
	}

	// Read pad mappings
	for (WORD b = 0; b < 4; b++) 
		ReadPadConfig(b);
}


void ReadPadConfig(INT idx) {

	WCHAR section[10];
	swprintf_s(section,L"PAD%u",idx+1);
	GamepadMap &PadMap = GamepadMapping[idx];

	WCHAR buffer[MAX_PATH];

	Gamepad[idx].native = ReadUINTFromFile(section, L"Native",0);
	if(Gamepad[idx].native) 
	{ 
		wNativeMode = 1;
		return; 
	}

	WCHAR NullGUIDStr[50];
	GUIDtoString(GUID_NULL,NullGUIDStr,50);

	ReadStringFromFile(section, L"ProductGUID", buffer, NullGUIDStr);
	StringToGUID(buffer,&Gamepad[idx].product);
	
	if(IsEqualGUID(Gamepad[idx].product,GUID_NULL))
	{
		ReadStringFromFile(section, L"Product", buffer, NullGUIDStr);
		StringToGUID(buffer,&Gamepad[idx].product);
	}

	Gamepad[idx].pid = HIWORD(Gamepad[idx].product.Data1);
	Gamepad[idx].vid = LOWORD(Gamepad[idx].product.Data1);

	ReadStringFromFile(section, L"InstanceGUID", buffer, NullGUIDStr);
	StringToGUID(buffer,&Gamepad[idx].instance);

	if(IsEqualGUID(Gamepad[idx].instance,GUID_NULL))
	{
		ReadStringFromFile(section, L"Instance", buffer, NullGUIDStr);
		StringToGUID(buffer,&Gamepad[idx].instance);
	}

	if (!(IsEqualGUID(Gamepad[idx].product,GUID_NULL))  && !(IsEqualGUID(Gamepad[idx].instance,GUID_NULL)))
	{ 
		Gamepad[idx].configured = true;
		PadMap.enabled = true;

	} else { return; }  

	Gamepad[idx].dwPadIndex = idx;

	Gamepad[idx].swapmotor = ReadUINTFromFile(section, L"SwapMotor",0);
	Gamepad[idx].tdeadzone = ReadUINTFromFile(section, L"TriggerDeadzone",XINPUT_GAMEPAD_TRIGGER_THRESHOLD);
	Gamepad[idx].ff.useforce = ReadUINTFromFile(section, L"UseForceFeedback",0);
	Gamepad[idx].gamepadtype = (BYTE)ReadUINTFromFile(section, L"ControllerType",1);
	Gamepad[idx].axistodpad = ReadUINTFromFile(section, L"AxisToDPad",0);
	Gamepad[idx].axistodpaddeadzone = ReadUINTFromFile(section, L"AxisToDPadDeadZone",0);
	Gamepad[idx].axistodpadoffset = ReadUINTFromFile(section, L"AxisToDPadOffset",0);
	Gamepad[idx].ff.forcepercent = (FLOAT) ReadUINTFromFile(section, L"ForcePercent",100) * (FLOAT) 0.01;	
	Gamepad[idx].ff.leftPeriod = ReadUINTFromFile(section, L"LeftMotorPeriod",60);
	Gamepad[idx].ff.rightPeriod = ReadUINTFromFile(section, L"RightMotorPeriod",20);

	for (INT i = 0; i < 10; ++i) PadMap.Button[i] = -1;
	for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].type = NONE;
	PadMap.DpadPOV = -1;

	ZeroMemory(buffer,bytesof(MAX_PATH, WCHAR));

	for (INT i=0; i<10; ++i) {
		if (ReadUINTFromFile(section,buttonNames[i],0) > 0) {
			PadMap.Button[i] = (ReadUINTFromFile(section,buttonNames[i],0) - 1);
		}
	}

	for (INT i=0; i<4; ++i) {
		if (ReadStringFromFile(section, povNames[i], buffer) > 0) {

			INT val = _wtoi(buffer);
			if(val > 0) 
			{
				PadMap.pov[i] = val - 1;
			} 
			else 
			{
				if(wcsstr(buffer,L"UP")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_UP;
				if(wcsstr(buffer,L"DOWN")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_DOWN;
				if(wcsstr(buffer,L"LEFT")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_LEFT;
				if(wcsstr(buffer,L"RIGHT")) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_RIGHT;
			}
		}

		if (ReadStringFromFile(section, axisNames[i], buffer) > 0) {
			LPWSTR a = buffer;
			if (towlower(*a) == L's') { // Slider
				PadMap.Axis[i].analogType = SLIDER;
				++a;
				PadMap.Axis[i].id = _wtoi(a);
			}else{
				// Axis
				PadMap.Axis[i].analogType = AXIS;
				PadMap.Axis[i].id = _wtoi(a);
			}
		}

		Gamepad[idx].adeadzone[i] =  (SHORT) ReadUINTFromFile(section, axisDZNames[i], 0);
		Gamepad[idx].antidz[i] = (SHORT) ReadUINTFromFile(section, axisADZNames[i], 0);
		Gamepad[idx].axislinear[i] = (SHORT) ReadUINTFromFile(section, axisLNames[i], 0);

		if (ReadStringFromFile(section, axisBNames[i*2],buffer) > 0) {
			INT ret = _wtoi(buffer);
			PadMap.Axis[i].hasDigital = true;
			PadMap.Axis[i].positiveButtonID = ret - 1;
		}
		if (ReadStringFromFile(section, axisBNames[i*2+1],buffer) > 0) {
			INT ret = _wtoi(buffer);
			PadMap.Axis[i].hasDigital = true;
			PadMap.Axis[i].negativeButtonID = ret - 1;
		}
	}

	if (ReadStringFromFile(section, L"Left Trigger", buffer) > 0) {
		LPWSTR a = buffer;
		if ((PadMap.Trigger[0].type = getTriggerType(a)) == DIGITAL) {
			PadMap.Trigger[0].id = _wtoi(a) - 1;
		} else {
			++a;
			PadMap.Trigger[0].id = _wtoi(a);
		}
	}

	if (ReadStringFromFile(section, L"Right Trigger", buffer) > 0) {
		LPWSTR a = buffer;
		if ((PadMap.Trigger[1].type = getTriggerType(a)) == DIGITAL) {
			PadMap.Trigger[1].id = _wtoi(a) - 1;
		} else {
			++a;
			PadMap.Trigger[1].id = _wtoi(a);
		}
	}

	if (UINT ret = ReadUINTFromFile(section, L"D-pad POV") > 0) {
		PadMap.DpadPOV = ret - 1;
	}
}

// NOTE: Letters corresponding to mapping types changed. Include in update notes.
MappingType getTriggerType(LPCWSTR s) {
	if (towlower(*s) == L'a') return AXIS;	// Axis
	if (towlower(*s) == L's') return SLIDER;	// Slider
	if (towlower(*s) == L'x') return HAXIS;	// Half range axis
	if (towlower(*s) == L'h') return HSLIDER;	// Half range slider
	return DIGITAL;							// Digital
}
