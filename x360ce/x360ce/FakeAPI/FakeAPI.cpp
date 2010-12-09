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
#include "FakeAPI.h"

FAKEAPI_CONIFG* FakeAPIConfig;
FAKEAPI_GAMEPAD_CONIFG* GamepadConfig[4];

ULONG ACLEntries[1] = {0};

FAKEAPI_CONIFG* FakeAPI_Config()
{
	return FakeAPIConfig;
}

FAKEAPI_GAMEPAD_CONIFG* FakeAPI_GamepadConfig(DWORD dwUserIndex)
{
	return GamepadConfig[dwUserIndex];
}

BOOL FakeAPI_Enable(BOOL state)
{
	FakeAPI_Config()->bEnabled = state;
	return state;
}

BOOL FakeAPI_Enable()
{
	return FakeAPI_Config()->bEnabled;
}

BOOL FakeAPI_Init(FAKEAPI_CONIFG* fconfig, FAKEAPI_GAMEPAD_CONIFG* gconfig)
{

	if(!fconfig->bEnabled) return FALSE;

	FakeAPIConfig = fconfig;

	for(WORD i = 0; i < 4; i++) {

		GamepadConfig[i] = gconfig;
		gconfig++;

		if(!IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID, GUID_NULL) && !IsEqualGUID(FakeAPI_GamepadConfig(i)->instanceGUID, GUID_NULL))
		{
			if(!FakeAPI_GamepadConfig(i)->dwVID) FakeAPI_GamepadConfig(i)->dwVID = LOWORD(FakeAPI_GamepadConfig(i)->productGUID.Data1);
			if(!FakeAPI_GamepadConfig(i)->dwPID) FakeAPI_GamepadConfig(i)->dwPID = HIWORD(FakeAPI_GamepadConfig(i)->productGUID.Data1);

			FakeAPI_GamepadConfig(i)->bEnabled = 1;
		}
	}
	
	if(FakeAPI_Config()->bEnabled) {
		FakeWMI();
		if(FakeAPI_Config()->dwFakeMode >= 2) FakeDI();
		if(FakeAPI_Config()->dwFakeWinTrust) FakeWinTrust();
	}

	return TRUE;
}

BOOL FakeAPI_Clean()
{

	if(FakeAPI_Config()->bEnabled) {
		LhUninstallAllHooks();
		LhWaitForPendingRemovals();

		FakeWMIClean();
		if(FakeAPI_Config()->dwFakeMode >= 2) FakeDIClean();
		if(FakeAPI_Config()->dwFakeWinTrust) FakeWinTrustClean();
	}

	return TRUE;
}

