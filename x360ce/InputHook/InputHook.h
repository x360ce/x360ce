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

#pragma once

#include <CGuid.h>
#include <vector>
#include <MinHook.h>
#include "Logger.h"

#include "Mutex.h"

static const char* status_names[] = {
    "MH_OK",
    "MH_ERROR_ALREADY_INITIALIZED",
    "MH_ERROR_NOT_INITIALIZED",
    "MH_ERROR_ALREADY_CREATED",
    "MH_ERROR_NOT_CREATED",
    "MH_ERROR_ENABLED",
    "MH_ERROR_DISABLED",
    "MH_ERROR_NOT_EXECUTABLE",
    "MH_ERROR_UNSUPPORTED_FUNCTION",
    "MH_ERROR_MEMORY_ALLOC",
    "MH_ERROR_MEMORY_PROTECT",
};

#define IH_CreateHook(pTarget, pDetour, ppOrgiginal) IH_CreateHookF(pTarget, pDetour, ppOrgiginal, #pTarget)
#define IH_EnableHook(pTarget) IH_EnableHookF(pTarget, #pTarget)

template<typename N>
inline void IH_CreateHookF(LPVOID pTarget, LPVOID pDetour, N* ppOriginal, const char* pTargetName)
{
    MH_STATUS status = MH_CreateHook(pTarget, pDetour, reinterpret_cast<void**>(ppOriginal));
    if (status == MH_OK || status == MH_ERROR_ALREADY_CREATED)
    {
        PrintLog("Hook for %s successed", pTargetName);
    }
    else
    {
        if (status == MH_UNKNOWN) PrintLog("Hook for %s failed with MH_UNKNOWN", pTargetName);
        else PrintLog("Hook for %s failed with %s", pTargetName, status_names[status]);
    }
}

inline void IH_EnableHookF(LPVOID pTarget, const char* pTargetName)
{
    MH_STATUS status = MH_EnableHook(pTarget);
    if (status == MH_OK || status == MH_ERROR_ENABLED)
    {
        PrintLog("Hook for %s enabled", pTargetName);
    }
    else
    {
        if (status == MH_UNKNOWN) PrintLog("Hook for %s failed with MH_UNKNOWN", pTargetName);
        else PrintLog("Hook for %s failed with %s", pTargetName, status_names[status]);
    }
}

class InputHookDevice
{
public:
    InputHookDevice(DWORD userindex, const GUID& productid, const GUID& instanceid)
        :m_enabled(true)
        , m_productid(productid)
        , m_instanceid(instanceid)
        , m_userindex(userindex)
    {}
    virtual ~InputHookDevice() {};

    inline void Enable()
    {
        m_enabled = true;
    }

    inline void Disable()
    {
        m_enabled = false;
    }

    inline bool GetHookState()
    {
        return m_enabled;
    }

    inline GUID GetProductGUID()
    {
        return m_productid;
    }

    inline GUID GetInstanceGUID()
    {
        return m_instanceid;
    }

    inline DWORD GetProductPIDVID()
    {
        return m_productid.Data1;
    }

    inline DWORD GetUserIndex()
    {
        return m_userindex;
    }

private:
    bool  m_enabled;
    GUID  m_productid;
    GUID  m_instanceid;
    DWORD m_userindex;
};

class InputHook
{
public:
    InputHook()
        :m_hookmask(HOOK_DISABLE)
        , m_fakepidvid(MAKELONG(0x045E, 0x028E))
        , m_timeout(60)
    {
    }
    virtual ~InputHook()
    {
        LockGuard lock(m_mutex);
        MH_Uninitialize();
        m_devices.clear();

        if (m_timeout_thread) CloseHandle(m_timeout_thread);
        m_timeout_thread = 0;
    };

    static const DWORD HOOK_NONE      = 0x00000000UL;
    static const DWORD HOOK_LL        = 0x00000001UL;
    static const DWORD HOOK_COM       = 0x00000002UL;
    static const DWORD HOOK_DI        = 0x00000004UL;
    static const DWORD HOOK_PIDVID    = 0x00000008UL;
    static const DWORD HOOK_NAME      = 0x00000010UL;
    static const DWORD HOOK_SA        = 0x00000020UL;

    static const DWORD HOOK_WT        = 0x10000000UL;
    static const DWORD HOOK_STOP      = 0x20000000UL;
    static const DWORD HOOK_NOTIMEOUT = 0x40000000UL;
    static const DWORD HOOK_DISABLE   = 0x80000000UL;

    typedef std::vector<InputHookDevice>::iterator iterator;
    typedef std::vector<InputHookDevice>::const_iterator const_iterator;

    iterator begin() { return m_devices.begin(); }
    iterator end() { return m_devices.end(); }

    const_iterator begin() const { return m_devices.begin(); }
    const_iterator end() const { return m_devices.end(); }

    const_iterator cbegin() const { return m_devices.cbegin(); }
    const_iterator cend() const { return m_devices.cend(); }

    inline void Enable()
    {
        m_hookmask &= ~HOOK_DISABLE;
    }

    inline void Disable()
    {
        m_hookmask |= HOOK_DISABLE;
    }

    inline void EnableHook(const DWORD& flag)
    {
        m_hookmask |= flag;
    }

    inline void DisableHook(const DWORD& flag)
    {
        m_hookmask &= ~flag;
    }

    inline const bool GetState(const DWORD& flag = HOOK_NONE) const
    {
        if (m_hookmask & HOOK_DISABLE || m_hookmask == HOOK_NONE) return false;
        return (m_hookmask & flag) == flag;
    }

    inline DWORD GetMask()
    {
        return m_hookmask;
    }

    inline void SetMask(const DWORD& mask)
    {
        m_hookmask = mask;
    }

    inline void SetFakePIDVID(const DWORD& pidvid)
    {
        m_fakepidvid = pidvid;
    }

    inline DWORD GetFakePIDVID()
    {
        return m_fakepidvid;
    }

    inline void SetTimeout(const DWORD& timeout)
    {
        m_timeout = timeout;
    }

    inline InputHookDevice& GetPadConfig(const DWORD& dwUserIndex)
    {
        return m_devices.at(dwUserIndex);
    }
    inline void AddHook(DWORD userindex, const GUID& productid, const GUID& instanceid)
    {
        InputHookDevice dev(userindex, productid, instanceid);
        m_devices.push_back(dev);
    }

    inline HMODULE GetEmulator()
    {
        return CurrentModule();
    }

    inline static DWORD WINAPI ThreadProc(_In_  LPVOID lpParameter)
    {
        DWORD* pTimeout = reinterpret_cast<DWORD*>(lpParameter);

        PrintLog("Waiting for hooks...");
        Sleep(*pTimeout * 1000);

        PrintLog("Hook timeout");
        MH_Uninitialize();
        return 0;
    }

    inline void ExecuteHooks()
    {
        if (!GetState())
        {
            m_devices.clear();
            return;
        }

        LockGuard lock(m_mutex);

        PrintLog("InputHook starting with mask 0x%08X", m_hookmask);

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

    void Reset()
    {
        m_devices.clear();
        if (m_timeout_thread) CloseHandle(m_timeout_thread);
        m_timeout_thread = 0;

        m_hookmask = HOOK_DISABLE;
        m_fakepidvid = MAKELONG(0x045E, 0x028E);
        m_timeout = 60;
    }

    void StartTimeoutThread()
    {
        if (!m_timeout_thread && m_timeout > 0 && !GetState(HOOK_NOTIMEOUT))
            m_timeout_thread = CreateThread(NULL, NULL, ThreadProc, &m_timeout, NULL, NULL);
    }

    void HookDICOM(REFIID riidltf, LPVOID *ppv);

private:
    DWORD m_hookmask;
    DWORD m_fakepidvid;
    DWORD m_timeout;
    HANDLE m_timeout_thread;

    std::vector<InputHookDevice> m_devices;

    void HookLL();
    void HookCOM();
    void HookDI();
    void HookWT();
    void HookSA();

    Mutex m_mutex;
};

