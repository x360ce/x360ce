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
#include "Utilities\Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"

XINPUT_ENABLE XInputIsEnabled;

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
		g_hWnd = CreateWindow( 
			L"x360ceWClass",  // name of window class
			L"x360ce",        // title-bar string 
			WS_OVERLAPPEDWINDOW, // top-level window 
			CW_USEDEFAULT,       // default horizontal position 
			CW_USEDEFAULT,       // default vertical position 
			CW_USEDEFAULT,       // default width 
			CW_USEDEFAULT,       // default height 
			NULL,         // no owner window 
			NULL,        // use class menu 
			hInst,     // handle to application instance 
			NULL);      // no window-creation data 
		return TRUE;
	}
	else {
		WriteLog(LOG_CORE,L"RegisterWindowClass Failed");
		return FALSE;
	}
}

HRESULT XInit(DWORD dwUserIndex)
{
	if(g_Gamepad[dwUserIndex].configured && !g_Gamepad[dwUserIndex].initialized) 
	{
		HRESULT hr=ERROR_SUCCESS;

		if(!g_hWnd) {	
			if(!Createx360ceWindow(g_hX360ceInstance)) {
				WriteLog(LOG_CORE,L"x360ce window not created, ForceFeedback will be disabled !");
			}
		}

		if(!g_Gamepad[dwUserIndex].pGamepad){ 

			if(InputHook_Enable()) {
				InputHook_Enable(FALSE);
				WriteLog(LOG_CORE,L"Temporary disable InputHook");
			}

			WriteLog(LOG_CORE,L"[PAD%d] Initializing UserIndex %d",dwUserIndex+1,dwUserIndex);

			hr = Enumerate(dwUserIndex); 
			if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

			hr = InitDirectInput(g_hWnd,dwUserIndex);
			if(FAILED(hr)) {
				WriteLog(LOG_CORE,L"[PAD%d] XInit fail with %s",dwUserIndex+1,DXErrStr(hr));
			}
		}
		else return ERROR_DEVICE_NOT_CONNECTED;

		if(!g_Gamepad[dwUserIndex].pGamepad) {
			WriteLog(LOG_CORE,L"XInit fail");
			return ERROR_DEVICE_NOT_CONNECTED;
		}
		else 
		{
			if(bInitBeep) MessageBeep(MB_OK);
			g_Gamepad[dwUserIndex].initialized = TRUE;
			WriteLog(LOG_CORE,L"[PAD%d] Device Initialized",dwUserIndex+1);
		}

		if(InputHook_Enable()) {
			InputHook_Enable(InputHook_LastState());
			WriteLog(LOG_CORE,L"Restore InputHook state");
		}
		return ERROR_SUCCESS;
	}
	return ERROR_DEVICE_NOT_CONNECTED; //should be never called
}

extern "C" DWORD WINAPI XInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
	//WriteLog(_T("XInputGetState"));
	if(g_Gamepad[dwUserIndex].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetState_t)(DWORD dwUserIndex, XINPUT_STATE* pState);
		XInputGetState_t nativeXInputGetState = (XInputGetState_t) GetProcAddress( g_hNativeInstance, "XInputGetState");
		return nativeXInputGetState(dwUserIndex,pState);
	}

	if (!pState || dwUserIndex >= XUSER_MAX_COUNT) return ERROR_BAD_ARGUMENTS; 

	HRESULT hr=ERROR_DEVICE_NOT_CONNECTED;

	hr = XInit(dwUserIndex);
	if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

	if(!g_Gamepad[dwUserIndex].pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	//Update device state if enabled or we not use enable
	if(XInputIsEnabled.bEnabled || !XInputIsEnabled.bUseEnabled)  
		hr = UpdateState(dwUserIndex);
	if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

#if defined(DEBUG) | defined(_DEBUG)
	WriteLog(LOG_XINPUT,L"UpdateState %d %d",dwUserIndex,hr);
#endif

	GamepadMap PadMap = GamepadMapping[dwUserIndex];

	XINPUT_STATE &xState = *pState; 

	xState.Gamepad.wButtons = 0;
	xState.Gamepad.bLeftTrigger = 0;
	xState.Gamepad.bRightTrigger = 0;
	xState.Gamepad.sThumbLX = 0;
	xState.Gamepad.sThumbLY = 0;
	xState.Gamepad.sThumbRX = 0;
	xState.Gamepad.sThumbRY = 0;

	if(!XInputIsEnabled.bEnabled && XInputIsEnabled.bUseEnabled) return ERROR_SUCCESS;

	// timestamp packet
	xState.dwPacketNumber=GetTickCount();

	// --- Map buttons ---
	for (INT i = 0; i < 10; ++i) {
		if ((PadMap.Button[i] >= 0) && ButtonPressed(PadMap.Button[i],dwUserIndex)) 
			xState.Gamepad.wButtons |= buttonIDs[i];
	}

	// --- Map POV to the D-pad ---
	if ((PadMap.DpadPOV >= 0) && !PadMap.PovIsButton) {

		//INT pov = POVState(PadMap.DpadPOV,dwUserIndex,Gamepad[dwUserIndex].povrotation);

		DWORD pov = g_Gamepad[dwUserIndex].state.rgdwPOV[PadMap.DpadPOV];
		DWORD povdeg = pov/100;

		// Up-left, up, up-right, up (at 360 degrees)
		if (IN_RANGE(povdeg,270,360) || IN_RANGE(povdeg,0,90) || povdeg == 0 ) { 
			xState.Gamepad.wButtons |= PadMap.pov[0];
		}
		// Up-right, right, down-right
		if (IN_RANGE(povdeg,0,180)) { 
			xState.Gamepad.wButtons |= PadMap.pov[3];	
		}
		// Down-right, down, down-left
		if (IN_RANGE(povdeg,90,270)) { 
			xState.Gamepad.wButtons |= PadMap.pov[1];
		}
		// Down-left, left, up-left	
		if (IN_RANGE(povdeg,180,360)) { 
			xState.Gamepad.wButtons |= PadMap.pov[2];
		}

	}
	else if((PadMap.DpadPOV < 0) && PadMap.PovIsButton) {
		for (INT i = 0; i < 4; ++i) {
			if ((PadMap.pov[i] >= 0) && ButtonPressed(PadMap.pov[i],dwUserIndex)) {
				xState.Gamepad.wButtons |= povIDs[i];
			}
		}
	}

	// Created so we can refer to each axis with an ID
	LONG axis[7] = {
		g_Gamepad[dwUserIndex].state.lX,
		g_Gamepad[dwUserIndex].state.lY,
		g_Gamepad[dwUserIndex].state.lZ,
		g_Gamepad[dwUserIndex].state.lRx,
		g_Gamepad[dwUserIndex].state.lRy,
		g_Gamepad[dwUserIndex].state.lRz,
		0
	};
	LONG slider[2] = {
		g_Gamepad[dwUserIndex].state.rglSlider[0],
		g_Gamepad[dwUserIndex].state.rglSlider[1]
	};

	// --- Map triggers ---
	BYTE *targetTrigger[2] = {
		&xState.Gamepad.bLeftTrigger,
		&xState.Gamepad.bRightTrigger
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
			*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, g_Gamepad[dwUserIndex].tdeadzone, 255);

		}
	}

	// --- Map thumbsticks ---

	// Created so we can refer to each axis with an ID
	SHORT *targetAxis[4] = {
		&xState.Gamepad.sThumbLX,
		&xState.Gamepad.sThumbLY,
		&xState.Gamepad.sThumbRX,
		&xState.Gamepad.sThumbRY
	};

	// NOTE: Could add symbolic constants as indexers, such as 
	// THUMB_LX_AXIS, THUMB_LX_POSITIVE, THUMB_LX_NEGATIVE
	if(g_Gamepad[dwUserIndex].axistodpad==0)
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
	if(g_Gamepad[dwUserIndex].axistodpad==1) {
		//WriteLog("x: %d, y: %d, z: %d",Gamepad[dwUserIndex].state.lX,Gamepad[dwUserIndex].state.lY,Gamepad[dwUserIndex].state.lZ);

		if(g_Gamepad[dwUserIndex].state.lX - g_Gamepad[dwUserIndex].axistodpadoffset > g_Gamepad[dwUserIndex].axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_LEFT_THUMB;
		if(g_Gamepad[dwUserIndex].state.lX - g_Gamepad[dwUserIndex].axistodpadoffset < -g_Gamepad[dwUserIndex].axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_RIGHT_THUMB;

		if(g_Gamepad[dwUserIndex].state.lY - g_Gamepad[dwUserIndex].axistodpadoffset < -g_Gamepad[dwUserIndex].axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;
		if(g_Gamepad[dwUserIndex].state.lY - g_Gamepad[dwUserIndex].axistodpadoffset > g_Gamepad[dwUserIndex].axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
	}
	//WILDS END

	for (int i = 0; i < 4; ++i)
	{

		if (g_Gamepad[dwUserIndex].antidz[i]) {
			SHORT antidz = g_Gamepad[dwUserIndex].antidz[i];
			LONG val = *(targetAxis[i]);
			SHORT direction = val > 0 ? 1 : -1;
			val = (LONG)(abs(val) / (32767 / (32767 - antidz * 1.0)) + antidz);
			val = min(val, 32767);
			if(val == g_Gamepad[dwUserIndex].antidz[i] || val == -g_Gamepad[dwUserIndex].antidz[i]) val = 0;
			*(targetAxis[i]) = (SHORT) (direction * val);
		}

		if (g_Gamepad[dwUserIndex].adeadzone[i]) {
			SHORT dz = g_Gamepad[dwUserIndex].adeadzone[i];
			LONG val = *(targetAxis[i]);
			if((val <= dz) && (val >= -dz) ) val = 0;
			*(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
		}

	// --- Do Linears ---

		if (g_Gamepad[dwUserIndex].axislinear[i]) {

			SHORT absval = (SHORT)((abs(*(targetAxis[i])) + (((32767.0 / 2.0) - (((abs((abs(*(targetAxis[i]))) - (32767.0 / 2.0)))))) * (g_Gamepad[dwUserIndex].axislinear[i] * 0.01))));
			*(targetAxis[i]) = *(targetAxis[i]) > 0 ? absval : -absval;
		}
	}

	if(SUCCEEDED(hr)) return ERROR_SUCCESS;
	return ERROR_DEVICE_NOT_CONNECTED;
}

extern "C" DWORD WINAPI XInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
	if(g_Gamepad[dwUserIndex].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputSetState_t)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
		XInputSetState_t nativeXInputSetState = (XInputSetState_t) GetProcAddress( g_hNativeInstance, "XInputSetState");
		return nativeXInputSetState(dwUserIndex,pVibration);
	}

	if (!pVibration || dwUserIndex >= XUSER_MAX_COUNT) return ERROR_BAD_ARGUMENTS; 

	HRESULT hr=ERROR_SUCCESS;

	XINPUT_VIBRATION &xVib = *pVibration;

	if(!g_Gamepad[dwUserIndex].pGamepad) return ERROR_DEVICE_NOT_CONNECTED;
	if(!g_Gamepad[dwUserIndex].ff.useforce) return ERROR_SUCCESS;

	WORD wLeftMotorSpeed = 0;
	WORD wRightMotorSpeed = 0;

	PrepareForce(dwUserIndex,LeftMotor);
	PrepareForce(dwUserIndex,RightMotor);

	if(!XInputIsEnabled.bEnabled && XInputIsEnabled.bUseEnabled)
	{ 
		SetDeviceForces(dwUserIndex,0,LeftMotor);
		SetDeviceForces(dwUserIndex,0,RightMotor);
		return ERROR_SUCCESS;
	}

	wLeftMotorSpeed =  static_cast<WORD>(xVib.wLeftMotorSpeed * g_Gamepad[dwUserIndex].ff.forcepercent);
	wRightMotorSpeed = static_cast<WORD>(xVib.wRightMotorSpeed * g_Gamepad[dwUserIndex].ff.forcepercent);

	hr = SetDeviceForces(dwUserIndex,wLeftMotorSpeed,LeftMotor);
	if(FAILED(hr))
		WriteLog(LOG_XINPUT,L"SetDeviceForces for pad %d failed with code HR = %s", dwUserIndex, DXErrStr(hr));
	hr = SetDeviceForces(dwUserIndex,wRightMotorSpeed,RightMotor);
	if(FAILED(hr))
		WriteLog(LOG_XINPUT,L"SetDeviceForces for pad %d failed with code HR = %s", dwUserIndex, DXErrStr(hr));
	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	if(g_Gamepad[dwUserIndex].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetCapabilities_t)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
		XInputGetCapabilities_t nativeXInputGetCapabilities = (XInputGetCapabilities_t) GetProcAddress( g_hNativeInstance, "XInputGetCapabilities");
		return nativeXInputGetCapabilities(dwUserIndex,dwFlags,pCapabilities);
	}

	if (!pCapabilities || (dwUserIndex >= XUSER_MAX_COUNT) || (dwFlags &~1) ) return ERROR_BAD_ARGUMENTS;

	XINPUT_CAPABILITIES &xCaps = *pCapabilities;
	xCaps.Type = XINPUT_DEVTYPE_GAMEPAD;
	xCaps.SubType = g_Gamepad[dwUserIndex].gamepadtype;
	xCaps.Flags = 0;
	xCaps.Vibration.wLeftMotorSpeed = xCaps.Vibration.wRightMotorSpeed = 0xFFFF;
	xCaps.Gamepad.bLeftTrigger = xCaps.Gamepad.bRightTrigger = 0xFF;
	//TODO -  max or center is correct? need dump from XInput device
	xCaps.Gamepad.sThumbLX = 0;
	xCaps.Gamepad.sThumbLY = 0;
	xCaps.Gamepad.sThumbRX = 0;
	xCaps.Gamepad.sThumbRY = 0;

	//WriteLog(LOG_XINPUT,L"XInputGetCapabilities:: SubType %i",pCapabilities->SubType);

	return ERROR_SUCCESS;
}

extern "C" VOID WINAPI XInputEnable(BOOL enable)
{
	if(g_Gamepad[0].native || g_Gamepad[1].native || g_Gamepad[2].native || g_Gamepad[3].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef VOID (WINAPI* XInputEnable_t)(BOOL enable);
		XInputEnable_t nativeXInputEnable = (XInputEnable_t) GetProcAddress( g_hNativeInstance, "XInputEnable");
		nativeXInputEnable(enable);
	}

	/*
	Nasty trick to support XInputEnable states, because not every game calls it so:
	- must support games that use it, and do enable/disable as needed by game
	if bEnabled is FALSE and bUseEnabled is TRUE = gamepad is disabled -> return Hook S_OK, ie. connected but state not updating
	if bEnabled is TRUE and bUseEnabled is TRUE = gamepad is enabled -> continue, ie. connected and updating state
	- must support games that not call it
	if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states 
	*/

	XInputIsEnabled.bEnabled = enable;
	XInputIsEnabled.bUseEnabled = TRUE;

	if(enable) WriteLog(LOG_XINPUT,L"XInput Enabled");
	else WriteLog(LOG_XINPUT,L"XInput Disabled");

}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	if(g_Gamepad[dwUserIndex].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetDSoundAudioDeviceGuids_t)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
		XInputGetDSoundAudioDeviceGuids_t nativeXInputGetDSoundAudioDeviceGuids = (XInputGetDSoundAudioDeviceGuids_t) GetProcAddress( g_hNativeInstance, "XInputGetDSoundAudioDeviceGuids");
		return nativeXInputGetDSoundAudioDeviceGuids(dwUserIndex,pDSoundRenderGuid,pDSoundCaptureGuid);
	}

	*pDSoundRenderGuid = GUID_NULL;
	*pDSoundCaptureGuid = GUID_NULL;

	if(!g_Gamepad[dwUserIndex].pGamepad) return ERROR_DEVICE_NOT_CONNECTED;
	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	if(g_Gamepad[dwUserIndex].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetBatteryInformation_t)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
		XInputGetBatteryInformation_t nativeXInputGetBatteryInformation = (XInputGetBatteryInformation_t) GetProcAddress( g_hNativeInstance, "XInputGetBatteryInformation");
		return nativeXInputGetBatteryInformation(dwUserIndex,devType,pBatteryInformation);
	}

	if(!g_Gamepad[dwUserIndex].pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	// Report a wired controller

	XINPUT_BATTERY_INFORMATION &xBatInfo = *pBatteryInformation;
	xBatInfo.BatteryLevel = BATTERY_LEVEL_FULL;
	xBatInfo.BatteryType = BATTERY_TYPE_WIRED;

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{
	if(g_Gamepad[dwUserIndex].native) {
		if(!g_hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetKeystroke_t)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);
		XInputGetKeystroke_t nativeXInputGetKeystroke = (XInputGetKeystroke_t) GetProcAddress( g_hNativeInstance, "XInputGetKeystroke");
		return nativeXInputGetKeystroke(dwUserIndex,dwReserved,pKeystroke);
	}

	if(!g_Gamepad[dwUserIndex].pGamepad) return ERROR_DEVICE_NOT_CONNECTED;


	UNREFERENCED_PARAMETER(pKeystroke);

	return ERROR_EMPTY;
}
