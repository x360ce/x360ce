#pragma once

#include "version.h"

#include <atomic>
#include <vector>
#include <dinput.h>

#include "NonCopyable.h"
#include "ControllerBase.h"
#include "Controller.h"
#include "ControllerCombiner.h"

#include "InputHookManager.h"

class ControllerManager : NonCopyable
{
public:
	ControllerManager()
	{
		std::string processName;
		ModuleFileName(&processName);
#ifndef _M_X64
		PrintLog("x360ce (x86) %s started for \"%s\"", PRODUCT_VERSION, processName.c_str());
#else
		PrintLog("x360ce (x64) %s started for \"%s\"", PRODUCT_VERSION, processName.c_str());
#endif
		std::string windows_name;
		if (GetWindowsVersionName(&windows_name))
			PrintLog("OS: \"%s\"", windows_name.c_str());

		// Pass the list rather than letting ReadConfig reach back through Get(),
		// which would re-enter the static initialisation still running on this thread.
		m_config.ReadConfig(m_controllers);

		bool bHookDI = InputHookManager::Get().GetInputHook().GetState(InputHook::HOOK_DI);
		if (bHookDI) InputHookManager::Get().GetInputHook().DisableHook(InputHook::HOOK_DI);
		HRESULT ret = DirectInput8Create(CurrentModule(), DIRECTINPUT_VERSION, IID_IDirectInput8A, (void**)&m_pDirectInput, NULL);
		if (bHookDI) InputHookManager::Get().GetInputHook().EnableHook(InputHook::HOOK_DI);

		if (m_pDirectInput)
		{
			m_hWnd = CreateWindowExA(0L,
				"Message",	// name of window class
				"x360ce",			// title-bar std::string
				WS_TILED,			// normal window
				CW_USEDEFAULT,		// default horizontal position
				CW_USEDEFAULT,		// default vertical position
				CW_USEDEFAULT,		// default width
				CW_USEDEFAULT,		// default height
				HWND_MESSAGE,		// message-only window
				NULL,				// no class menu
				CurrentModule(),	// handle to application instance
				NULL);				// no window-creation data

			if (!m_hWnd)
				PrintLog("CreateWindow failed with code 0x%x", HRESULT_FROM_WIN32(GetLastError()));
		}

		if (FAILED(ret) || !m_pDirectInput)
		{
			PrintLog("DirectInput cannot be initialized");
			MessageBox(NULL, "DirectInput cannot be initialized", "x360ce - Error", MB_ICONERROR);
			exit(ret);
		}

		StartScan();
	}

	~ControllerManager()
	{
		// The scan has ended with the process; a library still in use is never unloaded while it runs.
		if (m_scanStop)
		{
			SetEvent(m_scanStop);
			CloseHandle(m_scanStop);
		}
		if (m_scanThread)
			CloseHandle(m_scanThread);
		m_autoControllers.clear();

		// Remove controllers
		m_controllers.clear();

		// Destroy window
		if (m_hWnd)
			DestroyWindow(m_hWnd);

		PrintLog("ControllerManager shutdown");
	}

	DWORD DeviceInitialize(DWORD dwUserIndex, ControllerBase** ppController)
	{
		// Global disable
		if (m_config.m_globalDisable)
			return ERROR_DEVICE_NOT_CONNECTED;

		// Invalid dwUserIndex
		if (!(dwUserIndex < XUSER_MAX_COUNT))
			return ERROR_BAD_ARGUMENTS;

		ControllerBase* pController = nullptr;
		for (auto it = m_controllers.begin(); it != m_controllers.end(); ++it)
		{
			// Return the first controller that matches
			if ((*it)->m_user == dwUserIndex)
			{
				pController = (*it).get();
				break;
			}
		}

		// A controller the scan mapped into a free place, ready to read: never waits for the scan.
		if (!pController && m_config.m_autoMap)
			pController = m_slots[dwUserIndex].load(std::memory_order_acquire);

		if (!pController)
			return ERROR_DEVICE_NOT_CONNECTED;
		if (ppController) *ppController = pController;

		// passtrough
		if (pController->m_passthrough)
		{
			return ERROR_SUCCESS;
		}

		if (pController->m_failcount > 20)
			return ERROR_DEVICE_NOT_CONNECTED;

		if (!pController->Initalized())
		{
			DWORD result = pController->CreateDevice();
			if (result == ERROR_SUCCESS)
			{
				PrintLog("[PAD%d] Initialized for user %d", dwUserIndex + 1, dwUserIndex);
				if (m_config.m_initBeep) MessageBeep(MB_OK);
			}
			else
			{
				++pController->m_failcount;
				return result;
			}
		}

		if (!pController->Initalized())
			return ERROR_DEVICE_NOT_CONNECTED;
		else return ERROR_SUCCESS;
	}

	static ControllerManager& Get()
	{
		static ControllerManager instance;
		return instance;
	}

	std::unique_ptr<IDirectInput8A, COMDeleter>& GetDirectInput()
	{
		return m_pDirectInput;
	}

	HWND& GetWindow()
	{
		return m_hWnd;
	}

	std::vector<std::shared_ptr<ControllerBase>>& GetControllers()
	{
		return m_controllers;
	}

	Config& GetConfig()
	{
		return m_config;
	}

	bool XInputEnabled()
	{
		if (!enabled && useEnabled)
			return false;
		else
			return true;
	}

	/// <summary>Reads x360ce.ini again, forgets the controllers mapped from SDL, and scans again when the file asks for it.</summary>
	void Reset();

	void XInputEnable(BOOL enable)
	{
		/*
		Trick to support XInputEnable states, because not every game calls it, so:
		- must support games that call it:
		if bEnabled is FALSE and bUseEnabled is TRUE = device is disabled -> return S_OK, ie. connected but state not updating
		if bEnabled is TRUE and bUseEnabled is TRUE = device is enabled -> continue, ie. connected and updating state
		- must support games that not call it:
		if bUseEnabled is FALSE ie. XInputEnable was not called -> do not care about XInputEnable states
		*/

		enabled = (enable != 0);
		useEnabled = true;

		if (enabled) PrintLog("XInput Enabled");
		else PrintLog("XInput Disabled");
	}

private:
	HWND m_hWnd;
	Config m_config;
	std::unique_ptr<IDirectInput8A, COMDeleter> m_pDirectInput;
	std::vector<std::shared_ptr<ControllerBase>> m_controllers;

	#pragma region Mapping from SDL

	/// <summary>How often the scan looks for controllers plugged in, in milliseconds.</summary>
	static const DWORD ScanEveryMs = 2000;

	/// <summary>A DirectInput device the scan has looked at, by its instance: what it is, and whether it was dealt with.</summary>
	struct SeenDevice
	{
		GUID instance;
		bool xinput;
		bool sdlKnown;
		bool done;
	};

	/// <summary>The controller the scan put in each free place, published whole: a game's call reads it and never waits.</summary>
	std::atomic<ControllerBase*> m_slots[XUSER_MAX_COUNT] = {};
	/// <summary>The controllers in m_slots, kept until Reset or the end of the process.</summary>
	std::vector<std::shared_ptr<ControllerBase>> m_autoControllers;
	std::vector<SeenDevice> m_seen;
	/// <summary>The system XInput places a controller in m_slots passes through.</summary>
	bool m_xinputServed[XUSER_MAX_COUNT] = {};
	/// <summary>Held by a scan and by Reset; never by a game's call.</summary>
	Mutex m_scanLock;
	HANDLE m_scanThread = NULL;
	HANDLE m_scanStop = NULL;

	void StartScan();
	static DWORD WINAPI ScanThread(LPVOID parameter);
	void Scan();
	size_t Examine(const DIDEVICEINSTANCEA& instance);
	void Publish(int place, const std::shared_ptr<ControllerBase>& controller, bool* used);

	#pragma endregion

	bool enabled;
	bool useEnabled;
};
