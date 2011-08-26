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
#include <Shlwapi.h>

LPWSTR lpConfigFile = NULL;

void IniCleanup() 
{
	SAFE_DELETE_ARRAY(lpConfigFile);
}

void SetIniFileName(LPCWSTR ininame) 
{
	WCHAR strPath[MAX_PATH];
	lpConfigFile = new WCHAR[MAX_PATH];

	GetModuleFileName(NULL, strPath, MAX_PATH);
	PathRemoveFileSpec(strPath);
	PathAddBackslash(strPath);

	swprintf_s(lpConfigFile,MAX_PATH,L"%s%s",strPath, ininame);
}

DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput, LPWSTR strDefault)
{
	if(lpConfigFile) {
		DWORD ret;
		LPWSTR pStr;
		ret = GetPrivateProfileString(strFileSection, strKey, strDefault, strOutput, MAX_PATH, lpConfigFile);

		pStr = wcschr(strOutput, L' ');
		if (pStr) {
			*pStr=L'\0';
			strOutput = pStr;
		}
		return ret;
	}
	return 0;
}

DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput)
{
	if (lpConfigFile) return ReadStringFromFile(strFileSection, strKey, strOutput, NULL);
	return 0;
}

UINT ReadUINTFromFile(LPCWSTR strFileSection, LPCWSTR strKey ,INT uDefault)
{
	if (lpConfigFile) return GetPrivateProfileInt(strFileSection,strKey,uDefault,lpConfigFile);
	return 0;
}

UINT ReadUINTFromFile(LPCWSTR strFileSection, LPCWSTR strKey)
{
	if (lpConfigFile) return ReadUINTFromFile(strFileSection, strKey, NULL);
	return 0;
}