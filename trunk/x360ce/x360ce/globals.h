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
 
#include "svnrev.h"

//useful macros
#define IN_RANGE(val, min, max) ((val) > (min) && (val) < (max)) 
#define IN_RANGE2(val, min, max) ((val) >= (min) && (val) <= (max)) 
#define arrayof(x) (sizeof(x)/sizeof(x[0])) 
#define MAX_PATHW MAX_PATH*2
#define STRINGIFY(x) #x
#define TOSTRING(x) STRINGIFY(x)

#define VERSION_MAJOR 3
#define VERSION_MINOR 2
#define VERSION_PATCH 0
#define VERSION_STRING TOSTRING(VERSION_MAJOR) "." TOSTRING(VERSION_MINOR) "." TOSTRING(VERSION_PATCH) "." TOSTRING(SVN_REV)


// externs
extern HINSTANCE hX360ceInstance;
extern HINSTANCE hNativeInstance;
extern HWND hWnd;
extern DWORD dwAppPID;
extern LPCTSTR PIDName(DWORD);
extern VOID InitConfig();
extern BOOL bEnabled;
extern WORD wNativeMode;

// prototypes
void LoadOriginalDll();




