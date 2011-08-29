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

LPWSTR g_pConfigFile = NULL;

void IniCleanup() 
{
	SAFE_DELETE_ARRAY(g_pConfigFile);
}

void SetIniFileName(LPCWSTR ininame) 
{
	WCHAR strPath[MAX_PATH];
	g_pConfigFile = new WCHAR[MAX_PATH];

	GetModuleFileName(NULL, strPath, MAX_PATH);
	PathRemoveFileSpec(strPath);
	PathAddBackslash(strPath);

	swprintf_s(g_pConfigFile,MAX_PATH,L"%s%s",strPath, ininame);
}

DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput, LPWSTR strDefault)
{
	if(g_pConfigFile)
	{
		DWORD ret;
		LPWSTR pStr;
		ret = GetPrivateProfileString(strFileSection, strKey, strDefault, strOutput, MAX_PATH, g_pConfigFile);

		pStr = wcschr(strOutput, L'#');
		if (pStr) {
			*pStr=L'\0';
		}
		pStr = wcschr(strOutput, L';');
		if (pStr) {
			*pStr=L'\0';
		}
		return ret;
	}
	return 0;
}

DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput)
{
	if (g_pConfigFile)
		return ReadStringFromFile(strFileSection, strKey, strOutput, NULL);
	return 0;
}

long ReadLongFromFile(LPCWSTR strFileSection, LPCWSTR strKey ,INT uDefault)
{
	if (g_pConfigFile) 
	{
		WCHAR tmp[MAX_PATH];
		WCHAR def[MAX_PATH];
		DWORD ret;

		swprintf_s(def,MAX_PATH,L"%u",uDefault);
		ret = ReadStringFromFile(strFileSection,strKey,tmp,def);

		if(ret)
			return _wtol(tmp);
	}
	return -1;
}

long ReadLongFromFile(LPCWSTR strFileSection, LPCWSTR strKey)
{
	if (g_pConfigFile) return ReadLongFromFile(strFileSection, strKey, -1);
	return 0;
}