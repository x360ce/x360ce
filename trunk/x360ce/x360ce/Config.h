enum MappingType { NONE, DIGITAL, AXIS, SLIDER, HAXIS, HSLIDER };

struct AxisMap {
	INT id;
	MappingType analogType; // Type of analog mapping (only NONE, AXIS, and SLIDER are used)

	BOOL hasDigital; // Indicates if there is digital input mapped to the axis
	INT positiveButtonID, negativeButtonID; // button IDs corresponding to the positive/negative directions of the axis

	AxisMap() {
		analogType = NONE;
		hasDigital = false;
		positiveButtonID = negativeButtonID = -1;
	}
};
struct TriggerMap {
	INT id;			// Index for the mapped button/axis/slider
	MappingType type;
	TriggerMap() {
		type = NONE;
	}
};

struct GamepadMap {
	// Axis indexes are positive or negative numbers, zero is invalid.
	// All other indexer values start from zero.
	INT Button[10];
	DWORD pov[4];
	TriggerMap Trigger[2];
	AxisMap Axis[4];  // Index of axes to use. Negative index used if it needs to be inverted
	INT DpadPOV; // Index of POV switch to use for the D-pad
	BOOL enabled;
	GamepadMap() {
		// Set default values
		for (INT i = 0; i < 10; ++i) Button[i] = -1;
		for (INT i = 0; i < 4; ++i) pov[i] = (DWORD) -1;
		DpadPOV = -1;
		enabled = false;
	}

};

// Map internal IDs to XInput constants
static const WORD buttonIDs[10] = {
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
};

static const WORD povIDs[4] = {
	XINPUT_GAMEPAD_DPAD_UP,
	XINPUT_GAMEPAD_DPAD_DOWN,
	XINPUT_GAMEPAD_DPAD_LEFT,
	XINPUT_GAMEPAD_DPAD_RIGHT
};

extern BOOL bInitBeep;
extern struct GamepadMap GamepadMapping[4];

void InitConfig(LPCWSTR ininame);
void ReadConfig();
void ReadPadConfig(INT idx);
MappingType getTriggerType(LPCWSTR);