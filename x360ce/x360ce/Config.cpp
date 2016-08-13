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

void Config::ReadConfig()
{
	IniFile ini;
	std::string inipath("x360ce.ini");
	if (!ini.Load(inipath))
		CheckCommonDirectory(&inipath, "x360ce");
	if (!ini.Load(inipath)) return;

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
		if (pController)
		{
			if (ReadPadConfig(pController, section, &ini))
				ReadPadMapping(pController, section, &ini);
		}

		// If this is a combined device we need to add the combiner instead
		if (combined)
		{
			// Update controller
			controller->m_combined = true;
			controller->m_combinedIndex = combinedIndex;

			// Get Controllers
			// auto controllers = ControllerManager::Get().GetControllers();

			// Attempt to find an existing combiner for the index
			// auto found = std::find_if(controllers.begin(), controllers.end(), 
			auto found = std::find_if(ControllerManager::Get().GetControllers().begin(), ControllerManager::Get().GetControllers().end(),
				[combinedIndex](std::shared_ptr<ControllerBase> c)
			{
				return (c->m_combined) && (c->m_combinedIndex == combinedIndex);
			});

			// If not found, create it
			if (found == ControllerManager::Get().GetControllers().end())
			{
				// Create combiner
				std::shared_ptr<ControllerCombiner> combiner(new ControllerCombiner(combinedIndex));

				// Set the index
				combiner->m_combinedIndex = combinedIndex;

				// Add the controller to the combiner
				combiner->GetControllers().push_back(controller);

				// Add the combiner to the controllers collection
				ControllerManager::Get().GetControllers().push_back(combiner);
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
			ControllerManager::Get().GetControllers().push_back(controller);
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
		return false;

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

	// FFB options
	pIniFile->Get(section, "UseForceFeedback", &pController->m_useforce);
	if (pController->m_useforce)
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

	// Guide button
	pIniFile->Get<s8>(section, "GuideButton", &pMapping->guide, -1);

	// Fire buttons
	for (u32 i = 0; i < _countof(pMapping->Button); ++i)
	{
		// Buttons
		std::string button;
		if (pIniFile->Get(section, buttonNames[i], &button))
		{
			ParsePrefix(button, &pMapping->Button[i].type, &pMapping->Button[i].id);
		}
		else
		{
			std::string message = StringFormat("Mapping could not be determined for %s button %s", section.c_str(), buttonNames[i]);
			PrintLog(message.c_str());
		}
		// DeadZones
		pIniFile->Get(section, buttonDZNames[i], &pMapping->Button[i].buttondz);


	}

	//// D-PAD
	pIniFile->Get(section, "D-pad POV", &pMapping->DpadPOV);
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
		}
		else
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
		std::string posMap;
		if (pIniFile->Get(section, axisBNames[i * 2], &posMap))
		{
			ParsePrefix(posMap, &pMapping->Axis[i].positiveType, &pMapping->Axis[i].positiveButtonID);
		}

		if (pIniFile->Get(section, axisBNames[i * 2 + 1], &posMap))
		{
			ParsePrefix(posMap, &pMapping->Axis[i].negativeType, &pMapping->Axis[i].negativeButtonID);
		}
	}

	// Triggers
	for (u32 i = 0; i < _countof(pMapping->Trigger); ++i)
	{
		std::string trigger;
		if (pIniFile->Get(section, triggerNames[i], &trigger))
			ParsePrefix(trigger, &pMapping->Trigger[i].type, &pMapping->Trigger[i].id);

		pIniFile->Get(section, triggerDZNames[i], &pMapping->Trigger[i].triggerdz);

		// SeDoG mod
		pIniFile->Get(section, triggerBNames[i], &pMapping->Trigger[i].but);
	}
}

