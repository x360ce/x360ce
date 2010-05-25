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

LPDIENUMDEVICESCALLBACK lpOldCallback= NULL;

BOOL CALLBACK FakeEnumCallback( const DIDEVICEINSTANCE* pInst,VOID* pContext )
{

	// check for magic [x360ce], if is then this is a x360ce dll process
	if (pContext != NULL && _tcsstr( (TCHAR*)pContext, _T("[x360ce]") ))
	{
		return lpOldCallback(pInst,pContext);
	}

	if(pInst && pInst->dwSize!=0)
	{
		WriteLog(_T("[FAKEDI] FakeEnumCallback"));

		if(wFakeDI >= 1)
		{
			if(sizeof(DIDEVICEINSTANCEA) ==  pInst->dwSize) 	//ANSI or UNICODE ?
			{
				WriteLog(_T("[FAKEDI] FakeEnumCallback:: Using ANSI"));
				DIDEVICEINSTANCEA FakeInst;
				DIDEVICEINSTANCEA ANSIInst;

				memcpy(&ANSIInst,pInst,pInst->dwSize);

				GUID fakeguid = pInst->guidProduct;

				for(int i = 0; i < 4; i++)
				{
					if (Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == fakeguid.Data1)
					{
						memcpy(&FakeInst,&ANSIInst,ANSIInst.dwSize);

						DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

						FakeInst.guidProduct.Data1=dwFakePIDVID;
						FakeInst.guidProduct.Data2=0x0000;
						FakeInst.guidProduct.Data3=0x0000;
						BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
						memcpy(&FakeInst.guidProduct.Data4, pdata4, 8);

						TCHAR strOldguidProduct[50];
						TCHAR strNewguidProduct[50];
						GUIDtoString(strOldguidProduct,&ANSIInst.guidProduct);
						GUIDtoString(strNewguidProduct,&FakeInst.guidProduct);
						WriteLog(_T("[FAKEDI] GUID change from %s to %s"),strOldguidProduct,strNewguidProduct);

						FakeInst.dwDevType = 66069;
						FakeInst.wUsage = 5;
						FakeInst.wUsagePage = 1;

						sprintf_s(FakeInst.tszProductName, "%s", "XBOX 360 For Windows (Controller)");
						sprintf_s(FakeInst.tszInstanceName, "%s", "XBOX 360 For Windows (Controller)"); 

						/*TODO, need ansi to unicode conversion
						WriteLog(_T("Product Name change from %s to %s"),ANSIInst.tszProductName,FakeInst.tszProductName);
						WriteLog(_T("Instance Name change from %s to %s"),ANSIInst.tszInstanceName,FakeInst.tszInstanceName);
						*/

						return lpOldCallback((DIDEVICEINSTANCEW*) &FakeInst,pContext);
					}
				}
			}

			else
			{
				WriteLog(_T("[FAKEDI] FakeEnumCallback:: Using UNICODE"));
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
						memcpy(&pFakeInst.guidProduct.Data4, pdata4, 8);

						TCHAR strOldguidProduct[50];
						TCHAR strNewguidProduct[50];
						GUIDtoString(strOldguidProduct,&pInst->guidProduct);
						GUIDtoString(strNewguidProduct,&pFakeInst.guidProduct);
						WriteLog(_T("[FAKEDI] GUID change from %s to %s"),strOldguidProduct,strNewguidProduct);

						pFakeInst.dwDevType = 66069;
						pFakeInst.wUsage = 5;
						pFakeInst.wUsagePage = 1;

						_stprintf_s(pFakeInst.tszProductName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
						WriteLog(_T("[FAKEDI] Product Name change from %s to %s"),pInst->tszProductName,pFakeInst.tszProductName);
						_stprintf_s(pFakeInst.tszInstanceName, _T("%s"), _T("XBOX 360 For Windows (Controller)")); 	
						WriteLog(_T("[FAKEDI] Instance Name change from %s to %s"),pInst->tszInstanceName,pFakeInst.tszInstanceName);

						return lpOldCallback(&pFakeInst,pContext);
					}
				}
			}
		}
	}

	return lpOldCallback(pInst,pContext);
}


HRESULT (STDMETHODCALLTYPE *OldEnumDevices) (LPDIRECTINPUT8 This, DWORD dwDevType,LPDIENUMDEVICESCALLBACK lpCallback,LPVOID pvRef,DWORD dwFlags) = NULL;

HRESULT STDMETHODCALLTYPE NewEnumDevices (LPDIRECTINPUT8 This, DWORD dwDevType,LPDIENUMDEVICESCALLBACK lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if (lpCallback != NULL)
	{
		lpOldCallback= lpCallback;
		return OldEnumDevices(This,dwDevType,FakeEnumCallback,pvRef,dwFlags);
	}
	return OldEnumDevices(This,dwDevType,lpCallback,pvRef,dwFlags);
}

HRESULT (STDMETHODCALLTYPE *OldGetDeviceInfo) (LPDIRECTINPUTDEVICE8 This, LPDIDEVICEINSTANCE pdidi) = NULL;

HRESULT STDMETHODCALLTYPE NewGetDeviceInfo (LPDIRECTINPUTDEVICE8 This, LPDIDEVICEINSTANCE pdidi)
{
	HRESULT hr;
	hr = OldGetDeviceInfo ( This, pdidi );
	if(pdidi != NULL)
	{
		WriteLog(_T("[FAKEDI] NewGetDeviceInfo"));

		if(wFakeDI >= 2)
		{
			//ANSI or UNICODE ?
			if(sizeof(DIDEVICEINSTANCEA) ==  pdidi->dwSize)					//ANSI
			{
				WriteLog(_T("[FAKEDI] NewGetDeviceInfo:: Using ANSI"));
				DIDEVICEINSTANCEA Fakepdidi;
				DIDEVICEINSTANCEA ANSIpdidi;

				memcpy(&ANSIpdidi,pdidi,pdidi->dwSize);

				for(int i = 0; i < 4; i++)
				{
					if(Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == pdidi->guidProduct.Data1)
					{

						memcpy(&Fakepdidi,&ANSIpdidi,ANSIpdidi.dwSize);

						DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

						Fakepdidi.guidProduct.Data1=dwFakePIDVID;
						Fakepdidi.guidProduct.Data2=0x0000;
						Fakepdidi.guidProduct.Data3=0x0000;
						BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
						memcpy(&Fakepdidi.guidProduct.Data4, pdata4, 8);

						TCHAR strOldguidProduct[50];
						TCHAR strNewguidProduct[50];
						GUIDtoString(strOldguidProduct,&Fakepdidi.guidProduct);
						GUIDtoString(strNewguidProduct,&Fakepdidi.guidProduct);
						WriteLog(_T("[FAKEDI] GUID change from %s to %s"),strOldguidProduct,strNewguidProduct);

						Fakepdidi.dwDevType = 66069;
						Fakepdidi.wUsage = 5;
						Fakepdidi.wUsagePage = 1;

						sprintf_s(Fakepdidi.tszProductName, "%s", "XBOX 360 For Windows (Controller)");
						sprintf_s(Fakepdidi.tszInstanceName, "%s", "XBOX 360 For Windows (Controller)"); 

						/*TODO, need ansi to unicode conversion
						WriteLog(_T("Product Name change from %s to %s"),ANSIInst.tszProductName,FakeInst.tszProductName);
						WriteLog(_T("Instance Name change from %s to %s"),ANSIInst.tszInstanceName,FakeInst.tszInstanceName);
						*/

						hr=DI_OK;
						memcpy(pdidi,&Fakepdidi,Fakepdidi.dwSize);

					}
				}
			}
			else															//UNICODE
			{
				WriteLog(_T("[FAKEDI] NewGetDeviceInfo:: Using UNICODE"));
				for(int i = 0; i < 4; i++)
				{
					if(Gamepad[i].product.Data1 != NULL && Gamepad[i].product.Data1 == pdidi->guidProduct.Data1)
					{

						DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

						pdidi->guidProduct.Data1=dwFakePIDVID;
						pdidi->guidProduct.Data2=0x0000;
						pdidi->guidProduct.Data3=0x0000;
						BYTE pdata4[8] = {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44};
						memcpy(&(pdidi->guidProduct.Data4), pdata4, 8);

						TCHAR strNewguidProduct[50];
						TCHAR strOldguidProduct[50];
						GUIDtoString(strOldguidProduct,&pdidi->guidProduct);
						GUIDtoString(strNewguidProduct,&pdidi->guidProduct);
						WriteLog(_T("[FAKEDI] GUID change from %s to %s"),strOldguidProduct,strNewguidProduct);

						pdidi->dwDevType = 66069;
						pdidi->wUsage = 5;
						pdidi->wUsagePage = 1;
						WriteLog(_T("[FAKEDI] Product Name change from %s"), pdidi->tszProductName );
						_stprintf_s(pdidi->tszProductName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
						WriteLog(_T("to %s"), pdidi->tszProductName );
						WriteLog(_T("[FAKEDI] Instance Name change from %s"), pdidi->tszInstanceName );
						_stprintf_s(pdidi->tszInstanceName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
						WriteLog(_T("[FAKEDI] to %s"), pdidi->tszInstanceName );

						hr=DI_OK;
					}
				}
			}
		}
	}
	return hr;
}

HRESULT (STDMETHODCALLTYPE *OldGetProperty) (LPDIRECTINPUTDEVICE8 This, REFGUID rguidProp, LPDIPROPHEADER pdiph) = NULL;

HRESULT STDMETHODCALLTYPE NewGetProperty (LPDIRECTINPUTDEVICE8 This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	HRESULT hr;
	hr = OldGetProperty (This, rguidProp, pdiph);
	WriteLog(_T("[FAKEDI] NewGetProperty"));

	if ( (&rguidProp==&DIPROP_VIDPID) )
	{
		DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

		WriteLog(_T("[FAKEDI] Old VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
		((LPDIPROPDWORD)pdiph)->dwData = dwFakePIDVID;
		WriteLog(_T("[FAKEDI] New VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
	}
	if ( (&rguidProp==&DIPROP_PRODUCTNAME) )
	{
		WriteLog(_T("[FAKEDI] Old PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);
		_stprintf_s( ((LPDIPROPSTRING)pdiph)->wsz, _T("%s"), _T("XBOX 360 For Windows (Controller)") );
		WriteLog(_T("[FAKEDI] New PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);

	}
	return hr;
}

HRESULT (STDMETHODCALLTYPE *OldCreateDevice) (LPDIRECTINPUT8 This, REFGUID rguid, LPDIRECTINPUTDEVICE8 *lplpDirectInputDevice, LPUNKNOWN pUnkOuter) = NULL;

HRESULT STDMETHODCALLTYPE NewCreateDevice (LPDIRECTINPUT8 This, REFGUID rguid, LPDIRECTINPUTDEVICE8 * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	HRESULT hr;
	LPDIRECTINPUTDEVICE8 pDID;

	hr = OldCreateDevice (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(lplpDirectInputDevice != NULL)
	{
		WriteLog(_T("[FAKEDI] NewCreateDevice"));
		pDID = (LPDIRECTINPUTDEVICE8) *lplpDirectInputDevice;
		if(pDID != NULL)
		{
			if(OldGetDeviceInfo == NULL)
			{
				OldGetDeviceInfo = pDID->lpVtbl->GetDeviceInfo;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldGetDeviceInfo, NewGetDeviceInfo);
				DetourTransactionCommit();
			}
			if(OldGetProperty == NULL)
			{
				OldGetProperty = pDID->lpVtbl->GetProperty;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldGetProperty, NewGetProperty);
				DetourTransactionCommit();
			}
		}
	}
	return hr;
}

HRESULT (WINAPI *OldDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter) = DirectInput8Create;

HRESULT WINAPI NewDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{

	HRESULT hr;
	LPDIRECTINPUT8 pDI;

	hr = OldDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(ppvOut != NULL) 
	{
		WriteLog(_T("[FAKEDI] NewDirectInput8Create"));

		pDI = (LPDIRECTINPUT8) *ppvOut;
		if(pDI != NULL) 
		{
			if(OldEnumDevices == NULL) 
			{
				OldEnumDevices = pDI->lpVtbl->EnumDevices;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldEnumDevices, NewEnumDevices);
				DetourTransactionCommit();
			}
			if(OldCreateDevice == NULL)
			{
				OldCreateDevice = pDI->lpVtbl->CreateDevice;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OldCreateDevice, NewCreateDevice);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}

void FakeDI()
{
	WriteLog(_T("[FAKEAPI] FakeDI"));

	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)OldDirectInput8Create, NewDirectInput8Create);
	DetourTransactionCommit();

}
