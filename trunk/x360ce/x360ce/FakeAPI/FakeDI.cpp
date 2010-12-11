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
#include "FakeAPI.h"
#include "Utils.h"

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
TRACED_HOOK_HANDLE		hHookCallbackA = NULL;
TRACED_HOOK_HANDLE		hHookCallbackW = NULL;


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
BOOL CALLBACK FakeEnumCallbackA( const DIDEVICEINSTANCEA* pInst,VOID* pContext )
{
	if(!FakeAPI_Config()->bEnabled) return lpOriginalCallbackA(pInst,pContext);
	WriteLog(L"[FAKEDI]  FakeEnumCallbackA");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(L"[FAKEDI]  FakeEnumCallbackA:: Keyboard detected");
		return lpOriginalCallbackA(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(L"[FAKEDI]  FakeEnumCallbackA:: Mouse detected");
		return lpOriginalCallbackA(pInst,pContext);
	}

	if(FakeAPI_Config()->dwFakeMode == 3) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(FakeAPI_Config()->dwFakeMode == 2)
		{
			LPDIDEVICEINSTANCEA FakeInst = const_cast<LPDIDEVICEINSTANCEA>(pInst);

			for(int i = 0; i < 4; i++)
			{
				if (FakeAPI_GamepadConfig(i)->bEnabled && IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID,pInst->guidProduct))
				{
					DWORD dwFakePIDVID = static_cast<DWORD>(MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID));

					FakeInst->guidProduct.Data1=dwFakePIDVID;
					FakeInst->guidProduct.Data2=0x0000;
					FakeInst->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(FakeInst->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pInst->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(FakeInst->guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					FakeInst->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					FakeInst->wUsage = 0x05;
					FakeInst->wUsagePage = 0x01;

					LPSTR OldProductName = new CHAR[MAX_PATH];
					LPSTR OldInstanceName = new CHAR[MAX_PATH];

					strcpy_s(OldProductName,MAX_PATH,FakeInst->tszProductName);
					strcpy_s(OldInstanceName,MAX_PATH,FakeInst->tszInstanceName);

					strcpy_s(FakeInst->tszProductName, "XBOX 360 For Windows (Controller)");
					strcpy_s(FakeInst->tszInstanceName, "XBOX 360 For Windows (Controller)"); 

					WriteLog(L"[FAKEDI]  Product Name change from \"%hs\" to \"%hs\"",OldProductName,FakeInst->tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName,FakeInst->tszInstanceName);

					SAFE_DELETE_ARRAY(OldProductName);
					SAFE_DELETE_ARRAY(OldInstanceName);

					return lpOriginalCallbackA(FakeInst,pContext);
				}
			}
		}
	}

	return lpOriginalCallbackA(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL CALLBACK FakeEnumCallbackW( const DIDEVICEINSTANCEW* pInst,VOID* pContext )
{
	if(!FakeAPI_Config()->bEnabled) return lpOriginalCallbackW(pInst,pContext);
	WriteLog(L"[FAKEDI]  FakeEnumCallbackW");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(L"[FAKEDI]  FakeEnumCallbackA:: Keyboard detected");
		return lpOriginalCallbackW(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(L"[FAKEDI]  FakeEnumCallbackA:: Mouse detected");
		return lpOriginalCallbackW(pInst,pContext);
	}

	if(FakeAPI_Config()->dwFakeMode == 3) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(FakeAPI_Config()->dwFakeMode == 2)
		{
			//DIDEVICEINSTANCEW FakeInst;
			LPDIDEVICEINSTANCEW FakeInst = const_cast<LPDIDEVICEINSTANCEW>(pInst);

			for(int i = 0; i < 4; i++)
			{
				if (FakeAPI_GamepadConfig(i)->bEnabled && IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID,pInst->guidProduct))
				{
					DWORD dwFakePIDVID = static_cast<DWORD>(MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID));

					FakeInst->guidProduct.Data1=dwFakePIDVID;
					FakeInst->guidProduct.Data2=0x0000;
					FakeInst->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(FakeInst->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pInst->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(FakeInst->guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					FakeInst->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					FakeInst->wUsage = 0x05;
					FakeInst->wUsagePage = 0x01;

					LPWSTR OldProductName = new WCHAR[MAX_PATH];
					LPWSTR OldInstanceName = new WCHAR[MAX_PATH];

					wcscpy_s(OldProductName,MAX_PATH,FakeInst->tszProductName);
					wcscpy_s(OldInstanceName,MAX_PATH,FakeInst->tszInstanceName);

					wcscpy_s(FakeInst->tszProductName, L"XBOX 360 For Windows (Controller)");
					wcscpy_s(FakeInst->tszInstanceName, L"XBOX 360 For Windows (Controller)");  

					WriteLog(L"[FAKEDI]  Product Name change from \"%s\" to \"%s\"",OldProductName,FakeInst->tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from \"%s\" to \"%s\"",OldInstanceName,FakeInst->tszInstanceName);

					SAFE_DELETE_ARRAY(OldProductName);
					SAFE_DELETE_ARRAY(OldInstanceName);

					return lpOriginalCallbackW(FakeInst,pContext);
				}
			}
		}
	}
	return lpOriginalCallbackW(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeEnumDevicesA (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(L"[FAKEDI]  FakeEnumDevicesA");

	if(!lpOriginalCallbackA) {
		lpOriginalCallbackA = lpCallback;
		hHookCallbackA = new HOOK_TRACE_INFO();

		LhInstallHook(lpOriginalCallbackA,FakeEnumCallbackA,static_cast<PVOID>(NULL),hHookCallbackA);
		LhSetInclusiveACL(ACLEntries, 1,hHookCallbackA );
	}

	return OriginalEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!FakeAPI_Config()->bEnabled || !lpCallback) return OriginalEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(L"[FAKEDI]  FakeEnumDevicesW");

	if(!lpOriginalCallbackW) {
		lpOriginalCallbackW = lpCallback;
		hHookCallbackW = new HOOK_TRACE_INFO();

		LhInstallHook(lpOriginalCallbackW,FakeEnumCallbackW,static_cast<PVOID>(NULL),hHookCallbackW);
		LhSetInclusiveACL(ACLEntries, 1,hHookCallbackW );
	}

	return OriginalEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeGetDeviceInfoA (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalGetDeviceInfoA ( This, pdidi );
	WriteLog(L"[FAKEDI]  FakeGetDeviceInfoA");

	HRESULT hr;
	hr = OriginalGetDeviceInfoA ( This, pdidi );

	// Fast return if keyboard or mouse
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(L"[FAKEDI]  FakeGetDeviceInfoA:: Keyboard detected");
		return hr;
	}
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(L"[FAKEDI]  FakeGetDeviceInfoA:: Mouse detected");
		return hr;
	}

	if(pdidi) {
		if(FakeAPI_Config()->dwFakeMode >= 2) {

			for(int i = 0; i < 4; i++) {
				if(FakeAPI_GamepadConfig(i)->bEnabled && IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID, pdidi->guidProduct)) {

					DWORD dwFakePIDVID = static_cast<DWORD>(MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID));
					pdidi->guidProduct.Data1=dwFakePIDVID;
					pdidi->guidProduct.Data2=0x0000;
					pdidi->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pdidi->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pdidi->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(pdidi->guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					pdidi->wUsage = 0x05;
					pdidi->wUsagePage = 0x01;

					LPSTR OldProductName = new CHAR[MAX_PATH];
					LPSTR OldInstanceName = new CHAR[MAX_PATH];

					strcpy_s(OldProductName,MAX_PATH,pdidi->tszProductName);
					strcpy_s(OldInstanceName,MAX_PATH,pdidi->tszInstanceName);

					strcpy_s(pdidi->tszProductName, "XBOX 360 For Windows (Controller)");
					strcpy_s(pdidi->tszInstanceName, "XBOX 360 For Windows (Controller)"); 

					WriteLog(L"[FAKEDI]  Product Name change from \"%hs\" to \"%hs\"",OldProductName,pdidi->tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName,pdidi->tszInstanceName);

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
HRESULT STDMETHODCALLTYPE FakeGetDeviceInfoW (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalGetDeviceInfoW ( This, pdidi );
	WriteLog(L"[FAKEDI]  FakeGetDeviceInfoW");

	HRESULT hr;
	hr = OriginalGetDeviceInfoW ( This, pdidi );

	// Fast return if keyboard or mouse
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(L"[FAKEDI]  FakeGetDeviceInfoW:: Keyboard detected");
		return hr;
	}
	if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(L"[FAKEDI]  FakeGetDeviceInfoW:: Mouse detected");
		return hr;
	}

	if(pdidi) {

		if(FakeAPI_Config()->dwFakeMode >= 2) {

			for(int i = 0; i < 4; i++) {
				if(FakeAPI_GamepadConfig(i)->bEnabled && FakeAPI_GamepadConfig(i)->productGUID.Data1 == pdidi->guidProduct.Data1) {
					

					DWORD dwFakePIDVID = static_cast<DWORD>(MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID));
					pdidi->guidProduct.Data1=dwFakePIDVID;
					pdidi->guidProduct.Data2=0x0000;
					pdidi->guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pdidi->guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pdidi->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(pdidi->guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					pdidi->wUsage = 0x05;
					pdidi->wUsagePage = 0x01;

					LPWSTR OldProductName = new WCHAR[MAX_PATH];
					LPWSTR OldInstanceName = new WCHAR[MAX_PATH];

					wcscpy_s(OldProductName,MAX_PATH,pdidi->tszProductName);
					wcscpy_s(OldInstanceName,MAX_PATH,pdidi->tszInstanceName);
					
					wcscpy_s(pdidi->tszProductName, L"XBOX 360 For Windows (Controller)");
					wcscpy_s(pdidi->tszInstanceName, L"XBOX 360 For Windows (Controller)");  

					WriteLog(L"[FAKEDI]  Product Name change from \"%s\" to \"%s\"",OldProductName,pdidi->tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from \"%s\" to \"%s\"",OldInstanceName,pdidi->tszInstanceName);

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
HRESULT STDMETHODCALLTYPE FakeGetPropertyA (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalGetPropertyA(This, rguidProp, pdiph);
	HRESULT hr;
	hr = OriginalGetPropertyA(This, rguidProp, pdiph);
	WriteLog(L"[FAKEDI]  FakeGetPropertyW");

	if (FakeAPI_Config()->dwFakeMode >= 2 ) {

		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwFakePIDVID = static_cast<DWORD>(MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID));
			DWORD dwOriginalPIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

			reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwFakePIDVID;
			WriteLog(L"[FAKEDI]  VIDPID change from %08X to %08X",dwOriginalPIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
		}

		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR OriginalName[MAX_PATH];
			wcscpy_s(OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

			swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(L"[FAKEDI]  Product Name change from %s to %s",OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeGetPropertyW (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalGetPropertyW(This, rguidProp, pdiph);
	HRESULT hr;
	hr = OriginalGetPropertyW(This, rguidProp, pdiph);
	WriteLog(L"[FAKEDI]  FakeGetPropertyW");

	if (FakeAPI_Config()->dwFakeMode >= 2 ) {

		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwFakePIDVID = static_cast<DWORD>(MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID));
			DWORD dwOriginalPIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

			reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwFakePIDVID;
			WriteLog(L"[FAKEDI]  VIDPID change from %08X to %08X",dwOriginalPIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
		}

		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR OriginalName[MAX_PATH];
			wcscpy_s(OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

			swprintf_s(reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(L"[FAKEDI]  Product Name change from %s to %s",OriginalName,reinterpret_cast<LPDIPROPSTRING>(pdiph)->wsz);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeCreateDeviceA (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalCreateDeviceA (This, rguid, lplpDirectInputDevice, pUnkOuter);
	WriteLog(L"[FAKEDI]  FakeCreateDeviceA");

	HRESULT hr;

	hr = OriginalCreateDeviceA (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(*lplpDirectInputDevice) {
		//WCHAR strDevGUID[50];
		//GUIDtoString(strDevGUID,&rguid);
		//WriteLog(_T("[FAKEDI]  Device GUID : %s"),strDevGUID);

		if(!OriginalGetDeviceInfoA) {
			OriginalGetDeviceInfoA = (*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo;
			hHookGetDeviceInfoA = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetDeviceInfoA,FakeGetDeviceInfoA,static_cast<PVOID>(NULL),hHookGetDeviceInfoA);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetDeviceInfoA );
		}
		if(!OriginalGetPropertyA) {
			OriginalGetPropertyA = (*lplpDirectInputDevice)->lpVtbl->GetProperty;
			hHookGetPropertyA = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetPropertyA,FakeGetPropertyA,static_cast<PVOID>(NULL),hHookGetPropertyA);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetPropertyA );
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeCreateDeviceW (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalCreateDeviceW (This, rguid, lplpDirectInputDevice, pUnkOuter);
	WriteLog(L"[FAKEDI]  FakeCreateDeviceW");

	HRESULT hr;

	hr = OriginalCreateDeviceW (This, rguid, lplpDirectInputDevice, pUnkOuter);

	//WCHAR strDevGUID[50];
	//GUIDtoString(strDevGUID,&rguid);
	//WriteLog(_T("[FAKEDI]  Device GUID : %s"),strDevGUID);

	if(*lplpDirectInputDevice) {
		if(!OriginalGetDeviceInfoW) {
			OriginalGetDeviceInfoW = (*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo;
			hHookGetDeviceInfoW = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetDeviceInfoW,FakeGetDeviceInfoW,static_cast<PVOID>(NULL),hHookGetDeviceInfoW);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetDeviceInfoW );
		}
		if(!OriginalGetPropertyW) {
			OriginalGetPropertyW = (*lplpDirectInputDevice)->lpVtbl->GetProperty;
			hHookGetPropertyW = new HOOK_TRACE_INFO();

			LhInstallHook(OriginalGetPropertyW,FakeGetPropertyW,static_cast<PVOID>(NULL),hHookGetPropertyW);
			LhSetInclusiveACL(ACLEntries, 1,hHookGetPropertyW );
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI FakeDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);
	WriteLog(L"[FAKEDI]  FakeDirectInput8Create");

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
				WriteLog(L"[FAKEDI]  FakeDirectInput8Create - ANSI interface");
				if(!OriginalCreateDeviceA) {
					OriginalCreateDeviceA = pDIA->lpVtbl->CreateDevice;
					hHookCreateDeviceA = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalCreateDeviceA,FakeCreateDeviceA,static_cast<PVOID>(NULL),hHookCreateDeviceA);
					LhSetInclusiveACL(ACLEntries, 1,hHookCreateDeviceA );
				}
				if(!OriginalEnumDevicesA)
				{
					OriginalEnumDevicesA = pDIA->lpVtbl->EnumDevices;
					hHookEnumDevicesA = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalEnumDevicesA,FakeEnumDevicesA,static_cast<PVOID>(NULL),hHookEnumDevicesA);
					LhSetInclusiveACL(ACLEntries, 1,hHookEnumDevicesA );
				}
			}
		}
		if (IsEqualIID(riidltf,IID_IDirectInput8W)) {
			LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);
			if(pDIW)  {
				WriteLog(L"[FAKEDI]  FakeDirectInput8Create - UNICODE interface");
				if(!OriginalCreateDeviceW) {
					OriginalCreateDeviceW = pDIW->lpVtbl->CreateDevice;
					hHookCreateDeviceW = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalCreateDeviceW,FakeCreateDeviceW,static_cast<PVOID>(NULL),hHookCreateDeviceW);
					LhSetInclusiveACL(ACLEntries, 1,hHookCreateDeviceW );
				}
				if(!OriginalEnumDevicesW)
				{
					OriginalEnumDevicesW = pDIW->lpVtbl->EnumDevices;
					hHookEnumDevicesW = new HOOK_TRACE_INFO();

					LhInstallHook(OriginalEnumDevicesW,FakeEnumDevicesW,static_cast<PVOID>(NULL),hHookEnumDevicesW);
					LhSetInclusiveACL(ACLEntries, 1,hHookEnumDevicesW );
				}
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FakeDI()
{
	if(!OriginalDirectInput8Create) {
		OriginalDirectInput8Create = DirectInput8Create;
		hHookDirectInput8Create = new HOOK_TRACE_INFO();
		WriteLog(L"[FAKEAPI] FakeDirectInput8Create:: Attaching");

		LhInstallHook(OriginalDirectInput8Create,FakeDirectInput8Create,static_cast<PVOID>(NULL),hHookDirectInput8Create);
		LhSetInclusiveACL(ACLEntries, 1,hHookDirectInput8Create );
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void FakeDIClean()
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
	SAFE_DELETE(hHookCallbackA);
	SAFE_DELETE(hHookCallbackW);

}