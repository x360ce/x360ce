#include "stdafx.h"
#include "Common.h"
#include "Logger.h"
#include "Utils.h"

#include "Config.h"

#include "ControllerManager.h"
#include "InputHookManager.h"

#ifdef _DEBUG
#include <vld.h> 
#endif

static const char * legal_notice = {
    "\nx360ce - XBOX 360 Controller emulator\n"
    "https://code.google.com/p/x360ce/\n\n"
    "Copyright (C) 2010-2014 Robert Krawczyk\n\n"
    "This program is free software you can redistribute it and/or modify it under\n"
    "the terms of the GNU Lesser General Public License as published by the Free\n"
    "Software Foundation, either version 3 of the License, or any later version.\n\n"
};

VOID InitInstance()
{
    IniFile ini;
    std::string inipath("x360ce.ini");
    if (!ini.Load(inipath))
        CheckCommonDirectory(&inipath, "x360ce");
    if (!ini.Load(inipath)) return;

    bool con;
    bool file;

    ini.Get("Options", "Console", &con);
    ini.Get("Options", "Log", &file);

    if (con)
        LogConsole("x360ce", legal_notice);

    if (file)
    {
        SYSTEMTIME systime;
        GetLocalTime(&systime);
        std::string processName;
        ModuleFileName(&processName);

        std::string logfile = StringFormat("x360ce_%s_%02u-%02u-%02u_%08u.log", processName.c_str(), systime.wYear,
            systime.wMonth, systime.wDay, GetTickCount());

        LogFile(logfile);
    }

    // Get will initalize static InputHookManager object and we want to initialize it ASAP
    InputHookManager::Get();
}

extern "C" VOID WINAPI Reset()
{
    PrintLog("Restarting");

    // Only x360ce.App will call this so InputHook is not required, disable it.
    InputHookManager::Get().GetInputHook().Shutdown();
    ControllerManager::Get().GetControllers().clear();
    ControllerManager::Get().GetConfig().ReadConfig();
}


extern "C" BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved);

    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            DisableThreadLibraryCalls(hModule);
            InitInstance();
            break;

        case DLL_PROCESS_DETACH:
            break;
    }

    return TRUE;
}