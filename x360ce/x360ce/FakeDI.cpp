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
#include "FakeDI.h"

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT (WINAPI *OriginalDirectInput8Create)(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter) = DirectInput8Create;
HRESULT (STDMETHODCALLTYPE *OriginalCreateDevice) (LPDIRECTINPUT8 This, REFGUID rguid, LPDIRECTINPUTDEVICE8 *lplpDirectInputDevice, LPUNKNOWN pUnkOuter) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalGetProperty) (LPDIRECTINPUTDEVICE8 This, REFGUID rguidProp, LPDIPROPHEADER pdiph) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalGetDeviceInfo) (LPDIRECTINPUTDEVICE8 This, LPDIDEVICEINSTANCE pdidi) = NULL;
HRESULT (STDMETHODCALLTYPE *OriginalEnumDevices) (LPDIRECTINPUT8 This, DWORD dwDevType,LPDIENUMDEVICESCALLBACK lpCallback,LPVOID pvRef,DWORD dwFlags) = NULL;
LPDIENUMDEVICESCALLBACK lpOriginalCallback= NULL;
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL CALLBACK FakeEnumCallback( const DIDEVICEINSTANCE* pInst,VOID* pContext )
{
	WriteLog(_T("[FAKEDI]  FakeEnumCallback"));

	if (pContext)
	{
		DINPUT_GAMEPAD * check = (DINPUT_GAMEPAD*) pContext;
		if (check->id1 == X360CE_ID1 && check->id2 == X360CE_ID2){
			WriteLog(_T("[FAKEDI]  FakeEnumCallback:: x360ce detected"));
			return lpOriginalCallback(pInst,pContext);
		}
	}


	if(pInst && pInst->dwSize!=0)
	{
		if(wFakeDI >= 1)
		{
			if(sizeof(DIDEVICEINSTANCEA) ==  pInst->dwSize) 	//ANSI or UNICODE ?
			{
				WriteLog(_T("[FAKEDI]  FakeEnumCallback:: Using ANSI"));
				DIDEVICEINSTANCEA ANSIInst;

				memcpy(&ANSIInst,pInst,pInst->dwSize);
				DWORD type = !ANSIInst.dwDevType;

				if ((wFakeDI == 3) && !(type == DI8DEVTYPE_KEYBOARD) && !(ANSIInst.dwDevType == DI8DEVTYPE_MOUSE) ) return DIENUM_STOP;

				DIDEVICEINSTANCEA FakeInst;
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

						TCHAR strOriginalguidProduct[50];
						TCHAR strFakeguidProduct[50];
						GUIDtoString(strOriginalguidProduct,&ANSIInst.guidProduct);
						GUIDtoString(strFakeguidProduct,&FakeInst.guidProduct);
						WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

						FakeInst.dwDevType = 66069;
						FakeInst.wUsage = 5;
						FakeInst.wUsagePage = 1;

						sprintf_s(FakeInst.tszProductName, "%s", "XBOX 360 For Windows (Controller)");
						sprintf_s(FakeInst.tszInstanceName, "%s", "XBOX 360 For Windows (Controller)"); 

						/*TODO, need ansi to unicode conversion
						WriteLog(_T("Product Name change from %s to %s"),ANSIInst.tszProductName,FakeInst.tszProductName);
						WriteLog(_T("Instance Name change from %s to %s"),ANSIInst.tszInstanceName,FakeInst.tszInstanceName);
						*/

						return lpOriginalCallback((DIDEVICEINSTANCEW*) &FakeInst,pContext);
					}
				}
			}

			else
			{
				WriteLog(_T("[FAKEDI]  FakeEnumCallback:: Using UNICODE"));

				if ((wFakeDI == 3) && !(pInst->dwDevType == DI8DEVTYPE_KEYBOARD) && !(pInst->dwDevType == DI8DEVTYPE_MOUSE) ) return DIENUM_STOP;

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

						return lpOriginalCallback(&pFakeInst,pContext);
					}
				}
			}
		}
	}

	return lpOriginalCallback(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeEnumDevices (LPDIRECTINPUT8 This, DWORD dwDevType,LPDIENUMDEVICESCALLBACK lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if (lpCallback != NULL)
	{
		lpOriginalCallback= lpCallback;
		return OriginalEnumDevices(This,dwDevType,FakeEnumCallback,pvRef,dwFlags);
	}
	return OriginalEnumDevices(This,dwDevType,lpCallback,pvRef,dwFlags);
}

HRESULT STDMETHODCALLTYPE FakeGetDeviceInfo (LPDIRECTINPUTDEVICE8 This, LPDIDEVICEINSTANCE pdidi)
{
	HRESULT hr;
	hr = OriginalGetDeviceInfo ( This, pdidi );

	if(pdidi != NULL)
	{
		WriteLog(_T("[FAKEDI]  FakeGetDeviceInfo"));

		if((wFakeDI >= 2) && (wFakeDI != 3))
		{
			//ANSI or UNICODE ?
			if(sizeof(DIDEVICEINSTANCEA) ==  pdidi->dwSize)					//ANSI
			{
				WriteLog(_T("[FAKEDI]  FakeGetDeviceInfo:: Using ANSI"));
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

						TCHAR strOriginalguidProduct[50];
						TCHAR strFakeguidProduct[50];
						GUIDtoString(strOriginalguidProduct,&Fakepdidi.guidProduct);
						GUIDtoString(strFakeguidProduct,&Fakepdidi.guidProduct);
						WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

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
				WriteLog(_T("[FAKEDI]  FakeGetDeviceInfo:: Using UNICODE"));
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

						TCHAR strFakeguidProduct[50];
						TCHAR strOriginalguidProduct[50];
						GUIDtoString(strOriginalguidProduct,&pdidi->guidProduct);
						GUIDtoString(strFakeguidProduct,&pdidi->guidProduct);
						WriteLog(_T("[FAKEDI]  GUID change from %s to %s"),strOriginalguidProduct,strFakeguidProduct);

						pdidi->dwDevType = 66069;
						pdidi->wUsage = 5;
						pdidi->wUsagePage = 1;
						WriteLog(_T("[FAKEDI]  Product Name change from %s"), pdidi->tszProductName );
						_stprintf_s(pdidi->tszProductName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
						WriteLog(_T("to %s"), pdidi->tszProductName );
						WriteLog(_T("[FAKEDI]  Instance Name change from %s"), pdidi->tszInstanceName );
						_stprintf_s(pdidi->tszInstanceName, _T("%s"), _T("XBOX 360 For Windows (Controller)"));
						WriteLog(_T("[FAKEDI]  to %s"), pdidi->tszInstanceName );

						hr=DI_OK;
					}
				}
			}
		}
	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeGetProperty (LPDIRECTINPUTDEVICE8 This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	HRESULT hr;
	hr = OriginalGetProperty (This, rguidProp, pdiph);
	WriteLog(_T("[FAKEDI]  FakeGetProperty"));

	if ( (&rguidProp==&DIPROP_VIDPID) )
	{
		DWORD dwFakePIDVID = MAKELONG(wFakeVID,wFakePID);

		WriteLog(_T("[FAKEDI]  Original VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
		((LPDIPROPDWORD)pdiph)->dwData = dwFakePIDVID;
		WriteLog(_T("[FAKEDI]  Fake VIDPID = %08X"),((LPDIPROPDWORD)pdiph)->dwData);
	}
	if ( (&rguidProp==&DIPROP_PRODUCTNAME) )
	{
		WriteLog(_T("[FAKEDI]  Original PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);
		_stprintf_s( ((LPDIPROPSTRING)pdiph)->wsz, _T("%s"), _T("XBOX 360 For Windows (Controller)") );
		WriteLog(_T("[FAKEDI]  Fake PRODUCTNAME = %s"),((LPDIPROPSTRING)pdiph)->wsz);

	}
	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE FakeCreateDevice (LPDIRECTINPUT8 This, REFGUID rguid, LPDIRECTINPUTDEVICE8 * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	HRESULT hr;
	LPDIRECTINPUTDEVICE8 pDID;

	hr = OriginalCreateDevice (This, rguid, lplpDirectInputDevice, pUnkOuter);
	if(lplpDirectInputDevice != NULL)
	{
		WriteLog(_T("[FAKEDI]  FakeCreateDevice"));
		pDID = (LPDIRECTINPUTDEVICE8) *lplpDirectInputDevice;
		if(pDID != NULL)
		{
			if(OriginalGetDeviceInfo == NULL)
			{
				OriginalGetDeviceInfo = pDID->lpVtbl->GetDeviceInfo;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGetDeviceInfo, FakeGetDeviceInfo);
				DetourTransactionCommit();
			}
			if(OriginalGetProperty == NULL)
			{
				OriginalGetProperty = pDID->lpVtbl->GetProperty;
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalGetProperty, FakeGetProperty);
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
	LPDIRECTINPUT8 pDI;

	hr = OriginalDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(ppvOut != NULL) 
	{
		WriteLog(_T("[FAKEDI]  FakeDirectInput8Create"));

		pDI = (LPDIRECTINPUT8) *ppvOut;
		if(pDI != NULL) 
		{
			if(OriginalEnumDevices == NULL) 
			{
				OriginalEnumDevices = pDI->lpVtbl->EnumDevices;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalEnumDevices, FakeEnumDevices);
				DetourTransactionCommit();
			}
			if(OriginalCreateDevice == NULL)
			{
				OriginalCreateDevice = pDI->lpVtbl->CreateDevice;

				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());
				DetourAttach(&(PVOID&)OriginalCreateDevice, FakeCreateDevice);
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void FakeDI(bool state)
{
	WriteLog(_T("[FAKEAPI] FakeDI(%d)"),state);
	if(state){
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourAttach(&(PVOID&)OriginalDirectInput8Create, FakeDirectInput8Create);
	DetourTransactionCommit();
	}
	
	else{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());
	DetourDetach(&(PVOID&)OriginalDirectInput8Create, FakeDirectInput8Create);
	DetourTransactionCommit();
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
