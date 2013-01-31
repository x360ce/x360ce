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

#ifndef _InputHook_H_
#define _InputHook_H_

#include <CGuid.h>
#include <vector>
#include <MinHook.h>
#include "Log.h"

class iHookDevice
{
public:
    iHookDevice(GUID productid, GUID instanceid)
        :m_enabled(true)
        ,m_productid(productid)
        ,m_instanceid(instanceid)
    {}
    virtual ~iHookDevice() {};

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

    inline DWORD GetProductVIDPID()
    {
        return m_productid.Data1;
    }

private:
    bool  m_enabled;
    GUID  m_productid;
    GUID  m_instanceid;
};

class iHook
{
public:
    iHook()
        :m_hookmask(0)
        ,m_fakepidvid(MAKELONG(0x045E,0x028E))
    {
    }
    virtual ~iHook()
    {
        MH_Uninitialize();
        m_devices.clear();
    };

    static const DWORD HOOK_NONE        = 0x00000000;
    static const DWORD HOOK_COM         = 0x00000001;
    static const DWORD HOOK_DI          = 0x00000002;
    static const DWORD HOOK_VIDPID      = 0x00000004;
    static const DWORD HOOK_NAME        = 0x00000008;
    static const DWORD HOOK_STOP        = 0x00000010;
    static const DWORD HOOK_WT          = 0x00000020;
    static const DWORD HOOK_ENABLE      = 0x80000000;

    inline VOID Enable()
    {
        m_hookmask |= HOOK_ENABLE;
    }

    inline VOID Disable()
    {
        m_hookmask &= ~HOOK_ENABLE;
    }

    inline VOID EnableHook(const DWORD flag)
    {
        m_hookmask |= flag;
    }

    inline VOID DisableHook(const DWORD flag)
    {
        m_hookmask &= ~flag;
    }

    inline const bool CheckHook(const DWORD flag) const
    {
        return (m_hookmask & (flag | HOOK_ENABLE)) == (flag | HOOK_ENABLE);
    }

    inline const bool GetState() const
    {
        return (m_hookmask & HOOK_ENABLE) == HOOK_ENABLE;
    }

    inline void SetMask(const DWORD mask)
    {
        m_hookmask = mask;
    }

    inline void SetFakePIDVID(const DWORD pidvid)
    {
        m_fakepidvid = pidvid;
    }

    inline DWORD GetFakePIDVID()
    {
        return m_fakepidvid;
    }

    inline size_t GetHookCount()
    {
        return m_devices.size();
    }

    inline iHookDevice& GetPadConfig(const DWORD dwUserIndex)
    {
        return m_devices[dwUserIndex];
    }

    inline VOID AddHook(iHookDevice& config)
    {
        m_devices.push_back(config);
    }

    inline VOID AddHook(GUID& productid, GUID& instanceid)
    {
        m_devices.emplace_back(productid,instanceid);
    }

    inline VOID ExecuteHooks()
    {

        if(!GetState()) return;
        PrintLog(LOG_IHOOK,"InputHook starting with mask 0x%08X",m_hookmask);

        MH_Initialize();

        if(CheckHook(HOOK_COM))
            HookCOM();

        if(CheckHook(HOOK_DI))
            HookDI();

        if(CheckHook(HOOK_WT))
            HookWT();
        return;
    }

private:
    DWORD m_hookmask;
    DWORD m_fakepidvid;
    std::vector<iHookDevice> m_devices;

    void HookCOM();
    void HookDI();
    void HookWT();
};

#endif
