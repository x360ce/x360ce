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
#include "Utilities\Log.h"
#include <Softpub.h>

#include "InputHook.h"

static iHook *iHookThis = NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef LONG (WINAPI *tWinVerifyTrust)(HWND hwnd, GUID *pgActionID,LPVOID pWVTData);

tWinVerifyTrust hWinVerifyTrust = NULL;
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

LONG WINAPI HookWinVerifyTrust(HWND hwnd, GUID *pgActionID,LPVOID pWVTData)
{
	if(!iHookThis->CheckHook(iHook::HOOK_WT)) return HookWinVerifyTrust(hwnd,pgActionID,pWVTData);
	WriteLog(LOG_HOOKWT,L"*WinVerifyTrust*");

	UNREFERENCED_PARAMETER(hwnd);
	UNREFERENCED_PARAMETER(pgActionID);
	UNREFERENCED_PARAMETER(pWVTData);
	return 0;
}

void iHook::HookWT()
{
	if(!CheckHook(iHook::HOOK_WT)) return;
	iHookThis = this;

	if(!hWinVerifyTrust) 
	{
		hWinVerifyTrust = WinVerifyTrust;
		if(HooksSafeTransition(hWinVerifyTrust,true))
		{
			HooksInsertNewRedirection(hWinVerifyTrust,HookWinVerifyTrust,TEE_HOOK_NRM_JUMP);
			HooksSafeTransition(hWinVerifyTrust,false);
		}
	}
}

void iHook::HookWT_Cleanup()
{
	if(hWinVerifyTrust)
	{
		WriteLog(LOG_HOOKCOM,L"Removing WinVerifyTrust Hook");
		if(HooksSafeTransition(hWinVerifyTrust,true))
		{
			HooksRemoveRedirection(hWinVerifyTrust,true);
			HooksSafeTransition(hWinVerifyTrust,false);
		}
	}
}