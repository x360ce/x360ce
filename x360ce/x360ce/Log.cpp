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
#include "Log.h"
#include <Shlwapi.h>


const char* Log::TypeToString(LogType type)
{
    static const char* const buffer[] =
    {
        "[Core   ] ",
        "[XInput ] ",
        "[DInput ] ",
        "[IHook  ] ",
        "[HookDI ] ",
        "[HookCOM] ",
        "[HookWT ] ",
    };
    return buffer[type];
};

void Log::PrintNotice()
{
    char stamp[] = "TIME         THREAD   TYPE      DATA\n";
    if(GetStdOut()  != INVALID_HANDLE_VALUE)
    {
        char wcsNotice[] =
            "x360ce - XBOX 360 Controller emulator\n"
            "https://code.google.com/p/x360ce/\n\n"
            "Copyright (C) 2013 Robert Krawczyk\n\n"
            "This program is free software you can redistribute it and/or modify it under\n"
            "the terms of the GNU Lesser General Public License as published by the Free\n"
            "Software Foundation, either version 3 of the License, or any later version.\n";

        DWORD written = NULL;
        for(int i=0; i < 80; i++ ) WriteConsoleA(GetStdOut() , "=", 1, &written,NULL);
        WriteConsoleA(GetStdOut() , wcsNotice,(DWORD) strlen(wcsNotice), &written,NULL);
        for(int i=0; i < 80; i++ ) WriteConsoleA(GetStdOut() , "=", 1, &written,NULL);
        WriteConsoleA(GetStdOut() , "\n", 1, &written,NULL);
        WriteConsoleA(GetStdOut() , stamp, (DWORD) strlen(stamp), &written,NULL);
    }
    if(Stream().is_open())Stream() << stamp;
}

void Log::Init(bool file, bool console)
{
    char name[] = "x360ce";

    SYSTEMTIME systime;
    GetLocalTime(&systime);

    char buf[MAX_PATH];
    sprintf_s(buf, "%s\\%s_%u%02u%02u-%02u%02u%02u.log", name, name,
              systime.wYear, systime.wMonth, systime.wDay, systime.wHour, systime.wMinute, systime.wSecond);

    if(file)
    {
        if(!PathIsDirectoryA(name)) CreateDirectoryA(name, NULL);
        if(!Stream().is_open()) Stream().open(buf);
    }

    if(console)
    {
        AllocConsole();
        ShowWindow(GetConsoleWindow(),SW_MAXIMIZE);
        SetConsoleTitleA(name);
    }

    static bool once = false;
    if(once) return;
    PrintNotice();
    once = true;
}

void Log::Destroy()
{
    if(GetStdOut() != INVALID_HANDLE_VALUE)
    {
        CloseHandle(GetStdOut());
        FreeConsole();
    }
    if(Stream().is_open()) Stream().close();
}

void Log::Print(LogType logType, char* format, ...)
{
    Mutex().Lock();
    SYSTEMTIME systime;
    GetLocalTime(&systime);

    if(GetStdOut() != INVALID_HANDLE_VALUE)
    {
        char buf[MAX_PATH];
        DWORD written = NULL;

        sprintf_s(buf, "%02u:%02u:%02u.%03u %08u %s", systime.wHour, systime.wMinute,
                  systime.wSecond, systime.wMilliseconds,GetCurrentThreadId(), TypeToString(logType));
        WriteConsoleA(GetStdOut() , buf, (DWORD) strlen(buf),&written,NULL);
        if(Stream().is_open())Stream() << buf;

        va_list arglist;
        va_start(arglist,format);
        vsprintf_s(buf,format,arglist);
        va_end(arglist);

        strcat_s(buf,"\n");

        WriteConsoleA(GetStdOut() , buf, (DWORD) strlen(buf), &written, NULL);
        if(Stream().is_open())
        {
            Stream() <<buf;
            Stream().flush();
        }
    }
    Mutex().Unlock();
}
