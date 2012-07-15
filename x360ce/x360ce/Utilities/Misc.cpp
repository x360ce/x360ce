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
#include <dinput.h>
#include "globals.h"
#include <Shlwapi.h>

LPCWSTR HostFileName()
{
    static WCHAR strPath[MAX_PATH];
    GetModuleFileName(NULL, strPath, MAX_PATH);
    return PathFindFileName(strPath);
}

LPCWSTR DLLFileName(HINSTANCE hModule)
{
    static WCHAR strPath[MAX_PATH];
    GetModuleFileName(hModule, strPath, MAX_PATH);
    return PathFindFileName(strPath);
}

#if 0
HRESULT GUIDtoString(const GUID pg, LPWSTR str, int size)
{
    int ret = StringFromGUID2(pg,str,size);

    if(ret)
		return S_OK;

    return E_FAIL;
}
#endif

LPCWSTR GUIDtoString(const GUID g, LPWSTR str)
{
	if(str == NULL)
		return NULL;
	swprintf_s(str,40,L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
		g.Data1,g.Data2,g.Data3,g.Data4[0],g.Data4[1],g.Data4[2],g.Data4[3],g.Data4[4],g.Data4[5],g.Data4[6],g.Data4[7]);
	return str;
}

LPCSTR GUIDtoString(const GUID g, LPSTR str)
{
	if(str == NULL)
		return NULL;
	sprintf_s(str,40,"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
		g.Data1,g.Data2,g.Data3,g.Data4[0],g.Data4[1],g.Data4[2],g.Data4[3],g.Data4[4],g.Data4[5],g.Data4[6],g.Data4[7]);
	return str;
}

void StringToGUID(LPCWSTR szBuf, GUID *rGuid)
{
	if(!szBuf)
		return;

	GUID g = GUID_NULL;

	DWORD data2;
	DWORD data3;
	DWORD tmp[8];

	if ((wcschr(szBuf,L'{')) && (wcsrchr(szBuf,L'}')))
	{
		swscanf_s(szBuf,L"{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
			&g.Data1,&data2,&data3,&tmp[0],&tmp[1],&tmp[2],&tmp[3],&tmp[4],&tmp[5],&tmp[6],&tmp[7]);
	}
	else
	{
		swscanf_s(szBuf,L"%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X",
			&g.Data1,&data2,&data3,&tmp[0],&tmp[1],&tmp[2],&tmp[3],&tmp[4],&tmp[5],&tmp[6],&tmp[7]);
	}


	g.Data2 = (WORD) data2;
	g.Data3 = (WORD) data3;
	for(int i=0; i < 8; ++i)
	{
		g.Data4[i] = (unsigned char) tmp[i];
	}

	*rGuid = g;
	return;
}



LPWSTR const DXErrStr(HRESULT dierr)
{
    if (dierr == E_FAIL) return L"E_FAIL";

    if (dierr == DIERR_ACQUIRED) return L"DIERR_ACQUIRED";

    if (dierr == DI_DOWNLOADSKIPPED) return L"DI_DOWNLOADSKIPPED";

    if (dierr == DI_EFFECTRESTARTED) return L"DI_EFFECTRESTARTED";

    if (dierr == DI_OK) return L"DI_OK";

    if (dierr == DI_POLLEDDEVICE) return L"DI_POLLEDDEVICE";

    if (dierr == DI_SETTINGSNOTSAVED) return L"DI_SETTINGSNOTSAVED";

    if (dierr == DI_TRUNCATED) return L"DI_TRUNCATED";

    if (dierr == DI_TRUNCATEDANDRESTARTED) return L"DI_TRUNCATEDANDRESTARTED";

    if (dierr == DI_WRITEPROTECT) return L"DI_WRITEPROTECT";

    if (dierr == DIERR_ACQUIRED) return L"DIERR_ACQUIRED";

    if (dierr == DIERR_ALREADYINITIALIZED) return L"DIERR_ALREADYINITIALIZED";

    if (dierr == DIERR_BADDRIVERVER) return L"DIERR_BADDRIVERVER";

    if (dierr == DIERR_BETADIRECTINPUTVERSION) return L"DIERR_BETADIRECTINPUTVERSION";

    if (dierr == DIERR_DEVICEFULL) return L"DIERR_DEVICEFULL";

    if (dierr == DIERR_DEVICENOTREG) return L"DIERR_DEVICENOTREG";

    if (dierr == DIERR_EFFECTPLAYING) return L"DIERR_EFFECTPLAYING";

    if (dierr == DIERR_GENERIC) return L"DIERR_GENERIC";

    if (dierr == DIERR_HANDLEEXISTS) return L"DIERR_HANDLEEXISTS";

    if (dierr == DIERR_HASEFFECTS) return L"DIERR_HASEFFECTS";

    if (dierr == DIERR_INCOMPLETEEFFECT) return L"DIERR_INCOMPLETEEFFECT";

    if (dierr == DIERR_INPUTLOST) return L"DIERR_INPUTLOST";

    if (dierr == DIERR_INVALIDPARAM) return L"DIERR_INVALIDPARAM";

    if (dierr == DIERR_MAPFILEFAIL) return L"DIERR_MAPFILEFAIL";

    if (dierr == DIERR_MOREDATA) return L"DIERR_MOREDATA";

    if (dierr == DIERR_NOAGGREGATION) return L"DIERR_NOAGGREGATION";

    if (dierr == DIERR_NOINTERFACE) return L"DIERR_NOINTERFACE";

    if (dierr == DIERR_NOTACQUIRED) return L"DIERR_NOTACQUIRED";

    if (dierr == DIERR_NOTBUFFERED) return L"DIERR_NOTBUFFERED";

    if (dierr == DIERR_NOTDOWNLOADED) return L"DIERR_NOTDOWNLOADED";

    if (dierr == DIERR_NOTEXCLUSIVEACQUIRED) return L"DIERR_NOTEXCLUSIVEACQUIRED";

    if (dierr == DIERR_NOTFOUND) return L"DIERR_NOTFOUND";

    if (dierr == DIERR_NOTINITIALIZED) return L"DIERR_NOTINITIALIZED";

    if (dierr == DIERR_OBJECTNOTFOUND) return L"DIERR_OBJECTNOTFOUND";

    if (dierr == DIERR_OLDDIRECTINPUTVERSION) return L"DIERR_OLDDIRECTINPUTVERSION";

    if (dierr == DIERR_OTHERAPPHASPRIO) return L"DIERR_OTHERAPPHASPRIO";

    if (dierr == DIERR_OUTOFMEMORY) return L"DIERR_OUTOFMEMORY";

    if (dierr == DIERR_READONLY) return L"DIERR_READONLY";

    if (dierr == DIERR_REPORTFULL) return L"DIERR_REPORTFULL";

    if (dierr == DIERR_UNPLUGGED) return L"DIERR_UNPLUGGED";

    if (dierr == DIERR_UNSUPPORTED) return L"DIERR_UNSUPPORTED";

    if (dierr == E_HANDLE) return L"E_HANDLE";

    if (dierr == E_PENDING) return L"E_PENDING";

    if (dierr == E_POINTER) return L"E_POINTER";

    if (dierr == S_FALSE) return L"S_FALSE";


    LPWSTR buffer = NULL;
    _itow_s(dierr,buffer,MAX_PATH,16);	//as hex
    return buffer;	//return value of HRESULT

}