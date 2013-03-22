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

#ifndef _LOG_H_
#define _LOG_H_

#include <Shlwapi.h>
#include "Utilities\CriticalSection.h"

#pragma warning( disable : 4127 )

class Log
{
public:
    enum LogType { LOG_CORE, LOG_XINPUT, LOG_DINPUT, LOG_IHOOK, LOG_HOOKLL, LOG_HOOKCOM, LOG_HOOKDI, LOG_HOOKSA, LOG_HOOKWT };
    Log(bool file, bool console, bool local);
    ~Log();
    void Print(LogType logType, const char* format, ...);

private:
    static CriticalSection& Mutex()
    {
        static CriticalSection lock;
        return lock;
    }

    void PrintNotice();
    const char* TypeToString(LogType type);

    std::ofstream m_stream;
    HANDLE m_stdout;
};

extern Log* logger;
#define InitLog(log,con,local) do {if(!logger) logger = new Log(log,con,local);} while(false) 
#define PrintLog(type,format,...) do {if(logger) logger->Print(Log::type,format,__VA_ARGS__);} while(false)
#define DestroyLog() {delete logger; logger = NULL;}
#endif // _LOG_H_