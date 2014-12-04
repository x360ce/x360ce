#pragma once

#include <CGuid.h>
#include <vector>
#include <MinHook.h>
#include "Logger.h"

#include "Mutex.h"

static const char* status_names[] =
{
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

template<typename N>
void IH_CreateHookF(LPVOID pTarget, LPVOID pDetour, N* ppOriginal, const char* pTargetName)
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

#define IH_CreateHook(pTarget, pDetour, ppOrgiginal) IH_CreateHookF(pTarget, pDetour, ppOrgiginal, #pTarget)
#define IH_EnableHook(pTarget) IH_EnableHookF(pTarget, #pTarget)

class InputHookDevice
{
public:
    InputHookDevice(DWORD userindex, const GUID& productid, const GUID& instanceid)
        : m_productid(productid)
        , m_instanceid(instanceid)
        , m_userindex(userindex)
    {
    }
    virtual ~InputHookDevice() { };

    GUID GetProductGUID()
    {
        return m_productid;
    }

    GUID GetInstanceGUID()
    {
        return m_instanceid;
    }

    u32 GetProductPIDVID()
    {
        return m_productid.Data1;
    }

    u32 GetUserIndex()
    {
        return m_userindex;
    }

private:
    GUID  m_productid;
    GUID  m_instanceid;
    u32 m_userindex;
};

class InputHook
{
public:
    InputHook();
    ~InputHook()
    {
        Shutdown();
    };

    void Shutdown();

    static const u32 HOOK_LL =        0x00000001UL;
    static const u32 HOOK_COM =       0x00000002UL;
    static const u32 HOOK_DI =        0x00000004UL;
    static const u32 HOOK_PIDVID =    0x00000008UL;
    static const u32 HOOK_NAME =      0x00000010UL;
    static const u32 HOOK_SA =        0x00000020UL;
    static const u32 HOOK_WT =        0x10000000UL;
    static const u32 HOOK_STOP =      0x20000000UL;
    static const u32 HOOK_NOTIMEOUT = 0x40000000UL;

    typedef std::vector<InputHookDevice>::iterator iterator;
    typedef std::vector<InputHookDevice>::const_iterator const_iterator;

    iterator begin() { return m_devices.begin(); }
    iterator end() { return m_devices.end(); }

    const_iterator begin() const { return m_devices.begin(); }
    const_iterator end() const { return m_devices.end(); }

    const_iterator cbegin() const { return m_devices.cbegin(); }
    const_iterator cend() const { return m_devices.cend(); }

    const bool GetState(const DWORD& flag) const
    {
        if (!m_hookmask) return false;
        return (m_hookmask & flag) == flag;
    }

    void EnableHook(const DWORD& flag)
    {
        m_hookmask |= flag;
    }

    void DisableHook(const DWORD& flag)
    {
        m_hookmask &= ~flag;
    }

    DWORD GetFakePIDVID()
    {
        return m_fakepidvid;
    }

    InputHookDevice* GetPadConfig(const DWORD& dwUserIndex)
    {
        return &m_devices[dwUserIndex];
    }

    HMODULE& GetEmulator()
    {
        return CurrentModule();
    }

    void Reset();

    void StartTimeoutThread();

private:
    static DWORD WINAPI ThreadProc(_In_  LPVOID lpParameter);
    bool ReadGameDatabase(u32* mask);
    std::string MaskToName(u32 mask);

    u32 m_hookmask;
    u32 m_fakepidvid;
    u32 m_timeout;
    HANDLE m_timeout_thread;

    std::vector<InputHookDevice> m_devices;

    void HookLL();
    void HookCOM();
    void HookDI();
    void HookWT();
    void HookSA();

    Mutex m_mutex;
};

