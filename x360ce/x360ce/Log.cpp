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

#include "stdafx.h"
#include "globals.h"
#include "Log.h"
#include "Utilities/Misc.h"
#include <Shlwapi.h>
#include <Shlobj.h>

extern std::string exename;
Log* logger;

const char* Log::TypeToString(LogType type)
{
    static const char* const buffer[] =
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
    return buffer[type];
};

void Log::PrintNotice()
{
    char stamp[] = "TIME         THREAD   TYPE      DATA\n";
    if(m_stdout != INVALID_HANDLE_VALUE)
    {
        char wcsNotice[] =
            "x360ce - XBOX 360 Controller emulator\n"
            "https://code.google.com/p/x360ce/\n\n"
            "Copyright (C) 2013 Robert Krawczyk\n\n"
            "This program is free software you can redistribute it and/or modify it under\n"
            "the terms of the GNU Lesser General Public License as published by the Free\n"
            "Software Foundation, either version 3 of the License, or any later version.\n";

        DWORD written = NULL;
        for(int i=0; i < 80; i++ ) WriteConsoleA(m_stdout , "=", 1, &written,NULL);
        WriteConsoleA(m_stdout , wcsNotice,(DWORD) strlen(wcsNotice), &written,NULL);
        for(int i=0; i < 80; i++ ) WriteConsoleA(m_stdout , "=", 1, &written,NULL);
        WriteConsoleA(m_stdout , "\n", 1, &written,NULL);
        WriteConsoleA(m_stdout , stamp, (DWORD) strlen(stamp), &written,NULL);
    }
    if(m_stream.is_open()) m_stream << stamp;
}

Log::Log(bool file, bool console, bool local)
    :m_stdout(INVALID_HANDLE_VALUE)
{
    if(file)
    {
        char path[MAX_PATH];
        char buffer[MAX_PATH];
        char logname[MAX_PATH];

        SYSTEMTIME systime;
        GetLocalTime(&systime);

        if(local == false && SHGetFolderPathA(NULL,CSIDL_COMMON_APPDATA,NULL,SHGFP_TYPE_CURRENT,buffer) == S_OK)
        {
            PathCombineA(path,buffer,"x360ce");
            if(PathFileExistsA(path) == FALSE) local = true;
        }

        if(local == false)
        {
            PathCombineA(buffer,path,"logs");
            if(PathFileExistsA(buffer) == FALSE) CreateDirectoryA(buffer, NULL);

            sprintf_s(logname, "%s_%u%02u%02u-%02u%02u%02u.log", exename.c_str(),
                      systime.wYear, systime.wMonth, systime.wDay, systime.wHour, systime.wMinute, systime.wSecond);
            PathCombineA(path,buffer,logname);
        }
        else
        {
            strcpy_s(buffer,ModulePathA());
            sprintf_s(logname, "%s_%u%02u%02u-%02u%02u%02u.log", exename.c_str(),
                      systime.wYear, systime.wMonth, systime.wDay, systime.wHour, systime.wMinute, systime.wSecond);
            PathCombineA(path,buffer,logname);
        }
        m_stream.open(path);
    }

    if(console)
    {
        AllocConsole();
        m_stdout = GetStdHandle(STD_OUTPUT_HANDLE);
        ShowWindow(GetConsoleWindow(),SW_MAXIMIZE);
        SetConsoleTitleA("x360ce");
    }

    static bool once = false;
    if(once) return;
    PrintNotice();
    once = true;
}

Log::~Log()
{
    if(m_stdout != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_stdout);
        FreeConsole();
    }
    if(m_stream.is_open()) m_stream.close();
}

void Log::Print(LogType logType, const char* format, ...)
{
    bool log = m_stream.is_open();
    bool con = m_stdout != INVALID_HANDLE_VALUE;
    if(log || con)
    {
        Mutex().Lock();
        SYSTEMTIME systime;
        GetLocalTime(&systime);

        char buf[MAX_PATH];
        DWORD written = NULL;

        sprintf_s(buf, "%02u:%02u:%02u.%03u %08u %s", systime.wHour, systime.wMinute,
                  systime.wSecond, systime.wMilliseconds,GetCurrentThreadId(), TypeToString(logType));
        if(con) WriteConsoleA(m_stdout , buf, (DWORD) strlen(buf),&written,NULL);
        if(log) m_stream << buf;

        va_list arglist;
        va_start(arglist,format);
        vsprintf_s(buf,format,arglist);
        va_end(arglist);

        strcat_s(buf,"\n");
        if(con) WriteConsoleA(m_stdout , buf, (DWORD) strlen(buf), &written, NULL);
        if(log)
        {
            m_stream << buf;
            m_stream.flush();
        }
        Mutex().Unlock();
    }
}
