#include "stdafx.h"
#include "Common.h"
#include "IniFile.h"
#include "Logger.h"
#include "version.h"
#include "Utils.h"
#include "InputHook.h"

#include "IniFile.h"
#include "Config.h"

#include "ControllerBase.h"
#include "Controller.h"
#include "ControllerCombiner.h"
#include "ForceFeedbackBase.h"
#include "ForceFeedback.h"

#include "ControllerManager.h"
#include "Sdl.h"

const u16 Config::buttonIDs[14] =
{
	XINPUT_GAMEPAD_A,
	XINPUT_GAMEPAD_B,
	XINPUT_GAMEPAD_X,
	XINPUT_GAMEPAD_Y,
	XINPUT_GAMEPAD_LEFT_SHOULDER,
	XINPUT_GAMEPAD_RIGHT_SHOULDER,
	XINPUT_GAMEPAD_BACK,
	XINPUT_GAMEPAD_START,
	XINPUT_GAMEPAD_LEFT_THUMB,
	XINPUT_GAMEPAD_RIGHT_THUMB,
	XINPUT_GAMEPAD_DPAD_UP,
	XINPUT_GAMEPAD_DPAD_DOWN,
	XINPUT_GAMEPAD_DPAD_LEFT,
	XINPUT_GAMEPAD_DPAD_RIGHT
};

const u16 Config::povIDs[4] =
{
	XINPUT_GAMEPAD_DPAD_UP,
	XINPUT_GAMEPAD_DPAD_DOWN,
	XINPUT_GAMEPAD_DPAD_LEFT,
	XINPUT_GAMEPAD_DPAD_RIGHT
};

const char* const Config::buttonNames[] =
{
	"A",
	"B",
	"X",
	"Y",
	"Left Shoulder",
	"Right Shoulder",
	"Back",
	"Start",
	"Left Thumb",
	"Right Thumb",
	// D-Pad buttons.
	"D-pad Up",
	"D-pad Down",
	"D-pad Left",
	"D-pad Right",
};

const char* const Config::buttonDZNames[] =
{
	"A DeadZone",
	"B DeadZone",
	"X DeadZone",
	"Y DeadZone",
	"Left Shoulder DeadZone",
	"Right Shoulder DeadZone",
	"Back DeadZone",
	"Start DeadZone",
	"Left Thumb DeadZone",
	"Right Thumb DeadZone",
	// D-Pad buttons.
	"AxisToDPadUpDeadZone",
	"AxisToDPadDownDeadZone",
	"AxisToDPadLeftDeadZone",
	"AxisToDPadRightDeadZone",
};

const char* const Config::povNames[] =
{
	"D-pad Up",
	"D-pad Down",
	"D-pad Left",
	"D-pad Right",
};

const char* const Config::axisNames[] =
{
	"Left Analog X",
	"Left Analog Y",
	"Right Analog X",
	"Right Analog Y",
};

const char* const Config::axisDZNames[] =
{
	"Left Analog X DeadZone",
	"Left Analog Y DeadZone",
	"Right Analog X DeadZone",
	"Right Analog Y DeadZone",
};

const char* const Config::axisADZNames[] =
{
	"Left Analog X AntiDeadZone",
	"Left Analog Y AntiDeadZone",
	"Right Analog X AntiDeadZone",
	"Right Analog Y AntiDeadZone",
};

const char* const Config::axisLNames[] =
{
	"Left Analog X Linear",
	"Left Analog Y Linear",
	"Right Analog X Linear",
	"Right Analog Y Linear",
};

const char* const Config::axisBNames[] =
{
	"Left Analog X+ Button",
	"Left Analog X- Button",
	"Left Analog Y+ Button",
	"Left Analog Y- Button",
	"Right Analog X+ Button",
	"Right Analog X- Button",
	"Right Analog Y+ Button",
	"Right Analog Y- Button",
};

const char* const Config::triggerNames[] =
{
	"Left Trigger",
	"Right Trigger",
};

const char* const Config::triggerDZNames[] =
{
	"Left Trigger DeadZone",
	"Right Trigger DeadZone",
};

const char* const Config::triggerBNames[] =
{
	"Left Trigger But",
	"Right Trigger But",
};

void Config::ParsePrefix(const std::string& input, MappingType* pMappingType, s8* pValue)
{
	if (pMappingType)
	{
		switch (input[0])
		{
			case 'a': // Axis
				*pMappingType = AXIS;
				break;
			case 's': // Slider
				*pMappingType = SLIDER;
				break;
			case 'x': // Half range axis
				*pMappingType = HAXIS;
				break;
			case 'h': // Half range slider
				*pMappingType = HSLIDER;
				break;
			case 'z':
				*pMappingType = CBUT;
				break;
			case 'd':
				*pMappingType = DPADBUTTON;
				break;
			default: // Digital
				*pMappingType = DIGITAL;
		}
	}

	if (pValue && pMappingType)
	{
		if (*pMappingType != DIGITAL)
			Convert(input.c_str() + 1, pValue);
		else
			Convert(input, pValue);
	}
}

void Config::ReadConfig(std::vector<std::shared_ptr<ControllerBase>>& controllers)
{
	IniFile ini;
	std::string inipath("x360ce.ini");
	m_autoMap = false;
	if (!ini.Load(inipath))
		CheckCommonDirectory(&inipath, "x360ce");
	// Without a configuration file every controller is mapped from SDL's database.
	if (!ini.Load(inipath))
	{
		m_autoMap = true;
		return;
	}

	static bool once_flag = false;
	bool combineEnabled = false;

	if (!once_flag)
	{
		// Read global options
		ini.Get("Options", "Disable", &m_globalDisable);
		if (m_globalDisable) return;

		ini.Get("Options", "UseInitBeep", &m_initBeep, true);

		PrintLog("Using config file:");
		PrintLog(ini.GetIniPath().c_str());

		u32 ver = 0;
		ini.Get("Options", "Version", &ver);
		if (ver != VERSION_CONFIG)
			PrintLog("WARNING: Configuration file version mismatch detected");

		once_flag = true;
	}

	// Is combining enabled at a global level?
	ini.Get("Options", "CombineEnabled", &combineEnabled);

	// Are controllers no section lists mapped from SDL's database into free places?
	ini.Get("Options", "AutoMap", &m_autoMap);

	// Read pad mappings
	for (u32 i = 0; i < XUSER_MAX_COUNT; ++i)
	{
		std::string section;
		std::string key = StringFormat("PAD%u", i + 1);
		if (!ini.Get("Mappings", key, &section))
			continue;

		// Determine index
		u32 index = 0;
		if (!ini.Get(section, "UserIndex", &index))
			index = i;

		// Check for a combined controller
		u32 combinedIndex = 0;
		bool combined = false;
		if (combineEnabled)
		{
			// Is this a combined controller?
			ini.Get(section, "Combined", &combined, false);

			// Get the combined index
			ini.Get(section, "CombinedIndex", &combinedIndex);
		}

		// Create controller instance
		std::shared_ptr<Controller> controller(new Controller(index));

		// Read in configuration
		Controller* pController = controller.get();
		if (pController && ReadPad(pController, section, &ini) && pController->m_autoMap)
			ReadDatabaseRow(pController, section, &ini);

		// If this is a combined device we need to add the combiner instead
		if (combined)
		{
			// Update controller
			controller->m_combined = true;
			controller->m_combinedIndex = combinedIndex;

			// Attempt to find an existing combiner for the index
			auto found = std::find_if(controllers.begin(), controllers.end(),
				[combinedIndex](std::shared_ptr<ControllerBase> c)
			{
				return (c->m_combined) && (c->m_combinedIndex == combinedIndex);
			});

			// If not found, create it
			if (found == controllers.end())
			{
				// Create combiner
				std::shared_ptr<ControllerCombiner> combiner(new ControllerCombiner(combinedIndex));

				// Set the index
				combiner->m_combinedIndex = combinedIndex;

				// Add the controller to the combiner
				combiner->GetControllers().push_back(controller);

				// Add the combiner to the controllers collection
				controllers.push_back(combiner);
			}
			else
			{
				// Found should represent an existing combiner at this point
				auto combiner = std::dynamic_pointer_cast<ControllerCombiner>(*found);

				// Add the controller to the combiner
				combiner->GetControllers().push_back(controller);
			}
		}
		else
		{
			// Not a combined device. Just add like normal.
			controllers.push_back(controller);
		}
	}
}

bool Config::ReadPadConfig(Controller* pController, const std::string& section, IniFile* pIniFile)
{
	std::string buffer;

	if (pIniFile->Get(section, "ProductGUID", &buffer))
		StringToGUID(&pController->m_productid, buffer);

	if (pIniFile->Get(section, "InstanceGUID", &buffer))
		StringToGUID(&pController->m_instanceid, buffer);

	if (IsEqualGUID(pController->m_productid, GUID_NULL) || IsEqualGUID(pController->m_instanceid, GUID_NULL))
	{
		std::string message = StringFormat("[PAD%u] Misconfigured device, check GUIDs", pController->m_user);
		PrintLog(message.c_str());
		MessageBoxA(NULL, message.c_str(), "x360ce", MB_ICONERROR);
		return false;
	}

	// Full pass through
	pIniFile->Get(section, "PassThrough", &pController->m_passthrough);
	if (pController->m_passthrough)
	{
		int passthroughindex;
		pIniFile->Get<int>(section, "PassThroughIndex", &passthroughindex, -1);
		if (passthroughindex < 0 || passthroughindex > 3)
		{
			pController->m_passthroughindex = pController->m_user;
		}
		else
		{
			pController->m_passthroughindex = passthroughindex;
		}

		return false;
	}

	// Mapping from the controller databases; the section's own keys win over them.
	bool autoMap = false;
	pIniFile->Get(section, "AutoMap", &autoMap);
	if (autoMap)
		pController->m_autoMap = true;

	// Forces-only pass through
	pIniFile->Get(section, "ForcesPassThrough", &pController->m_forcespassthrough);

	/******************************************************************************
	 * We load controllers by their InstanceGUID or ProductGUID. We also allow
	 * users to specify which player they want the controller to represent by
	 * specifying a UserIndex. But what if more than one of the controllers being
	 * loaded are actual XInput device? For example what if there are two 360
	 * controllers connected to the system? We could get into a situation where
	 * physical controller 2 has been mapped to player 1 and vice versa.
	 *
	 * This is not normally a problem but it becomes a problem when we want to
	 * support pass-through on more than one controller. Whenever it's time to
	 * read values from the physical device or time to send vibration data to the
	 * physical device, we need to know which one.
	 *
	 * It would be best to detect if the device is also an XInput device and set
	 * this automaticaly instead of requiring it to be in the config. But that
	 * is byond my knowledge of XInput at this time so hopefully someone else
	 * can add it. - JBIENZ 2015-04-16
	 ******************************************************************************/
	// Pass through index
	// TODO: Auto Detect during initialization
	pIniFile->Get<u32>(section, "PassThroughIndex", &pController->m_passthroughindex, 0);

	// Device type
	pIniFile->Get<u8>(section, "ControllerType", &pController->m_gamepadtype, 1);

	// FFB options, read whole so a controller given force feedback later has the defaults.
	pController->m_forceKey = pIniFile->Get(section, "UseForceFeedback", &pController->m_useforce);
	{
		pIniFile->Get(section, "SwapMotor", &pController->m_ForceFeedback->m_SwapMotors);
		pIniFile->Get(section, "FFBType", &pController->m_ForceFeedback->m_Type);
		pIniFile->Get<u32>(section, "LeftMotorPeriod", &pController->m_ForceFeedback->m_LeftPeriod, 120);
		pIniFile->Get<u32>(section, "RightMotorPeriod", &pController->m_ForceFeedback->m_RightPeriod, 60);
		pIniFile->Get<LONG>(section, "LeftMotorDirection", &pController->m_ForceFeedback->m_LeftDirection, 0);
		pIniFile->Get<LONG>(section, "RightMotorDirection", &pController->m_ForceFeedback->m_RightDirection, 0);
		// Force feedback strength settings.
		pIniFile->Get<u32>(section, "LeftMotorStrength", &pController->m_ForceFeedback->m_LeftStrength, 100);
		pController->m_ForceFeedback->m_LeftStrength *= 100;
		pIniFile->Get<u32>(section, "RightMotorStrength", &pController->m_ForceFeedback->m_RightStrength, 100);
		pController->m_ForceFeedback->m_RightStrength *= 100;
		pIniFile->Get<u32>(section, "ForcePercent", &pController->m_ForceFeedback->m_OveralStrength, 100);
		pController->m_ForceFeedback->m_OveralStrength *= 100;

	}

	return true;
}

void Config::ReadPadMapping(Controller* pController, const std::string& section, IniFile* pIniFile)
{
	Mapping* pMapping = &pController->m_mapping;
	// A key counts when it holds a value; "0" is a value, the control switched off.
	auto has = [&](const std::string& key, std::string* value)
	{
		return pIniFile->Get(section, key, value) && !value->empty();
	};
	std::string value;

	// Guide button: counted from 1, as every other button key and as x360ce 3 writes it; 0 or -1 is none.
	s8 guide = 0;
	if (pIniFile->Get<s8>(section, "GuideButton", &guide, 0))
		pMapping->iniKeys |= 1u << KEY_GUIDE;
	pMapping->guide = guide > 0 ? guide - 1 : -1;

	// Fire buttons
	for (u32 i = 0; i < _countof(pMapping->Button); ++i)
	{
		// Buttons
		std::string button;
		if (pIniFile->Get(section, buttonNames[i], &button))
		{
			ParsePrefix(button, &pMapping->Button[i].type, &pMapping->Button[i].id);
			if (!button.empty())
				pMapping->iniKeys |= 1u << (KEY_BUTTON + i);
		}
		else if (!pController->m_autoMap)
		{
			std::string message = StringFormat("Mapping could not be determined for %s button %s", section.c_str(), buttonNames[i]);
			PrintLog(message.c_str());
		}
		// DeadZones
		if (pIniFile->Get(section, buttonDZNames[i], &pMapping->Button[i].buttondz))
			pMapping->buttonDzKeys |= (u16)(1u << i);


	}

	//// D-PAD
	if (pIniFile->Get(section, "D-pad POV", &pMapping->DpadPOV))
		pMapping->iniKeys |= 1u << KEY_DPAD;
	//if (pMapping->DpadPOV >= 0)
	//{
	//	for (u32 i = 0; i < _countof(pMapping->pov); ++i)
	//	{
	//		// D-PAD directions
	//		s16 val = 0;
	//		pIniFile->Get<s16>(section, povNames[i], &val, -1);
	//		if (val > 0 && val < 128)
	//		{
	//			pMapping->pov[i] = val - 1;
	//			pMapping->PovIsButton = true;
	//		}
	//		else if (val > -1)
	//		{
	//			pMapping->pov[i] = val;
	//			pMapping->PovIsButton = false;
	//		}
	//	}
	//}

	for (u32 i = 0; i < _countof(pMapping->Axis); ++i)
	{
		// Axes
		std::string axis;
		if (pIniFile->Get(section, axisNames[i], &axis))
		{
			ParsePrefix(axis, &pMapping->Axis[i].analogType, &pMapping->Axis[i].id);
			if (!axis.empty())
				pMapping->iniKeys |= 1u << (KEY_AXIS + i);
		}
		else if (!pController->m_autoMap)
		{
			std::string message = StringFormat("Mapping could not be determined for %s axis %s", section.c_str(), axisNames[i]);
			PrintLog(message.c_str());
		}

		// DeadZones
		pIniFile->Get(section, axisDZNames[i], &pMapping->Axis[i].axisdeadzone);

		// Anti DeadZones
		pIniFile->Get(section, axisADZNames[i], &pMapping->Axis[i].antideadzone);

		// Linearity
		pIniFile->Get(section, axisLNames[i], &pMapping->Axis[i].axislinear);

		// Axis to DPAD options
		pIniFile->Get(section, "AxisToDPad", &pMapping->Axis[i].axistodpad);
		pIniFile->Get(section, "AxisToDPadDeadZone", &pMapping->Axis[i].a2ddeadzone);
		pIniFile->Get(section, "AxisToDPadOffset", &pMapping->Axis[i].a2doffset);

	
		// Axis to button mappings
		if (has(axisBNames[i * 2], &value))
		{
			ParsePrefix(value, &pMapping->Axis[i].positiveType, &pMapping->Axis[i].positiveButtonID);
			pMapping->iniKeys |= 1u << (KEY_AXIS_POSITIVE + i);
		}

		if (has(axisBNames[i * 2 + 1], &value))
		{
			ParsePrefix(value, &pMapping->Axis[i].negativeType, &pMapping->Axis[i].negativeButtonID);
			pMapping->iniKeys |= 1u << (KEY_AXIS_NEGATIVE + i);
		}
	}

	// Triggers
	for (u32 i = 0; i < _countof(pMapping->Trigger); ++i)
	{
		if (has(triggerNames[i], &value))
		{
			ParsePrefix(value, &pMapping->Trigger[i].type, &pMapping->Trigger[i].id);
			pMapping->iniKeys |= 1u << (KEY_TRIGGER + i);
		}

		pIniFile->Get(section, triggerDZNames[i], &pMapping->Trigger[i].triggerdz);

		// SeDoG mod
		if (pIniFile->Get(section, triggerBNames[i], &pMapping->Trigger[i].but))
			pMapping->iniKeys |= 1u << (KEY_TRIGGER_BUT + i);
	}

	// A section that names its controller and maps none of its controls is mapped from SDL's database.
	if (!pMapping->iniKeys)
		pController->m_autoMap = true;
}

bool Config::ReadPad(Controller* pController, const std::string& section, IniFile* pIniFile)
{
	if (!ReadPadConfig(pController, section, pIniFile))
		return false;
	ReadPadMapping(pController, section, pIniFile);
	return true;
}

void Config::ReadDatabaseRow(Controller* pController, const std::string& section, IniFile* pIniFile)
{
	// The section names its controller's vendor and product in its ProductGUID: theirs, then "PIDVID".
	Sdl::Device device;
	device.vendorId = LOWORD(pController->m_productid.Data1);
	device.productId = HIWORD(pController->m_productid.Data1);
	pIniFile->Get(section, "ProductName", &device.name);
	if (memcmp(pController->m_productid.Data4 + 2, "PIDVID", 6) != 0)
		return;
	std::string row = Sdl::FindX360ceRow(device);
	if (row.empty())
		return;
	// The row's keys, then the section's own over them: read as one section.
	IniFile merged;
	const std::string pad("pad");
	Sdl::ReadRowKeys(row, &merged, pad);
	std::vector<std::string> keys;
	pIniFile->GetKeys(section, &keys);
	for (const auto& key : keys)
	{
		std::string value;
		if (pIniFile->Get(section, key, &value) && !value.empty())
			merged.Set(pad, key, value);
	}
	pController->m_mapping = Mapping();
	ReadPad(pController, pad, &merged);
	pController->m_rowFound = true;
	PrintLog("[PAD%u] x360ce database row: %s", pController->m_user + 1, row.c_str());
}

void Config::MergeSdl(Mapping* mapping, const Mapping& sdl)
{
	const u32 keys = mapping->iniKeys;
	auto unset = [keys](u32 bit) { return (keys & (1u << bit)) == 0; };
	for (u32 i = 0; i < _countof(mapping->Button); ++i)
	{
		if (!unset(KEY_BUTTON + i))
			continue;
		mapping->Button[i].type = sdl.Button[i].type;
		mapping->Button[i].id = sdl.Button[i].id;
		// An axis pressing a button needs a dead zone, or it presses it at rest.
		const MappingType t = sdl.Button[i].type;
		bool onAxis = t == AXIS || t == HAXIS || t == SLIDER || t == HSLIDER;
		if (onAxis && (mapping->buttonDzKeys & (1u << i)) == 0)
			mapping->Button[i].buttondz = DEFAULT_BUTTON_DEADZONE;
	}
	if (unset(KEY_GUIDE))
		mapping->guide = sdl.guide;
	if (unset(KEY_DPAD))
		mapping->DpadPOV = sdl.DpadPOV;
	for (u32 i = 0; i < _countof(mapping->Axis); ++i)
	{
		if (unset(KEY_AXIS + i))
		{
			mapping->Axis[i].analogType = sdl.Axis[i].analogType;
			mapping->Axis[i].id = sdl.Axis[i].id;
		}
		if (unset(KEY_AXIS_POSITIVE + i))
		{
			mapping->Axis[i].positiveType = sdl.Axis[i].positiveType;
			mapping->Axis[i].positiveButtonID = sdl.Axis[i].positiveButtonID;
		}
		if (unset(KEY_AXIS_NEGATIVE + i))
		{
			mapping->Axis[i].negativeType = sdl.Axis[i].negativeType;
			mapping->Axis[i].negativeButtonID = sdl.Axis[i].negativeButtonID;
		}
	}
	for (u32 i = 0; i < _countof(mapping->Trigger); ++i)
	{
		if (unset(KEY_TRIGGER + i))
		{
			mapping->Trigger[i].type = sdl.Trigger[i].type;
			mapping->Trigger[i].id = sdl.Trigger[i].id;
		}
	}
}


/// <summary>Test hook: the mapping a section of an ini file gives, as Sdl::Describe writes it, empty for none.</summary>
/// <remarks>Tests\Common\SdlNativeTest.cs reads GuideButton through it. Returns the length the text needs, its terminating nought included.</remarks>
extern "C" DWORD WINAPI IniMapping(LPCSTR iniPath, LPCSTR section, LPSTR buffer, DWORD size)
{
	IniFile ini;
	Controller controller(0);
	if (iniPath && section && ini.Load(iniPath))
		Config().ReadPadMapping(&controller, section, &ini);
	std::string text = Sdl::Describe(controller.m_mapping);
	if (buffer && size > 0)
		strncpy_s(buffer, size, text.c_str(), _TRUNCATE);
	return (DWORD)text.size() + 1;
}
