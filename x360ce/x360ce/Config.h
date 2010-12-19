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
 
#ifndef _CONFIG_H_
#define _CONFIG_H_

#include "InputHook\InputHook.h"

enum MappingType { NONE, DIGITAL, AXIS, SLIDER, HAXIS, HSLIDER };

struct AxisMap {
	INT id;
	MappingType analogType; // Type of analog mapping (only NONE, AXIS, and SLIDER are used)

	BOOL hasDigital; // Indicates if there is digital input mapped to the axis
	INT positiveButtonID, negativeButtonID; // button IDs corresponding to the positive/negative directions of the axis

	AxisMap() {
		analogType = NONE;
		hasDigital = false;
		positiveButtonID = negativeButtonID = -1;
	}
};
struct TriggerMap {
	INT id;			// Index for the mapped button/axis/slider
	MappingType type;
	TriggerMap() {
		type = NONE;
	}
};

struct GamepadMap {
	// Axis indexes are positive or negative numbers, zero is invalid.
	// All other indexer values start from zero.
	INT Button[10];
	DWORD pov[4];
	TriggerMap Trigger[2];
	AxisMap Axis[4];  // Index of axes to use. Negative index used if it needs to be inverted
	INT DpadPOV; // Index of POV switch to use for the D-pad
	BOOL PovIsButton;
	BOOL enabled;
	GamepadMap() {
		// Set default values
		for (INT i = 0; i < 10; ++i) Button[i] = -1;
		for (INT i = 0; i < 4; ++i) pov[i] = (DWORD) -1;
		DpadPOV = -1;
		PovIsButton = 0;
		enabled = false;
	}

};

// Map internal IDs to XInput constants
static const WORD buttonIDs[10] = {
	XINPUT_GAMEPAD_A,
	XINPUT_GAMEPAD_B,
	XINPUT_GAMEPAD_X,
	XINPUT_GAMEPAD_Y,
	XINPUT_GAMEPAD_LEFT_SHOULDER,
	XINPUT_GAMEPAD_RIGHT_SHOULDER,
	XINPUT_GAMEPAD_BACK,
	XINPUT_GAMEPAD_START,
	XINPUT_GAMEPAD_LEFT_THUMB,
	XINPUT_GAMEPAD_RIGHT_THUMB,
};

static const WORD povIDs[4] = {
	XINPUT_GAMEPAD_DPAD_UP,
	XINPUT_GAMEPAD_DPAD_DOWN,
	XINPUT_GAMEPAD_DPAD_LEFT,
	XINPUT_GAMEPAD_DPAD_RIGHT
};

extern BOOL bInitBeep;
extern struct GamepadMap GamepadMapping[4];
extern BOOL enableconsole;

void InitConfig(LPCWSTR ininame);
void ReadConfig();
void ReadPadConfig(DWORD idx);
MappingType getTriggerType(LPCWSTR);

extern IHOOK_CONIFG x360ce_InputHookConfig;
extern IHOOK_GAMEPAD_CONIFG x360ce_InputHookGamepadConfig[4];

#endif
