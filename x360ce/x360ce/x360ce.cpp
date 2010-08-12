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
#include "Config.h"
#include "DirectInput.h"

#pragma warning(disable:4310)

BOOL bEnabled = FALSE;
BOOL bUseEnabled= FALSE;

DWORD dwlastUserIndex = (DWORD) -1;

XINPUT_CAPABILITIES XCAPS[4];
bool capsready[4] = {0,0,0,0};

HRESULT XInit(DWORD dwUserIndex)
{
	HRESULT hr=S_OK;
	if(!Gamepad[dwUserIndex].product.Data1) return ERROR_DEVICE_NOT_CONNECTED;

	if(!Gamepad[dwUserIndex].g_pGamepad && dwUserIndex != dwlastUserIndex){ 

		WriteLog(_T("[XINIT]   Initializing Gamepad %d"),dwUserIndex+1);
		WriteLog(_T("[XINIT]   User ID: %d, Last User ID: %d"),dwUserIndex,dwlastUserIndex);

		hr = Enumerate(dwUserIndex); 
		if(SUCCEEDED(hr)) {
			WriteLog(_T("[XINIT]   [PAD%d] Enumeration finished"),dwUserIndex+1);
		}
		if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

		hr = InitDirectInput(hWnd,dwUserIndex);
		if(FAILED(hr)) {
			WriteLog(_T("[XINIT]   XInit fail with %s"),DXErrStr(hr));
		}
	}
	else return ERROR_DEVICE_NOT_CONNECTED;

	if(!Gamepad[dwUserIndex].g_pGamepad) {
		WriteLog(_T("[XINIT]   XInit fail"));
		return ERROR_DEVICE_NOT_CONNECTED;
	}
	else UpdateState(dwUserIndex);

	dwlastUserIndex = dwUserIndex;
	return hr;
}

extern "C" DWORD WINAPI XInputGetState(DWORD dwUserIndex, XINPUT_STATE* pState)
{

	//WriteLog(_T("XInputGetState"));
	if(Gamepad[dwUserIndex].native) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetState_Type)(DWORD dwUserIndex, XINPUT_STATE* pState);
		XInputGetState_Type nativeXInputGetState = (XInputGetState_Type) GetProcAddress( hNativeInstance, "XInputGetState");
		return nativeXInputGetState(dwUserIndex,pState);
	}

	if (!pState || dwUserIndex > XUSER_MAX_COUNT-1) return ERROR_BAD_ARGUMENTS; 

	HRESULT hr=ERROR_DEVICE_NOT_CONNECTED;

	hr = XInit(dwUserIndex);
	if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

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

#ifdef DEBUG
	WriteLog(_T("UpdateState %d %d"),dwUserIndex,hr);
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
		if ((PadMap.Button[i] >= 0)
			&&	ButtonPressed(PadMap.Button[i],dwUserIndex)
			) {
				xGamepad.wButtons |= buttonIDs[i];
		}
	}

	// --- Map POV to the D-pad ---
	if (PadMap.DpadPOV >= 0 ) {

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
	else {
		for (INT i = 0; i < 4; ++i) {
			if ((PadMap.pov[i] >= 0)&&	ButtonPressed(PadMap.pov[i],dwUserIndex)) {
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

	// --- Do Linears ---
	for (int i = 0; i < 4; ++i) {
		SHORT absval = (SHORT)((abs(*(targetAxis[i])) + (((32767.0 / 2.0) - (((abs((abs(*(targetAxis[i]))) - (32767.0 / 2.0)))))) * (Gamepad[dwUserIndex].axislinear[i] * 0.01))));
		*(targetAxis[i]) = *(targetAxis[i]) > 0 ? absval : -absval;
	}

	return hr;
}

extern "C" DWORD WINAPI XInputSetState(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration)
{

	if(Gamepad[dwUserIndex].native) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputSetState_Type)(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
		XInputSetState_Type nativeXInputSetState = (XInputSetState_Type) GetProcAddress( hNativeInstance, "XInputSetState");
		return nativeXInputSetState(dwUserIndex,pVibration);
	}

	if (!pVibration || dwUserIndex > XUSER_MAX_COUNT-1) return ERROR_BAD_ARGUMENTS; 

	if(!bEnabled && bUseEnabled) return S_OK;

	HRESULT hr=ERROR_SUCCESS;
	if(!bEnabled && bUseEnabled) return S_OK;

	//hr = XInit(dwUserIndex);
	//if(FAILED(hr)) return ERROR_DEVICE_NOT_CONNECTED;

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;
	if(!Gamepad[dwUserIndex].ff.useforce) return ERROR_SUCCESS;

	WORD wLeftMotorSpeed = 0;
	WORD wRightMotorSpeed = 0;

	if(!Gamepad[dwUserIndex].ff.forceready) {
		HRESULT hrLeftForce = E_FAIL, hrRightForce = E_FAIL;
		hrLeftForce = PrepareForce(dwUserIndex,0);
		hrRightForce = PrepareForce(dwUserIndex,1);
		if FAILED(hrLeftForce) WriteLog(_T("[XINPUT] PrepareForce for pad %d failed with code hrLeftForce = %s"), dwUserIndex, DXErrStr(hrLeftForce));
		if FAILED(hrRightForce) WriteLog(_T("[XINPUT] PrepareForce for pad %d failed with code hrRightForce = %s"), dwUserIndex, DXErrStr(hrRightForce));
		if (SUCCEEDED(hrLeftForce) && SUCCEEDED(hrRightForce))
			Gamepad[dwUserIndex].ff.forceready = TRUE;
	}

	if(Gamepad[dwUserIndex].swapmotor) {
		wRightMotorSpeed = (WORD)((FLOAT)pVibration->wLeftMotorSpeed * Gamepad[dwUserIndex].ff.forcepercent);
		wLeftMotorSpeed =  (WORD)((FLOAT)pVibration->wRightMotorSpeed * Gamepad[dwUserIndex].ff.forcepercent);
	}
	else {
		wLeftMotorSpeed =  (WORD)((FLOAT)pVibration->wLeftMotorSpeed * Gamepad[dwUserIndex].ff.forcepercent);
		wRightMotorSpeed = (WORD)((FLOAT)pVibration->wRightMotorSpeed * Gamepad[dwUserIndex].ff.forcepercent);
	}

	if(Gamepad[dwUserIndex].ff.forceready) {
		hr = SetDeviceForces(dwUserIndex,wLeftMotorSpeed,0);
		if(FAILED(hr))WriteLog(_T("[XINPUT] SetDeviceForces for pad %d failed with code HR = %s"), dwUserIndex, DXErrStr(hr));
		hr = SetDeviceForces(dwUserIndex,wRightMotorSpeed,1);
		if(FAILED(hr))WriteLog(_T("[XINPUT] SetDeviceForces for pad %d failed with code HR = %s"), dwUserIndex, DXErrStr(hr));
	}
	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetCapabilities(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities)
{
	if(Gamepad[dwUserIndex].native) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef DWORD (WINAPI* XInputGetCapabilities_Type)(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
		XInputGetCapabilities_Type nativeXInputGetCapabilities = (XInputGetCapabilities_Type) GetProcAddress( hNativeInstance, "XInputGetCapabilities");
		return nativeXInputGetCapabilities(dwUserIndex,dwFlags,pCapabilities);
	}

	if (!pCapabilities || (dwUserIndex > (XUSER_MAX_COUNT-1)) || (dwFlags &~1) ) return ERROR_BAD_ARGUMENTS; //thats correct

	if(!capsready[dwUserIndex]) {
		XCAPS[dwUserIndex].Type = 0;
		XCAPS[dwUserIndex].SubType = Gamepad[dwUserIndex].gamepadtype;
		XCAPS[dwUserIndex].Flags = 0;
		XCAPS[dwUserIndex].Vibration.wLeftMotorSpeed = pCapabilities->Vibration.wRightMotorSpeed = 0xFFFF;

		XCAPS[dwUserIndex].Gamepad.wButtons = 0xFFFF;	
		XCAPS[dwUserIndex].Gamepad.bLeftTrigger = 0xFF;
		XCAPS[dwUserIndex].Gamepad.bRightTrigger = 0xFF;
		//center is more reliable because SHORT is signed
		XCAPS[dwUserIndex].Gamepad.sThumbLX = 0;
		XCAPS[dwUserIndex].Gamepad.sThumbLY = 0;
		XCAPS[dwUserIndex].Gamepad.sThumbRX = 0;
		XCAPS[dwUserIndex].Gamepad.sThumbRY = 0;
		capsready[dwUserIndex] = true;
	}

	*pCapabilities = XCAPS[dwUserIndex];
	WriteLog(_T("[XINPUT]  XInputGetCapabilities:: SubType %i"),pCapabilities->SubType);

	return ERROR_SUCCESS;
}

extern "C" VOID WINAPI XInputEnable(BOOL enable)
{
	if(wNativeMode) {
		if(!hNativeInstance) LoadOriginalDll();
		typedef VOID (WINAPI* XInputEnable_Type)(BOOL enable);
		XInputEnable_Type nativeXInputEnable = (XInputEnable_Type) GetProcAddress( hNativeInstance, "XInputSetState");
		return nativeXInputEnable(enable);
	}

	WriteLog(_T("[XINPUT]  XInputEnable called, state %d"),enable);

	bEnabled = enable;
	bUseEnabled = TRUE;

}

extern "C" DWORD WINAPI XInputGetDSoundAudioDeviceGuids (DWORD dwUserIndex, GUID* pDSoundRenderGuid, GUID* pDSoundCaptureGuid)
{
	UNREFERENCED_PARAMETER(pDSoundRenderGuid);
	UNREFERENCED_PARAMETER(pDSoundCaptureGuid);

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;
	return ERROR_SUCCESS;
}

extern "C" DWORD WINAPI XInputGetBatteryInformation(DWORD  dwUserIndex, BYTE devType, XINPUT_BATTERY_INFORMATION* pBatteryInformation)
{
	UNREFERENCED_PARAMETER(devType);

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	// Report a wired controller
	pBatteryInformation->BatteryType = BATTERY_TYPE_WIRED;
	return ERROR_SUCCESS;

}

extern "C" DWORD WINAPI XInputGetKeystroke(DWORD dwUserIndex, DWORD dwReserved, PXINPUT_KEYSTROKE pKeystroke)
{

	if(!Gamepad[dwUserIndex].g_pGamepad) return ERROR_DEVICE_NOT_CONNECTED;

	pKeystroke->Flags = NULL;
	pKeystroke->HidCode = NULL;
	pKeystroke->Unicode = NULL;
	pKeystroke->UserIndex = NULL;
	dwReserved=NULL;

	return ERROR_SUCCESS;
}
