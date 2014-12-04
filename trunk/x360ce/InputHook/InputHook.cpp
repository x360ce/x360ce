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

#include "Logger.h"
#include "Mutex.h"

#include "InputHook.h"

bool InputHook::ReadGameDatabase(u32* mask)
{
    std::string inipath;
    CheckCommonDirectory(&inipath, "x360ce.gdb", "x360ce");

    SWIP ini;
    if (ini.Load(inipath))
    {
        PrintLog("Using game database file:");
        PrintLog(ini.GetIniPath().c_str());

        std::string processName;
        ModuleFileName(&processName);
        if (ini.Get(processName, "HookMask", mask))
        {
            ini.Get(processName, "Timeout", &m_timeout, 0);

            std::string gameName;
            ini.Get(processName, "Name", &gameName);

            if (!gameName.empty())
                PrintLog("InputHook found \"%s\" in database", gameName.c_str());

            return true;
        }
    }
    return false;
}

InputHook::InputHook() :
m_hookmask(0),
m_fakepidvid(MAKELONG(0x045E, 0x028E)),
m_timeout(30),
m_timeout_thread(INVALID_HANDLE_VALUE)
{
    LockGuard lock(m_mutex);
    PrintLog("InputHook starting...");

    std::string inipath;
    CheckCommonDirectory(&inipath, "x360ce.ini", "x360ce");

    SWIP ini;
    ini.Load(inipath);

    if (!ReadGameDatabase(&m_hookmask))
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

            ini.Get("InputHook", "HookNoTimeout", &check);
            if (check) m_hookmask |= HOOK_NOTIMEOUT;
        }
    }

    if (!m_hookmask)
        return;

    if (GetState(HOOK_PIDVID))
    {
        u32 vid;
        u32 pid;
        ini.Get("InputHook", "FakeVID", &vid, 0x045E);
        ini.Get("InputHook", "FakePID", &pid, 0x028E);

        if (vid != 0x045E || pid != 0x28E)
            m_fakepidvid = MAKELONG(vid, pid);
    }

    if (!m_timeout)
        ini.Get("InputHook", "Timeout", &m_timeout, 30);

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


    std::string maskname = MaskToName(m_hookmask);
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

void InputHook::Shutdown()
{
    if (MH_Uninitialize() == MH_OK)
    {
        m_devices.clear();
        m_hookmask = 0;

        if (m_timeout_thread)
            CloseHandle(m_timeout_thread);

        PrintLog("InputHook shutdown");
    }
}

void InputHook::StartTimeoutThread()
{
    if (m_timeout_thread == INVALID_HANDLE_VALUE && m_timeout > 0 && !GetState(HOOK_NOTIMEOUT))
        m_timeout_thread = CreateThread(NULL, NULL, ThreadProc, this, NULL, NULL);
}

DWORD WINAPI InputHook::ThreadProc(_In_  LPVOID lpParameter)
{
    InputHook* pInputHook = reinterpret_cast<InputHook*>(lpParameter);

    PrintLog("Waiting %us for hooks...", pInputHook->m_timeout);
    Sleep(pInputHook->m_timeout * 1000);

    pInputHook->Shutdown();
    PrintLog("Hook timeouted after %us", pInputHook->m_timeout);
    return 0;
}

std::string InputHook::MaskToName(u32 mask)
{
    std::string ret;

    if (mask & HOOK_LL) ret.append("HOOK_LL ");
    if (mask & HOOK_COM) ret.append("HOOK_COM ");
    if (mask & HOOK_DI) ret.append("HOOK_DI ");
    if (mask & HOOK_SA) ret.append("HOOK_SA ");
    if (mask & HOOK_WT) ret.append("HOOK_WT ");

    ret.pop_back();
    return ret;
}

