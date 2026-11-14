#pragma once

#include <memory>
#include <vector>

class Controller;
class ControllerBase;

class Config
{
	friend class ControllerManager;
public:
	Config() : m_autoMap(false), m_initBeep(false), m_globalDisable(false) {}
	~Config() {}

	static const DWORD INVALIDBUTTONINDEX = (DWORD)-1;

	enum MappingType { NONE, DIGITAL, AXIS, SLIDER, HAXIS, HSLIDER, CBUT, DPADBUTTON };

	enum PovIDs
	{
		DPAD_UP,
		DPAD_DOWN,
		DPAD_LEFT,
		DPAD_RIGHT
	};

	struct AxisMap
	{
		MappingType analogType; // Type of analog mapping (only NONE, AXIS, and SLIDER are used)
		s8 id;
		s8 positiveButtonID, negativeButtonID; // button IDs corresponding to the positive/negative directions of the axis
		MappingType positiveType, negativeType; // Indicates if there is digital input mapped to the axis

		s32 a2ddeadzone;
		s32 a2doffset;
		s16 axisdeadzone;
		s16 antideadzone;
		s16 axislinear;

		bool axistodpad;

		AxisMap()
		{
			analogType = NONE;
			id = 0;
			positiveButtonID = 0;
			positiveType = NONE;
			negativeButtonID = 0;
			negativeType = NONE;
			a2ddeadzone = 0;
			a2doffset = 0;
			axisdeadzone = 0;
			antideadzone = 0;
			axislinear = 0;
			axistodpad = false;
		}
	};

	struct ButtonMap
	{
		MappingType type;
		s8 id;			// Index for the mapped button/axis/slider
		s8 but;
		u16 buttondz;
		ButtonMap()
		{
			id = 0;
			but = 0;
			type = NONE;
			buttondz = 0;
		}
	};

	struct TriggerMap
	{
		MappingType type;
		s8 id;			// Index for the mapped button/axis/slider
		s8 but;
		u8 triggerdz;
		TriggerMap()
		{
			id = 0;
			but = 0;
			type = NONE;
			triggerdz = 0;
		}
	};

	struct Mapping
	{
		// Axis indexes are positive or negative numbers, zero is invalid.
		// All other indexer values start from zero.
		TriggerMap Trigger[2];
		AxisMap Axis[4];  // Index of axes to use. Negative index used if it needs to be inverted
		s32 pov[4];
		ButtonMap Button[14];
		s8 guide; // Index of the guide button from 0, -1 for none; x360ce.ini's GuideButton counts from 1
		s8 DpadPOV; // Index of POV switch to use for the D-pad
		bool PovIsButton;

		/// <summary>The mapping keys the section holds, one bit per control (the KEY_ values); an SDL mapping fills only the others.</summary>
		u32 iniKeys;
		/// <summary>The button dead zone keys the section holds, one bit per button.</summary>
		u16 buttonDzKeys;

		Mapping()
		{
			pov[DPAD_UP] = 36000;
			pov[DPAD_DOWN] = 18000;
			pov[DPAD_LEFT] = 27000;
			pov[DPAD_RIGHT] = 9000;

			guide = -1;
			DpadPOV = 0;
			PovIsButton = false;
			iniKeys = 0;
			buttonDzKeys = 0;
		}
	};

	// The bit of each control in Mapping::iniKeys.
	static const u32 KEY_BUTTON = 0;			// Button[0..13]: bits 0 to 13
	static const u32 KEY_GUIDE = 14;
	static const u32 KEY_DPAD = 15;
	static const u32 KEY_AXIS = 16;				// Axis[0..3]: bits 16 to 19
	static const u32 KEY_AXIS_POSITIVE = 20;	// Axis[0..3] positive direction: bits 20 to 23
	static const u32 KEY_AXIS_NEGATIVE = 24;	// Axis[0..3] negative direction: bits 24 to 27
	static const u32 KEY_TRIGGER = 28;			// Trigger[0..1]: bits 28 and 29
	static const u32 KEY_TRIGGER_BUT = 30;		// Trigger[0..1].but: bits 30 and 31

	/// <summary>How far an axis must move to press a button an SDL mapping puts on it, when the section names no dead zone: a quarter of the way, as x360ce 3 writes.</summary>
	static const u16 DEFAULT_BUTTON_DEADZONE = 8192;

	/// <summary>Copies into a mapping each control of an SDL mapping that the section has no key for.</summary>
	static void MergeSdl(Mapping* mapping, const Mapping& sdl);

	static const u16 buttonIDs[14];
	static const u16 povIDs[4];

	static const char* const buttonNames[];
	static const char* const buttonDZNames[];
	static const char* const povNames[];
	static const char* const axisNames[];
	static const char* const axisDZNames[];
	static const char* const axisADZNames[];
	static const char* const axisLNames[];
	static const char* const axisBNames[];
	static const char* const triggerNames[];
	static const char* const triggerDZNames[];
	static const char* const triggerBNames[];

	void ReadConfig(std::vector<std::shared_ptr<ControllerBase>>& controllers);

	/// <summary>
	/// Whether connected controllers no section lists are mapped from SDL into free places: always without an
	/// x360ce.ini, otherwise when [Options] AutoMap=1.
	/// </summary>
	bool m_autoMap;

	/// <summary>Reads a section's mapping keys into a controller, and which of them the section holds.</summary>
	void ReadPadMapping(Controller* pController, const std::string& section, IniFile* pIniFile);

private:
	bool ReadPadConfig(Controller* pController, const std::string& section, IniFile* pIniFile);

	void ParsePrefix(const std::string& input, MappingType* pMappingType, s8* pValue);

	bool m_initBeep;
	bool m_globalDisable;
};




