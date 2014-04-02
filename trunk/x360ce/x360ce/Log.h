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

// UGLY

#ifndef _LOG_H_
#define _LOG_H_

#include <Shlwapi.h>
#include "Logger.h"

#pragma warning( disable : 4127 )

enum LogType { LOG_CORE, LOG_XINPUT, LOG_DINPUT, LOG_IHOOK, LOG_HOOKLL, LOG_HOOKCOM, LOG_HOOKDI, LOG_HOOKSA, LOG_HOOKWT };
void PrintLog(LogType type, const char* format, ...);
void InitLog(char * logfilename, bool con);

void PrintNotice();

#endif // _LOG_H_