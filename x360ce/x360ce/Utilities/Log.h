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

#ifndef _LOG_H_
#define _LOG_H_

enum LogType { LOG_CORE, LOG_XINPUT, LOG_DINPUT, LOG_IHOOK, LOG_HOOKDI, LOG_HOOKWMI, LOG_HOOKWT };

// prototypes
void WriteStamp();
void LogEnable(BOOL log);
BOOL CreateLog(LPWSTR logbasename,size_t logbasename_size, LPWSTR foldename,size_t foldename_size);
void Console();
BOOL WriteLog(LogType logType, LPWSTR str,...);
void LogCleanup();

#endif // _LOG_H_