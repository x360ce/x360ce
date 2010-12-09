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

#ifndef _FAKEAPI_H_
#define _FAKEAPI_H_

#include <CGuid.h>
#include "EasyHook.h"

extern ULONG ACLEntries[1];

struct FAKEAPI_GAMEPAD_CONIFG
{
	BOOL  bEnabled;
	GUID  productGUID;
	GUID  instanceGUID;
	DWORD dwVID;
	DWORD dwPID;
	FAKEAPI_GAMEPAD_CONIFG()
	{
		ZeroMemory(this,sizeof(FAKEAPI_GAMEPAD_CONIFG));
	}
};

struct FAKEAPI_CONIFG
{
	BOOL  bEnabled;
	DWORD dwFakeMode;
	DWORD dwFakeVID;
	DWORD dwFakePID;
	DWORD dwFakeWinTrust;
	FAKEAPI_CONIFG()
	{
		ZeroMemory(this,sizeof(FAKEAPI_CONIFG));
	}
};

void FakeWMI();
void FakeDI();
void FakeWinTrust();

void FakeWMIClean();
void FakeDIClean();
void FakeWinTrustClean();

FAKEAPI_CONIFG* FakeAPI_Config();
FAKEAPI_GAMEPAD_CONIFG* FakeAPI_GamepadConfig(DWORD dwUserIndex);

BOOL FakeAPI_Enable(BOOL state);
BOOL FakeAPI_Enable();

BOOL FakeAPI_Init(FAKEAPI_CONIFG* fconfig, FAKEAPI_GAMEPAD_CONIFG* gconfig);
BOOL FakeAPI_Clean();

#endif
