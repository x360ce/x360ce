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
#include "Utilities\Log.h"
#include "Utilities\Misc.h"

#define CINTERFACE
#include <dinput.h>


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
typedef HRESULT (WINAPI *tDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter);
typedef HRESULT (STDMETHODCALLTYPE *tCreateDeviceA) (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A *lplpDirectInputDeviceA, LPUNKNOWN pUnkOuter);
typedef HRESULT (STDMETHODCALLTYPE *tCreateDeviceW) (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W *lplpDirectInputDeviceW, LPUNKNOWN pUnkOuter);
typedef HRESULT (STDMETHODCALLTYPE *tGetPropertyA) (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
typedef HRESULT (STDMETHODCALLTYPE *tGetPropertyW) (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph);
typedef HRESULT (STDMETHODCALLTYPE *tGetDeviceInfoA) (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi);
typedef HRESULT (STDMETHODCALLTYPE *tGetDeviceInfoW) (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi);
typedef HRESULT (STDMETHODCALLTYPE *tEnumDevicesA) (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags);
typedef HRESULT (STDMETHODCALLTYPE *tEnumDevicesW) (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags);
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

MologieDetours::Detour<tDirectInput8Create>* hDirectInput8Create = NULL;
MologieDetours::Detour<tCreateDeviceA>* hCreateDeviceA = NULL;;
MologieDetours::Detour<tCreateDeviceW>* hCreateDeviceW = NULL;;
MologieDetours::Detour<tGetPropertyA>* hGetPropertyA = NULL;;
MologieDetours::Detour<tGetPropertyW>* hGetPropertyW = NULL;;
MologieDetours::Detour<tGetDeviceInfoA>* hGetDeviceInfoA = NULL;;
MologieDetours::Detour<tGetDeviceInfoW>* hGetDeviceInfoW = NULL;;
MologieDetours::Detour<tEnumDevicesA>* hEnumDevicesA = NULL;;
MologieDetours::Detour<tEnumDevicesW>* hEnumDevicesW = NULL;;

LPDIENUMDEVICESCALLBACKA lpTrueCallbackA= NULL;
LPDIENUMDEVICESCALLBACKW lpTrueCallbackW= NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL FAR PASCAL HookEnumCallbackA( const DIDEVICEINSTANCEA* pInst,VOID* pContext )
{
	if(!InputHookConfig.bEnabled) return lpTrueCallbackA(pInst,pContext);
	WriteLog(LOG_HOOKDI,L"HookEnumCallbackA");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) 
	{
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Keyboard detected");
		return lpTrueCallbackA(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) 
	{
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Mouse detected");
		return lpTrueCallbackA(pInst,pContext);
	}

	if(InputHookConfig.dwHookMode > HOOK_ALL) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(InputHookConfig.dwHookMode >= HOOK_COMPAT)
		{

			for(int i = 0; i < InputHookConfig.sConfiguredPads; i++)
			{
				if (GamepadConfig[i].bEnabled && IsEqualGUID(GamepadConfig[i].productGUID,pInst->guidProduct))
				{

					DIDEVICEINSTANCEA HookInst;
					memcpy(&HookInst,pInst,pInst->dwSize);

					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));

					HookInst.guidProduct.Data1=dwHookPIDVID;
					HookInst.guidProduct.Data2=0x0000;
					HookInst.guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&HookInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strTrueguidProduct[50];
					WCHAR strHookguidProduct[50];
					GUIDtoString(pInst->guidProduct,strTrueguidProduct,50);
					GUIDtoString(HookInst.guidProduct,strHookguidProduct,50);
					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

					HookInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					HookInst.wUsage = 0x05;
					HookInst.wUsagePage = 0x01;

					if(InputHookConfig.dwHookMode >= HOOK_ALL) 
					{

						LPSTR OldProductName = new CHAR[MAX_PATH];
						LPSTR OldInstanceName = new CHAR[MAX_PATH];

						strcpy_s(OldProductName,MAX_PATH,HookInst.tszProductName);
						strcpy_s(OldInstanceName,MAX_PATH,HookInst.tszInstanceName);

						strcpy_s(HookInst.tszProductName, "XBOX 360 For Windows (Controller)");
						strcpy_s(HookInst.tszInstanceName, "XBOX 360 For Windows (Controller)"); 

						WriteLog(LOG_HOOKDI,L"Product Name change from \"%hs\" to \"%hs\"",OldProductName,HookInst.tszProductName);
						WriteLog(LOG_HOOKDI,L"Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName,HookInst.tszInstanceName);

						SAFE_DELETE_ARRAY(OldProductName);
						SAFE_DELETE_ARRAY(OldInstanceName);
					}

					return lpTrueCallbackA(&HookInst,pContext);
				}
			}
		}
	}

	return lpTrueCallbackA(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL FAR PASCAL HookEnumCallbackW( const DIDEVICEINSTANCEW* pInst,VOID* pContext )
{
	if(!InputHookConfig.bEnabled) return lpTrueCallbackW(pInst,pContext);
	WriteLog(LOG_HOOKDI,L"HookEnumCallbackW");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) 
	{
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Keyboard detected");
		return lpTrueCallbackW(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) 
	{
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Mouse detected");
		return lpTrueCallbackW(pInst,pContext);
	}

	if(InputHookConfig.dwHookMode > HOOK_ALL) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(InputHookConfig.dwHookMode >= HOOK_COMPAT)
		{
			for(int i = 0; i < 4; i++)
			{
				if (GamepadConfig[i].bEnabled && IsEqualGUID(GamepadConfig[i].productGUID,pInst->guidProduct))
				{
					DIDEVICEINSTANCEW HookInst;
					memcpy(&HookInst,pInst,pInst->dwSize);

					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));

					HookInst.guidProduct.Data1=dwHookPIDVID;
					HookInst.guidProduct.Data2=0x0000;
					HookInst.guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&HookInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strTrueguidProduct[50];
					WCHAR strHookguidProduct[50];
					GUIDtoString(pInst->guidProduct,strTrueguidProduct,50);
					GUIDtoString(HookInst.guidProduct,strHookguidProduct,50);
					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

					HookInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					HookInst.wUsage = 0x05;
					HookInst.wUsagePage = 0x01;

					if(InputHookConfig.dwHookMode >= HOOK_ALL) 
					{

						LPWSTR OldProductName = new WCHAR[MAX_PATH];
						LPWSTR OldInstanceName = new WCHAR[MAX_PATH];

						wcscpy_s(OldProductName,MAX_PATH,HookInst.tszProductName);
						wcscpy_s(OldInstanceName,MAX_PATH,HookInst.tszInstanceName);

						wcscpy_s(HookInst.tszProductName, L"XBOX 360 For Windows (Controller)");
						wcscpy_s(HookInst.tszInstanceName, L"XBOX 360 For Windows (Controller)");  

						WriteLog(LOG_HOOKDI,L"Product Name change from \"%s\" to \"%s\"",OldProductName,HookInst.tszProductName);
						WriteLog(LOG_HOOKDI,L"Instance Name change from \"%s\" to \"%s\"",OldInstanceName,HookInst.tszInstanceName);

						SAFE_DELETE_ARRAY(OldProductName);
						SAFE_DELETE_ARRAY(OldInstanceName);
					}

					return lpTrueCallbackW(&HookInst,pContext);
				}
			}
		}
	}
	return lpTrueCallbackW(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesA (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!InputHookConfig.bEnabled) return hEnumDevicesA->GetOriginalFunction()(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(LOG_HOOKDI,L"HookEnumDevicesA");

	if (lpCallback)
	{
		lpTrueCallbackA= lpCallback;
		return hEnumDevicesA->GetOriginalFunction()(This,dwDevType,HookEnumCallbackA,pvRef,dwFlags);
	}

	return hEnumDevicesA->GetOriginalFunction()(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!InputHookConfig.bEnabled) return hEnumDevicesW->GetOriginalFunction()(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(LOG_HOOKDI,L"HookEnumDevicesW");

	if (lpCallback)
	{
		lpTrueCallbackW = lpCallback;
		return hEnumDevicesW->GetOriginalFunction()(This,dwDevType,HookEnumCallbackW,pvRef,dwFlags);
	}

	return hEnumDevicesW->GetOriginalFunction()(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoA (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
{	
	HRESULT hr = hGetDeviceInfoA->GetOriginalFunction() ( This, pdidi );
	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA");

	if(FAILED(hr)) return hr;

	if(pdidi) {

	// Fast return if keyboard or mouse
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Keyboard detected");
		return hr;
	}
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Mouse detected");
		return hr;
	}

		if(InputHookConfig.dwHookMode >= HOOK_COMPAT) {

			for(int i = 0; i < 4; i++) {
				if(GamepadConfig[i].bEnabled && IsEqualGUID(GamepadConfig[i].productGUID, pdidi->guidProduct)) {

					WCHAR strTrueguidProduct[50];
					WCHAR strHookguidProduct[50];

					GUIDtoString(pdidi->guidProduct,strTrueguidProduct,50);

					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));
					pdidi->guidProduct.Data1=dwHookPIDVID;
					pdidi->guidProduct.Data2=0x0000;
					pdidi->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pdidi->guidProduct.Data4, pdata4, sizeof(pdata4));

					GUIDtoString(pdidi->guidProduct,strHookguidProduct,50);

					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

					pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					pdidi->wUsage = 0x05;
					pdidi->wUsagePage = 0x01;

					if(InputHookConfig.dwHookMode >= HOOK_ALL) 
					{

						LPSTR OldProductName = new CHAR[MAX_PATH];
						LPSTR OldInstanceName = new CHAR[MAX_PATH];

						strcpy_s(OldProductName,MAX_PATH,pdidi->tszProductName);
						strcpy_s(OldInstanceName,MAX_PATH,pdidi->tszInstanceName);

						strcpy_s(pdidi->tszProductName, "XBOX 360 For Windows (Controller)");
						strcpy_s(pdidi->tszInstanceName, "XBOX 360 For Windows (Controller)"); 

						WriteLog(LOG_HOOKDI,L"Product Name change from \"%hs\" to \"%hs\"",OldProductName,pdidi->tszProductName);
						WriteLog(LOG_HOOKDI,L"Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName,pdidi->tszInstanceName);

						SAFE_DELETE_ARRAY(OldProductName);
						SAFE_DELETE_ARRAY(OldInstanceName);
					}

					hr=DI_OK;
				}
			}
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoW (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi)
{
	HRESULT hr = hGetDeviceInfoW->GetOriginalFunction()( This, pdidi );
	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW");

	if(FAILED(hr)) return hr;

	if(pdidi) 
	{

	// Fast return if keyboard or mouse
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Keyboard detected");
		return hr;
	}
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Mouse detected");
		return hr;
	}

		if(InputHookConfig.dwHookMode >= HOOK_COMPAT) {

			for(int i = 0; i < 4; i++) {
				if(GamepadConfig[i].productGUID.Data1 == pdidi->guidProduct.Data1) {
					
					WCHAR strTrueguidProduct[50];
					WCHAR strHookguidProduct[50];

					GUIDtoString(pdidi->guidProduct,strTrueguidProduct,50);

					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));
					pdidi->guidProduct.Data1=dwHookPIDVID;
					pdidi->guidProduct.Data2=0x0000;
					pdidi->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pdidi->guidProduct.Data4, pdata4, sizeof(pdata4));

					GUIDtoString(pdidi->guidProduct,strHookguidProduct,50);

					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

					pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					pdidi->wUsage = 0x05;
					pdidi->wUsagePage = 0x01;

					if(InputHookConfig.dwHookMode >= HOOK_ALL) 
					{

						LPWSTR OldProductName = new WCHAR[MAX_PATH];
						LPWSTR OldInstanceName = new WCHAR[MAX_PATH];

						wcscpy_s(OldProductName,MAX_PATH,pdidi->tszProductName);
						wcscpy_s(OldInstanceName,MAX_PATH,pdidi->tszInstanceName);

						wcscpy_s(pdidi->tszProductName, L"XBOX 360 For Windows (Controller)");
						wcscpy_s(pdidi->tszInstanceName, L"XBOX 360 For Windows (Controller)");  

						WriteLog(LOG_HOOKDI,L"Product Name change from \"%s\" to \"%s\"",OldProductName,pdidi->tszProductName);
						WriteLog(LOG_HOOKDI,L"Instance Name change from \"%s\" to \"%s\"",OldInstanceName,pdidi->tszInstanceName);

						SAFE_DELETE_ARRAY(OldProductName);
						SAFE_DELETE_ARRAY(OldInstanceName);
					}

					hr=DI_OK;
				}
			}
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetPropertyA (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	HRESULT hr = hGetPropertyA->GetOriginalFunction()(This, rguidProp, pdiph);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetPropertyA");

	if(FAILED(hr)) return hr;

	if (InputHookConfig.dwHookMode >= HOOK_COMPAT ) 
	{
		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));
			DWORD dwTruePIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

			reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
			WriteLog(LOG_HOOKDI,L"VIDPID change from %08X to %08X",dwTruePIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
		}
	}

	if (InputHookConfig.dwHookMode >= HOOK_ALL ) 
	{
		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR TrueName[MAX_PATH];
			wcscpy_s(TrueName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

			swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(LOG_HOOKDI,L"Product Name change from %s to %s",TrueName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetPropertyW (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	HRESULT hr = hGetPropertyW->GetOriginalFunction()(This, rguidProp, pdiph);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetPropertyW");

	if(FAILED(hr)) return hr;

	if (InputHookConfig.dwHookMode >= HOOK_COMPAT ) 
	{
		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));
			DWORD dwTruePIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

			reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
			WriteLog(LOG_HOOKDI,L"VIDPID change from %08X to %08X",dwTruePIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
		}
	}

	if (InputHookConfig.dwHookMode >= HOOK_ALL ) 
	{
		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR TrueName[MAX_PATH];
			wcscpy_s(TrueName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

			swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(LOG_HOOKDI,L"Product Name change from %s to %s",TrueName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceA (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	HRESULT hr = hCreateDeviceA->GetOriginalFunction()(This, rguid, lplpDirectInputDevice, pUnkOuter);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookCreateDeviceA");

	if(FAILED(hr)) return hr;

	if(*lplpDirectInputDevice) {

		if(!hGetDeviceInfoA) {
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Hooking");

			hGetDeviceInfoA = new MologieDetours::Detour<tGetDeviceInfoA>((*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo,HookGetDeviceInfoA);
		}
		if(!hGetPropertyA) {
			WriteLog(LOG_HOOKDI,L"HookGetPropertyA:: Hooking");

			hGetPropertyA = new MologieDetours::Detour<tGetPropertyA>((*lplpDirectInputDevice)->lpVtbl->GetProperty,HookGetPropertyA);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceW (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	HRESULT hr = hCreateDeviceW->GetOriginalFunction()(This, rguid, lplpDirectInputDevice, pUnkOuter);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookCreateDeviceW");

	if(FAILED(hr)) return hr;

	if(*lplpDirectInputDevice) {
		if(!hGetDeviceInfoW) {
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Hooking");

			hGetDeviceInfoW = new MologieDetours::Detour<tGetDeviceInfoW>((*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo,HookGetDeviceInfoW);
		}
		if(!hGetPropertyW) {
			WriteLog(LOG_HOOKDI,L"HookGetPropertyW:: Hooking");

			hGetPropertyW = new MologieDetours::Detour<tGetPropertyW>((*lplpDirectInputDevice)->lpVtbl->GetProperty,HookGetPropertyW);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	HRESULT hr = hDirectInput8Create->GetOriginalFunction()(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(!InputHookConfig.bEnabled) return hr;
	WriteLog(LOG_HOOKDI,L"HookDirectInput8Create");

	if(ppvOut) {
		if(IsEqualIID(riidltf,IID_IDirectInput8A)) {
			LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppvOut);
			if(pDIA)  {
				WriteLog(LOG_HOOKDI,L"HookDirectInput8Create - ANSI interface");
				if(!hCreateDeviceA) {
					WriteLog(LOG_HOOKDI,L"HookCreateDeviceA:: Hooking");

					hCreateDeviceA = new MologieDetours::Detour<tCreateDeviceA>(pDIA->lpVtbl->CreateDevice,HookCreateDeviceA);
				}
				if(!hEnumDevicesW)
				{
					WriteLog(LOG_HOOKDI,L"HookEnumDevicesA:: Hooking");

					hEnumDevicesA = new MologieDetours::Detour<tEnumDevicesA>(pDIA->lpVtbl->EnumDevices,HookEnumDevicesA);
				}
			}
		}
		if (IsEqualIID(riidltf,IID_IDirectInput8W)) {
			LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);
			if(pDIW)  {
				WriteLog(LOG_HOOKDI,L"HookDirectInput8Create - UNICODE interface");
				if(!hCreateDeviceW) {
					WriteLog(LOG_HOOKDI,L"HookCreateDeviceW:: Hooking");

					hCreateDeviceW = new MologieDetours::Detour<tCreateDeviceW>(pDIW->lpVtbl->CreateDevice,HookCreateDeviceW);

				}
				if(!hEnumDevicesW)
				{
					WriteLog(LOG_HOOKDI,L"HookEnumDevicesW:: Hooking");

					hEnumDevicesW = new MologieDetours::Detour<tEnumDevicesW>(pDIW->lpVtbl->EnumDevices,HookEnumDevicesW);
				}
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void HookDI()
{
	if(!hDirectInput8Create) {
		hDirectInput8Create = new MologieDetours::Detour<tDirectInput8Create>(DirectInput8Create, HookDirectInput8Create);
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void HookDIClean()
{
	if(hGetDeviceInfoA) {
		WriteLog(LOG_HOOKDI, L"HookGetDeviceInfoA:: Removing Hook");
		SAFE_DELETE(hGetDeviceInfoA);
	}
	if(hGetDeviceInfoW) {
		WriteLog(LOG_HOOKDI, L"HookGetDeviceInfoW:: Removing Hook");

		SAFE_DELETE(hGetDeviceInfoW);
	}

	if(hGetPropertyA) {
		WriteLog(LOG_HOOKDI,L"HookGetPropertyA:: Removing Hook");

		SAFE_DELETE(hGetPropertyA);
	}
	if(hGetPropertyW) {
		WriteLog(LOG_HOOKDI,L"HookGetPropertyW:: Removing Hook");

		SAFE_DELETE(hGetPropertyW);
	}

	if(hCreateDeviceA) {
		WriteLog(LOG_HOOKDI,L"HookCreateDeviceA:: Removing Hook");

		SAFE_DELETE(hCreateDeviceA);
	}
	if(hCreateDeviceW) {
		WriteLog(LOG_HOOKDI,L"HookCreateDeviceW:: Removing Hook");

		SAFE_DELETE(hCreateDeviceW);
	}

	if(hEnumDevicesA)
	{
		WriteLog(LOG_HOOKDI,L"HookEnumDevicesA:: Removing Hook");

		SAFE_DELETE(hEnumDevicesA);
	}
	if(hEnumDevicesW)
	{
		WriteLog(LOG_HOOKDI,L"HookEnumDevicesW:: Removing Hook");

		SAFE_DELETE(hEnumDevicesW);
	}

	if(hDirectInput8Create) {
		WriteLog(LOG_HOOKDI,L"HookDirectInput8Create:: Removing Hook");

		SAFE_DELETE(hDirectInput8Create);
	}

}