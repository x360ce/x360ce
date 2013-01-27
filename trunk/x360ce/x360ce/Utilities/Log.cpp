/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
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
#include "Utilities\Log.h"
#include <Shlwapi.h>

HANDLE hConsole = INVALID_HANDLE_VALUE;
BOOL writelog = FALSE;
BOOL enableconsole = FALSE;
//LPWSTR lpLogFileName = NULL;

std::wstring logfilename;

static LPCWSTR LogTypeNames[] =
{
    L"[Core]    ",
    L"[XInput]  ",
    L"[DInput]  ",
    L"[IHook]   ",
    L"[HookDI]  ",
    L"[HookCOM] ",
    L"[HookWT]  ",
};

void WriteStamp()
{
    if(hConsole != INVALID_HANDLE_VALUE)
    {
		DWORD dwChars = NULL;

		WCHAR wcsLicense[] = 
			L"x360ce - XBOX 360 Controller emulator\n"
			L"Copyright (C) 2013 Robert Krawczyk\n\n"
			L"This program is free software you can redistribute it and/or modify it under\n"
			L"the terms of the GNU Lesser General Public License as published by the Free\n"
			L"Software Foundation, either version 3 of the License, or any later version.\n";


		for(int i=0;i < 80; i++ ) WriteConsole(hConsole, L"=", 1,&dwChars,NULL);
		WriteConsole(hConsole, wcsLicense,(DWORD) wcslen(wcsLicense),&dwChars,NULL);
		for(int i=0;i < 80; i++ ) WriteConsole(hConsole, L"=", 1,&dwChars,NULL);
		WriteConsole(hConsole, L"\n", 1,&dwChars,NULL);
    }

    if (writelog)
    {
		if(logfilename.empty()) return;
		std::wofstream out;
		out.open(logfilename,std::ios::app);
		if(!out.is_open()) return;
		out << L"TIME         THREAD   TYPE      DATA" << std::endl;
		out.close();
    }
}

void ConsoleEnable(BOOL console)
{
    enableconsole = console;
}

void Console()
{

	if(enableconsole && hConsole == INVALID_HANDLE_VALUE)
	{
		CONSOLE_SCREEN_BUFFER_INFO csbi;
		AllocConsole();
		ShowWindow(GetConsoleWindow(),SW_MAXIMIZE);
		hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
		SetConsoleTitle(L"x360ce");
		GetConsoleScreenBufferInfo(hConsole,&csbi);
	}
}

void LogEnable(BOOL log)
{
    writelog = log;
}

void LogCleanup()
{
    if(hConsole != INVALID_HANDLE_VALUE)
	{
		CloseHandle(hConsole);
		FreeConsole();
	}
}

BOOL CreateLog()
{
    BOOL bRet = FALSE;

    if (writelog)
    {
		std::wstring name = L"x360ce";

        SYSTEMTIME systime;
        GetLocalTime(&systime);

		std::wostringstream s;
		s << std::setfill(L'0') << name << L"\\" << name << L'_' 
			<< std::setfill(L'0') << std::setw(2) << systime.wYear 
			<< std::setfill(L'0') << std::setw(2) << systime.wMonth
			<< std::setfill(L'0') << std::setw(2) << systime.wDay << L'-' 
			<< std::setfill(L'0') << std::setw(2) << systime.wHour 
			<< std::setfill(L'0') << std::setw(2) << systime.wMinute 
			<< std::setfill(L'0') << std::setw(2) << systime.wSecond << L".log";

		logfilename = s.str();

        if(!PathIsDirectory(name.c_str())) CreateDirectory(name.c_str(), NULL);
        bRet = TRUE;
    }

    return bRet;
}

BOOL WriteLog(LogType logType, LPWSTR str,...)
{
    SYSTEMTIME systime;
    GetLocalTime(&systime);

	DWORD ret = FALSE;

    if(enableconsole && hConsole != INVALID_HANDLE_VALUE)
    {
		WCHAR buf[MAX_PATH];
		DWORD dwChars = NULL;

        swprintf_s(buf,L"%02u:%02u:%02u.%03u %08u %s",\
                systime.wHour, systime.wMinute, systime.wSecond, systime.wMilliseconds,GetCurrentThreadId(),LogTypeNames[logType]);

		WriteConsole(hConsole, buf, (DWORD) wcslen(buf),&dwChars,NULL);
        va_list arglist;
        va_start(arglist,str);
        vswprintf_s(buf,str,arglist);
		WriteConsole(hConsole, buf, (DWORD) wcslen(buf),&dwChars,NULL);
        va_end(arglist);
		WriteConsole(hConsole, L" \n", (DWORD) wcslen(L" \n"),&dwChars,NULL);
        ret = TRUE;
    }

    if (writelog)
    {
		if(logfilename.empty()) return FALSE;

		std::wofstream out;
		out.open(logfilename,std::ios::app);
		if(!out.is_open()) return FALSE;

		out << std::setfill(L'0') << std::setw(2) << std::dec << systime.wHour << L":"
			<< std::setfill(L'0') << std::setw(2) << std::dec << systime.wMinute << L":"
			<< std::setfill(L'0') << std::setw(2) << std::dec << systime.wSecond << L"."
			<< std::setfill(L'0') << std::setw(3) << std::dec << systime.wMilliseconds << L" "
			<< std::setfill(L'0') << std::setw(8) << std::dec << GetCurrentThreadId() << L" "
			<< LogTypeNames[logType];

        va_list argptr;
        va_start(argptr,str);
		WCHAR buff[MAX_PATH];
		_vsnwprintf_s(buff,_TRUNCATE,str,argptr);
        va_end(argptr);
		out << buff << std::endl;
		out.close();
        ret = TRUE;
    }
	return ret;
}
