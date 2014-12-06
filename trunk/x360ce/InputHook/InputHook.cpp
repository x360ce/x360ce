/*  InputHook Detour Library for XBOX360 Controller Emulator
 *
 *  https://code.google.com/p/x360ce/
 *
 *  Copyright (C) 2012-2014 Robert Krawczyk
 *
 *  InputHook is free software: you can redistribute it and/or modify it under the terms
 *  of the GNU Lesser General Public License as published by the Free Software Foundation,
 *  either version 3 of the License, or any later version.
 *
 *  InputHook is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 *  without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 *  PURPOSE.  See the GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along with InputHook.
 *  If not, see <http://www.gnu.org/licenses/>.
 */

#include "stdafx.h"

#include <CGuid.h>
#include <vector>
#include <MinHook.h>
#include <XInput.h>
#include <process.h>

#include "Logger.h"
#include "Mutex.h"

#include "InputHook.h"

bool InputHook::ReadGameDatabase()
{
    SWIP ini;
    std::string inipath("x360ce.gdb");
    if (!ini.Load(inipath))
        CheckCommonDirectory(&inipath, "x360ce");
    if (!ini.Load(inipath)) return false;

    PrintLog("Using game database file:");
    PrintLog(ini.GetIniPath().c_str());

    std::string processName;
    ModuleFileName(&processName);
    if (ini.Get(processName, "HookMask", &m_hookmask))
    {
        ini.Get(processName, "Timeout", &m_timeout, 30);

        std::string gameName;
        ini.Get(processName, "Name", &gameName);

        if (!gameName.empty())
            PrintLog("InputHook found \"%s\" in database", gameName.c_str());

        return true;
    }

    return false;
}

InputHook::InputHook() :
m_hookmask(0),
m_fakepidvid(MAKELONG(0x045E, 0x028E)),
m_timeout(0),
m_timeout_thread(INVALID_HANDLE_VALUE)
{
    PrintLog("InputHook starting...");

    SWIP ini;
    std::string inipath("x360ce.ini");
    if (!ini.Load(inipath))
        CheckCommonDirectory(&inipath, "x360ce");
    if (!ini.Load(inipath)) return;

    if (!ReadGameDatabase())
    {
        ini.Get("InputHook", "HookMask", &m_hookmask);
        if (!m_hookmask)
        {
            bool check = false;
            ini.Get("InputHook", "HookLL", &check);
            if (check) m_hookmask |= HOOK_LL;

            ini.Get("InputHook", "HookCOM", &check);
            if (check) m_hookmask |= HOOK_COM;

            ini.Get("InputHook", "HookDI", &check);
            if (check) m_hookmask |= HOOK_DI;

            ini.Get("InputHook", "HookPIDVID", &check);
            if (check) m_hookmask |= HOOK_PIDVID;

            ini.Get("InputHook", "HookSA", &check);
            if (check) m_hookmask |= HOOK_SA;

            ini.Get("InputHook", "HookNAME", &check);
            if (check) m_hookmask |= HOOK_NAME;

            ini.Get("InputHook", "HookSTOP", &check);
            if (check) m_hookmask |= HOOK_STOP;

            ini.Get("InputHook", "HookWT", &check);
            if (check) m_hookmask |= HOOK_WT;
        }

        ini.Get("InputHook", "Timeout", &m_timeout, 30);

        if (GetState(HOOK_PIDVID))
        {
            u32 vid;
            u32 pid;
            ini.Get("InputHook", "FakeVID", &vid, 0x045E);
            ini.Get("InputHook", "FakePID", &pid, 0x028E);

            if (vid != 0x045E || pid != 0x28E)
                m_fakepidvid = MAKELONG(vid, pid);
        }
    }

    if (m_hookmask)
    {
        // Initalize InputHook Devices
        for (u32 i = 0; i < XUSER_MAX_COUNT; ++i)
        {
            std::string section;
            std::string key = StringFormat("PAD%u", i + 1);
            if (!ini.Get("Mappings", key, &section))
                continue;

            u32 index = 0;
            if (!ini.Get(section, "UserIndex", &index))
                index = i;

            std::string buffer;
            GUID productid = GUID_NULL;
            GUID instanceid = GUID_NULL;

            if (ini.Get(section, "ProductGUID", &buffer))
                StringToGUID(&productid, buffer);

            if (ini.Get(section, "InstanceGUID", &buffer))
                StringToGUID(&instanceid, buffer);

            if (!IsEqualGUID(productid, GUID_NULL) && !IsEqualGUID(instanceid, GUID_NULL))
            {
                m_devices.push_back(InputHookDevice(index, productid, instanceid));
            }
        }
    }

    if (!m_devices.empty())
    {
        std::string maskname;
        if (MaskToName(&maskname, m_hookmask))
            PrintLog("HookMask 0x%08X: %s", m_hookmask, maskname.c_str());

        MH_Initialize();

        if (GetState(HOOK_LL))
            HookLL();

        if (GetState(HOOK_COM))
            HookCOM();

        if (GetState(HOOK_DI))
            HookDI();

        if (GetState(HOOK_SA))
            HookSA();

        if (GetState(HOOK_WT))
            HookWT();

        MH_EnableHook(MH_ALL_HOOKS);
    }
}

void InputHook::Shutdown()
{
    m_devices.clear();

    if (MH_Uninitialize() == MH_OK)
    {
        m_hookmask = 0;

        if (m_timeout_thread)
            CloseHandle(m_timeout_thread);

        PrintLog("InputHook shutdown");
    }
}

void InputHook::StartTimeoutThread()
{
    if (m_timeout_thread == INVALID_HANDLE_VALUE && m_timeout > 0)
        m_timeout_thread = (HANDLE)_beginthreadex(NULL, NULL, ThreadProc, this, NULL, NULL);
}

u32 WINAPI InputHook::ThreadProc(void* lpParameter)
{
    InputHook* pInputHook = reinterpret_cast<InputHook*>(lpParameter);

    PrintLog("Waiting %us for hooks...", pInputHook->m_timeout);
    Sleep(pInputHook->m_timeout * 1000);

    pInputHook->Shutdown();
    PrintLog("InputHook timed after %us", pInputHook->m_timeout);

    _endthreadex(0);
    return 0;
}

bool InputHook::MaskToName(std::string* mask_string, u32 mask)
{
    if (mask & HOOK_LL) mask_string->append("HOOK_LL ");
    if (mask & HOOK_COM) mask_string->append("HOOK_COM ");
    if (mask & HOOK_DI) mask_string->append("HOOK_DI ");
    if (mask & HOOK_PIDVID) mask_string->append("HOOK_PIDVID ");
    if (mask & HOOK_NAME) mask_string->append("HOOK_NAME ");
    if (mask & HOOK_SA) mask_string->append("HOOK_SA ");
    if (mask & HOOK_WT) mask_string->append("HOOK_WT ");
    if (mask & HOOK_STOP) mask_string->append("HOOK_STOP ");

    mask_string->pop_back();
    return !mask_string->empty();
}

