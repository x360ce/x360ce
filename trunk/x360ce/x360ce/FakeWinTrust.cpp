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
#include "Utils.h"
#include <Softpub.h>
#include "FakeAPI.h"

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
LONG (WINAPI *OriginalWinVerifyTrust)(HWND hwnd, GUID *pgActionID,LPVOID pWVTData) = NULL;
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

LONG WINAPI FakeWinVerifyTrust(HWND hwnd, GUID *pgActionID,LPVOID pWVTData)
{
	UNREFERENCED_PARAMETER(hwnd);
	UNREFERENCED_PARAMETER(pgActionID);
	UNREFERENCED_PARAMETER(pWVTData);
	return 0;
}

void FakeWinTrust()
{
	if(!OriginalWinVerifyTrust) {
		OriginalWinVerifyTrust =WinVerifyTrust;

		WriteLog(_T("[FAKEAPI] FakeWinTrust:: Attaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)OriginalWinVerifyTrust, FakeWinVerifyTrust);
		DetourTransactionCommit();
	}
}

void FakeWinTrust_Detach()
{
	if(OriginalWinVerifyTrust) {
		WriteLog(_T("[FAKEWT]  FakeWinVerifyTrust:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalWinVerifyTrust, FakeWinVerifyTrust);
		DetourTransactionCommit();
	}
}