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
#include "x360ce.h"
#include "Utils.h"
#include "Config.h"
#include "DirectInput.h"

#pragma warning(disable:4310)

BOOL bEnabled = FALSE;
BOOL bUseEnabled= FALSE;

bool bPAD[4] = {FALSE,FALSE,FALSE,FALSE};

XINPUT_CAPABILITIES *LPXCAPS[4]={NULL,NULL,NULL,NULL};

BOOL RegisterWindowClass(HINSTANCE hinstance) 
{ 
	WNDCLASS wc; 

	// Fill in the window class structure with parameters 
	// that describe the main window. 

	wc.style = CS_HREDRAW | CS_VREDRAW;
	wc.lpfnWndProc = DefWindowProc;     // points to window procedure 
	wc.cbClsExtra = 0;                // no extra class memory 
	wc.cbWndExtra = 0;                // no extra window memory 
	wc.hInstance = hinstance;         // handle to instance 
	wc.hIcon = NULL;              // predefined app. icon 
	wc.hCursor = NULL;                    // predefined arrow 
	wc.hbrBackground = NULL;    // white background brush 
	wc.lpszMenuName =  L"x360ceMenu";    // name of menu resource
	wc.lpszClassName = L"x360ceWClass";  // name of window class 

	// Register the window class. 
	return RegisterClass(&wc); 
} 

BOOL Createx360ceWindow(HINSTANCE hInst)
{
	if(RegisterWindowClass(hInst)) {
		hWnd = CreateWindow( 
			L"x360ceWClass",  // name of window class
			L"x360ce",        // title-bar string 
			WS_OVERLAPPEDWINDOW, // top-level window 
			CW_USEDEFAULT,       // default horizontal position 
			CW_USEDEFAULT,       // default vertical position 
			CW_USEDEFAULT,       // default width 
			CW_USEDEFAULT,       // default height 
			(HWND) NULL,         // no owner window 
			(HMENU) NULL,        // use class menu 
			hInst,     // handle to application instance 
			(LPVOID) NULL);      // no window-creation data 
		return TRUE;
	}
	else {
		WriteLog(L"[CORE]    RegisterWindowClass Failed");
		return FALSE;
	}
}

void XDeInit()
{
	for(DWORD dwUserIndex=0; dwUserIndex<XUSER_MAX_COUNT; dwUserIndex++) {
		SAFE_DELETE(LPXCAPS[dwUserIndex]);
	}

}

HRESULT XInit(DWORD dwUserIndex)
{
	bPAD[dwUserIndex] = true;

	HRESULT hr=S_OK;

	if(!hWnd) {	
		if(!Createx360ceWindow(hX360ceInstance)) {
			WriteLog(L"[CORE]    x360ce window not created, ForceFeedback will be disabled !");
		}
	}

	if(!Gamepad[dwUserIndex].productGUID.Data1) return ERROR_DEVICE_NOT_CONNECTED;

	if(!Gamepad[dwUserIndex].g_pGamepad){ 

		WriteLog(L"[XINIT]   Initializing Gamepad %d",dwUserIndex+1);

		hr = Enumerate(dwUserIndex); 
		if(SUCCEEDED(hr)) {
			WriteLog(L"[XINIT]   [PAD%d] Enumerated",dwUserIndex+1);
		}
		if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

		hr = InitDirectInput(hWnd,dwUserIndex);
		if(FAILED(hr)) {
			WriteLog(L"[XINIT]   XInit fail with %s",DXErrStr(hr));
		}
	}
	else return ERROR_DEVICE_NOT_CONNECTED;

	if(!Gamepad[dwUserIndex].g_pGamepad) {
		WriteLog(L"[XINIT]   XInit fail");
		return ERROR_DEVICE_NOT_CONNECTED;
	}
	else UpdateState(dwUserIndex);

	if(!FakeAPI_Enable()) {
		FakeAPI_Enable(1);
		WriteLog(L"[DINPUT]  Restore FakeAPI state");
	}

	return hr;
}

extern VOID DetachFakeAPI();

extern "C" DWORD WINAPI XInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{

	//WriteLog(_T("XInputGetState"));
	if(Gamepad[dwUserIndex].native) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetState_t)(DWORD dwUserIndex, XINPUT_STATE* pState);
		XInputGetState_t nativeXInputGetState = (XInputGetState_t) GetProcAddress( hNativeInstance, "XInputGetState");
		return nativeXInputGetState(dwUserIndex,pState);
	}

	if (!pState || dwUserIndex >= XUSER_MAX_COUNT) return ERROR_BAD_ARGUMENTS; 

	HRESULT hr=ERROR_DEVICE_NOT_CONNECTED;

	if(!bPAD[dwUserIndex]) {
		hr = XInit(dwUserIndex);
		if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;
	}

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	/*
	Nasty trick to support XInputEnable states, because not every game calls it so:
	- must support games that use it, and do enable/disable as needed by game
	if bEnabled is FALSE and bUseEnabled is TRUE = gamepad is disabled -> return fake S_OK, ie. connected but state not updating
	if bEnabled is TRUE and bUseEnabled is TRUE = gamepad is enabled -> continue, ie. connected and updating state
	- must support games that not call it
	if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states 
	*/

	if(!bEnabled && bUseEnabled) return S_OK;

	// poll data from device
	hr = UpdateState(dwUserIndex);

	if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

#if defined(DEBUG) | defined(_DEBUG)
	WriteLog(L"UpdateState %d %d",dwUserIndex,hr);
#endif

	GamepadMap PadMap = GamepadMapping[dwUserIndex];

	XINPUT_GAMEPAD &xGamepad = pState->Gamepad;

	xGamepad.wButtons = 0;
	xGamepad.bLeftTrigger = 0;
	xGamepad.bRightTrigger = 0;
	xGamepad.sThumbLX = 0;
	xGamepad.sThumbLY = 0;
	xGamepad.sThumbRX = 0;
	xGamepad.sThumbRY = 0;

	// timestamp packet
	pState->dwPacketNumber=GetTickCount();

	// --- Map buttons ---
	for (INT i = 0; i < 10; ++i) {
		if ((PadMap.Button[i] >= 0) && ButtonPressed(PadMap.Button[i],dwUserIndex)) 
			xGamepad.wButtons |= buttonIDs[i];
	}

	// --- Map POV to the D-pad ---
	if ((PadMap.DpadPOV >= 0) && !PadMap.PovIsButton) {

		//INT pov = POVState(PadMap.DpadPOV,dwUserIndex,Gamepad[dwUserIndex].povrotation);

		DWORD pov = Gamepad[dwUserIndex].state.rgdwPOV[PadMap.DpadPOV];
		DWORD povdeg = pov/100;

		// Up-left, up, up-right, up (at 360 degrees)
		if (IN_RANGE(povdeg,270,360) || IN_RANGE(povdeg,0,90) || povdeg == 0 ) { 
			xGamepad.wButtons |= PadMap.pov[0];
		}
		// Up-right, right, down-right
		if (IN_RANGE(povdeg,0,180)) { 
			xGamepad.wButtons |= PadMap.pov[3];	
		}
		// Down-right, down, down-left
		if (IN_RANGE(povdeg,90,270)) { 
			xGamepad.wButtons |= PadMap.pov[1];
		}
		// Down-left, left, up-left	
		if (IN_RANGE(povdeg,180,360)) { 
			xGamepad.wButtons |= PadMap.pov[2];
		}

	}
	else if((PadMap.DpadPOV < 0) && PadMap.PovIsButton) {
		for (INT i = 0; i < 4; ++i) {
			if ((PadMap.pov[i] >= 0) && ButtonPressed(PadMap.pov[i],dwUserIndex)) {
				xGamepad.wButtons |= povIDs[i];
			}
		}
	}

	// Created so we can refer to each axis with an ID
	LONG axis[7] = {
		Gamepad[dwUserIndex].state.lX,
		Gamepad[dwUserIndex].state.lY,
		Gamepad[dwUserIndex].state.lZ,
		Gamepad[dwUserIndex].state.lRx,
		Gamepad[dwUserIndex].state.lRy,
		Gamepad[dwUserIndex].state.lRz,
		0
	};
	LONG slider[2] = {
		Gamepad[dwUserIndex].state.rglSlider[0],
		Gamepad[dwUserIndex].state.rglSlider[1]
	};

	// --- Map triggers ---
	BYTE *targetTrigger[2] = {
		&xGamepad.bLeftTrigger,
		&xGamepad.bRightTrigger
	};

	for (INT i = 0; i < 2; ++i) {

		MappingType triggerType = PadMap.Trigger[i].type;

		if (triggerType == DIGITAL) {
			if (ButtonPressed(PadMap.Trigger[i].id,dwUserIndex)) {
				*(targetTrigger[i]) = 255;
			}
		} 
		else {
			LONG *values;
			switch (triggerType) {
			case AXIS:
			case HAXIS:
				values = axis;
				break;
			case SLIDER:
			case HSLIDER:
				values = slider;
				break;
			default:
				values = axis;
				break;
			}

			LONG v = 0;
			if(PadMap.Trigger[i].id > 0) {
				v = values[PadMap.Trigger[i].id -1];
			}
			else {
				v = -values[-PadMap.Trigger[i].id -1] - 1;
			}

			/*
			--- v is the full range (-32767 .. +32767) that should be projected to 0...255

			--- Full ranges
			AXIS:	(	0 to 255 from -32767 to 32767) using axis
			SLIDER:	(	0 to 255 from -32767 to 32767) using slider
			------------------------------------------------------------------------------
			--- Half ranges
			HAXIS:	(	0 to 255 from 0 to 32767) using axis
			HSLIDER:	(	0 to 255 from 0 to 32767) using slider
			*/

			LONG v2=0;
			LONG offset=0;
			LONG scaling=1;

			switch (triggerType) {
				// Full range
			case AXIS:
			case SLIDER:
				scaling = 256; offset = 32767;
				break;
				// Half range
			case HAXIS:
			case HSLIDER:
				scaling = 128; offset = 0;
				break;
			default:
				scaling = 1; offset = 0;
				break;
			}

			v2 = (v + offset) / scaling;

			// Add deadzones
			*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, Gamepad[dwUserIndex].tdeadzone, 255);

		}
	}

	// --- Map thumbsticks ---

	// Created so we can refer to each axis with an ID
	SHORT *targetAxis[4] = {
		&xGamepad.sThumbLX,
		&xGamepad.sThumbLY,
		&xGamepad.sThumbRX,
		&xGamepad.sThumbRY
	};

	// NOTE: Could add symbolic constants as indexers, such as 
	// THUMB_LX_AXIS, THUMB_LX_POSITIVE, THUMB_LX_NEGATIVE
	if(Gamepad[dwUserIndex].axistodpad==0)
	{


		for (INT i = 0; i < 4; ++i) {
			LONG *values = axis;
			// Analog input
			if (PadMap.Axis[i].analogType == AXIS) values = axis;
			if (PadMap.Axis[i].analogType == SLIDER) values = slider;
			if (PadMap.Axis[i].analogType != NONE) {

				if(PadMap.Axis[i].id > 0 ) {
					SHORT val = (SHORT) values[PadMap.Axis[i].id - 1];
					*(targetAxis[i])= (SHORT) clamp(val,-32767,32767);
				}
				else if(PadMap.Axis[i].id < 0 ) {
					SHORT val = (SHORT) -values[-PadMap.Axis[i].id - 1];
					*(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
				}
			}

			// Digital input, positive direction
			if (PadMap.Axis[i].hasDigital && PadMap.Axis[i].positiveButtonID >= 0) {

				if (ButtonPressed(PadMap.Axis[i].positiveButtonID,dwUserIndex))
					*(targetAxis[i]) = 32767;	
			}	
			// Digital input, negative direction
			if (PadMap.Axis[i].hasDigital && PadMap.Axis[i].negativeButtonID >= 0) {

				if (ButtonPressed(PadMap.Axis[i].negativeButtonID,dwUserIndex))
					*(targetAxis[i]) = -32767;
			}	
		}
	}

	//WILDS - Axis to D-Pad
	if(Gamepad[dwUserIndex].axistodpad==1) {
		//WriteLog("x: %d, y: %d, z: %d",Gamepad[dwUserIndex].state.lX,Gamepad[dwUserIndex].state.lY,Gamepad[dwUserIndex].state.lZ);

		if(Gamepad[dwUserIndex].state.lX - Gamepad[dwUserIndex].axistodpadoffset > Gamepad[dwUserIndex].axistodpaddeadzone)
			xGamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if(Gamepad[dwUserIndex].state.lX - Gamepad[dwUserIndex].axistodpadoffset < -Gamepad[dwUserIndex].axistodpaddeadzone)
			xGamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;

		if(Gamepad[dwUserIndex].state.lY - Gamepad[dwUserIndex].axistodpadoffset < -Gamepad[dwUserIndex].axistodpaddeadzone)
			xGamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
		if(Gamepad[dwUserIndex].state.lY - Gamepad[dwUserIndex].axistodpadoffset > Gamepad[dwUserIndex].axistodpaddeadzone)
			xGamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
	}
	//WILDS END

	for (int i = 0; i < 4; ++i)
	{

		if (Gamepad[dwUserIndex].antidz[i]) {
			SHORT antidz = Gamepad[dwUserIndex].antidz[i];
			LONG val = *(targetAxis[i]);
			SHORT direction = val > 0 ? 1 : -1;
			val = (LONG)(abs(val) / (32767 / (32767 - antidz * 1.0)) + antidz);
			val = min(val, 32767);
			if(val == Gamepad[dwUserIndex].antidz[i] || val == -Gamepad[dwUserIndex].antidz[i]) val = 0;
			*(targetAxis[i]) = (SHORT) (direction * val);
		}

		if (Gamepad[dwUserIndex].adeadzone[i]) {
			SHORT dz = Gamepad[dwUserIndex].adeadzone[i];
			LONG val = *(targetAxis[i]);
			if((val <= dz) && (val >= -dz) ) val = 0;
			*(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
		}

	// --- Do Linears ---

		if (Gamepad[dwUserIndex].axislinear[i]) {

			SHORT absval = (SHORT)((abs(*(targetAxis[i])) + (((32767.0 / 2.0) - (((abs((abs(*(targetAxis[i]))) - (32767.0 / 2.0)))))) * (Gamepad[dwUserIndex].axislinear[i] * 0.01))));
			*(targetAxis[i]) = *(targetAxis[i]) > 0 ? absval : -absval;
		}
	}

	if(SUCCEEDED(hr)) return ERROR_SUCCESS;
	return ERROR_DEVICE_NOT_CONNECTED;
}

extern "C" DWORD WINAPI XInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{

	if(Gamepad[dwUserIndex].native) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputSetState_t)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
		XInputSetState_t nativeXInputSetState = (XInputSetState_t) GetProcAddress( hNativeInstance, "XInputSetState");
		return nativeXInputSetState(dwUserIndex,pVibration);
	}

	if (!pVibration || dwUserIndex >= XUSER_MAX_COUNT) return ERROR_BAD_ARGUMENTS; 

	if(!bEnabled && bUseEnabled) return S_OK;
	HRESULT hr=ERROR_SUCCESS;

	//hr = XInit(dwUserIndex);
	//if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;
	if(!Gamepad[dwUserIndex].ff.useforce) return ERROR_SUCCESS;

	WORD wLeftMotorSpeed = 0;
	WORD wRightMotorSpeed = 0;

	PrepareForce(dwUserIndex,LeftMotor);
	PrepareForce(dwUserIndex,RightMotor);

	wLeftMotorSpeed =  (WORD)((DOUBLE)pVibration->wLeftMotorSpeed * Gamepad[dwUserIndex].ff.forcepercent);
	wRightMotorSpeed = (WORD)((DOUBLE)pVibration->wRightMotorSpeed * Gamepad[dwUserIndex].ff.forcepercent);

	hr = SetDeviceForces(dwUserIndex,wLeftMotorSpeed,LeftMotor);
	if(FAILED(hr))
		WriteLog(L"[XINPUT] SetDeviceForces for pad %d failed with code HR = %s", dwUserIndex, DXErrStr(hr));
	hr = SetDeviceForces(dwUserIndex,wRightMotorSpeed,RightMotor);
	if(FAILED(hr))
		WriteLog(L"[XINPUT] SetDeviceForces for pad %d failed with code HR = %s", dwUserIndex, DXErrStr(hr));

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	if(Gamepad[dwUserIndex].native) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetCapabilities_t)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
		XInputGetCapabilities_t nativeXInputGetCapabilities = (XInputGetCapabilities_t) GetProcAddress( hNativeInstance, "XInputGetCapabilities");
		return nativeXInputGetCapabilities(dwUserIndex,dwFlags,pCapabilities);
	}

	if (!pCapabilities || (dwUserIndex >= XUSER_MAX_COUNT) || (dwFlags &~1) ) return ERROR_BAD_ARGUMENTS; //thats correct

	if(!LPXCAPS[dwUserIndex]) {

		LPXCAPS[dwUserIndex] = new XINPUT_CAPABILITIES;
		ZeroMemory(LPXCAPS[dwUserIndex],sizeof(XINPUT_CAPABILITIES));
		LPXCAPS[dwUserIndex]->Type = XINPUT_DEVTYPE_GAMEPAD;
		LPXCAPS[dwUserIndex]->SubType = Gamepad[dwUserIndex].gamepadtype;
		//XCAPS[dwUserIndex].Flags = 0;
		LPXCAPS[dwUserIndex]->Vibration.wLeftMotorSpeed = LPXCAPS[dwUserIndex]->Vibration.wRightMotorSpeed = 0xFFFF;

		LPXCAPS[dwUserIndex]->Gamepad.wButtons = 0xFFFF;	
		LPXCAPS[dwUserIndex]->Gamepad.bLeftTrigger = 0xFF;
		LPXCAPS[dwUserIndex]->Gamepad.bRightTrigger = 0xFF;
		//center is more reliable because SHORT is signed
		//XCAPS[dwUserIndex].Gamepad.sThumbLX = 0;
		//XCAPS[dwUserIndex].Gamepad.sThumbLY = 0;
		//XCAPS[dwUserIndex].Gamepad.sThumbRX = 0;
		//XCAPS[dwUserIndex].Gamepad.sThumbRY = 0;
	}

	*pCapabilities = *LPXCAPS[dwUserIndex];
	WriteLog(L"[XINPUT]  XInputGetCapabilities:: SubType %i",pCapabilities->SubType);

	return ERROR_SUCCESS;
}

extern "C" VOID WINAPI XInputEnable(BOOL enable)
{
	if(wNativeMode) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef VOID (WINAPI* XInputEnable_t)(BOOL enable);
		XInputEnable_t nativeXInputEnable = (XInputEnable_t) GetProcAddress( hNativeInstance, "XInputEnable");
		return nativeXInputEnable(enable);
	}

	WriteLog(L"[XINPUT]  XInputEnable called, state %d",enable);

	bEnabled = enable;
	bUseEnabled = TRUE;

}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	if(wNativeMode) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetDSoundAudioDeviceGuids_t)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
		XInputGetDSoundAudioDeviceGuids_t nativeXInputGetDSoundAudioDeviceGuids = (XInputGetDSoundAudioDeviceGuids_t) GetProcAddress( hNativeInstance, "XInputGetDSoundAudioDeviceGuids");
		return nativeXInputGetDSoundAudioDeviceGuids(dwUserIndex,pDSoundRenderGuid,pDSoundCaptureGuid);
	}

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;
	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	if(wNativeMode) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetBatteryInformation_t)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
		XInputGetBatteryInformation_t nativeXInputGetBatteryInformation = (XInputGetBatteryInformation_t) GetProcAddress( hNativeInstance, "XInputGetBatteryInformation");
		return nativeXInputGetBatteryInformation(dwUserIndex,devType,pBatteryInformation);
	}

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	// Report a wired controller
	pBatteryInformation->BatteryType = BATTERY_TYPE_WIRED;
	return ERROR_SUCCESS;

}

extern "C" DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	if(wNativeMode) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetKeystroke_t)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);
		XInputGetKeystroke_t nativeXInputGetKeystroke = (XInputGetKeystroke_t) GetProcAddress( hNativeInstance, "XInputGetKeystroke");
		return nativeXInputGetKeystroke(dwUserIndex,dwReserved,pKeystroke);
	}

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	pKeystroke->Flags = NULL;
	pKeystroke->HidCode = NULL;
	pKeystroke->Unicode = NULL;
	pKeystroke->UserIndex = NULL;
	dwReserved=NULL;

	return ERROR_SUCCESS;
}
