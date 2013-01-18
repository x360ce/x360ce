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
#include "InputHook.h"

iHook *iHookThis = NULL;

iHookPadConfig::iHookPadConfig()
	:bEnabled(0)
	,gProductGUID(GUID_NULL)
	,gInstanceGUID(GUID_NULL)
	,dwVIDPID(0)
{

}

iHook::iHook()
	:bEnabled(0)
	,dwHookMode(0)
	,dwHookVIDPID(MAKELONG(0x028E,0x045E))
	,bHookWMIANSI(0)
	,bHookWinTrust(0)
	,dwHookCount(0)
{
	iHookThis = this;
}

iHook::~iHook()
{
	HookWMI_UNI_Clean();
	//HookWMI_ANSI_Clean();
	HookDIClean();
	HookWinTrustClean();
}

BOOL iHook::AddHook(iHookPadConfig &config)
{
	dwHookCount++;
	vPadConf.push_back(config);
	return TRUE;
}

BOOL iHook::ExecuteHooks()
{
	if(!bEnabled) return FALSE;

	if(!bHookWMIANSI)
		HookWMI_UNI();
//	else
		//HookWMI_ANSI();

	if(dwHookMode & HOOK_DI)
		HookDI();

	if(bHookWinTrust)
		HookWinTrust();

	return TRUE;
}