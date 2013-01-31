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

#ifndef _LOG_H_
#define _LOG_H_

#include <Shlwapi.h>
#include "Utilities\CriticalSection.h"

class Log
{
private:
    Log::Log() {};
    Log(const Log &) {};
    Log& operator=(const Log&) {};

    static std::ofstream& Stream()
    {
        static std::ofstream stream;
        return stream;
    }

    static HANDLE& GetStdOut()
    {
        static HANDLE hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
        return hStdOut;
    }

    static CriticalSection& Mutex()
    {
        static CriticalSection lock;
        return lock;
    }

public:

    enum LogType { LOG_CORE, LOG_XINPUT, LOG_DINPUT, LOG_IHOOK, LOG_HOOKLL, LOG_HOOKCOM, LOG_HOOKDI, LOG_HOOKWT };

    static Log& getInstance()
    {
        Mutex().Lock();
        static Log instance;
        Mutex().Unlock();

        return instance;
    }

    void Log::Init(bool file, bool console);
    void Print(LogType logType, char* format, ...);
    void Destroy();

protected:
    void PrintNotice();
    const char* TypeToString(LogType type);
};

#define InitLog(log,con) Log::getInstance().Init(log,con)
#define PrintLog(type,format,...) Log::getInstance().Print(Log::type,format,__VA_ARGS__)
#define DestroyLog() Log::getInstance().Destroy()

#endif // _LOG_H_