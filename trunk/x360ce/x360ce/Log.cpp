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

#include "stdafx.h"
#include "globals.h"
#include "Log.h"
#include "Misc.h"
#include <Shlwapi.h>
#include <Shlobj.h>

const char notice[] =
"\nx360ce - XBOX 360 Controller emulator\n"
"https://code.google.com/p/x360ce/\n\n"
"Copyright (C) 2013 Robert Krawczyk\n\n"
"This program is free software you can redistribute it and/or modify it under\n"
"the terms of the GNU Lesser General Public License as published by the Free\n"
"Software Foundation, either version 3 of the License, or any later version.\n";

static const char* logtypes[] =
{
	"[Core   ] ",
	"[XInput ] ",
	"[DInput ] ",
	"[IHook  ] ",
	"[HookLL ] ",
	"[HookCOM] ",
	"[HookDI ] ",
	"[HookSA ] ",
	"[HookWT ] ",
};

void PrintLog(LogType type, const char* format, ...)
{
	va_list vaargs;
	va_start(vaargs, format);
	LogPrint(format, vaargs);
	va_end(vaargs);
}

void InitLog(char * logfilename, bool con)
{
	if (con) LogConsole("x360ce");
	if (logfilename)
	{		
		LogFile(logfilename);
	}
}

void PrintNotice()
{
	LogPrintConsole(notice);
}
