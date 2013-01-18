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

#ifndef _InputHook_H_
#define _InputHook_H_

#include <CGuid.h>
#include <vector>
#include "detours.h"

using namespace MologieDetours;

class iHookPadConfig
{
public:
	iHookPadConfig();
	~iHookPadConfig(){};

	inline BOOL Enable()
	{
		return bEnabled = TRUE;
	}

	inline BOOL Disable()
	{
		return bEnabled = FALSE;
	}

	inline GUID SetProductGUID(const GUID &guid)
	{
		dwVIDPID = guid.Data1;
		return gProductGUID = guid;
	}

	inline GUID SetInstanceGUID(GUID &guid)
	{
		return gInstanceGUID = guid;
	}

	inline DWORD GetHookState()
	{
		return bEnabled;
	}

	inline GUID GetProductGUID()
	{
		return gProductGUID;
	}

	inline GUID GetInstanceGUID()
	{
		return gInstanceGUID;
	}

	inline DWORD GetProductVIDPID()
	{
		return dwVIDPID;
	}

private:
	BOOL  bEnabled;
	GUID  gProductGUID;
	GUID  gInstanceGUID;
	DWORD dwVIDPID;
};

class iHook
{
public:
	iHook();
	~iHook();
	//enum HookMode { HOOK_NONE = 0, HOOK_NORMAL, HOOK_COMPAT, HOOK_ALL};

	static const DWORD HOOK_NONE   = 0x00000000;
	static const DWORD HOOK_WMI    = 0x00000001;
	static const DWORD HOOK_DI     = 0x00000002;
	static const DWORD HOOK_VIDPID = 0x00000004;
	static const DWORD HOOK_NAME   = 0x00000008;
	static const DWORD HOOK_STOP   = 0x80000000;

	BOOL AddHook(iHookPadConfig &config);

	inline BOOL Enable()
	{
		return bEnabled = TRUE;
	}

	inline BOOL Disable()
	{
		return bEnabled = FALSE;
	}

	inline DWORD SetHookMode(DWORD mode)
	{
		return dwHookMode = mode;
	}

	inline DWORD SetHookVIDPID(DWORD vidpid)
	{
		return dwHookVIDPID = vidpid;
	}

	inline DWORD GetHookVIDPID()
	{
		return dwHookVIDPID;
	}

	inline BOOL EnableANSIMode(BOOL enable)
	{
		return bHookWMIANSI = enable;
	}

	inline BOOL EnableTrustHook(BOOL enable)
	{
		return bHookWinTrust = enable;
	}

	inline DWORD GetState()
	{
		return bEnabled;
	}

	inline DWORD GetHookMode()
	{
		return dwHookMode;
	}

	inline DWORD GetHookCount()
	{
		return dwHookCount;
	}

	inline iHookPadConfig& GetPadConfig(DWORD dwUserIndex)
	{
		return vPadConf[dwUserIndex];
	}

	inline HMODULE GetDinput8()
	{
		return hDinput8;
	}

	inline HMODULE SetDinput8(HMODULE hMod)
	{
		return hDinput8 = hMod;
	}

	BOOL iHook::ExecuteHooks();

private:
	BOOL  bEnabled;
	DWORD dwHookMode;
	DWORD dwHookVIDPID;
	BOOL  bHookWMIANSI;
	BOOL  bHookWinTrust;
	DWORD dwHookCount;
	HMODULE hDinput8;
protected:
	std::vector<iHookPadConfig> vPadConf;
};

extern iHook *iHookThis;
//typedef HRESULT (WINAPI *tDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
//extern tDirectInput8Create RealDirectInput8Create;

void HookWMI_UNI();
void HookWMI_ANSI();
void HookDI();
void HookWinTrust();

void HookWMI_UNI_Clean();
void HookWMI_ANSI_Clean();
void HookDIClean();
void HookWinTrustClean();

#if 0
VOID InputHook_Enable(BOOL state);
DWORD InputHook_Mode();
BOOL InputHook_Enable();

BOOL InputHook_Init(IHOOK_CONIFG* fconfig, IHOOK_GAMEPAD_CONIFG* gconfig);
BOOL InputHook_Clean();
#endif

#endif
