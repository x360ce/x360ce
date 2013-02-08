/*  x360ce - XBOX360 Controller Emulator
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

#ifndef _CRITICALSECTION_H_
#define _CRITICALSECTION_H_

#include <Windows.h>

class CriticalSection
{
private:
    CRITICAL_SECTION m_cs;
public:

    CriticalSection()
    {
        __try
        {
            InitializeCriticalSection(&m_cs);
        }
        __except(GetExceptionCode() == STATUS_NO_MEMORY ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
        {
            MessageBoxA(NULL,"Cannot initialize critical section, fatal error","Error",MB_ICONERROR);
            ExitProcess(1);
        }
    }

    virtual ~CriticalSection()
    {
        DeleteCriticalSection(&m_cs);
    }

    void Lock()
    {
        EnterCriticalSection(&m_cs);
    }

    void Unlock()
    {
        LeaveCriticalSection(&m_cs);
    }
};

#endif // _CRITICALSECTION_H_