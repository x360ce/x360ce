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
#include "InputHook.h"
#include "Utilities\Log.h"
#include "Utilities\Misc.h"

#define CINTERFACE
#include <dinput.h>

//EasyHook
TRACED_HOOK_HANDLE		hHookDirectInput8Create = NULL;
TRACED_HOOK_HANDLE		hHookCreateDeviceA = NULL;
TRACED_HOOK_HANDLE		hHookCreateDeviceW = NULL;
TRACED_HOOK_HANDLE		hHookGetPropertyA = NULL;
TRACED_HOOK_HANDLE		hHookGetPropertyW = NULL;
TRACED_HOOK_HANDLE		hHookGetDeviceInfoA = NULL;
TRACED_HOOK_HANDLE		hHookGetDeviceInfoW = NULL;
TRACED_HOOK_HANDLE		hHookEnumDevicesA = NULL;
TRACED_HOOK_HANDLE		hHookEnumDevicesW = NULL;
//TRACED_HOOK_HANDLE		hHookCallbackA = NULL;
//TRACED_HOOK_HANDLE		hHookCallbackW = NULL;


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT (WINAPI *OriginalDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalCreateDeviceA) (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A *lplpDirectInputDeviceA, LPUNKNOWN pUnkOuter) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalCreateDeviceW) (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W *lplpDirectInputDeviceW, LPUNKNOWN pUnkOuter) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalGetPropertyA) (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalGetPropertyW) (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalGetDeviceInfoA) (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalGetDeviceInfoW) (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalEnumDevicesA) (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalEnumDevicesW) (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags) = NULL;
LPDIENUMDEVICESCALLBACKA lpOriginalCallbackA= NULL;
LPDIENUMDEVICESCALLBACKW lpOriginalCallbackW= NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL CALLBACK HookEnumCallbackA( const DIDEVICEINSTANCEA* pInst,VOID* pContext )
{
	if(!InputHook_Config()->bEnabled) return lpOriginalCallbackA(pInst,pContext);
	WriteLog(LOG_HOOKDI,L"HookEnumCallbackA");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Keyboard detected");
		return lpOriginalCallbackA(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Mouse detected");
		return lpOriginalCallbackA(pInst,pContext);
	}

	if(InputHook_Config()->dwHookMode == 3) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(InputHook_Config()->dwHookMode == 2)
		{
			LPDIDEVICEINSTANCEA HookInst = const_cast<LPDIDEVICEINSTANCEA>(pInst);

			for(int i = 0; i < 4; i++)
			{
				if (InputHook_GamepadConfig(i)->bEnabled && IsEqualGUID(InputHook_GamepadConfig(i)->productGUID,pInst->guidProduct))
				{
					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHook_Config()->dwHookVID,InputHook_Config()->dwHookPID));

					HookInst->guidProduct.Data1=dwHookPIDVID;
					HookInst->guidProduct.Data2=0x0000;
					HookInst->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&HookInst->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strHookguidProduct[50];
					GUIDtoString(pInst->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(HookInst->guidProduct,strHookguidProduct,50);
					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strOriginalguidProduct,strHookguidProduct);

					HookInst->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					HookInst->wUsage = 0x05;
					HookInst->wUsagePage = 0x01;

					LPSTR OldProductName = new CHAR[MAX_PATH];
					LPSTR OldInstanceName = new CHAR[MAX_PATH];

					strcpy_s(OldProductName,MAX_PATH,HookInst->tszProductName);
					strcpy_s(OldInstanceName,MAX_PATH,HookInst->tszInstanceName);

					strcpy_s(HookInst->tszProductName, "XBOX 360 For Windows (Controller)");
					strcpy_s(HookInst->tszInstanceName, "XBOX 360 For Windows (Controller)"); 

					WriteLog(LOG_HOOKDI,L"Product Name change from \"%hs\" to \"%hs\"",OldProductName,HookInst->tszProductName);
					WriteLog(LOG_HOOKDI,L"Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName,HookInst->tszInstanceName);

					SAFE_DELETE_ARRAY(OldProductName);
					SAFE_DELETE_ARRAY(OldInstanceName);

					return lpOriginalCallbackA(HookInst,pContext);
				}
			}
		}
	}

	return lpOriginalCallbackA(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL CALLBACK HookEnumCallbackW( const DIDEVICEINSTANCEW* pInst,VOID* pContext )
{
	if(!InputHook_Config()->bEnabled) return lpOriginalCallbackW(pInst,pContext);
	WriteLog(LOG_HOOKDI,L"HookEnumCallbackW");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Keyboard detected");
		return lpOriginalCallbackW(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackA:: Mouse detected");
		return lpOriginalCallbackW(pInst,pContext);
	}

	if(InputHook_Config()->dwHookMode == 3) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(InputHook_Config()->dwHookMode == 2)
		{
			//DIDEVICEINSTANCEW HookInst;
			LPDIDEVICEINSTANCEW HookInst = const_cast<LPDIDEVICEINSTANCEW>(pInst);

			for(int i = 0; i < 4; i++)
			{
				if (InputHook_GamepadConfig(i)->bEnabled && IsEqualGUID(InputHook_GamepadConfig(i)->productGUID,pInst->guidProduct))
				{
					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHook_Config()->dwHookVID,InputHook_Config()->dwHookPID));

					HookInst->guidProduct.Data1=dwHookPIDVID;
					HookInst->guidProduct.Data2=0x0000;
					HookInst->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&HookInst->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strHookguidProduct[50];
					GUIDtoString(pInst->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(HookInst->guidProduct,strHookguidProduct,50);
					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strOriginalguidProduct,strHookguidProduct);

					HookInst->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					HookInst->wUsage = 0x05;
					HookInst->wUsagePage = 0x01;

					LPWSTR OldProductName = new WCHAR[MAX_PATH];
					LPWSTR OldInstanceName = new WCHAR[MAX_PATH];

					wcscpy_s(OldProductName,MAX_PATH,HookInst->tszProductName);
					wcscpy_s(OldInstanceName,MAX_PATH,HookInst->tszInstanceName);

					wcscpy_s(HookInst->tszProductName, L"XBOX 360 For Windows (Controller)");
					wcscpy_s(HookInst->tszInstanceName, L"XBOX 360 For Windows (Controller)");  

					WriteLog(LOG_HOOKDI,L"Product Name change from \"%s\" to \"%s\"",OldProductName,HookInst->tszProductName);
					WriteLog(LOG_HOOKDI,L"Instance Name change from \"%s\" to \"%s\"",OldInstanceName,HookInst->tszInstanceName);

					SAFE_DELETE_ARRAY(OldProductName);
					SAFE_DELETE_ARRAY(OldInstanceName);

					return lpOriginalCallbackW(HookInst,pContext);
				}
			}
		}
	}
	return lpOriginalCallbackW(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesA (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!InputHook_Config()->bEnabled) return OriginalEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(LOG_HOOKDI,L"HookEnumDevicesA");

	if (lpCallback)
	{
		lpOriginalCallbackA= lpCallback;
		return OriginalEnumDevicesA(This,dwDevType,HookEnumCallbackA,pvRef,dwFlags);
	}

	return OriginalEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!InputHook_Config()->bEnabled || !lpCallback) return OriginalEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(LOG_HOOKDI,L"HookEnumDevicesW");

	if (lpCallback)
	{
		lpOriginalCallbackW= lpCallback;
		return OriginalEnumDevicesW(This,dwDevType,HookEnumCallbackW,pvRef,dwFlags);
	}

	return OriginalEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoA (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
{
	if(!InputHook_Config()->bEnabled) return OriginalGetDeviceInfoA ( This, pdidi );
	WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA");

	HRESULT hr;
	hr = OriginalGetDeviceInfoA ( This, pdidi );

	// Fast return if keyboard or mouse
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Keyboard detected");
		return hr;
	}
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Mouse detected");
		return hr;
	}

	if(pdidi) {
		if(InputHook_Config()->dwHookMode >= 2) {

			for(int i = 0; i < 4; i++) {
				if(InputHook_GamepadConfig(i)->bEnabled && IsEqualGUID(InputHook_GamepadConfig(i)->productGUID, pdidi->guidProduct)) {

					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHook_Config()->dwHookVID,InputHook_Config()->dwHookPID));
					pdidi->guidProduct.Data1=dwHookPIDVID;
					pdidi->guidProduct.Data2=0x0000;
					pdidi->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pdidi->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strHookguidProduct[50];
					GUIDtoString(pdidi->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(pdidi->guidProduct,strHookguidProduct,50);
					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strOriginalguidProduct,strHookguidProduct);

					pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					pdidi->wUsage = 0x05;
					pdidi->wUsagePage = 0x01;

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
	if(!InputHook_Config()->bEnabled) return OriginalGetDeviceInfoW ( This, pdidi );
	WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW");

	HRESULT hr;
	hr = OriginalGetDeviceInfoW ( This, pdidi );

	// Fast return if keyboard or mouse
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Keyboard detected");
		return hr;
	}
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Mouse detected");
		return hr;
	}

	if(pdidi) {

		if(InputHook_Config()->dwHookMode >= 2) {

			for(int i = 0; i < 4; i++) {
				if(InputHook_GamepadConfig(i)->bEnabled && InputHook_GamepadConfig(i)->productGUID.Data1 == pdidi->guidProduct.Data1) {
					

					DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHook_Config()->dwHookVID,InputHook_Config()->dwHookPID));
					pdidi->guidProduct.Data1=dwHookPIDVID;
					pdidi->guidProduct.Data2=0x0000;
					pdidi->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pdidi->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strHookguidProduct[50];
					GUIDtoString(pdidi->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(pdidi->guidProduct,strHookguidProduct,50);
					WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strOriginalguidProduct,strHookguidProduct);

					pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					pdidi->wUsage = 0x05;
					pdidi->wUsagePage = 0x01;

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
	if(!InputHook_Config()->bEnabled) return OriginalGetPropertyA(This, rguidProp, pdiph);
	HRESULT hr;
	hr = OriginalGetPropertyA(This, rguidProp, pdiph);
	WriteLog(LOG_HOOKDI,L"HookGetPropertyW");

	if (InputHook_Config()->dwHookMode >= 2 ) {

		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHook_Config()->dwHookVID,InputHook_Config()->dwHookPID));
			DWORD dwOriginalPIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

			reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
			WriteLog(LOG_HOOKDI,L"VIDPID change from %08X to %08X",dwOriginalPIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
		}

		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR OriginalName[MAX_PATH];
			wcscpy_s(OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

			swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(LOG_HOOKDI,L"Product Name change from %s to %s",OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetPropertyW (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	if(!InputHook_Config()->bEnabled) return OriginalGetPropertyW(This, rguidProp, pdiph);
	HRESULT hr;
	hr = OriginalGetPropertyW(This, rguidProp, pdiph);
	WriteLog(LOG_HOOKDI,L"HookGetPropertyW");

	if (InputHook_Config()->dwHookMode >= 2 ) {

		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHook_Config()->dwHookVID,InputHook_Config()->dwHookPID));
			DWORD dwOriginalPIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

			reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
			WriteLog(LOG_HOOKDI,L"VIDPID change from %08X to %08X",dwOriginalPIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
		}

		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR OriginalName[MAX_PATH];
			wcscpy_s(OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

			swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(LOG_HOOKDI,L"Product Name change from %s to %s",OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceA (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	if(!InputHook_Config()->bEnabled) return OriginalCreateDeviceA (This, rguid, lplpDirectInputDevice, pUnkOuter);
	WriteLog(LOG_HOOKDI,L"HookCreateDeviceA");

	HRESULT hr;

	hr = OriginalCreateDeviceA (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(*lplpDirectInputDevice) {
		//WCHAR strDevGUID[50];
		//GUIDtoString(strDevGUID,&rguid);
		//WriteLog(_T("[HookDI]  Device GUID : %s"),strDevGUID);

		if(!OriginalGetDeviceInfoA) {
			OriginalGetDeviceInfoA = (*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo;
			hHookGetDeviceInfoA = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetDeviceInfoA,HookGetDeviceInfoA,static_cast<PVOID>(NULL),hHookGetDeviceInfoA);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetDeviceInfoA );
		}
		if(!OriginalGetPropertyA) {
			OriginalGetPropertyA = (*lplpDirectInputDevice)->lpVtbl->GetProperty;
			hHookGetPropertyA = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetPropertyA,HookGetPropertyA,static_cast<PVOID>(NULL),hHookGetPropertyA);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetPropertyA );
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceW (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	if(!InputHook_Config()->bEnabled) return OriginalCreateDeviceW (This, rguid, lplpDirectInputDevice, pUnkOuter);
	WriteLog(LOG_HOOKDI,L"HookCreateDeviceW");

	HRESULT hr;

	hr = OriginalCreateDeviceW (This, rguid, lplpDirectInputDevice, pUnkOuter);

	//WCHAR strDevGUID[50];
	//GUIDtoString(strDevGUID,&rguid);
	//WriteLog(_T("[HookDI]  Device GUID : %s"),strDevGUID);

	if(*lplpDirectInputDevice) {
		if(!OriginalGetDeviceInfoW) {
			OriginalGetDeviceInfoW = (*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo;
			hHookGetDeviceInfoW = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetDeviceInfoW,HookGetDeviceInfoW,static_cast<PVOID>(NULL),hHookGetDeviceInfoW);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetDeviceInfoW );
		}
		if(!OriginalGetPropertyW) {
			OriginalGetPropertyW = (*lplpDirectInputDevice)->lpVtbl->GetProperty;
			hHookGetPropertyW = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetPropertyW,HookGetPropertyW,static_cast<PVOID>(NULL),hHookGetPropertyW);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetPropertyW );
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookDIrectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	if(!InputHook_Config()->bEnabled) return OriginalDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);
	WriteLog(LOG_HOOKDI,L"HookDIrectInput8Create");

	/*
	LPOLESTR str1;
	StringFromIID(riidltf,&str1);
	WriteLog(_T("rclsid: %s"),str1);
	*/

	HRESULT hr;
	hr = OriginalDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(ppvOut) {
		if(IsEqualIID(riidltf,IID_IDirectInput8A)) {
			LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppvOut);
			if(pDIA)  {
				WriteLog(LOG_HOOKDI,L"HookDIrectInput8Create - ANSI interface");
				if(!OriginalCreateDeviceA) {
					OriginalCreateDeviceA = pDIA->lpVtbl->CreateDevice;
					hHookCreateDeviceA = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalCreateDeviceA,HookCreateDeviceA,static_cast<PVOID>(NULL),hHookCreateDeviceA);
					LhSetInclusiveACL(ACLEntries, 1,hHookCreateDeviceA );
				}
				if(!OriginalEnumDevicesA)
				{
					OriginalEnumDevicesA = pDIA->lpVtbl->EnumDevices;
					hHookEnumDevicesA = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalEnumDevicesA,HookEnumDevicesA,static_cast<PVOID>(NULL),hHookEnumDevicesA);
					LhSetInclusiveACL(ACLEntries, 1,hHookEnumDevicesA );
				}
			}
		}
		if (IsEqualIID(riidltf,IID_IDirectInput8W)) {
			LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);
			if(pDIW)  {
				WriteLog(LOG_HOOKDI,L"HookDIrectInput8Create - UNICODE interface");
				if(!OriginalCreateDeviceW) {
					OriginalCreateDeviceW = pDIW->lpVtbl->CreateDevice;
					hHookCreateDeviceW = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalCreateDeviceW,HookCreateDeviceW,static_cast<PVOID>(NULL),hHookCreateDeviceW);
					LhSetInclusiveACL(ACLEntries, 1,hHookCreateDeviceW );
				}
				if(!OriginalEnumDevicesW)
				{
					OriginalEnumDevicesW = pDIW->lpVtbl->EnumDevices;
					hHookEnumDevicesW = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalEnumDevicesW,HookEnumDevicesW,static_cast<PVOID>(NULL),hHookEnumDevicesW);
					LhSetInclusiveACL(ACLEntries, 1,hHookEnumDevicesW );
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
	if(!OriginalDirectInput8Create) {
		OriginalDirectInput8Create = DirectInput8Create;
		hHookDirectInput8Create = new HOOK_TRACE_INFO();
		WriteLog(LOG_IHOOK,L"HookDIrectInput8Create:: Hooking");

		LhInstallHook(OriginalDirectInput8Create,HookDIrectInput8Create,static_cast<PVOID>(NULL),hHookDirectInput8Create);
		LhSetInclusiveACL(ACLEntries, 1,hHookDirectInput8Create );
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void HookDIClean()
{
	SAFE_DELETE(hHookDirectInput8Create);
	SAFE_DELETE(hHookCreateDeviceA);
	SAFE_DELETE(hHookCreateDeviceW);
	SAFE_DELETE(hHookGetPropertyA);
	SAFE_DELETE(hHookGetPropertyW);
	SAFE_DELETE(hHookGetDeviceInfoA);
	SAFE_DELETE(hHookGetDeviceInfoW);
	SAFE_DELETE(hHookEnumDevicesA);
	SAFE_DELETE(hHookEnumDevicesW);

}