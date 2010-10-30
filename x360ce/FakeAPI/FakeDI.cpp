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
#include "..\Utilities\Utils.h"

#include <detours.h>
#define CINTERFACE	//needed for detours
#include <dinput.h>

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
			DIDEVICEINSTANCEA FakeInst;

			for(int i = 0; i < 4; i++)
			{
				if (FakeAPI_GamepadConfig(i)->bEnabled && IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID,pInst->guidProduct))
				{
					memcpy(&FakeInst,pInst,pInst->dwSize);

					DWORD dwFakePIDVID = (DWORD) MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID);

					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pInst->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(FakeInst.guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					FakeInst.wUsage = 0x05;
					FakeInst.wUsagePage = 0x01;

					sprintf_s(FakeInst.tszProductName, "%s", "XBOX 360 For Windows (Controller)");
					sprintf_s(FakeInst.tszInstanceName, "%s", "XBOX 360 For Windows (Controller)"); 

					WriteLog(L"[FAKEDI]  Product Name change from %S to %S",&pInst->tszProductName,FakeInst.tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from %S to %S",&pInst->tszInstanceName,FakeInst.tszInstanceName);

					return lpOriginalCallbackA(&FakeInst,pContext);
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
			DIDEVICEINSTANCEW FakeInst;

			for(int i = 0; i < 4; i++)
			{
				if (FakeAPI_GamepadConfig(i)->bEnabled && IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID,pInst->guidProduct))
				{
					memcpy(&FakeInst,pInst,pInst->dwSize);

					DWORD dwFakePIDVID = (DWORD) MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID);

					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pInst->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(FakeInst.guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					FakeInst.wUsage = 0x05;
					FakeInst.wUsagePage = 0x01;

					swprintf_s(FakeInst.tszProductName, L"%s", L"XBOX 360 For Windows (Controller)");
					swprintf_s(FakeInst.tszInstanceName, L"%s", L"XBOX 360 For Windows (Controller)");  

					WriteLog(L"[FAKEDI]  Product Name change from %s to %s",pInst->tszProductName,FakeInst.tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from %s to %s",pInst->tszInstanceName,FakeInst.tszInstanceName);

					return lpOriginalCallbackW(&FakeInst,pContext);
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

	if (lpCallback)
	{
		lpOriginalCallbackA= lpCallback;
		return OriginalEnumDevicesA(This,dwDevType,&FakeEnumCallbackA,pvRef,dwFlags);
	}
	return OriginalEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(!FakeAPI_Config()->bEnabled) return OriginalEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
	WriteLog(L"[FAKEDI]  FakeEnumDevicesW");

	if (lpCallback)
	{
		lpOriginalCallbackW= lpCallback;
		return OriginalEnumDevicesW(This,dwDevType,&FakeEnumCallbackW,pvRef,dwFlags);
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

			DIDEVICEINSTANCEA FakeInst;

			for(int i = 0; i < 4; i++) {
				if(FakeAPI_GamepadConfig(i)->bEnabled && IsEqualGUID(FakeAPI_GamepadConfig(i)->productGUID, pdidi->guidProduct)) {

					memcpy(&FakeInst,pdidi,pdidi->dwSize);

					DWORD dwFakePIDVID = (DWORD) MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID);

					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pdidi->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(FakeInst.guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					FakeInst.wUsage = 0x05;
					FakeInst.wUsagePage = 0x01;

					sprintf_s(FakeInst.tszProductName, "%s", "XBOX 360 For Windows (Controller)");
					sprintf_s(FakeInst.tszInstanceName, "%s", "XBOX 360 For Windows (Controller)"); 

					WriteLog(L"[FAKEDI]  Product Name change from %S to %S",&pdidi->tszProductName,FakeInst.tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from %S to %S",&pdidi->tszInstanceName,FakeInst.tszInstanceName);

					memcpy(pdidi,&FakeInst,FakeInst.dwSize);
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
			DIDEVICEINSTANCEW FakeInst;

			for(int i = 0; i < 4; i++) {
				if(FakeAPI_GamepadConfig(i)->bEnabled && FakeAPI_GamepadConfig(i)->productGUID.Data1 == pdidi->guidProduct.Data1) {
					
					memcpy(&FakeInst,pdidi,pdidi->dwSize);

					DWORD dwFakePIDVID = (DWORD) MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID);

					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					WCHAR strOriginalguidProduct[50];
					WCHAR strFakeguidProduct[50];
					GUIDtoString(pdidi->guidProduct,strOriginalguidProduct,50);
					GUIDtoString(FakeInst.guidProduct,strFakeguidProduct,50);
					WriteLog(L"[FAKEDI]  GUID change from %s to %s",strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
					FakeInst.wUsage = 0x05;
					FakeInst.wUsagePage = 0x01;

					swprintf_s(FakeInst.tszProductName, L"%s", L"XBOX 360 For Windows (Controller)");
					swprintf_s(FakeInst.tszInstanceName, L"%s", L"XBOX 360 For Windows (Controller)");  

					WriteLog(L"[FAKEDI]  Product Name change from %s to %s",pdidi->tszProductName,FakeInst.tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from %s to %s",pdidi->tszInstanceName,FakeInst.tszInstanceName);

					memcpy(pdidi,&FakeInst,FakeInst.dwSize);
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
	WriteLog(L"[FAKEDI]  FakeGetPropertyA");

	HRESULT hr;
	hr = OriginalGetPropertyA(This, rguidProp, pdiph);

	if (FakeAPI_Config()->dwFakeMode >= 2 ) {

		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwFakePIDVID = (DWORD) MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID);
			DWORD dwOriginalPIDVID = ((LPDIPROPDWORD)pdiph)->dwData;

			((LPDIPROPDWORD)pdiph)->dwData = dwFakePIDVID;
			WriteLog(L"[FAKEDI]  VIDPID change from %08X to %08X",dwOriginalPIDVID,((LPDIPROPDWORD)pdiph)->dwData);
		}

		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR OriginalName[MAX_PATH];
			wcscpy_s(OriginalName,((LPDIPROPSTRING)pdiph)->wsz);

			swprintf_s( ((LPDIPROPSTRING)pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(L"[FAKEDI]  Product Name change from %s to %s",OriginalName,((LPDIPROPSTRING)pdiph)->wsz);

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
			DWORD dwFakePIDVID = (DWORD) MAKELONG(FakeAPI_Config()->dwFakeVID,FakeAPI_Config()->dwFakePID);
			DWORD dwOriginalPIDVID = ((LPDIPROPDWORD)pdiph)->dwData;

			((LPDIPROPDWORD)pdiph)->dwData = dwFakePIDVID;
			WriteLog(L"[FAKEDI]  VIDPID change from %08X to %08X",dwOriginalPIDVID,((LPDIPROPDWORD)pdiph)->dwData);
		}

		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WCHAR OriginalName[MAX_PATH];
			wcscpy_s(OriginalName,((LPDIPROPSTRING)pdiph)->wsz);

			swprintf_s( ((LPDIPROPSTRING)pdiph)->wsz, L"%s", L"XBOX 360 For Windows (Controller)" );
			WriteLog(L"[FAKEDI]  Product Name change from %s to %s",OriginalName,((LPDIPROPSTRING)pdiph)->wsz);

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
	LPDIRECTINPUTDEVICE8A pDID;

	hr = OriginalCreateDeviceA (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(lplpDirectInputDevice) {
		//WCHAR strDevGUID[50];
		//GUIDtoString(strDevGUID,&rguid);
		//WriteLog(_T("[FAKEDI]  Device GUID : %s"),strDevGUID);
		pDID = (LPDIRECTINPUTDEVICE8A) *lplpDirectInputDevice;
		if(pDID) {
			if(!OriginalGetDeviceInfoA) {
				OriginalGetDeviceInfoA = pDID->lpVtbl->GetDeviceInfo;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGetDeviceInfoA, FakeGetDeviceInfoA);
				DetourTransactionCommit();
			}
			if(!OriginalGetPropertyA) {
				OriginalGetPropertyA = pDID->lpVtbl->GetProperty;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGetPropertyA, FakeGetPropertyA);
				DetourTransactionCommit();
			}
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
	LPDIRECTINPUTDEVICE8W pDID;

	hr = OriginalCreateDeviceW (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(lplpDirectInputDevice) {
		//WCHAR strDevGUID[50];
		//GUIDtoString(strDevGUID,&rguid);
		//WriteLog(_T("[FAKEDI]  Device GUID : %s"),strDevGUID);
		pDID = (LPDIRECTINPUTDEVICE8W) *lplpDirectInputDevice;
		if(pDID) {
			if(!OriginalGetDeviceInfoW) {
				OriginalGetDeviceInfoW = pDID->lpVtbl->GetDeviceInfo;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGetDeviceInfoW, FakeGetDeviceInfoW);
				DetourTransactionCommit();
			}
			if(!OriginalGetPropertyW) {
				OriginalGetPropertyW = pDID->lpVtbl->GetProperty;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGetPropertyW, FakeGetPropertyW);
				DetourTransactionCommit();
			}
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI FakeDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	//if(!FakeAPI_Config()->bEnabled) return OriginalDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);
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
			LPDIRECTINPUT8A pDIA = (LPDIRECTINPUT8A) *ppvOut;
			if(pDIA)  {
				WriteLog(L"[FAKEDI]  FakeDirectInput8Create - ANSI interface");
				if(!OriginalCreateDeviceA) {
					OriginalCreateDeviceA = pDIA->lpVtbl->CreateDevice;
					DetourTransactionBegin();
					DetourUpdateThread(GetCurrentThread());
					DetourAttach(&(PVOID&)OriginalCreateDeviceA, FakeCreateDeviceA);
					DetourTransactionCommit();
				}
				if(!OriginalEnumDevicesA)
				{
					OriginalEnumDevicesA = pDIA->lpVtbl->EnumDevices;
					DetourTransactionBegin();
					DetourUpdateThread(GetCurrentThread());
					DetourAttach(&(PVOID&)OriginalEnumDevicesA, FakeEnumDevicesA);
					DetourTransactionCommit();
				}
			}
		}
		if (IsEqualIID(riidltf,IID_IDirectInput8W)) {
			LPDIRECTINPUT8W pDIW = (LPDIRECTINPUT8W) *ppvOut;
			if(pDIW)  {
				WriteLog(L"[FAKEDI]  FakeDirectInput8Create - UNICODE interface");
				if(!OriginalCreateDeviceW) {
					OriginalCreateDeviceW = pDIW->lpVtbl->CreateDevice;
					DetourTransactionBegin();
					DetourUpdateThread(GetCurrentThread());
					DetourAttach(&(PVOID&)OriginalCreateDeviceW, FakeCreateDeviceW);
					DetourTransactionCommit();
				}
				if(!OriginalEnumDevicesW)
				{
					OriginalEnumDevicesW = pDIW->lpVtbl->EnumDevices;
					DetourTransactionBegin();
					DetourUpdateThread(GetCurrentThread());
					DetourAttach(&(PVOID&)OriginalEnumDevicesW, FakeEnumDevicesW);
					DetourTransactionCommit();
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

		WriteLog(L"[FAKEAPI] FakeDirectInput8Create:: Attaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)OriginalDirectInput8Create, FakeDirectInput8Create);
		DetourTransactionCommit();
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


void FakeDI_Clean()
{
	if(OriginalGetDeviceInfoA) {
		WriteLog(L"[FAKEDI]  FakeGetDeviceInfoA:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetDeviceInfoA, FakeGetDeviceInfoA);
		DetourTransactionCommit();
	}
	if(OriginalGetDeviceInfoW) {
		WriteLog(L"[FAKEDI]  FakeGetDeviceInfoW:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetDeviceInfoW, FakeGetDeviceInfoW);
		DetourTransactionCommit();
	}

	if(OriginalGetPropertyA) {
		WriteLog(L"[FAKEDI]  FakeGetPropertyA:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetPropertyA, FakeGetPropertyA);
		DetourTransactionCommit();
	}
	if(OriginalGetPropertyW) {
		WriteLog(L"[FAKEDI]  FakeGetPropertyW:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetPropertyW, FakeGetPropertyW);
		DetourTransactionCommit();
	}

	if(OriginalCreateDeviceA) {
		WriteLog(L"[FAKEDI]  FakeCreateDeviceA:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalCreateDeviceA, FakeCreateDeviceA);
		DetourTransactionCommit();
	}
	if(OriginalCreateDeviceW) {
		WriteLog(L"[FAKEDI]  FakeCreateDeviceW:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalCreateDeviceW, FakeCreateDeviceW);
		DetourTransactionCommit();
	}

	if(OriginalEnumDevicesA)
	{
		WriteLog(L"[FAKEDI]  FakeEnumDevicesA:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalEnumDevicesA, FakeEnumDevicesA);
		DetourTransactionCommit();
	}
	if(OriginalEnumDevicesW)
	{
		WriteLog(L"[FAKEDI]  FakeEnumDevicesW:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalEnumDevicesW, FakeEnumDevicesW);
		DetourTransactionCommit();
	}

	if(OriginalDirectInput8Create) {
		WriteLog(L"[FAKEDI]  FakeDirectInput8Create:: Detaching");
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalDirectInput8Create, FakeDirectInput8Create);
		DetourTransactionCommit();
	}
}
