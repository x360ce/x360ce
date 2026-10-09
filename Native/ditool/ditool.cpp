// ditool.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <sstream>
#include <iostream>
#include <iomanip>

#include "Utils.h"
#include "Logger.h"

HINSTANCE hInst = NULL;
LPDIRECTINPUT8 g_pDI;

BOOL CALLBACK EnumDevicesCallback(LPCDIDEVICEINSTANCE pInst, VOID* pContext)
{
	if (pInst)
	{
		std::string guidProduct;
		std::string guidInstance;

		GUIDtoString(&guidProduct, pInst->guidProduct);
		GUIDtoString(&guidInstance, pInst->guidInstance);

		PrintLog("ProductName : %s\nInstanceName: %s", pInst->tszProductName, pInst->tszInstanceName);
		PrintLog("guidProduct : %s\nguidInstance: %s", guidProduct.c_str(), guidInstance.c_str());
		PrintLog("DevType     : 0x%08X 0x%08X", LOBYTE(pInst->dwDevType), HIBYTE(pInst->dwDevType));
		PrintLog("\n");
	}
	return DIENUM_CONTINUE;
}

int _tmain(int argc, _TCHAR* argv[])
{
	LogFile("ditool.txt");

	setlocale(LC_ALL, "");
	hInst = GetModuleHandle(NULL);
	HRESULT hr = DirectInput8Create(hInst, DIRECTINPUT_VERSION, IID_IDirectInput8, (VOID**)&g_pDI, NULL);

	if (SUCCEEDED(hr)) g_pDI->EnumDevices(DI8DEVCLASS_ALL, EnumDevicesCallback, NULL, DIEDFL_ATTACHEDONLY);

	_getch();

	return 0;
}

