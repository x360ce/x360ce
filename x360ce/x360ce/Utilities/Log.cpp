/*  x360ce - XBOX360 Controler Emulator
 *  Copyright (C) 2002-2010 ToCA Edit
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
#include <Shlwapi.h>

BOOL writelog = FALSE;
BOOL enableconsole = FALSE;
LPWSTR lpLogFileName = NULL;
LPWSTR lpLogFolderName = NULL;

void ConsoleEnable(BOOL console) 
{
	enableconsole = console;
}

void Console()
{
	if(enableconsole){
		AllocConsole();

		HANDLE handle_out = GetStdHandle(STD_OUTPUT_HANDLE);
		int hCrt = _open_osfhandle((long) handle_out, _O_TEXT);
		FILE* hf_out = _wfdopen(hCrt, L"w");
		setvbuf(hf_out, NULL, _IONBF, 1);
		*stdout = *hf_out;
	}
}

void LogEnable(BOOL log) 
{
	writelog = log;
}

void LogCleanup() 
{
	SAFE_DELETE_ARRAY(lpLogFileName);
}

BOOL CreateLog(LPWSTR logbasename,size_t logbasename_size, LPWSTR dirname,size_t dirname_size)
{

	UNREFERENCED_PARAMETER(logbasename_size);

	BOOL bRet = FALSE;

	if (writelog && logbasename && dirname) 
	{
		lpLogFileName = new WCHAR[MAX_PATH];
		lpLogFolderName = new WCHAR[dirname_size+1];

		wcscpy_s(lpLogFileName,MAX_PATH,logbasename);
		wcscpy_s(lpLogFolderName,dirname_size,dirname);

		SYSTEMTIME systime;
		GetLocalTime(&systime);

		swprintf_s(lpLogFileName,MAX_PATH,L"%s\\%s_%u%02u%02u-%02u%02u%02u.log",
			lpLogFolderName,logbasename,systime.wYear,systime.wMonth,systime.wDay,systime.wHour,systime.wMinute,systime.wSecond);

		if(!PathIsDirectory(lpLogFolderName)) CreateDirectory(lpLogFolderName, NULL);

		SAFE_DELETE_ARRAY(lpLogFolderName);

		bRet = TRUE;
	}

	return bRet;
}

BOOL WriteLog(LPWSTR str,...)
{
	SYSTEMTIME systime;
	GetLocalTime(&systime);

	BOOL ret = FALSE;

	if(enableconsole)
	{
		wprintf(L"%02u:%02u:%02u.%03u:: ", systime.wHour, systime.wMinute, systime.wSecond, systime.wMilliseconds);
		va_list arglist;
		va_start(arglist,str);
		vwprintf(str,arglist);
		va_end(arglist);
		wprintf(L" \n");
		ret = TRUE;
	}

	if (writelog) {
		FILE * fp;
		_wfopen_s(&fp, lpLogFileName, L"a");

		//fp is null, file is not open.
		if (fp==NULL)
			return 0;
		fwprintf(fp, L"%02u:%02u:%02u.%03u:: ", systime.wHour, systime.wMinute, systime.wSecond, systime.wMilliseconds);
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
