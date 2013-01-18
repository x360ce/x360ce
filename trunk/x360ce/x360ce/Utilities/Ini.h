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

#ifndef _INI_H_
#define _INI_H_

#include "globals.h"
#include <Shlwapi.h>
#include <string.h>

class InI
{
public:

	InI(){};
	virtual ~InI(void){};

	inline void SetIniFileName(LPCWSTR ininame)
	{
		WCHAR strPath[MAX_PATH];
		WCHAR tmp[MAX_PATH];
		//g_pConfigFile = new WCHAR[MAX_PATH];

		GetModuleFileName(NULL, strPath, MAX_PATH);
		PathRemoveFileSpec(strPath);
		PathAddBackslash(strPath);

		swprintf_s(tmp,MAX_PATH,L"%s%s",strPath, ininame);
		wsConfigFile = tmp;
	}

	inline DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput, LPWSTR strDefault)
	{
		if(wsConfigFile.empty()) return 0;

		DWORD ret;
		LPWSTR pStr;
		ret = GetPrivateProfileString(strFileSection, strKey, strDefault, strOutput, MAX_PATH, wsConfigFile.c_str());

		pStr = wcschr(strOutput, L'#');
		if (pStr) *pStr=L'\0';
		pStr = wcschr(strOutput, L';');
		if (pStr) *pStr=L'\0';

		return ret;
	}

	inline DWORD ReadStringFromFile(LPCWSTR strFileSection, LPCWSTR strKey, LPWSTR strOutput)
	{
		return ReadStringFromFile(strFileSection, strKey, strOutput, NULL);
	}

	inline long ReadLongFromFile(LPCWSTR strFileSection, LPCWSTR strKey ,INT uDefault)
	{

		WCHAR tmp[MAX_PATH];
		WCHAR def[MAX_PATH];
		DWORD ret;

		swprintf_s(def,MAX_PATH,L"%u",uDefault);
		ret = ReadStringFromFile(strFileSection,strKey,tmp,def);

		if(ret) return _wtol(tmp);
		else return 0;

	}

	inline long ReadLongFromFile(LPCWSTR strFileSection, LPCWSTR strKey)
	{
		return ReadLongFromFile(strFileSection, strKey, 0);
	}

private:
	std::wstring wsConfigFile;
};

#endif // _INI_H_
