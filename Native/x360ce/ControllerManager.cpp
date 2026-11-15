#include "stdafx.h"

#include "Common.h"
#include "InputHook.h"

#include "Config.h"
#include "InputHookManager.h"
#include "ControllerManager.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"
#include "ControllerBase.h"
#include "Controller.h"
#include "ControllerCombiner.h"
#include "Sdl.h"

#include "XInputModuleManager.h"

namespace
{
	BOOL CALLBACK EnumDevicesCallback(LPCDIDEVICEINSTANCEA instance, LPVOID context)
	{
		((std::vector<DIDEVICEINSTANCEA>*)context)->push_back(*instance);
		return DIENUM_CONTINUE;
	}

	/// <summary>Turns the DirectInput and SetupAPI hooks off while the library reads devices for itself, as Controller::CreateDevice does.</summary>
	class HooksOff : NonCopyable
	{
	public:
		HooksOff()
		{
			InputHook& hook = InputHookManager::Get().GetInputHook();
			m_di = hook.GetState(InputHook::HOOK_DI);
			m_sa = hook.GetState(InputHook::HOOK_SA);
			if (m_di) hook.DisableHook(InputHook::HOOK_DI);
			if (m_sa) hook.DisableHook(InputHook::HOOK_SA);
		}

		~HooksOff()
		{
			InputHook& hook = InputHookManager::Get().GetInputHook();
			if (m_sa) hook.EnableHook(InputHook::HOOK_SA);
			if (m_di) hook.EnableHook(InputHook::HOOK_DI);
		}

	private:
		bool m_di;
		bool m_sa;
	};

	/// <summary>The first place no section and no mapped controller holds, or -1.</summary>
	int FreePlace(const bool* used)
	{
		for (int i = 0; i < XUSER_MAX_COUNT; ++i)
			if (!used[i])
				return i;
		return -1;
	}

	/// <summary>Adds a section's controller, or each controller a combining section joins, to the list of those the file maps.</summary>
	void AddListed(const std::shared_ptr<ControllerBase>& controller, std::vector<Controller*>* listed)
	{
		auto combiner = std::dynamic_pointer_cast<ControllerCombiner>(controller);
		if (combiner)
		{
			for (auto& child : combiner->GetControllers())
				AddListed(child, listed);
			return;
		}
		Controller* pController = dynamic_cast<Controller*>(controller.get());
		if (pController)
			listed->push_back(pController);
	}
}

void ControllerManager::StartScan()
{
	if (m_scanThread || !m_config.m_autoMap)
		return;
	// The scan runs as long as the process: the library stays loaded while it does.
	HMODULE pinned;
	GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN, (LPCSTR)&ScanThread, &pinned);
	m_scanStop = CreateEventA(NULL, TRUE, FALSE, NULL);
	m_scanThread = CreateThread(NULL, 0, ScanThread, this, 0, NULL);
	if (!m_scanThread)
		PrintLog("The controller scan cannot start: 0x%x", GetLastError());
}

DWORD WINAPI ControllerManager::ScanThread(LPVOID parameter)
{
	ControllerManager* self = (ControllerManager*)parameter;
	do
		self->Scan();
	while (WaitForSingleObject(self->m_scanStop, ScanEveryMs) == WAIT_TIMEOUT);
	return 0;
}

void ControllerManager::Reset()
{
	LockGuard lock(m_scanLock);
	for (auto& slot : m_slots)
		slot.store(nullptr, std::memory_order_release);
	m_autoControllers.clear();
	m_seen.clear();
	for (auto& served : m_xinputServed)
		served = false;
	m_controllers.clear();
	m_config.ReadConfig(m_controllers);
	StartScan();
}

/// <summary>Looks at a DirectInput device once: whether it is an XInput controller, and whether a controller database knows it.</summary>
/// <returns>Its place in m_seen.</returns>
size_t ControllerManager::Examine(const DIDEVICEINSTANCEA& instance)
{
	for (size_t i = 0; i < m_seen.size(); ++i)
		if (IsEqualGUID(m_seen[i].instance, instance.guidInstance))
			return i;
	SeenDevice seen = { instance.guidInstance, false, false, false, 0, 0, std::string() };
	HooksOff off;
	IDirectInputDevice8A* created = nullptr;
	if (SUCCEEDED(m_pDirectInput->CreateDevice(instance.guidInstance, &created, NULL)))
	{
		std::unique_ptr<IDirectInputDevice8A, COMDeleter> device(created);
		seen.xinput = Sdl::IsXInputDevice(device.get());
		if (seen.xinput)
			PrintLog("[AUTO] \"%s\" is an XInput controller", instance.tszProductName);
		else if (SUCCEEDED(device->SetDataFormat(&c_dfDIJoystick2)))
		{
			Sdl::Device sdl;
			Sdl::ReadDevice(device.get(), &sdl);
			seen.vendorId = sdl.vendorId;
			seen.productId = sdl.productId;
			seen.name = sdl.name;
			bool x360ce = !Sdl::FindX360ceRow(sdl).empty();
			bool sdlKnows = !Sdl::FindRow(sdl).empty();
			seen.known = x360ce || sdlKnows;
			PrintLog("[AUTO] \"%s\" (%04X:%04X): %s", instance.tszProductName, sdl.vendorId, sdl.productId,
				x360ce && sdlKnows ? "x360ce's and SDL's databases know it" : x360ce ? "x360ce's database knows it"
				: sdlKnows ? "SDL's database knows it" : "neither database knows it, left unmapped");
		}
	}
	m_seen.push_back(seen);
	return m_seen.size() - 1;
}

void ControllerManager::Publish(int place, const std::shared_ptr<ControllerBase>& controller, bool* used)
{
	m_autoControllers.push_back(controller);
	m_slots[place].store(controller.get(), std::memory_order_release);
	used[place] = true;
}

/// <summary>
/// Maps the controllers no section lists into free places: Xbox controllers pass through to Windows' own XInput at
/// their own place when it is free, then every other controller x360ce's or SDL's database knows takes the next free place, in DirectInput's
/// order. Runs on its own thread, at the start and then every ScanEveryMs, so a controller plugged in is found.
/// </summary>
void ControllerManager::Scan()
{
	LockGuard lock(m_scanLock);
	if (!m_config.m_autoMap || !m_pDirectInput)
		return;
	// The places the sections hold, and the controllers they read.
	bool used[XUSER_MAX_COUNT] = {};
	std::vector<Controller*> listed;
	for (auto& controller : m_controllers)
	{
		if (controller->m_user < XUSER_MAX_COUNT)
			used[controller->m_user] = true;
		AddListed(controller, &listed);
	}
	for (int i = 0; i < XUSER_MAX_COUNT; ++i)
		if (m_slots[i].load(std::memory_order_relaxed))
			used[i] = true;

	std::vector<DIDEVICEINSTANCEA> devices;
	{
		HooksOff off;
		m_pDirectInput->EnumDevices(DI8DEVCLASS_GAMECTRL, EnumDevicesCallback, &devices, DIEDFL_ATTACHEDONLY);
	}
	std::vector<size_t> seen;
	for (const auto& device : devices)
		seen.push_back(Examine(device));

	// The devices sections read: by instance, else the first of the product, as Controller::CreateDevice opens them.
	std::vector<bool> isListed(devices.size());
	std::vector<u32> passedThrough;
	bool xinputReadByDirectInput = false;
	for (Controller* c : listed)
	{
		if (c->m_passthrough)
			passedThrough.push_back(c->m_passthroughindex);
		size_t at = devices.size();
		for (size_t i = 0; i < devices.size() && at == devices.size(); ++i)
			if (IsEqualGUID(devices[i].guidInstance, c->m_instanceid))
				at = i;
		for (size_t i = 0; i < devices.size() && at == devices.size(); ++i)
			if (!isListed[i] && IsEqualGUID(devices[i].guidProduct, c->m_productid))
				at = i;
		if (at == devices.size())
			continue;
		isListed[at] = true;
		if (m_seen[seen[at]].xinput && !c->m_passthrough)
			xinputReadByDirectInput = true;
	}

	// Xbox controllers. Not while a section reads one through DirectInput: its XInput place cannot be told, and a
	// game would see it twice.
	if (!xinputReadByDirectInput && XInputModuleManager::Available())
	{
		for (u32 x = 0; x < XUSER_MAX_COUNT; ++x)
		{
			if (m_xinputServed[x] || std::find(passedThrough.begin(), passedThrough.end(), x) != passedThrough.end())
				continue;
			XINPUT_STATE state;
			if (XInputModuleManager::Get().XInputGetState(x, &state) != ERROR_SUCCESS)
				continue;
			int place = used[x] ? FreePlace(used) : (int)x;
			if (place < 0)
				break;
			std::shared_ptr<Controller> controller(new Controller(place));
			controller->m_passthrough = true;
			controller->m_passthroughindex = x;
			Publish(place, controller, used);
			m_xinputServed[x] = true;
			PrintLog("[PAD%d] XInput controller %u passed through", place + 1, x + 1);
		}
	}

	// Every other controller a database knows; one neither knows stays unmapped.
	for (size_t i = 0; i < devices.size(); ++i)
	{
		SeenDevice& s = m_seen[seen[i]];
		if (isListed[i] || s.xinput || !s.known || s.done)
			continue;
		int place = FreePlace(used);
		if (place < 0)
			break;
		s.done = true;
		// Read as a section of its own: its GUIDs, and x360ce's database row for it when there is one.
		std::shared_ptr<Controller> controller(new Controller(place));
		IniFile pad;
		const std::string section("pad");
		std::string text;
		GUIDtoString(&text, devices[i].guidProduct);
		pad.Set(section, "ProductGUID", text);
		GUIDtoString(&text, devices[i].guidInstance);
		pad.Set(section, "InstanceGUID", text);
		Sdl::Device device;
		device.vendorId = s.vendorId;
		device.productId = s.productId;
		device.name = s.name;
		std::string row = Sdl::FindX360ceRow(device);
		Sdl::ReadRowKeys(row, &pad, section);
		controller->m_autoMap = true;
		m_config.ReadPad(controller.get(), section, &pad);
		controller->m_rowFound = !row.empty();
		if (controller->CreateDevice() == ERROR_SUCCESS && (controller->m_rowFound || controller->m_sdlKnown))
		{
			Publish(place, controller, used);
			PrintLog("[PAD%d] \"%s\" mapped from %s", place + 1, devices[i].tszProductName,
				controller->m_rowFound ? "x360ce's database" : "SDL's database");
		}
	}
}
