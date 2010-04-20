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
DWORD ReadStringFromFile(LPCTSTR strFileSection, LPCTSTR strKey, LPTSTR strOutput);
DWORD ReadStringFromFile(LPCTSTR strFileSection, LPCTSTR strKey, LPTSTR strOutput, LPTSTR strDefault);
UINT ReadUINTFromFile(LPCTSTR strFileSection, LPCTSTR strKey);
UINT ReadUINTFromFile(LPCTSTR strFileSection, LPCTSTR strKey ,UINT uDefault);
HWND GetWindowHandle(DWORD);
LPTSTR const DXErrStr(HRESULT dierr);
INT WriteLog(LPCTSTR,...);
LONG clamp(LONG val, LONG min, LONG max);
LONG deadzone(LONG val, LONG min, LONG max, LONG lowerDZ, LONG upperDZ);
void GUIDtoString(wchar_t *data, const GUID *pg);
int StringToGUID(GUID *pg, TCHAR *dataw);


// externs

extern TCHAR tstrConfigFile[MAX_PATH];
extern BOOL writelog;
extern BOOL logready; 
extern LPTSTR logfilename;