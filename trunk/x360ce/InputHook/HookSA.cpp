#include "stdafx.h"
#include "globals.h"
#include "Logger.h"
#include "Misc.h"

#include <Setupapi.h>
#pragma comment(lib,"Setupapi.lib")

#include "InputHook.h"

namespace HookSA
{
    static InputHook *s_InputHook = nullptr;

    typedef BOOL(WINAPI* SetupDiGetDeviceInstanceIdW_t)(
        _In_       HDEVINFO DeviceInfoSet,
        _In_       PSP_DEVINFO_DATA DeviceInfoData,
        _Out_opt_  PWSTR DeviceInstanceId,
        _In_       DWORD DeviceInstanceIdSize,
        _Out_opt_  PDWORD RequiredSize
        );

    // NOTE: SetupDiGetDeviceInstanceIdW is called inside SetupDiGetDeviceInstanceIdA
    static SetupDiGetDeviceInstanceIdW_t TrueSetupDiGetDeviceInstanceIdW = nullptr;

    BOOL WINAPI HookSetupDiGetDeviceInstanceIdW(
        _In_       HDEVINFO DeviceInfoSet,
        _In_       PSP_DEVINFO_DATA DeviceInfoData,
        _Out_opt_  PWSTR DeviceInstanceId,
        _In_       DWORD DeviceInstanceIdSize,
        _Out_opt_  PDWORD RequiredSize
        )
    {
        BOOL ret = TrueSetupDiGetDeviceInstanceIdW(DeviceInfoSet, DeviceInfoData, DeviceInstanceId, DeviceInstanceIdSize, RequiredSize);
        if (!s_InputHook->GetState(InputHook::HOOK_SA)) return ret;
        PrintLog("SetupDiGetDeviceInstanceId");

        if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) return ret;

        if (DeviceInstanceId && ret)
        {
            if (wcsstr(DeviceInstanceId, L"IG_") && !s_InputHook->GetState(InputHook::HOOK_PIDVID))
            {
                PrintLog("Xinput device skipped");
                return ret;
            }

            DWORD dwPid = 0, dwVid = 0;

            wchar_t* strVid = wcsstr(DeviceInstanceId, L"VID_");
            if (!strVid || swscanf_s(strVid, L"VID_%4X", &dwVid) < 1)
                return ret;

            wchar_t* strPid = wcsstr(DeviceInstanceId, L"PID_");
            if (!strPid || swscanf_s(strPid, L"PID_%4X", &dwPid) < 1)
                return ret;

            for (auto padcfg = s_InputHook->begin(); padcfg != s_InputHook->end(); ++padcfg)
            {
                if (padcfg->GetHookState() && padcfg->GetProductPIDVID() == (DWORD)MAKELONG(dwVid, dwPid))
                {
                    const wchar_t* strUSB = wcsstr(DeviceInstanceId, L"USB\\");
                    const wchar_t* strRoot = wcsstr(DeviceInstanceId, L"root\\");
                    OLECHAR tempstr[MAX_PATH];

                    DWORD dwHookVid = s_InputHook->GetState(InputHook::HOOK_PIDVID) ? LOWORD(s_InputHook->GetFakePIDVID()) : LOWORD(padcfg->GetProductPIDVID());
                    DWORD dwHookPid = s_InputHook->GetState(InputHook::HOOK_PIDVID) ? HIWORD(s_InputHook->GetFakePIDVID()) : HIWORD(padcfg->GetProductPIDVID());

                    if (strUSB || strRoot)
                    {
                        const wchar_t* p = wcsrchr(DeviceInstanceId, L'\\');
                        if (p) swprintf_s(tempstr, L"USB\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);
                        else swprintf_s(tempstr, L"USB\\VID_%04X&PID_%04X&IG_%02d", dwHookVid, dwHookPid, padcfg->GetUserIndex());

                        if (DeviceInstanceIdSize < wcslen(tempstr))
                        {
                            SetLastError(ERROR_INSUFFICIENT_BUFFER);
                            if (RequiredSize) *RequiredSize = (DWORD)wcslen(tempstr) + 1;
                            //return FALSE; //NOTE: return FALSE here breaks Beat Hazard
                            continue;
                        }

                        if (DeviceInstanceIdSize > wcslen(tempstr))
                        {
                            PrintLog("Device string change:", DeviceInstanceId);
                            PrintLog("%ls", DeviceInstanceId);
                            wcscpy_s(DeviceInstanceId, DeviceInstanceIdSize, tempstr);
                            if (RequiredSize) *RequiredSize = (DWORD)wcslen(tempstr) + 1;
                            PrintLog("%ls", DeviceInstanceId);
                            continue;
                        }
                    }

                    wchar_t* strHID = wcsstr(DeviceInstanceId, L"HID\\");

                    if (strHID)
                    {
                        wchar_t* p = wcsrchr(DeviceInstanceId, L'\\');
                        swprintf_s(tempstr, L"HID\\VID_%04X&PID_%04X&IG_%02d%s", dwHookVid, dwHookPid, padcfg->GetUserIndex(), p);

                        if (DeviceInstanceIdSize < wcslen(tempstr))
                        {
                            SetLastError(ERROR_INSUFFICIENT_BUFFER);
                            if (RequiredSize) *RequiredSize = (DWORD)wcslen(tempstr) + 1;
                            //return FALSE; //NOTE: return FALSE here breaks Beat Hazard
                            continue;
                        }

                        if (DeviceInstanceIdSize > wcslen(tempstr))
                        {
                            PrintLog("Device string change:", DeviceInstanceId);
                            PrintLog("%ls", DeviceInstanceId);
                            wcscpy_s(DeviceInstanceId, DeviceInstanceIdSize, tempstr);
                            if (RequiredSize) *RequiredSize = (DWORD)wcslen(tempstr) + 1;
                            PrintLog("%ls", DeviceInstanceId);
                            continue;
                        }
                    }
                }
            }
        }

        return ret;
    }
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void InputHook::HookSA()
{
    PrintLog("Hooking SetupApi");
    HookSA::s_InputHook = this;

    IH_CreateHook(SetupDiGetDeviceInstanceIdW, HookSA::HookSetupDiGetDeviceInstanceIdW, &HookSA::TrueSetupDiGetDeviceInstanceIdW);
}
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////