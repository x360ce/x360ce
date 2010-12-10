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

#ifndef _UTILS_H_
#define _UTILS_H_

// externs
//BOOL writelog;
//LPWSTR lpConfigFile;
//LPWSTR lpLogFileName;

// prototypes
DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput);
DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput, LPWSTR strDefault);
UINT ReadUINTFromFile(LPCWSTR strFileSection, LPCWSTR strKey);
UINT ReadUINTFromFile(LPCWSTR strFileSection, LPCWSTR strKey ,INT uDefault);
LPCWSTR ModuleFileName();
LPWSTR const DXErrStr(HRESULT dierr);
LONG clamp(LONG val, LONG min, LONG max);
LONG deadzone(LONG val, LONG min, LONG max, LONG lowerDZ, LONG upperDZ);
HRESULT GUIDtoString(const GUID pg, LPWSTR data, int size);
HRESULT StringToGUID(LPWSTR szBuf, GUID *rGuid);
void SetIniFileName(LPCWSTR ininame);
void LogEnable(BOOL log);
VOID CreateLog(LPWSTR logbasename, LPWSTR foldename);
BOOL WriteLog(LPWSTR str,...);
void IniCleanup();
void LogCleanup();
void Console();

#endif // _UTILS_H_
