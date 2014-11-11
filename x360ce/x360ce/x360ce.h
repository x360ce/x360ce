/*  x360ce - XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2002-2010 Racer_S
 *  Copyright (C) 2010-2013 Robert Krawczyk
 *
 *  x360ce is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  x360ce is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with x360ce.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#ifndef _X360CE_H_
#define _X360CE_H_

static const char * legal_notice = {
	"\nx360ce - XBOX 360 Controller emulator\n"
	"https://code.google.com/p/x360ce/\n\n"
	"Copyright (C) 2013 Robert Krawczyk\n\n"
	"This program is free software you can redistribute it and/or modify it under\n"
	"the terms of the GNU Lesser General Public License as published by the Free\n"
	"Software Foundation, either version 3 of the License, or any later version.\n"
};

extern HWND hMsgWnd;

class XInputEnabled
{
public:
	bool bEnabled;
	bool bUseEnabled;
	XInputEnabled() :
		bEnabled(false),
		bUseEnabled(false)
	{}
	virtual ~XInputEnabled() {};
};

struct xinput_dll
{
	HMODULE dll;

	// XInput 1.3 and older functions
	DWORD(WINAPI* XInputGetState)(DWORD dwUserIndex, XINPUT_STATE* pState);
	DWORD(WINAPI* XInputSetState)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
	DWORD(WINAPI* XInputGetCapabilities)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
	VOID(WINAPI* XInputEnable)(BOOL enable);
	DWORD(WINAPI* XInputGetDSoundAudioDeviceGuids)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
	DWORD(WINAPI* XInputGetBatteryInformation)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
	DWORD(WINAPI* XInputGetKeystroke)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);

	// XInput 1.3 undocumented functions
	DWORD(WINAPI* XInputGetStateEx)(DWORD dwUserIndex, XINPUT_STATE *pState); // 100
	DWORD(WINAPI* XInputWaitForGuideButton)(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid); // 101
	DWORD(WINAPI* XInputCancelGuideButtonWait)(DWORD dwUserIndex); // 102
	DWORD(WINAPI* XInputPowerOffController)(DWORD dwUserIndex); // 103

	// XInput 1.4 functions
	DWORD(WINAPI* XInputGetAudioDeviceIds)(DWORD dwUserIndex, LPWSTR pRenderDeviceId, UINT* pRenderCount, LPWSTR pCaptureDeviceId, UINT* pCaptureCount);

	// XInput 1.4 undocumented functionss
	DWORD(WINAPI* XInputGetBaseBusInformation)(DWORD dwUserIndex, struct XINPUT_BUSINFO* pBusinfo); // 104
	DWORD(WINAPI* XInputGetCapabilitiesEx)(DWORD unk1, DWORD dwUserIndex, DWORD dwFlags, struct XINPUT_CAPABILITIESEX* pCapabilitiesEx); // 108

	xinput_dll(){ ZeroMemory(this, sizeof(xinput_dll)); }
};

extern xinput_dll xinput;

template<typename N>
inline void LoadFunctionF(HMODULE module, const char* funcname, N* ppfunc)
{
	if (ppfunc) *ppfunc = reinterpret_cast<N>(GetProcAddress(module, funcname));
}

#define LoadFunction(handle_struct, func) LoadFunctionF(handle_struct ## .dll, #func, &handle_struct ## .func)
#define LoadFunctionOrdinal(handle_struct, ordinal, func) LoadFunctionF(handle_struct ## .dll, (const char*)ordinal, &handle_struct ## .func)
#define LoadFunctionOrdinal2(handle_struct, ordinal) LoadFunctionF(handle_struct ## .dll, (const char*)ordinal, &handle_struct ## .___XXX___ ## ordinal)

#endif
