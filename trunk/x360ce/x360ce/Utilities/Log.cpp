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

#include "stdafx.h"
#include "globals.h"
#include "Utilities\Log.h"
#include <Shlwapi.h>

HANDLE hConsole = INVALID_HANDLE_VALUE;
BOOL writelog = FALSE;
BOOL enableconsole = FALSE;
LPWSTR lpLogFileName = NULL;

static LPCWSTR LogTypeNames[] =
{
    L"[Core]    ",
    L"[XInput]  ",
    L"[DInput]  ",
    L"[IHook]   ",
    L"[HookDI]  ",
    L"[HookWMI] ",
    L"[HookWT]  ",
};

void WriteStamp()
{
    if(enableconsole)
    {
        wprintf(L"%s",L"TIME           THREAD  TYPE      DATA");
        wprintf(L"\n");
		fflush(stdout);
    }

    if (writelog)
    {
		if(!lpLogFileName) return;
        FILE * fp;
        _wfopen_s(&fp, lpLogFileName, L"a");

        //fp is null, file is not open.
        if (fp==NULL)
            return;

        fwprintf(fp, L"%s",L"TIME           THREAD  TYPE      DATA");
        fwprintf(fp, L"\n");
        fclose(fp);
    }
}

void ConsoleEnable(BOOL console)
{
    enableconsole = console;
}

void Console()
{
    if(enableconsole)
    {

    }
}

void LogEnable(BOOL log)
{
    writelog = log;
}

void LogCleanup()
{
    if(enableconsole)FreeConsole();
    //SAFE_DELETE_ARRAY(lpLogFileName);
	HeapFree(hHeap,NULL,lpLogFileName);
}

BOOL CreateLog(LPWSTR logbasename,size_t logbasename_size, LPWSTR dirname,size_t dirname_size)
{

    UNREFERENCED_PARAMETER(logbasename_size);

    BOOL bRet = FALSE;

    if (writelog && logbasename && dirname)
    {

		lpLogFileName = (LPWSTR) HeapAlloc(hHeap,HEAP_ZERO_MEMORY,MAX_PATH*sizeof(WCHAR));
		LPWSTR lpLogFolderName = (LPWSTR) HeapAlloc(hHeap,HEAP_ZERO_MEMORY,(dirname_size+1)*sizeof(WCHAR));

        wcscpy_s(lpLogFileName,MAX_PATH,logbasename);
        wcscpy_s(lpLogFolderName,dirname_size,dirname);

        SYSTEMTIME systime;
        GetLocalTime(&systime);

        swprintf_s(lpLogFileName,MAX_PATH,L"%s\\%s_%u%02u%02u-%02u%02u%02u.log",
                   lpLogFolderName,logbasename,systime.wYear,systime.wMonth,systime.wDay,systime.wHour,systime.wMinute,systime.wSecond);

        if(!PathIsDirectory(lpLogFolderName)) CreateDirectory(lpLogFolderName, NULL);

		HeapFree(hHeap,NULL,lpLogFolderName);

        bRet = TRUE;
    }

    return bRet;
}

BOOL WriteLog(LogType logType, LPWSTR str,...)
{
    SYSTEMTIME systime;
    GetLocalTime(&systime);

    BOOL ret = FALSE;

    if(enableconsole)
    {
		if(hConsole == INVALID_HANDLE_VALUE)
		{
			CONSOLE_SCREEN_BUFFER_INFO csbi;
			AllocConsole();
			hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
			SetConsoleTitle(L"x360ce");
			GetConsoleScreenBufferInfo(hConsole,&csbi);

			SetConsoleTextAttribute(hConsole,csbi.wAttributes| FOREGROUND_INTENSITY);
		}

		WCHAR buf[MAX_PATH];
		DWORD dwChars = NULL;

        swprintf_s(buf,L"%02u:%02u:%02u.%03u %08u %s",\
                systime.wHour, systime.wMinute, systime.wSecond, systime.wMilliseconds,GetCurrentThreadId(),LogTypeNames[logType]);

		WriteConsole(hConsole, buf, wcslen(buf),&dwChars,NULL);


        va_list arglist;
        va_start(arglist,str);
        vswprintf_s(buf,str,arglist);

		WriteConsole(hConsole, buf, wcslen(buf),&dwChars,NULL);

        va_end(arglist);
        //wprintf(L" \n");
		WriteConsole(hConsole, L" \n", wcslen(L" \n"),&dwChars,NULL);

        ret = TRUE;
    }

    if (writelog)
    {
		if(!lpLogFileName) return FALSE;
        FILE * fp;
        _wfopen_s(&fp, lpLogFileName, L"a");

        //fp is null, file is not open.
        if (fp==NULL)
            return 0;

        fwprintf(fp, L"%02u:%02u:%02u.%03u %08u %s",\
                 systime.wHour, systime.wMinute, systime.wSecond, systime.wMilliseconds,GetCurrentThreadId(),LogTypeNames[logType]);
        va_list arglist;
        va_start(arglist,str);
        vfwprintf(fp,str,arglist);
        va_end(arglist);
        fwprintf(fp, L" \n");
        fclose(fp);
        ret = TRUE;
    }

    return ret;
}
