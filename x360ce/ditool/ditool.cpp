// ditool.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <string>
#include <sstream>
#include <iostream>
#include <iomanip>
#include "Logger.h"

using namespace std;

INITIALIZE_LOGGER;

HINSTANCE hInst = NULL;
LPDIRECTINPUT8 g_pDI;

inline const std::string GUIDtoStringA(const GUID &g)
{
    char tmp[40];

    sprintf_s(tmp, 40, "{%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X}",
        g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7]);
    return tmp;
}

BOOL CALLBACK EnumDevicesCallback( LPCDIDEVICEINSTANCE pInst, VOID* pContext )
{
	if(pInst)
	{
        std::string guidProduct = GUIDtoStringA(pInst->guidProduct);
        std::string guidInstance = GUIDtoStringA(pInst->guidInstance);

        PrintLog("ProductName : %s\nInstanceName: %s", pInst->tszProductName, pInst->tszInstanceName);
        PrintLog("guidProduct : %s\nguidInstance: %s", guidProduct.c_str(), guidInstance.c_str());
        PrintLog("DevType     : 0x%08X 0x%08X", LOBYTE(pInst->dwDevType), HIBYTE(pInst->dwDevType));
        PrintLog("\n");
	}
	return DIENUM_CONTINUE;
}

int _tmain(int argc, _TCHAR* argv[])
{
    LogFile("ditool.log");
    LogConsole("ditool");

	setlocale( LC_ALL, "" );
	hInst = GetModuleHandle(NULL);
	HRESULT hr = DirectInput8Create( hInst, DIRECTINPUT_VERSION,IID_IDirectInput8, ( VOID** )&g_pDI, NULL );

	if(SUCCEEDED(hr)) g_pDI->EnumDevices( DI8DEVCLASS_ALL, EnumDevicesCallback, NULL, DIEDFL_ATTACHEDONLY );

	_getch();

	return 0;
}

