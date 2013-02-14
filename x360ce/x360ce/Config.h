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

#ifndef _CONFIG_H_
#define _CONFIG_H_

#include "Utilities\Ini.h"

// disable C4351 - new behavior: elements of array 'array' will be default initialized
#pragma warning( disable:4351 )

enum MappingType { NONE, DIGITAL, AXIS, SLIDER, HAXIS, HSLIDER, CBUT };

enum PovIDs
{
    GAMEPAD_DPAD_UP,
    GAMEPAD_DPAD_DOWN,
    GAMEPAD_DPAD_LEFT,
    GAMEPAD_DPAD_RIGHT
};

struct AxisMap
{
    MappingType analogType; // Type of analog mapping (only NONE, AXIS, and SLIDER are used)
    int8_t id;
    int8_t positiveButtonID, negativeButtonID; // button IDs corresponding to the positive/negative directions of the axis
    bool hasDigital; // Indicates if there is digital input mapped to the axis

    AxisMap()
    {
        analogType = NONE;
        id = 0;
        positiveButtonID = negativeButtonID = 0;
        hasDigital = false;
    }
};
struct TriggerMap
{
    MappingType type;
    int8_t id;			// Index for the mapped button/axis/slider
    int8_t but;
    TriggerMap()
    {
        id = 0;
        but = 0;
        type = NONE;
    }
};

struct Mapping
{
    // Axis indexes are positive or negative numbers, zero is invalid.
    // All other indexer values start from zero.
    TriggerMap Trigger[2];
    AxisMap Axis[4];  // Index of axes to use. Negative index used if it needs to be inverted
    int32_t pov[4];
    int8_t Button[10];
    int8_t guide;
    int8_t DpadPOV; // Index of POV switch to use for the D-pad
    bool PovIsButton;
    Mapping()
        :Trigger()
        ,Axis()
        ,Button()
    {
        pov[GAMEPAD_DPAD_UP] = 36000;
        pov[GAMEPAD_DPAD_DOWN] = 18000;
        pov[GAMEPAD_DPAD_LEFT] = 27000;
        pov[GAMEPAD_DPAD_RIGHT] = 9000;

        guide = 0;
        DpadPOV = 0;
        PovIsButton = false;
    }
};

// Map internal IDs to XInput constants
static const uint16_t buttonIDs[10] =
{
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

static const uint16_t povIDs[4] =
{
    XINPUT_GAMEPAD_DPAD_UP,
    XINPUT_GAMEPAD_DPAD_DOWN,
    XINPUT_GAMEPAD_DPAD_LEFT,
    XINPUT_GAMEPAD_DPAD_RIGHT
};

void InitConfig(char* ininame);
void ReadConfig(bool skip);
void ReadPadConfig(DWORD dwUserIndex, Ini &ini);

#endif
