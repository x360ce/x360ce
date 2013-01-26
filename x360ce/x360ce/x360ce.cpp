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
#include "x360ce.h"
#include "Utilities\Log.h"
#include "Utilities\Misc.h"
#include "Config.h"
#include "DirectInput.h"
#include "InputHook\InputHook.h"

XINPUT_ENABLE XInputIsEnabled;

WNDPROC oldWndProc = NULL;
HWND g_hWnd = NULL;

extern CRITICAL_SECTION cs;
extern DWORD startThread;
extern BOOL cleanDeinit;

extern iHook* g_iHook;

/**********************************************************************************************/
/**********************************************************************************************/
/**********************************************************************************************/

// XINPUT FUNCTIONS TYPES
typedef DWORD (WINAPI* XInputGetState_t)(DWORD dwUserIndex, XINPUT_STATE* pState);
typedef DWORD (WINAPI* XInputSetState_t)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
typedef DWORD (WINAPI* XInputGetCapabilities_t)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
typedef VOID (WINAPI* XInputEnable_t)(BOOL enable);
typedef DWORD (WINAPI* XInputGetDSoundAudioDeviceGuids_t)(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid);
typedef DWORD (WINAPI* XInputGetBatteryInformation_t)(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation);
typedef DWORD (WINAPI* XInputGetKeystroke_t)(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke);

typedef DWORD (WINAPI* XInputGetStateEx_t)(DWORD dwUserIndex, XINPUT_STATE *pState);
typedef DWORD (WINAPI* XInputWaitForGuideButton_t)(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid);
typedef DWORD (WINAPI* XInputCancelGuideButtonWait_t)(DWORD dwUserIndex);
typedef DWORD (WINAPI* XInputPowerOffController_t)(DWORD dwUserIndex);

// XINPUT FUNCTIONS POINTERS
XInputGetState_t nXInputGetState = NULL;
XInputSetState_t nXInputSetState = NULL;
XInputGetCapabilities_t nXInputGetCapabilities = NULL;
XInputEnable_t nXInputEnable = NULL;
XInputGetDSoundAudioDeviceGuids_t nXInputGetDSoundAudioDeviceGuids = NULL;
XInputGetBatteryInformation_t nXInputGetBatteryInformation = NULL;
XInputGetKeystroke_t nXInputGetKeystroke = NULL;

XInputGetStateEx_t nXInputGetStateEx = NULL;
XInputWaitForGuideButton_t nXInputWaitForGuideButton = NULL;
XInputCancelGuideButtonWait_t nXInputCancelGuideButtonWait = NULL;
XInputPowerOffController_t nXInputPowerOffController = NULL;

/**********************************************************************************************/
/**********************************************************************************************/
/**********************************************************************************************/

enum funcType {GETSTATE, SETSTATE, GETCAPS, ENABLE, AUDIO, BATTERY, KEYSTROKE, GETSTATEEX, WAITGUIDE, CANCELGUIDE, POWEROFF};

inline VOID InitalizeFunction(funcType func)
{
	LoadXInputDLL();

	switch(func)
	{
	case GETSTATE:
		if(!nXInputGetState) nXInputGetState = (XInputGetState_t)GetProcAddress(hNative,"XInputGetState");
		break;
	case SETSTATE:
		if(!nXInputSetState) nXInputSetState = (XInputSetState_t)GetProcAddress(hNative,"XInputSetState");
		break;
	case GETCAPS:
		if(!nXInputGetCapabilities) nXInputGetCapabilities = (XInputGetCapabilities_t)GetProcAddress(hNative,"XInputGetCapabilities");
		break;
	case ENABLE:
		if(!nXInputEnable) nXInputEnable = (XInputEnable_t)GetProcAddress(hNative,"XInputEnable");
		break;
	case AUDIO:
		if(!nXInputGetDSoundAudioDeviceGuids) nXInputGetDSoundAudioDeviceGuids = (XInputGetDSoundAudioDeviceGuids_t)GetProcAddress(hNative,"XInputGetDSoundAudioDeviceGuids");
		break;
	case BATTERY:
		if(!nXInputGetBatteryInformation) nXInputGetBatteryInformation = (XInputGetBatteryInformation_t)GetProcAddress(hNative,"XInputGetBatteryInformation");
		break;
	case KEYSTROKE:
		if(!nXInputGetKeystroke) nXInputGetKeystroke = (XInputGetKeystroke_t)GetProcAddress(hNative,"XInputGetKeystroke");
		break;

	case GETSTATEEX:
		if(!nXInputGetStateEx) nXInputGetStateEx = (XInputGetStateEx_t)GetProcAddress(hNative,(LPCSTR)100);
		break;
	case WAITGUIDE:
		if(!nXInputWaitForGuideButton) nXInputWaitForGuideButton = (XInputWaitForGuideButton_t)GetProcAddress(hNative,(LPCSTR)101);
		break;
	case CANCELGUIDE:
		if(!nXInputCancelGuideButtonWait) nXInputCancelGuideButtonWait = (XInputCancelGuideButtonWait_t)GetProcAddress(hNative,(LPCSTR)102);
		break;
	case POWEROFF:
		if(!nXInputPowerOffController) nXInputPowerOffController = (XInputPowerOffController_t)GetProcAddress(hNative,(LPCSTR)103);
		break;
	}
}

LRESULT CALLBACK WndProc(
	__in  HWND hWnd,
	__in  UINT uMsg,
	__in  WPARAM wParam,
	__in  LPARAM lParam
	)

{	
	switch ( uMsg )
	{
	case MYQUITMSG:
		EnterCriticalSection(&cs);
		if(startThread == GetCurrentThreadId())
		{
			if(hNative)
			{
				WriteLog(LOG_CORE,L"Unloading %s",GetFilePath(hNative).c_str());
				FreeLibrary(hNative);
				hNative = NULL;
			}

			SetWindowLongPtr(g_hWnd, GWLP_WNDPROC, (LONG_PTR) oldWndProc);
			WriteLog(LOG_CORE,L"Destroying message window");
			DestroyWindow(g_hWnd);
			g_hWnd = NULL;
			cleanDeinit = TRUE;
		}
		LeaveCriticalSection(&cs);
		break;
	}
	return CallWindowProc(oldWndProc, hWnd, uMsg, wParam, lParam);
}

VOID MakeWindow()
{
	g_hWnd = CreateWindow(
		L"Message",	// name of window class
		L"x360ce",			// title-bar std::string
		WS_TILED,			// normal window
		CW_USEDEFAULT,		// default horizontal position
		CW_USEDEFAULT,		// default vertical position
		CW_USEDEFAULT,		// default width
		CW_USEDEFAULT,		// default height
		HWND_MESSAGE,		// message-only window
		NULL,				// no class menu
		hThis,	// handle to application instance
		NULL);				// no window-creation data

	if(!g_hWnd) WriteLog(LOG_CORE,L"CreateWindow failed with code 0x%x", HRESULT_FROM_WIN32(GetLastError()));
	else oldWndProc = (WNDPROC) SetWindowLongPtr(g_hWnd,GWLP_WNDPROC,(LONG_PTR) WndProc);
}

HRESULT XInit(DINPUT_GAMEPAD &gamepad)
{
	EnterCriticalSection(&cs);
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(!gamepad.enumfail)
	{
		HRESULT hr=ERROR_SUCCESS;

		if(!g_hWnd) MakeWindow();

		if(!gamepad.pGamepad)
		{
			WriteLog(LOG_CORE,L"[PAD%d] Initializing UserIndex %d",gamepad.dwUserIndex+1,gamepad.dwUserIndex);

			hr = InitDirectInput(g_hWnd,gamepad);
			if(FAILED(hr))
			{
				WriteLog(LOG_CORE,L"[PAD%d] XInit fail with %h",gamepad.dwUserIndex+1,hr);
			}
			if(SUCCEEDED(hr))
			{
				if(bInitBeep) MessageBeep(MB_OK);

				gamepad.initialized = TRUE;
				WriteLog(LOG_CORE,L"[PAD%d] Device Initialized",gamepad.dwUserIndex+1);
			}
		}
		LeaveCriticalSection(&cs);
		return ERROR_SUCCESS;
	}
	LeaveCriticalSection(&cs);
	return ERROR_DEVICE_NOT_CONNECTED;
}

extern "C" DWORD WINAPI XInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{
	//WriteLog(LOG_XINPUT,L"XInputGetState");
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(GETSTATE);
		return nXInputGetState(dwUserIndex,pState);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];
	if (!pState || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

	HRESULT hr = XInit(gamepad);
	if(!gamepad.pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	//Update device state if enabled or we not use enable
	if(XInputIsEnabled.bEnabled || !XInputIsEnabled.bUseEnabled)
		hr = UpdateState(gamepad);

	if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

#if defined(DEBUG) | defined(_DEBUG)
	//WriteLog(LOG_XINPUT,L"UpdateState %d %d",dwUserIndex,hr);
#endif

	GamepadMap &PadMap = GamepadMapping[dwUserIndex];
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
	for (int i = 0; i < 10; ++i)
	{
		if (((int)PadMap.Button[i] >= 0) && ButtonPressed(PadMap.Button[i],gamepad))
			xState.Gamepad.wButtons |= buttonIDs[i];
	}

	// --- Map POV to the D-pad ---
	if (((int)PadMap.DpadPOV >= 0) && !PadMap.PovIsButton)
	{
		//INT pov = POVState(PadMap.DpadPOV,dwUserIndex,Gamepad[dwUserIndex].povrotation);

		int povdeg = gamepad.state.rgdwPOV[PadMap.DpadPOV];
		if(povdeg >= 0) 
		{
			// Up-left, up, up-right, up (at 360 degrees)
			if (IN_RANGE2(povdeg,PadMap.pov[GAMEPAD_DPAD_LEFT]+1,PadMap.pov[GAMEPAD_DPAD_UP]) || IN_RANGE2(povdeg,0,PadMap.pov[GAMEPAD_DPAD_RIGHT]-1))
				xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

			// Up-right, right, down-right
			if (IN_RANGE(povdeg,0,PadMap.pov[GAMEPAD_DPAD_DOWN]))
				xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

			// Down-right, down, down-left
			if (IN_RANGE(povdeg,PadMap.pov[GAMEPAD_DPAD_RIGHT],PadMap.pov[GAMEPAD_DPAD_LEFT]))
				xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;

			// Down-left, left, up-left
			if (IN_RANGE(povdeg,PadMap.pov[GAMEPAD_DPAD_DOWN],PadMap.pov[GAMEPAD_DPAD_UP]))
				xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;
		}
	}
	else if(((int)PadMap.DpadPOV < 0) && PadMap.PovIsButton)
	{
		for (int i = 0; i < 4; ++i)
		{
			if (((int)PadMap.pov[i] >= 0) && ButtonPressed(PadMap.pov[i],gamepad))
			{
				xState.Gamepad.wButtons |= povIDs[i];
			}
		}
	}

	// Created so we can refer to each axis with an ID
	LONG axis[7] =
	{
		gamepad.state.lX,
		gamepad.state.lY,
		gamepad.state.lZ,
		gamepad.state.lRx,
		gamepad.state.lRy,
		gamepad.state.lRz,
		0
	};
	LONG slider[2] =
	{
		gamepad.state.rglSlider[0],
		gamepad.state.rglSlider[1]
	};

	// --- Map triggers ---
	BYTE *targetTrigger[2] =
	{
		&xState.Gamepad.bLeftTrigger,
		&xState.Gamepad.bRightTrigger
	};

	for (INT i = 0; i < 2; ++i)
	{

		MappingType triggerType = PadMap.Trigger[i].type;

		if (triggerType == DIGITAL)
		{
			if (ButtonPressed(PadMap.Trigger[i].id,gamepad))
			{
				*(targetTrigger[i]) = 255;
			}
		}
		else
		{
			LONG *values;

			switch (triggerType)
			{
			case AXIS:
			case HAXIS:
			case CBUT: // add /////////////////////////////////////////////////////////
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

			if(PadMap.Trigger[i].id > 0)
			{
				v = values[PadMap.Trigger[i].id -1];
			}
			else
			{
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

			switch (triggerType)
			{
				// Full range
			case AXIS:
			case SLIDER:
				scaling = 255;
				offset = 32767;
				break;

				// Half range
			case HAXIS:
			case HSLIDER:
			case CBUT: // add /////////////////////////////////////////////////////////
				scaling = 127;
				offset = 0;
				break;

			default:
				scaling = 1;
				offset = 0;
				break;
			}


			//v2 = (v + offset) / scaling;
			// Add deadzones
			//*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, gamepad.tdeadzone, 255);


			/////////////////////////////////////////////////////////////////////////////////////////
			if (triggerType == CBUT)
			{

				if (ButtonPressed(PadMap.Trigger[0].but,gamepad)
					&& ButtonPressed(PadMap.Trigger[1].but,gamepad))
				{
					*(targetTrigger[0]) = 255;
					*(targetTrigger[1]) = 255;
				}

				if (ButtonPressed(PadMap.Trigger[0].but,gamepad)
					&& !ButtonPressed(PadMap.Trigger[1].but,gamepad))
				{
					v2 = (offset-v) / scaling;
					*(targetTrigger[0]) = 255;
					*(targetTrigger[1]) = 255 - (BYTE) deadzone(v2, 0, 255, gamepad.tdeadzone, 255);
				}

				if (!ButtonPressed(PadMap.Trigger[0].but,gamepad)
					&& ButtonPressed(PadMap.Trigger[1].but,gamepad))
				{
					v2 = (offset+v) / scaling;
					*(targetTrigger[0]) = 255 - (BYTE) deadzone(v2, 0, 255, gamepad.tdeadzone, 255);
					*(targetTrigger[1]) = 255;
				}

				if (!ButtonPressed(PadMap.Trigger[0].but,gamepad)
					&& !ButtonPressed(PadMap.Trigger[1].but,gamepad))
				{
					v2 = (offset+v) / scaling;
					*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, gamepad.tdeadzone, 255);
				}

			}
			else
			{
				v2 = (offset+v) / scaling;
				*(targetTrigger[i]) = (BYTE) deadzone(v2, 0, 255, gamepad.tdeadzone, 255);
			}

			/////////////////////////////////////////////////////////////////////////////////////////



		}
	}

	// --- Map thumbsticks ---

	// Created so we can refer to each axis with an ID
	SHORT *targetAxis[4] =
	{
		&xState.Gamepad.sThumbLX,
		&xState.Gamepad.sThumbLY,
		&xState.Gamepad.sThumbRX,
		&xState.Gamepad.sThumbRY
	};

	// NOTE: Could add symbolic constants as indexers, such as
	// THUMB_LX_AXIS, THUMB_LX_POSITIVE, THUMB_LX_NEGATIVE
	if(gamepad.axistodpad==0)
	{


		for (INT i = 0; i < 4; ++i)
		{
			LONG *values = axis;

			// Analog input
			if (PadMap.Axis[i].analogType == AXIS) values = axis;

			if (PadMap.Axis[i].analogType == SLIDER) values = slider;

			if (PadMap.Axis[i].analogType != NONE)
			{

				if(PadMap.Axis[i].id > 0 )
				{
					SHORT val = (SHORT) values[PadMap.Axis[i].id - 1];
					*(targetAxis[i])= (SHORT) clamp(val,-32767,32767);
				}
				else if(PadMap.Axis[i].id < 0 )
				{
					SHORT val = (SHORT) -values[-PadMap.Axis[i].id - 1];
					*(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
				}
			}

			// Digital input, positive direction
			if (PadMap.Axis[i].hasDigital && PadMap.Axis[i].positiveButtonID >= 0)
			{

				if (ButtonPressed(PadMap.Axis[i].positiveButtonID,gamepad))
					*(targetAxis[i]) = 32767;
			}

			// Digital input, negative direction
			if (PadMap.Axis[i].hasDigital && PadMap.Axis[i].negativeButtonID >= 0)
			{

				if (ButtonPressed(PadMap.Axis[i].negativeButtonID,gamepad))
					*(targetAxis[i]) = -32767;
			}
		}
	}

	//WILDS - Axis to D-Pad
	if(gamepad.axistodpad==1)
	{
		//WriteLog("x: %d, y: %d, z: %d",Gamepad[dwUserIndex].state.lX,Gamepad[dwUserIndex].state.lY,Gamepad[dwUserIndex].state.lZ);

		if(gamepad.state.lX - gamepad.axistodpadoffset > gamepad.axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_LEFT;

		if(gamepad.state.lX - gamepad.axistodpadoffset < -gamepad.axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_RIGHT;

		if(gamepad.state.lY - gamepad.axistodpadoffset < -gamepad.axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_UP;

		if(gamepad.state.lY - gamepad.axistodpadoffset > gamepad.axistodpaddeadzone)
			xState.Gamepad.wButtons |= XINPUT_GAMEPAD_DPAD_DOWN;
	}

	//WILDS END

	for (int i = 0; i < 4; ++i)
	{

		if (gamepad.antidz[i])
		{
			SHORT antidz = gamepad.antidz[i];
			LONG val = *(targetAxis[i]);
			SHORT direction = val > 0 ? 1 : -1;
			val = (LONG)(abs(val) / (32767 / (32767 - antidz * 1.0)) + antidz);
			val = min(val, 32767);

			if(val == gamepad.antidz[i] || val == -gamepad.antidz[i]) val = 0;

			*(targetAxis[i]) = (SHORT) (direction * val);
		}

		if (gamepad.adeadzone[i])
		{
			SHORT dz = gamepad.adeadzone[i];
			LONG val = *(targetAxis[i]);

			if((val <= dz) && (val >= -dz) ) val = 0;

			*(targetAxis[i]) = (SHORT) clamp(val,-32767,32767);
		}

		// --- Do Linears ---

		if (gamepad.axislinear[i])
		{

			SHORT absval = (SHORT)((abs(*(targetAxis[i])) + (((32767.0 / 2.0) - (((abs((abs(*(targetAxis[i]))) - (32767.0 / 2.0)))))) * (gamepad.axislinear[i] * 0.01))));
			*(targetAxis[i]) = *(targetAxis[i]) > 0 ? absval : -absval;
		}
	}

	if(SUCCEEDED(hr)) return ERROR_SUCCESS;

	return ERROR_DEVICE_NOT_CONNECTED;
}

extern "C" DWORD WINAPI XInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(SETSTATE);
		return nXInputSetState(dwUserIndex,pVibration);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];
	if (!pVibration || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

	HRESULT hr=ERROR_SUCCESS;

	XINPUT_VIBRATION &xVib = *pVibration;

	//WriteLog(LOG_XINPUT,L"%u",xVib.wLeftMotorSpeed);
	//WriteLog(LOG_XINPUT,L"%u",xVib.wRightMotorSpeed);

	if(!gamepad.pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	if(!gamepad.ff.useforce) return ERROR_SUCCESS;

	WORD wLeftMotorSpeed = 0;
	WORD wRightMotorSpeed = 0;

	PrepareForce(gamepad,FFB_LEFTMOTOR);
	PrepareForce(gamepad,FFB_RIGHTMOTOR);

	if(!XInputIsEnabled.bEnabled && XInputIsEnabled.bUseEnabled)
	{
		SetDeviceForces(gamepad,0,FFB_LEFTMOTOR);
		SetDeviceForces(gamepad,0,FFB_RIGHTMOTOR);
		return ERROR_SUCCESS;
	}

	WORD left =  static_cast<WORD>(xVib.wLeftMotorSpeed * gamepad.ff.forcepercent);
	WORD right = static_cast<WORD>(xVib.wRightMotorSpeed * gamepad.ff.forcepercent);

	wLeftMotorSpeed = gamepad.swapmotor ? right : left;
	wRightMotorSpeed = gamepad.swapmotor ? left : right;

	hr = SetDeviceForces(gamepad,wLeftMotorSpeed,FFB_LEFTMOTOR);

	if(FAILED(hr))
		WriteLog(LOG_XINPUT,L"SetDeviceForces for pad %d failed with code HR = %X", dwUserIndex, hr);

	hr = SetDeviceForces(gamepad,wRightMotorSpeed,FFB_RIGHTMOTOR);

	if(FAILED(hr))
		WriteLog(LOG_XINPUT,L"SetDeviceForces for pad %d failed with code HR = %X", dwUserIndex, hr);

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(GETCAPS);
		return nXInputGetCapabilities(dwUserIndex,dwFlags,pCapabilities);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	if (!pCapabilities || !(dwUserIndex < XUSER_MAX_COUNT) || (dwFlags > XINPUT_FLAG_GAMEPAD) ) return ERROR_BAD_ARGUMENTS;

	XINPUT_CAPABILITIES &xCaps = *pCapabilities;
	xCaps.Type = 0;
	xCaps.SubType = gamepad.gamepadtype; //customizable subtype
	xCaps.Flags = 0; // we do not support sound
	xCaps.Vibration.wLeftMotorSpeed = xCaps.Vibration.wRightMotorSpeed = 0xFF;
	xCaps.Gamepad.bLeftTrigger = xCaps.Gamepad.bRightTrigger = 0xFF;

	xCaps.Gamepad.sThumbLX = (SHORT) -64;
	xCaps.Gamepad.sThumbLY = (SHORT) -64;
	xCaps.Gamepad.sThumbRX = (SHORT) -64;
	xCaps.Gamepad.sThumbRY = (SHORT) -64;
	xCaps.Gamepad.wButtons = (WORD) 0xF3FF;

	return ERROR_SUCCESS;
}

extern "C" VOID WINAPI XInputEnable(BOOL enable)
{
	if(g_Disable) return;
	if(wNativeMode)
	{
		InitalizeFunction(ENABLE);
		nXInputEnable(enable);
	}

	/*
	Nasty trick to support XInputEnable states, because not every game calls it so:
	- must support games that use it, and do enable/disable as needed by game
	if bEnabled is FALSE and bUseEnabled is TRUE = gamepad is disabled -> return Hook S_OK, ie. connected but state not updating
	if bEnabled is TRUE and bUseEnabled is TRUE = gamepad is enabled -> continue, ie. connected and updating state
	- must support games that not call it
	if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states
	*/

	XInputIsEnabled.bEnabled = (enable != 0);
	XInputIsEnabled.bUseEnabled = true;

	if(enable) WriteLog(LOG_XINPUT,L"XInput Enabled");
	else WriteLog(LOG_XINPUT,L"XInput Disabled");

}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids(DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(AUDIO);
		return nXInputGetDSoundAudioDeviceGuids(dwUserIndex,pDSoundRenderGuid,pDSoundCaptureGuid);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	if(!pDSoundRenderGuid || !pDSoundCaptureGuid || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

	if(!gamepad.pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	*pDSoundRenderGuid = GUID_NULL;
	*pDSoundCaptureGuid = GUID_NULL;

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(BATTERY);
		return nXInputGetBatteryInformation(dwUserIndex,devType,pBatteryInformation);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	if (!pBatteryInformation || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

	if(!gamepad.pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	// Report a wired controller
	XINPUT_BATTERY_INFORMATION &xBatInfo = *pBatteryInformation;
	xBatInfo.BatteryLevel = BATTERY_LEVEL_FULL;
	xBatInfo.BatteryType = BATTERY_TYPE_WIRED;

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, XINPUT_KEYSTROKE* pKeystroke)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		//WriteLog(LOG_XINPUT,L"flags: %u, hidcode: %u, unicode: %c, user: %u, vk: 0x%X",pKeystroke->Flags,pKeystroke->HidCode,pKeystroke->Unicode,pKeystroke->UserIndex,pKeystroke->VirtualKey);

		InitalizeFunction(KEYSTROKE);
		return nXInputGetKeystroke(dwUserIndex,dwReserved,pKeystroke);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	if (!pKeystroke || !(dwUserIndex < XUSER_MAX_COUNT)) return ERROR_BAD_ARGUMENTS;

	if(!gamepad.pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	XINPUT_KEYSTROKE &xkey = *pKeystroke;

	XINPUT_STATE xState;
	ZeroMemory(&xState,sizeof(XINPUT_STATE));
	XInputGetState(dwUserIndex,&xState);

	static WORD repeat[14];
	static WORD flags[14];

	WORD vkey = NULL;
	WORD curretFlags = NULL;

	int i = 0;
	for(i = 0; i < 14; i++)
	{
		if(xState.Gamepad.wButtons & allButtonIDs[i])
		{
			if(flags[i] == NULL)
			{
				vkey = keyIDs[i];
				curretFlags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN;
				break;
			}
			if((flags[i] & XINPUT_KEYSTROKE_KEYDOWN))
			{
				if(repeat[i] <= 0)
				{
					repeat[i] = 5;
					vkey = keyIDs[i];
					curretFlags = flags[i] = XINPUT_KEYSTROKE_KEYDOWN | XINPUT_KEYSTROKE_REPEAT;
					break;
				}
				else
				{
					repeat[i]--;
					continue;
				}
			}
		}
		if(!(xState.Gamepad.wButtons & allButtonIDs[i]))
		{
			if(flags[i] & XINPUT_KEYSTROKE_KEYDOWN)
			{
				repeat[i] = 5*4;
				vkey = keyIDs[i];
				curretFlags = flags[i] = XINPUT_KEYSTROKE_KEYUP;
				break;
			}
			if(flags[i] & XINPUT_KEYSTROKE_KEYUP)
			{
				curretFlags = flags[i] = NULL;
				break;
			}
		}
	}

	DWORD ret = ERROR_EMPTY;

	if(vkey)
	{
		xkey.UserIndex = (BYTE)dwUserIndex;
		xkey.Unicode = NULL;
		xkey.HidCode = NULL;
		xkey.Flags = curretFlags;
		xkey.VirtualKey = vkey;
		ret = ERROR_SUCCESS;
	}

	//WriteLog(LOG_XINPUT,L"ret: %u, flags: %u, hid: %u, unicode: %c, user: %u, vk: 0x%X",ret,pKeystroke->Flags,pKeystroke->HidCode,pKeystroke->Unicode,pKeystroke->UserIndex,pKeystroke->VirtualKey);

	return ret;
}

//undocumented
extern "C" DWORD WINAPI XInputGetStateEx(DWORD dwUserIndex, XINPUT_STATE *pState)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(GETSTATEEX);
		return nXInputGetStateEx(dwUserIndex,pState);
	}

	DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];
	GamepadMap &PadMap = GamepadMapping[dwUserIndex];
	XINPUT_STATE &xState = *pState;

	if (PadMap.guide && ButtonPressed(PadMap.guide,gamepad))
		xState.Gamepad.wButtons |= 0x400;

	//WriteLog(LOG_XINPUT,L"XInputGetStateEx %u",xState.Gamepad.wButtons);

	return XInputGetState(dwUserIndex,pState);
}

extern "C" DWORD WINAPI XInputWaitForGuideButton(DWORD dwUserIndex, DWORD dwFlag, LPVOID pVoid)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(WAITGUIDE);
		return nXInputWaitForGuideButton(dwUserIndex,dwFlag,pVoid);
	}

	//DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputCancelGuideButtonWait(DWORD dwUserIndex)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(CANCELGUIDE);
		return nXInputCancelGuideButtonWait(dwUserIndex);
	}

	//DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputPowerOffController(DWORD dwUserIndex)
{
	if(g_Disable) return ERROR_DEVICE_NOT_CONNECTED;

	if(dwUserIndex+1 > g_Gamepads.size() || g_Gamepads[dwUserIndex].passthrough)
	{
		InitalizeFunction(POWEROFF);
		return nXInputPowerOffController(dwUserIndex);
	}

	//DINPUT_GAMEPAD &gamepad = g_Gamepads[dwUserIndex];

	return ERROR_SUCCESS;
}