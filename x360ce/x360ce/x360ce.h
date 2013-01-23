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

#ifndef _X360CE_H_
#define _X360CE_H_

#define FFB_LEFTMOTOR 0
#define FFB_RIGHTMOTOR 1

extern HINSTANCE hThis;
extern HINSTANCE hNative;
extern HWND g_hWnd;

struct XINPUT_ENABLE
{
    bool bEnabled;
    bool bUseEnabled;
    XINPUT_ENABLE()
    {
        bEnabled = FALSE;
        bUseEnabled = FALSE;
    }
};

void LoadSystemXInputDLL();

#endif
