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
	WriteLog(LOG_HOOKDI,L"*EnumCallbackA*");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
	{
		WriteLog(LOG_HOOKDI,L"Keyboard detected - skipping");
		return lpTrueCallbackA(pInst,pContext);
	}

	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
	{
		WriteLog(LOG_HOOKDI,L"Mouse detected - skipping");
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
	WriteLog(LOG_HOOKDI,L"*EnumCallbackW*");

	// Fast return if keyboard or mouse
	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
	{
		WriteLog(LOG_HOOKDI,L"Keyboard detected - skipping");
		return lpTrueCallbackW(pInst,pContext);
	}

	if (((pInst->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
	{
		WriteLog(LOG_HOOKDI,L"Mouse detected - skipping");
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
	tEnumDevicesA oEnumDevicesA = (tEnumDevicesA) HooksGetTrampolineAddress(hEnumDevicesA);
	if(iHookThis->CheckHook(iHook::HOOK_DI))
	{
		WriteLog(LOG_HOOKDI,L"*EnumDevicesA*");

		if (lpCallback)
		{
			lpTrueCallbackA = lpCallback;
			return oEnumDevicesA(This,dwDevType,HookEnumCallbackA,pvRef,dwFlags);
		}
		else return oEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
	}
	else return oEnumDevicesA(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookEnumDevicesW (LPDIRECTINPUT8W This, DWORD dwDevType,LPDIENUMDEVICESCALLBACKW lpCallback,LPVOID pvRef,DWORD dwFlags)
{
	tEnumDevicesW oEnumDevicesW = (tEnumDevicesW) HooksGetTrampolineAddress(hEnumDevicesW);
	if(iHookThis->CheckHook(iHook::HOOK_DI))
	{
		WriteLog(LOG_HOOKDI,L"*EnumDevicesW*");

		if (lpCallback)
		{
			lpTrueCallbackW = lpCallback;
			return oEnumDevicesW(This,dwDevType,HookEnumCallbackW,pvRef,dwFlags);
		}
		else return oEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
	}
	else return oEnumDevicesW(This,dwDevType,lpCallback,pvRef,dwFlags);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookGetDeviceInfoA (LPDIRECTINPUTDEVICE8A This, LPDIDEVICEINSTANCEA pdidi)
{
	tGetDeviceInfoA oGetDeviceInfoA = (tGetDeviceInfoA) HooksGetTrampolineAddress(hGetDeviceInfoA);
	HRESULT hr = oGetDeviceInfoA( This, pdidi );

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*GetDeviceInfoA*");

	if(FAILED(hr)) return hr;

	if(pdidi)
	{

		// Fast return if keyboard or mouse
		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
		{
			WriteLog(LOG_HOOKDI,L"Keyboard detected - skipping");
			return hr;
		}

		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
		{
			WriteLog(LOG_HOOKDI,L"Mouse detected - skipping");
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
	tGetDeviceInfoW oGetDeviceInfoW = (tGetDeviceInfoW) HooksGetTrampolineAddress(hGetDeviceInfoW);
	HRESULT hr = oGetDeviceInfoW( This, pdidi );

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*GetDeviceInfoW*");

	if(FAILED(hr)) return hr;

	if(pdidi)
	{

		// Fast return if keyboard or mouse
		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_KEYBOARD))
		{
			WriteLog(LOG_HOOKDI,L"Keyboard detected - skipping");
			return hr;
		}

		if (((pdidi->dwDevType & 0xFF) == DI8DEVTYPE_MOUSE))
		{
			WriteLog(LOG_HOOKDI,L"Mouse detected - skipping");
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
	tGetPropertyA oGetPropertyA = (tGetPropertyA) HooksGetTrampolineAddress(hGetPropertyA);
	HRESULT hr = oGetPropertyA(This, rguidProp, pdiph);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*GetPropertyA*");

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
	tGetPropertyW oGetPropertyW = (tGetPropertyW) HooksGetTrampolineAddress(hGetPropertyW);
	HRESULT hr = oGetPropertyW(This, rguidProp, pdiph);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*GetPropertyW*");

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
	tCreateDeviceA oCreateDeviceA = (tCreateDeviceA) HooksGetTrampolineAddress(hCreateDeviceA);
	HRESULT hr = oCreateDeviceA(This, rguid, lplpDirectInputDevice, pUnkOuter);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*CreateDeviceA*");

	if(FAILED(hr)) return hr;

	if(*lplpDirectInputDevice)
	{
		LPDIRECTINPUTDEVICE8A &ref = *lplpDirectInputDevice;
		if(!hGetDeviceInfoA && ref->lpVtbl->GetDeviceInfo)
		{
			hGetDeviceInfoA = ref->lpVtbl->GetDeviceInfo;
			if(HooksSafeTransition(hGetPropertyA,true))
			{
				WriteLog(LOG_HOOKDI,L"Hooking GetDeviceInfoA");
				HooksInsertNewRedirection(hGetDeviceInfoA,HookGetDeviceInfoA,TEE_HOOK_NRM_JUMP);
				HooksSafeTransition(hGetDeviceInfoA,false);
			}
		}

		if(!hGetPropertyA && ref->lpVtbl->GetProperty)
		{
			hGetPropertyA = ref->lpVtbl->GetProperty;
			if(HooksSafeTransition(hGetPropertyA,true))
			{
				WriteLog(LOG_HOOKDI,L"Hooking GetPropertyA");
				HooksInsertNewRedirection(hGetPropertyA,HookGetPropertyA,TEE_HOOK_NRM_JUMP);
				HooksSafeTransition(hGetPropertyA,false);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT STDMETHODCALLTYPE HookCreateDeviceW (LPDIRECTINPUT8W This, REFGUID rguid, LPDIRECTINPUTDEVICE8W * lplpDirectInputDevice, LPUNKNOWN pUnkOuter)
{
	tCreateDeviceW oCreateDeviceW = (tCreateDeviceW) HooksGetTrampolineAddress(hCreateDeviceW);
	HRESULT hr = oCreateDeviceW(This, rguid, lplpDirectInputDevice, pUnkOuter);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*CreateDeviceW*");

	if(FAILED(hr)) return hr;

	if(*lplpDirectInputDevice)
	{
		LPDIRECTINPUTDEVICE8W &ref = *lplpDirectInputDevice;
		if(!hGetDeviceInfoW && ref->lpVtbl->GetDeviceInfo)
		{
			hGetDeviceInfoW = ref->lpVtbl->GetDeviceInfo;
			if(HooksSafeTransition(hGetPropertyW,true))
			{
				WriteLog(LOG_HOOKDI,L"Hooking GetDeviceInfoW");
				HooksInsertNewRedirection(hGetDeviceInfoW,HookGetDeviceInfoW,TEE_HOOK_NRM_JUMP);
				HooksSafeTransition(hGetDeviceInfoW,false);
			}
		}

		if(!hGetPropertyW && ref->lpVtbl->GetProperty)
		{
			hGetPropertyW = ref->lpVtbl->GetProperty;
			if(HooksSafeTransition(hGetPropertyW,true))
			{
				WriteLog(LOG_HOOKDI,L"Hooking GetPropertyW");
				HooksInsertNewRedirection(hGetPropertyW,HookGetPropertyW,TEE_HOOK_NRM_JUMP);
				HooksSafeTransition(hGetPropertyW,false);
			}
		}
	}

	return hr;
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
HRESULT WINAPI HookDirectInput8Create(HINSTANCE hinst, DWORD dwVersion, REFIID riidltf, LPVOID *ppvOut, LPUNKNOWN punkOuter)
{
	tDirectInput8Create oDirectInput8Create = (tDirectInput8Create) HooksGetTrampolineAddress(hDirectInput8Create);
	HRESULT hr = oDirectInput8Create(hinst,dwVersion,riidltf,ppvOut,punkOuter);

	if(!iHookThis->CheckHook(iHook::HOOK_DI)) return hr;
	WriteLog(LOG_HOOKDI,L"*DirectInput8Create*");

	if(ppvOut)
	{
		if(IsEqualIID(riidltf,IID_IDirectInput8A))
		{
			LPDIRECTINPUT8A pDIA = static_cast<LPDIRECTINPUT8A>(*ppvOut);

			if(pDIA)
			{
				WriteLog(LOG_HOOKDI,L"DirectInput8Create - ANSI interface");
				if(!hCreateDeviceA && pDIA->lpVtbl->CreateDevice)
				{	
					hCreateDeviceA = pDIA->lpVtbl->CreateDevice;
					if(HooksSafeTransition(hCreateDeviceA,true))
					{
						WriteLog(LOG_HOOKDI,L"Hooking CreateDeviceA");
						HooksInsertNewRedirection(hCreateDeviceA,HookCreateDeviceA,TEE_HOOK_NRM_JUMP);
						HooksSafeTransition(hCreateDeviceA,false);
					}
				}
				if(!hEnumDevicesA && pDIA->lpVtbl->EnumDevices)
				{
					hEnumDevicesA = pDIA->lpVtbl->EnumDevices;
					if(HooksSafeTransition(hEnumDevicesA,true))
					{
						WriteLog(LOG_HOOKDI,L"Hooking EnumDevicesA");
						HooksInsertNewRedirection(hEnumDevicesA,HookEnumDevicesA,TEE_HOOK_NRM_JUMP);
						HooksSafeTransition(hEnumDevicesA,false);
					}
				}
			}
		}

		if (IsEqualIID(riidltf,IID_IDirectInput8W))
		{
			LPDIRECTINPUT8W pDIW = static_cast<LPDIRECTINPUT8W>(*ppvOut);

			if(pDIW)
			{
				WriteLog(LOG_HOOKDI,L"DirectInput8Create - UNICODE interface");
				if(!hCreateDeviceW && pDIW->lpVtbl->CreateDevice)
				{
					hCreateDeviceW = pDIW->lpVtbl->CreateDevice;
					if(HooksSafeTransition(hCreateDeviceW,true))
					{
						WriteLog(LOG_HOOKDI,L"Hooking CreateDeviceW");
						HooksInsertNewRedirection(hCreateDeviceW,HookCreateDeviceW,TEE_HOOK_NRM_JUMP);
						HooksSafeTransition(hCreateDeviceW,false);
					}
				}
				if(!hEnumDevicesW && pDIW->lpVtbl->EnumDevices)
				{
					hEnumDevicesW = pDIW->lpVtbl->EnumDevices;
					if(HooksSafeTransition(hEnumDevicesW,true))
					{
						WriteLog(LOG_HOOKDI,L"Hooking EnumDevicesW");
						HooksInsertNewRedirection(hEnumDevicesW,HookEnumDevicesW,TEE_HOOK_NRM_JUMP);
						HooksSafeTransition(hEnumDevicesW,false);
					}
				}
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
	WriteLog(LOG_HOOKDI,L"Hooking DirectInput8Create");
	iHookThis = this;

	if(!hDirectInput8Create) 
	{
		hDirectInput8Create = DirectInput8Create;
		if(HooksSafeTransition(hDirectInput8Create,true))
		{
			HooksInsertNewRedirection(hDirectInput8Create,HookDirectInput8Create,TEE_HOOK_NRM_JUMP);
			HooksSafeTransition(hDirectInput8Create,false);
		}
	}
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void iHook::HookDI_Cleanup()
{
	WriteLog(LOG_HOOKDI,L"Removing DirectInput Hooks");
	if(hGetDeviceInfoA)
	{
		WriteLog(LOG_HOOKDI, L"Removing GetDeviceInfoA Hook");
		if(HooksSafeTransition(hGetDeviceInfoA,true))
		{
			HooksRemoveRedirection(hGetDeviceInfoA,true);
			HooksSafeTransition(hGetDeviceInfoA,false);
			hGetDeviceInfoA = NULL;
		}
	}

	if(hGetDeviceInfoW)
	{
		WriteLog(LOG_HOOKDI, L"Removing GetDeviceInfoW Hook");
		if(HooksSafeTransition(hGetDeviceInfoW,true))
		{
			HooksRemoveRedirection(hGetDeviceInfoW,true);
			HooksSafeTransition(hGetDeviceInfoW,false);
			hGetDeviceInfoW = NULL;
		}
	}

	if(hGetPropertyA)
	{
		WriteLog(LOG_HOOKDI,L"Removing GetPropertyA Hook");
		if(HooksSafeTransition(hGetPropertyA,true))
		{
			HooksRemoveRedirection(hGetPropertyA,true);
			HooksSafeTransition(hGetPropertyA,false);
			hGetPropertyA = NULL;
		}
	}

	if(hGetPropertyW)
	{
		WriteLog(LOG_HOOKDI,L"Removing GetProperty Hook");
		if(HooksSafeTransition(hGetPropertyW,true))
		{
			HooksRemoveRedirection(hGetPropertyW,true);
			HooksSafeTransition(hGetPropertyW,false);
			hGetPropertyW = NULL;
		}
	}

	if(hCreateDeviceA)
	{
		WriteLog(LOG_HOOKDI,L"Removing CreateDeviceA Hook");
		if(HooksSafeTransition(hCreateDeviceA,true))
		{
			HooksRemoveRedirection(hCreateDeviceA,true);
			HooksSafeTransition(hCreateDeviceA,false);
			hCreateDeviceA = NULL;
		}
	}

	if(hCreateDeviceW)
	{
		WriteLog(LOG_HOOKDI,L"Removing CreateDeviceW Hook");
		if(HooksSafeTransition(hCreateDeviceW,true))
		{
			HooksRemoveRedirection(hCreateDeviceW,true);
			HooksSafeTransition(hCreateDeviceW,false);
			hCreateDeviceW = NULL;
		}
	}

	if(hEnumDevicesA)
	{
		WriteLog(LOG_HOOKDI,L"Removing EnumDevicesA Hook");
		if(HooksSafeTransition(hEnumDevicesA,true))
		{
			HooksRemoveRedirection(hEnumDevicesA,true);
			HooksSafeTransition(hEnumDevicesA,false);
			hEnumDevicesA = NULL;
		}
	}

	if(hEnumDevicesW)
	{
		WriteLog(LOG_HOOKDI,L"Removing EnumDevicesW Hook");
		if(HooksSafeTransition(hEnumDevicesW,true))
		{
			HooksRemoveRedirection(hEnumDevicesW,true);
			HooksSafeTransition(hEnumDevicesW,false);
			hEnumDevicesW = NULL;
		}
	}

	if(hDirectInput8Create)
	{
		WriteLog(LOG_HOOKDI,L"Removing DirectInput8Create Hook");
		if(HooksSafeTransition(hDirectInput8Create,true))
		{
			HooksRemoveRedirection(hDirectInput8Create,true);
			HooksSafeTransition(hDirectInput8Create,false);
			hDirectInput8Create = NULL;
		}
	}
}