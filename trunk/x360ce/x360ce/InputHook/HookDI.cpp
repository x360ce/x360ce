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

#define CINTERFACE
#include <dinput.h>

#include "Utilities\Log.h"
#include "Utilities\Misc.h"

static iHook* iHookThis;

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

static tDirectInput8Create hDirectInput8Create = NULL;
static tCreateDeviceA hCreateDeviceA = NULL;
static tCreateDeviceW hCreateDeviceW = NULL;
static tGetPropertyA hGetPropertyA = NULL;
static tGetPropertyW hGetPropertyW = NULL;
static tGetDeviceInfoA hGetDeviceInfoA = NULL;
static tGetDeviceInfoW hGetDeviceInfoW = NULL;
static tEnumDevicesA hEnumDevicesA = NULL;
static tEnumDevicesW hEnumDevicesW = NULL;

static LPDIENUMDEVICESCALLBACKA lpTrueCallbackA= NULL;
static LPDIENUMDEVICESCALLBACKW lpTrueCallbackW= NULL;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL FAR PASCAL HookEnumCallbackA( const DIDEVICEINSTANCEA* pInst,VOID* pContext )
{
	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return lpTrueCallbackA(pInst,pContext);
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

	if(iHookThis->CheckHook(iHook::HOOK_STOP)) return DIENUM_STOP;

	if(pInst && pInst->dwSize == sizeof(DIDEVICEINSTANCEA))
	{
		for(size_t i = 0; i < iHookThis->GetHookCount(); i++)
		{
			iHookPadConfig &padconf = iHookThis->GetPadConfig(i);
			if(padconf.GetHookState() && IsEqualGUID(padconf.GetProductGUID(),pInst->guidProduct))
			{
				DIDEVICEINSTANCEA HookInst;
				memcpy(&HookInst,pInst,pInst->dwSize);

				GUID guidProduct = { iHookThis->GetFakeVIDPID(), 0x0000, 0x0000, {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44} };
				HookInst.guidProduct = guidProduct;

				std::string strTrueguidProduct = GUIDtoStringA(pInst->guidProduct);
				std::string strHookguidProduct = GUIDtoStringA(HookInst.guidProduct);

				WriteLog(LOG_HOOKDI,L"GUID change from %hs to %hs",strTrueguidProduct.c_str(),strHookguidProduct.c_str());

				HookInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
				HookInst.wUsage = 0x05;
				HookInst.wUsagePage = 0x01;

				if(iHookThis->CheckHook(iHook::HOOK_NAME))
				{

					std::string OldProductName = HookInst.tszProductName;
					std::string OldInstanceName = HookInst.tszInstanceName;

					strcpy_s(HookInst.tszProductName, "XBOX 360 For Windows (Controller)");
					strcpy_s(HookInst.tszInstanceName, "XBOX 360 For Windows (Controller)");

					WriteLog(LOG_HOOKDI,L"Product Name change from \"%hs\" to \"%hs\"",OldProductName.c_str(),HookInst.tszProductName);
					WriteLog(LOG_HOOKDI,L"Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName.c_str(),HookInst.tszInstanceName);
				}

				return lpTrueCallbackA(&HookInst,pContext);
			}
		}
	}

	return lpTrueCallbackA(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BOOL FAR PASCAL HookEnumCallbackW( const DIDEVICEINSTANCEW* pInst,VOID* pContext )
{
	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return lpTrueCallbackW(pInst,pContext);
	WriteLog(LOG_HOOKDI,L"HookEnumCallbackW");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
	{
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackW:: Keyboard detected");
		return lpTrueCallbackW(pInst,pContext);
	}

	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
	{
		WriteLog(LOG_HOOKDI,L"HookEnumCallbackW:: Mouse detected");
		return lpTrueCallbackW(pInst,pContext);
	}

	if(iHookThis->CheckHook(iHook::HOOK_STOP)) return DIENUM_STOP;

	if(pInst && pInst->dwSize == sizeof(DIDEVICEINSTANCEW))
	{

		for(size_t i = 0; i < iHookThis->GetHookCount(); i++)
		{
			iHookPadConfig &padconf = iHookThis->GetPadConfig(i);
			if(padconf.GetHookState() && IsEqualGUID(padconf.GetProductGUID(),pInst->guidProduct))
			{
				DIDEVICEINSTANCEW HookInst;
				memcpy(&HookInst,pInst,pInst->dwSize);

				//DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));

				GUID guidProduct = { iHookThis->GetFakeVIDPID(), 0x0000, 0x0000, {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44} };
				HookInst.guidProduct = guidProduct;

				WCHAR strTrueguidProduct[50];
				WCHAR strHookguidProduct[50];
				GUIDtoString(pInst->guidProduct,strTrueguidProduct);
				GUIDtoString(HookInst.guidProduct,strHookguidProduct);
				WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

				HookInst.dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
				HookInst.wUsage = 0x05;
				HookInst.wUsagePage = 0x01;

				if(iHookThis->CheckHook(iHook::HOOK_NAME))
				{
					std::wstring OldProductName(HookInst.tszProductName);
					std::wstring OldInstanceName(HookInst.tszInstanceName);

					wcscpy_s(HookInst.tszProductName, L"XBOX 360 For Windows (Controller)");
					wcscpy_s(HookInst.tszInstanceName, L"XBOX 360 For Windows (Controller)");

					WriteLog(LOG_HOOKDI,L"Product Name change from \"%s\" to \"%s\"",OldProductName.c_str(),HookInst.tszProductName);
					WriteLog(LOG_HOOKDI,L"Instance Name change from \"%s\" to \"%s\"",OldInstanceName.c_str(),HookInst.tszInstanceName);
				}

				return lpTrueCallbackW(&HookInst,pContext);
			}
		}
	}

	return lpTrueCallbackW(pInst,pContext);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesA (LPDIRECTINPUT8A This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKA lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(iHookThis->CheckHook(iHook::HOOK_DI))
	{
		WriteLog(LOG_HOOKDI,L"HookEnumDevicesA");

		if (lpCallback)
		{
			lpTrueCallbackA = lpCallback;
			return hEnumDevicesA(This,dwDevType,HookEnumCallbackA,pvRef,dwFlags);
		}
		else return hEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
	}
	else return hEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	if(iHookThis->CheckHook(iHook::HOOK_DI))
	{
		WriteLog(LOG_HOOKDI,L"HookEnumDevicesW");

		if (lpCallback)
		{
			lpTrueCallbackW = lpCallback;
			return hEnumDevicesW(This,dwDevType,HookEnumCallbackW,pvRef,dwFlags);
		}
		else return hEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
	}
	else return hEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoA (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
{
	HRESULT hr = hGetDeviceInfoA( This, pdidi );

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA");

	if(FAILED(hr)) return hr;

	if(pdidi)
	{

		// Fast return if keyboard or mouse
		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
		{
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Keyboard detected");
			return hr;
		}

		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
		{
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Mouse detected");
			return hr;
		}

		for(size_t i = 0; i < iHookThis->GetHookCount(); i++)
		{
			iHookPadConfig &padconf = iHookThis->GetPadConfig(i);
			if(padconf.GetHookState() && IsEqualGUID(padconf.GetProductGUID(), pdidi->guidProduct))
			{

				WCHAR strTrueguidProduct[50];
				WCHAR strHookguidProduct[50];

				GUIDtoString(pdidi->guidProduct,strTrueguidProduct);

				//DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));

				GUID guidProduct = { iHookThis->GetFakeVIDPID(), 0x0000, 0x0000, {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44} };
				pdidi->guidProduct = guidProduct;

				GUIDtoString(pdidi->guidProduct,strHookguidProduct);

				WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

				pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
				pdidi->wUsage = 0x05;
				pdidi->wUsagePage = 0x01;

				if(iHookThis->CheckHook(iHook::HOOK_NAME))
				{
					std::string OldProductName(pdidi->tszProductName);
					std::string OldInstanceName(pdidi->tszInstanceName);

					strcpy_s(pdidi->tszProductName, "XBOX 360 For Windows (Controller)");
					strcpy_s(pdidi->tszInstanceName, "XBOX 360 For Windows (Controller)");

					WriteLog(LOG_HOOKDI,L"Product Name change from \"%hs\" to \"%hs\"",OldProductName.c_str(),pdidi->tszProductName);
					WriteLog(LOG_HOOKDI,L"Instance Name change from \"%hs\" to \"%hs\"",OldInstanceName.c_str(),pdidi->tszInstanceName);
				}

				hr=DI_OK;
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoW (LPDIRECTINPUTDEVICE8W This, LPDIDEVICEINSTANCEW pdidi)
{
	HRESULT hr = hGetDeviceInfoW( This, pdidi );

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW");

	if(FAILED(hr)) return hr;

	if(pdidi)
	{

		// Fast return if keyboard or mouse
		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
		{
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Keyboard detected");
			return hr;
		}

		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
		{
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Mouse detected");
			return hr;
		}

		for(size_t i = 0; i < iHookThis->GetHookCount(); i++)
		{
			iHookPadConfig &padconf = iHookThis->GetPadConfig(i);
			if(padconf.GetHookState() && IsEqualGUID(padconf.GetProductGUID(), pdidi->guidProduct))
			{

				WCHAR strTrueguidProduct[50];
				WCHAR strHookguidProduct[50];

				GUIDtoString(pdidi->guidProduct,strTrueguidProduct);

				//DWORD dwHookPIDVID = static_cast<DWORD>(MAKELONG(InputHookConfig.dwHookVID,InputHookConfig.dwHookPID));

				GUID guidProduct = { iHookThis->GetFakeVIDPID(), 0x0000, 0x0000, {0x00, 0x00, 0x50, 0x49, 0x44, 0x56, 0x49, 0x44} };
				pdidi->guidProduct = guidProduct;

				GUIDtoString(pdidi->guidProduct,strHookguidProduct);

				WriteLog(LOG_HOOKDI,L"GUID change from %s to %s",strTrueguidProduct,strHookguidProduct);

				pdidi->dwDevType = (MAKEWORD(DI8DEVTYPE_GAMEPAD, DI8DEVTYPEGAMEPAD_STANDARD) | DIDEVTYPE_HID); //66069 == 0x00010215
				pdidi->wUsage = 0x05;
				pdidi->wUsagePage = 0x01;

				if(iHookThis->CheckHook(iHook::HOOK_NAME))
				{
					std::wstring OldProductName(pdidi->tszProductName);
					std::wstring OldInstanceName(pdidi->tszInstanceName);

					wcscpy_s(pdidi->tszProductName, L"XBOX 360 For Windows (Controller)");
					wcscpy_s(pdidi->tszInstanceName, L"XBOX 360 For Windows (Controller)");

					WriteLog(LOG_HOOKDI,L"Product Name change from \"%s\" to \"%s\"",OldProductName.c_str(),pdidi->tszProductName);
					WriteLog(LOG_HOOKDI,L"Instance Name change from \"%s\" to \"%s\"",OldInstanceName.c_str(),pdidi->tszInstanceName);
				}

				hr=DI_OK;
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetPropertyA (LPDIRECTINPUTDEVICE8A This, REFGUID rguidProp, LPDIPROPHEADER pdiph)
{
	HRESULT hr = hGetPropertyA(This, rguidProp, pdiph);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetPropertyA");

	if(FAILED(hr)) return hr;

	if(&rguidProp == &DIPROP_VIDPID)
	{
		DWORD dwHookPIDVID = iHookThis->GetFakeVIDPID();
		DWORD dwTruePIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

		reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
		WriteLog(LOG_HOOKDI,L"VIDPID change from %08X to %08X",dwTruePIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
	}

	if(iHookThis->CheckHook(iHook::HOOK_NAME))
	{
		if (&rguidProp == &DIPROP_PRODUCTNAME)
		{
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
	HRESULT hr = hGetPropertyW(This, rguidProp, pdiph);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookGetPropertyW");

	if(FAILED(hr)) return hr;


	if(&rguidProp == &DIPROP_VIDPID)
	{
		DWORD dwHookPIDVID = iHookThis->GetFakeVIDPID();
		DWORD dwTruePIDVID = reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData;

		reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData = dwHookPIDVID;
		WriteLog(LOG_HOOKDI,L"VIDPID change from %08X to %08X",dwTruePIDVID,reinterpret_cast<LPDIPROPDWORD>(pdiph)->dwData);
	}

	if(iHookThis->CheckHook(iHook::HOOK_NAME))
	{
		if(&rguidProp == &DIPROP_PRODUCTNAME)
		{
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
	HRESULT hr = hCreateDeviceA(This, rguid, lplpDirectInputDevice, pUnkOuter);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookCreateDeviceA");

	if(FAILED(hr)) return hr;

	if(*lplpDirectInputDevice)
	{
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		if(!hGetDeviceInfoA)
		{
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoA:: Hooking");
			hGetDeviceInfoA = (*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo;
			DetourAttach(&(PVOID&)hGetDeviceInfoA, HookGetDeviceInfoA);
		}

		if(!hGetPropertyA)
		{
			WriteLog(LOG_HOOKDI,L"HookGetPropertyA:: Hooking");
			hGetPropertyA = (*lplpDirectInputDevice)->lpVtbl->GetProperty;
			DetourAttach(&(PVOID&)hGetPropertyA, HookGetPropertyA);

		}
		DetourTransactionCommit();
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceW (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	HRESULT hr = hCreateDeviceW(This, rguid, lplpDirectInputDevice, pUnkOuter);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookCreateDeviceW");

	if(FAILED(hr)) return hr;

	if(*lplpDirectInputDevice)
	{
		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		if(!hGetDeviceInfoW)
		{
			WriteLog(LOG_HOOKDI,L"HookGetDeviceInfoW:: Hooking");
			hGetDeviceInfoW = (*lplpDirectInputDevice)->lpVtbl->GetDeviceInfo;
			DetourAttach(&(PVOID&)hGetDeviceInfoW, HookGetDeviceInfoW);

		}

		if(!hGetPropertyW)
		{
			WriteLog(LOG_HOOKDI,L"HookGetPropertyW:: Hooking");
			hGetPropertyW = (*lplpDirectInputDevice)->lpVtbl->GetProperty;
			DetourAttach(&(PVOID&)hGetPropertyW, HookGetPropertyW);
		}
		DetourTransactionCommit();
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	HRESULT hr = hDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"HookDirectInput8Create");

	if(ppvOut)
	{
		if(IsEqualIID(riidltf,IID_IDirectInput8A))
		{
			LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppvOut);

			if(pDIA)
			{
				WriteLog(LOG_HOOKDI,L"HookDirectInput8Create - ANSI interface");
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());

				if(!hCreateDeviceA)
				{
					hCreateDeviceA = pDIA->lpVtbl->CreateDevice;
					WriteLog(LOG_HOOKDI,L"HookCreateDeviceW:: Hooking");
					DetourAttach(&(PVOID&)hCreateDeviceA, HookCreateDeviceA);
				}

				if(!hEnumDevicesA)
				{
					hEnumDevicesA = pDIA->lpVtbl->EnumDevices;
					WriteLog(LOG_HOOKDI,L"HookEnumDevicesW:: Hooking");
					DetourAttach(&(PVOID&)hEnumDevicesA, HookEnumDevicesA);
				}
				DetourTransactionCommit();
			}
		}

		if (IsEqualIID(riidltf,IID_IDirectInput8W))
		{
			LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);

			if(pDIW)
			{
				WriteLog(LOG_HOOKDI,L"HookDirectInput8Create - UNICODE interface");
				DetourTransactionBegin();
				DetourUpdateThread(GetCurrentThread());

				if(!hCreateDeviceW)
				{
					hCreateDeviceW = pDIW->lpVtbl->CreateDevice;
					WriteLog(LOG_HOOKDI,L"HookCreateDeviceW:: Hooking");
					DetourAttach(&(PVOID&)hCreateDeviceW, HookCreateDeviceW);
				}

				if(!hEnumDevicesW)
				{
					hEnumDevicesW = pDIW->lpVtbl->EnumDevices;
					WriteLog(LOG_HOOKDI,L"HookEnumDevicesW:: Hooking");
					DetourAttach(&(PVOID&)hEnumDevicesW, HookEnumDevicesW);
				}
				DetourTransactionCommit();
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void iHook::HookDI()
{
	if(!CheckHook(iHook::HOOK_DI)) return;
	WriteLog(LOG_HOOKDI,L"HookDI:: Hooking");

	iHookThis = this;

	if(!hDirectInput8Create)
	{
		hDirectInput8Create = (tDirectInput8Create) GetProcAddress( GetDinput8(), "DirectInput8Create");

		DetourTransactionBegin();
		DetourUpdateThread(GetCurrentThread());
		DetourAttach(&(PVOID&)hDirectInput8Create, HookDirectInput8Create);
		DetourTransactionCommit();
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void iHook::HookDIClean()
{
	DetourTransactionBegin();
	DetourUpdateThread(GetCurrentThread());

	if(hGetDeviceInfoA)
	{
		WriteLog(LOG_HOOKDI, L"HookGetDeviceInfoA:: Removing Hook");
		DetourDetach(&(PVOID&)hGetDeviceInfoA, HookGetDeviceInfoA);
	}

	if(hGetDeviceInfoW)
	{
		WriteLog(LOG_HOOKDI, L"HookGetDeviceInfoW:: Removing Hook");
		DetourDetach(&(PVOID&)hGetDeviceInfoW, HookGetDeviceInfoW);
	}

	if(hGetPropertyA)
	{
		WriteLog(LOG_HOOKDI,L"HookGetPropertyA:: Removing Hook");
		DetourDetach(&(PVOID&)hGetPropertyA, HookGetPropertyA);
	}

	if(hGetPropertyW)
	{
		WriteLog(LOG_HOOKDI,L"HookGetPropertyW:: Removing Hook");
		DetourDetach(&(PVOID&)hGetPropertyW, HookGetPropertyW);
	}

	if(hCreateDeviceA)
	{
		WriteLog(LOG_HOOKDI,L"HookCreateDeviceA:: Removing Hook");
		DetourDetach(&(PVOID&)hCreateDeviceA, HookCreateDeviceA);
	}

	if(hCreateDeviceW)
	{
		WriteLog(LOG_HOOKDI,L"HookCreateDeviceW:: Removing Hook");
		DetourDetach(&(PVOID&)hCreateDeviceW, HookCreateDeviceW);
	}

	if(hEnumDevicesA)
	{
		WriteLog(LOG_HOOKDI,L"HookEnumDevicesA:: Removing Hook");
		DetourDetach(&(PVOID&)hEnumDevicesA, HookEnumDevicesA);
	}

	if(hEnumDevicesW)
	{
		WriteLog(LOG_HOOKDI,L"HookEnumDevicesW:: Removing Hook");
		DetourDetach(&(PVOID&)hEnumDevicesW, HookEnumDevicesW);
	}

	if(hDirectInput8Create)
	{
		WriteLog(LOG_HOOKDI,L"HookDirectInput8Create:: Removing Hook");
		DetourDetach(&(PVOID&)hDirectInput8Create, HookDirectInput8Create);
	}

	DetourTransactionCommit();
}