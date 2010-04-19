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
#include "utils.h"
#include "config.h"
#include "directinput.h"

WORD wFakeAPI=0;
BOOL bInitBeep=0;

WORD wFakeWMI=0;
WORD wFakeDI=0;
WORD wFakeWinTrust=0;

WORD wFakeVID=0;
WORD wFakePID=0;

WORD wNativeMode=0;

DWORD hexToDword(LPTSTR buf)
{
	long v = wcstoul(buf, NULL, 16);
	return v;
}

LPCTSTR buttonNames[10] = {
	_T("A"),
	_T("B"),
	_T("X"),
	_T("Y"),
	_T("Left Shoulder"),
	_T("Right Shoulder"),
	_T("Back"),
	_T("Start"),
	_T("Left Thumb"),
	_T("Right Thumb"),
};

LPCTSTR povNames[4] = {
	_T("D-pad Up"),
	_T("D-pad Down"),
	_T("D-pad Left"),
	_T("D-pad Right")
};

LPCTSTR axisNames[4] = {
	_T("Left Analog X"),
	_T("Left Analog Y"),
	_T("Right Analog X"),
	_T("Right Analog Y")
};
LPCTSTR axisBNames[8] = {
	_T("Left Analog X+ Button"),
	_T("Left Analog X- Button"),
	_T("Left Analog Y+ Button"),
	_T("Left Analog Y- Button"),
	_T("Right Analog X+ Button"),
	_T("Right Analog X- Button"),
	_T("Right Analog Y+ Button"),
	_T("Right Analog Y- Button")
};
LPCTSTR padNames[4] = {
	_T("PAD1"),
	_T("PAD2"),
	_T("PAD3"),
	_T("PAD4"),
};

GamepadMap GamepadMapping[4];

VOID InitConfig() {

	LPTSTR pStr;
	TCHAR strPath[MAX_PATH];
	extern TCHAR tstrConfigFile[MAX_PATH];	
	GetModuleFileName (NULL, strPath, MAX_PATH);
	pStr = _tcsrchr(strPath, _T('\\'));
	if (pStr != NULL)
		*(++pStr)=_T('\0'); 

	TCHAR buffer[MAX_PATH];

	_stprintf_s(tstrConfigFile,_T("%s%s"),strPath, _T("x360ce.ini"));

	// Read global options
	ReadFromFile(_T("Options"), _T("UseInitBeep"), buffer, _T("1"));	bInitBeep = _tstoi(buffer);
	ReadFromFile(_T("Options"), _T("Log"), buffer, _T("0"));			writelog = _tstoi(buffer);

	//FakeAPI
	ReadFromFile(_T("FakeAPI"), _T("FakeWMI"), buffer, _T("0"));		wFakeWMI = (WORD) _tstoi(buffer);
	ReadFromFile(_T("FakeAPI"), _T("FakeDI"), buffer, _T("0"));			wFakeDI = (WORD) _tstoi(buffer);
	ReadFromFile(_T("FakeAPI"), _T("FakeWinTrust"), buffer, _T("0"));	wFakeWinTrust = (WORD) _tstoi(buffer);

	wFakeAPI = wFakeWMI + wFakeDI;

	if(wFakeAPI)
	{
		ReadFromFile(_T("FakeAPI"), _T("FakeVID"), buffer, _T("0"));		wFakeVID = (WORD) hexToDword(buffer);
		ReadFromFile(_T("FakeAPI"), _T("FakePID"), buffer, _T("0"));		wFakePID = (WORD) hexToDword(buffer);
	}


	// Read pad mappings
	for (WORD b = 0; b < 4; b++) 
		ReadPadConfig(b);
}


VOID ReadPadConfig(INT idx) {

	TCHAR section[20];
	_stprintf_s(section,_T("PAD%d"),idx+1);
	GamepadMap &PadMap = GamepadMapping[idx];

	TCHAR buffer[MAX_PATH];

	ReadFromFile(section, _T("Native"), buffer, _T("0"));					Gamepad[idx].native = _ttoi(buffer);
	if(Gamepad[idx].native) 
	{ 
		wNativeMode = 1;
		return; 
	}

	//ReadFromFile(section, _T("VID"), buffer, _T("0"));						Gamepad[idx].vid = (WORD) hexToDword(buffer);
	//ReadFromFile(section, _T("PID"), buffer, _T("0"));						Gamepad[idx].pid = (WORD) hexToDword(buffer);

	ReadFromFile(section, _T("Product"), buffer, _T("0"));
	StringToGUID(&Gamepad[idx].product,buffer);

	Gamepad[idx].pid = HIWORD(Gamepad[idx].product.Data1);
	Gamepad[idx].vid = LOWORD(Gamepad[idx].product.Data1);

	ReadFromFile(section, _T("Instance"), buffer, _T("0"));
	StringToGUID(&Gamepad[idx].instance,buffer);

	if (Gamepad[idx].product.Data1 > 0) { PadMap.enabled = true; } else { return; }  

	Gamepad[idx].dwPadIndex = idx+1;

	ReadFromFile(section, _T("SwapMotor"), buffer, _T("0"));				Gamepad[idx].swapmotor = _ttoi(buffer);
	ReadFromFile(section, _T("LeftMotorDirection"), buffer, _T("0"));		Gamepad[idx].wLMotorDirection = (WORD) _ttoi(buffer);
	ReadFromFile(section, _T("RightMotorDirection"), buffer, _T("1"));		Gamepad[idx].wRMotorDirection = (WORD) _ttoi(buffer);
	ReadFromFile(section, _T("TriggerDeadzone"), buffer, _T("25"));			Gamepad[idx].tdeadzone = _ttoi(buffer);
	ReadFromFile(section, _T("UseForceFeedback"), buffer, _T("0"));			Gamepad[idx].useforce = _ttoi(buffer);
	ReadFromFile(section, _T("ControllerType"), buffer, _T("1"));			Gamepad[idx].gamepadtype = _ttoi(buffer);			//WILDS
	ReadFromFile(section, _T("AxisToDPad"), buffer, _T("0"));				Gamepad[idx].axistodpad = _ttoi(buffer);			//WILDS
	ReadFromFile(section, _T("AxisToDPadDeadZone"), buffer, _T("256"));		Gamepad[idx].axistodpaddeadzone = _ttoi(buffer);	//WILDS
	ReadFromFile(section, _T("AxisToDPadOffset"), buffer, _T("0"));			Gamepad[idx].axistodpadoffset = _ttoi(buffer);		//WILDS
	ReadFromFile(section, _T("ForcePercent"), buffer, _T("100"));			
	
		DWORD forcepercent = _ttoi(buffer);
		Gamepad[idx].forcepercent = ((float)forcepercent * (float)0.01);


	for (INT i = 0; i < 10; ++i) PadMap.Button[i] = -1;
	for (INT i = 0; i < 2; ++i) PadMap.Trigger[i].type = NONE;
	PadMap.DpadPOV = -1;

	// All values start from 1 in the INI file

	for (INT i=0; i<10; ++i) {
		if (ReadFromFile(section, buttonNames[i], buffer) > 0) {
			INT val = _tstoi(buffer);
			PadMap.Button[i] = val - 1;
		}
	}

	for (INT i=0; i<4; ++i) {
		if (ReadFromFile(section, povNames[i], buffer) > 0) {
			
			if(_tcsstr(buffer,_T("UP"))) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_UP;
			if(_tcsstr(buffer,_T("DOWN"))) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_DOWN;
			if(_tcsstr(buffer,_T("LEFT"))) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_LEFT;
			if(_tcsstr(buffer,_T("RIGHT"))) PadMap.pov[i] = XINPUT_GAMEPAD_DPAD_RIGHT;
		}
	}

	for (INT i=0; i<4; ++i) {
		if (ReadFromFile(section, axisNames[i], buffer) > 0) {
			LPTSTR a = buffer;
			if (_totlower(*a) == 's') { // Slider
				PadMap.Axis[i].analogType = SLIDER;
				++a;
				PadMap.Axis[i].id = _ttoi(a);
			}else{
				// Axis
				PadMap.Axis[i].analogType = AXIS;
				PadMap.Axis[i].id = _ttoi(a);
			}
		}
	}

	for (INT i=0; i<4; ++i) {
		if (ReadFromFile(section, axisBNames[i*2], buffer) > 0) {
			PadMap.Axis[i].hasDigital = true;
			PadMap.Axis[i].positiveButtonID = _ttoi(buffer) - 1;
		}
		if (ReadFromFile(section, axisBNames[i*2+1], buffer) > 0) {
			PadMap.Axis[i].hasDigital = true;
			PadMap.Axis[i].negativeButtonID = _ttoi(buffer) - 1;
		}
	}
	if (ReadFromFile(section, _T("Left Trigger"), buffer) > 0) {
		LPTSTR a = buffer;
		if ((PadMap.Trigger[0].type = getTriggerType(a)) == DIGITAL) {
			PadMap.Trigger[0].id = _ttoi(a) - 1;
		} else {
			++a;
			PadMap.Trigger[0].id = _ttoi(a);
		}
	}

	if (ReadFromFile(section, _T("Right Trigger"), buffer) > 0) {
		LPTSTR a = buffer;
		if ((PadMap.Trigger[1].type = getTriggerType(a)) == DIGITAL) {
			PadMap.Trigger[1].id = _ttoi(a) - 1;
		} else {
			++a;
			PadMap.Trigger[1].id = _ttoi(a);
		}
	}

	if (ReadFromFile(section, _T("D-pad POV"), buffer) > 0) {
		INT val = _tstoi(buffer);
		PadMap.DpadPOV = val - 1;
	}
}

// NOTE: Letters corresponding to mapping types changed. Include in update notes.
MappingType getTriggerType(LPCTSTR s) {
	if (_totlower(*s) == 'a') return AXIS;	// Axis
	if (_totlower(*s) == 's') return SLIDER;	// Slider
	if (_totlower(*s) == 'x') return HAXIS;	// Half range axis
	if (_totlower(*s) == 'h') return HSLIDER;	// Half range slider
	return DIGITAL;							// Digital
}
