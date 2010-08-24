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

#include <detours.h>
#define CINTERFACE	//needed for detours
#include "DirectInput.h"

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
	WriteLog(_T("[FAKEDI]  FakeEnumCallbackA"));

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(_T("[FAKEDI]  FakeEnumCallbackA:: Keyboard detected"));
		return lpOriginalCallbackA(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(_T("[FAKEDI]  FakeEnumCallbackA:: Mouse detected"));
		return lpOriginalCallbackA(pInst,pContext);
	}

	if(wFakeMode == 3) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(wFakeMode == 2)
		{
			DIDEVICEINSTANCEA FakeInst;
			GUID fakeguid = pInst->guidProduct;

			for(int i = 0; i < 4; i++)
			{
				if (Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == fakeguid.Data1)
				{
					memcpy(&FakeInst,pInst,pInst->dwSize);

					DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					TCHAR strOriginalguidProduct[50];
					TCHAR strFakeguidProduct[50];
					GUIDtoString(strOriginalguidProduct,&pInst->guidProduct);
					GUIDtoString(strFakeguidProduct,&FakeInst.guidProduct);
					WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = 66069;
					FakeInst.wUsage = 5;
					FakeInst.wUsagePage = 1;

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
	WriteLog(_T("[FAKEDI]  FakeEnumCallbackW"));

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD)) {
		WriteLog(_T("[FAKEDI]  FakeEnumCallbackA:: Keyboard detected"));
		return lpOriginalCallbackW(pInst,pContext);
	}
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE)) {
		WriteLog(_T("[FAKEDI]  FakeEnumCallbackA:: Mouse detected"));
		return lpOriginalCallbackW(pInst,pContext);
	}

	if(wFakeMode == 3) return DIENUM_STOP;

	if(pInst && pInst->dwSize!=0)
	{
		if(wFakeMode == 2)
		{
			DIDEVICEINSTANCEW pFakeInst;
			GUID fakeguid = pInst->guidProduct;

			for(int i = 0; i < 4; i++)
			{
				if (Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == fakeguid.Data1)
				{
					memcpy(&pFakeInst,pInst,pInst->dwSize);

					DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

					pFakeInst.guidProduct.Data1=dwFakePIDVID;
					pFakeInst.guidProduct.Data2=0x0000;
					pFakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&pFakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					TCHAR strOriginalguidProduct[50];
					TCHAR strFakeguidProduct[50];
					GUIDtoString(strOriginalguidProduct,&pInst->guidProduct);
					GUIDtoString(strFakeguidProduct,&pFakeInst.guidProduct);
					WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

					pFakeInst.dwDevType = 66069;
					pFakeInst.wUsage = 5;
					pFakeInst.wUsagePage = 1;

					_stprintf_s(pFakeInst.tszProductName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
					WriteLog(_T("[FAKEDI]  Product Name change from %s to %s"),pInst->tszProductName,pFakeInst.tszProductName);
					_stprintf_s(pFakeInst.tszInstanceName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));      
					WriteLog(_T("[FAKEDI]  Instance Name change from %s to %s"),pInst->tszInstanceName,pFakeInst.tszInstanceName);

					return lpOriginalCallbackW(&pFakeInst,pContext);
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
	WriteLog(_T("[FAKEDI]  FakeEnumDevicesA"));
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
	WriteLog(_T("[FAKEDI]  FakeEnumDevicesW"));
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
	HRESULT hr;
	hr = OriginalGetDeviceInfoA ( This, pdidi );

	if(pdidi) {
		WriteLog(_T("[FAKEDI]  FakeGetDeviceInfoA"));

		if(wFakeMode >= 2) {
			for(int i = 0; i < 4; i++) {
				if(Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == pdidi->guidProduct.Data1) {
					DIDEVICEINSTANCEA OrigInst, FakeInst;
					memcpy(&OrigInst,pdidi,pdidi->dwSize);
					memcpy(&FakeInst,pdidi,pdidi->dwSize);

					DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					TCHAR strOriginalguidProduct[50];
					TCHAR strFakeguidProduct[50];
					GUIDtoString(strOriginalguidProduct,&OrigInst.guidProduct);
					GUIDtoString(strFakeguidProduct,&FakeInst.guidProduct);
					WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = 66069;
					FakeInst.wUsage = 5;
					FakeInst.wUsagePage = 1;

					sprintf_s(FakeInst.tszProductName, "%s", "XBOX 360 For Windows (Controller)");
					sprintf_s(FakeInst.tszInstanceName, "%s", "XBOX 360 For Windows (Controller)");

					WriteLog(L"[FAKEDI]  Product Name change from %S to %S",OrigInst.tszProductName,FakeInst.tszProductName);
					WriteLog(L"[FAKEDI]  Instance Name change from %S to %S",OrigInst.tszInstanceName,FakeInst.tszInstanceName);

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
	HRESULT hr;
	hr = OriginalGetDeviceInfoW ( This, pdidi );

	if(pdidi) {
		WriteLog(_T("[FAKEDI]  FakeGetDeviceInfoW"));

		if(wFakeMode >= 2) {
			for(int i = 0; i < 4; i++) {
				if(Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == pdidi->guidProduct.Data1) {
					DIDEVICEINSTANCEW OrigInst, FakeInst;						
					memcpy(&OrigInst,pdidi,pdidi->dwSize);
					memcpy(&FakeInst,pdidi,pdidi->dwSize);

					DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);
					FakeInst.guidProduct.Data1=dwFakePIDVID;
					FakeInst.guidProduct.Data2=0x0000;
					FakeInst.guidProduct.Data3=0x0000;
					BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
					memcpy(&FakeInst.guidProduct.Data4, pdata4, sizeof(pdata4));

					TCHAR strFakeguidProduct[50];
					TCHAR strOriginalguidProduct[50];
					GUIDtoString(strOriginalguidProduct,&OrigInst.guidProduct);
					GUIDtoString(strFakeguidProduct,&FakeInst.guidProduct);
					WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

					FakeInst.dwDevType = 66069;
					FakeInst.wUsage = 5;
					FakeInst.wUsagePage = 1;
					_stprintf_s(FakeInst.tszProductName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
					_stprintf_s(FakeInst.tszInstanceName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
					WriteLog(_T("[FAKEDI]  Product Name change from %s to %s"), OrigInst.tszProductName, FakeInst.tszProductName );
					WriteLog(_T("[FAKEDI]  Instance Name change from %s to %s"), OrigInst.tszInstanceName, FakeInst.tszInstanceName );

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
	HRESULT hr;
	hr = OriginalGetPropertyA(This, rguidProp, pdiph);
	WriteLog(_T("[FAKEDI]  FakeGetPropertyA"));

	if (wFakeMode >= 2 ) {
		if ( (&rguidProp == &DIPROP_VIDPID) ) {
			DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

			WriteLog(_T("[FAKEDI]  Original VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
			((LPDIPROPDWORD)pdiph)->dwData = dwFakePIDVID;
			WriteLog(_T("[FAKEDI]  Fake VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
		}
		if ( (&rguidProp == &DIPROP_PRODUCTNAME) ) {
			WriteLog(_T("[FAKEDI]  Original PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);
			_stprintf_s( ((LPDIPROPSTRING)pdiph)->wsz, _T("%s"), _T("XBOX 360 For Windows (Controller)") );
			WriteLog(_T("[FAKEDI]  Fake PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);

		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeGetPropertyW (LPDIRECTINPUTDEVICE8W This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	HRESULT hr;
	hr = OriginalGetPropertyW(This, rguidProp, pdiph);
	WriteLog(_T("[FAKEDI]  FakeGetPropertyW"));

	if (wFakeMode >= 2 ) {
		if ( (&rguidProp == &DIPROP_VIDPID)) {
			DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

			WriteLog(_T("[FAKEDI]  Original VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
			((LPDIPROPDWORD)pdiph)->dwData = dwFakePIDVID;
			WriteLog(_T("[FAKEDI]  Fake VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
		}
		if ((&rguidProp == &DIPROP_PRODUCTNAME)) {
			WriteLog(_T("[FAKEDI]  Original PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);
			_stprintf_s( ((LPDIPROPSTRING)pdiph)->wsz, _T("%s"), _T("XBOX 360 For Windows (Controller)") );
			WriteLog(_T("[FAKEDI]  Fake PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeCreateDeviceA (LPDIRECTINPUT8A This, REFGUID rguid, LPDIRECTINPUTDEVICE8A * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	HRESULT hr;
	LPDIRECTINPUTDEVICE8A pDID;

	hr = OriginalCreateDeviceA (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(lplpDirectInputDevice) {
		WriteLog(_T("[FAKEDI]  FakeCreateDeviceA"));
		//TCHAR strDevGUID[50];
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
	HRESULT hr;
	LPDIRECTINPUTDEVICE8W pDID;

	hr = OriginalCreateDeviceW (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(lplpDirectInputDevice) {
		WriteLog(_T("[FAKEDI]  FakeCreateDeviceW"));
		//TCHAR strDevGUID[50];
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

	HRESULT hr;

	/*
	LPOLESTR str1;
	StringFromIID(riidltf,&str1);
	WriteLog(_T("rclsid: %s"),str1);
	*/

	hr = OriginalDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(ppvOut) {
		if(IsEqualGUID(riidltf,IID_IDirectInput8A)) {
			LPDIRECTINPUT8A pDIA = (LPDIRECTINPUT8A) *ppvOut;
			if(pDIA)  {
				WriteLog(_T("[FAKEDI]  FakeDirectInput8Create - ANSI interface"));
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
		if (IsEqualGUID(riidltf,IID_IDirectInput8W)) {
			LPDIRECTINPUT8W pDIW = (LPDIRECTINPUT8W) *ppvOut;
			if(pDIW)  {
				WriteLog(_T("[FAKEDI]  FakeDirectInput8Create - UNICODE interface"));
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

		WriteLog(_T("[FAKEAPI] FakeDirectInput8Create:: Attaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)OriginalDirectInput8Create, FakeDirectInput8Create);
		DetourTransactionCommit();
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


void FakeDI_Detach()
{
	if(OriginalGetDeviceInfoA) {
		WriteLog(_T("[FAKEDI]  FakeGetDeviceInfoA:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetDeviceInfoA, FakeGetDeviceInfoA);
		DetourTransactionCommit();
	}
	if(OriginalGetDeviceInfoW) {
		WriteLog(_T("[FAKEDI]  FakeGetDeviceInfoW:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetDeviceInfoW, FakeGetDeviceInfoW);
		DetourTransactionCommit();
	}

	if(OriginalGetPropertyA) {
		WriteLog(_T("[FAKEDI]  FakeGetPropertyA:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetPropertyA, FakeGetPropertyA);
		DetourTransactionCommit();
	}
	if(OriginalGetPropertyW) {
		WriteLog(_T("[FAKEDI]  FakeGetPropertyW:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalGetPropertyW, FakeGetPropertyW);
		DetourTransactionCommit();
	}

	if(OriginalCreateDeviceA) {
		WriteLog(_T("[FAKEDI]  FakeCreateDeviceA:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalCreateDeviceA, FakeCreateDeviceA);
		DetourTransactionCommit();
	}
	if(OriginalCreateDeviceW) {
		WriteLog(_T("[FAKEDI]  FakeCreateDeviceW:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalCreateDeviceW, FakeCreateDeviceW);
		DetourTransactionCommit();
	}

	if(OriginalEnumDevicesA)
	{
		WriteLog(_T("[FAKEDI]  FakeEnumDevicesA:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalEnumDevicesA, FakeEnumDevicesA);
		DetourTransactionCommit();
	}
	if(OriginalEnumDevicesW)
	{
		WriteLog(_T("[FAKEDI]  FakeEnumDevicesW:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalEnumDevicesW, FakeEnumDevicesW);
		DetourTransactionCommit();
	}

	if(OriginalDirectInput8Create) {
		WriteLog(_T("[FAKEDI]  FakeDirectInput8Create:: Detaching"));
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourDetach(&(PVOID&)OriginalDirectInput8Create, FakeDirectInput8Create);
		DetourTransactionCommit();
	}
}
