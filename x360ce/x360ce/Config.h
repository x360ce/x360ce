#pragma once

// disable C4351 - new behavior: elements of array 'array' will be default initialized
#pragma warning( disable:4351 )

enum MappingType { NONE, DIGITAL, AXIS, SLIDER, HAXIS, HSLIDER, CBUT };

enum PovIDs
{
    GAMEPAD_DPAD_UP,
    GAMEPAD_DPAD_DOWN,
    GAMEPAD_DPAD_LEFT,
    GAMEPAD_DPAD_RIGHT
};

struct AxisMap
{
    MappingType analogType; // Type of analog mapping (only NONE, AXIS, and SLIDER are used)
    s8 id;
    s8 positiveButtonID, negativeButtonID; // button IDs corresponding to the positive/negative directions of the axis
    bool hasDigital; // Indicates if there is digital input mapped to the axis

    AxisMap()
    {
        analogType = NONE;
        id = 0;
        positiveButtonID = negativeButtonID = 0;
        hasDigital = false;
    }
};
struct TriggerMap
{
    MappingType type;
    s8 id;			// Index for the mapped button/axis/slider
    s8 but;
    TriggerMap()
    {
        id = 0;
        but = 0;
        type = NONE;
    }
};

struct Mapping
{
    // Axis indexes are positive or negative numbers, zero is invalid.
    // All other indexer values start from zero.
    TriggerMap Trigger[2];
    AxisMap Axis[4];  // Index of axes to use. Negative index used if it needs to be inverted
    s32 pov[4];
    s8 Button[10];
    s8 guide;
    s8 DpadPOV; // Index of POV switch to use for the D-pad
    bool PovIsButton;
    Mapping()
        :Trigger()
        , Axis()
        , Button()
    {
        pov[GAMEPAD_DPAD_UP] = 36000;
        pov[GAMEPAD_DPAD_DOWN] = 18000;
        pov[GAMEPAD_DPAD_LEFT] = 27000;
        pov[GAMEPAD_DPAD_RIGHT] = 9000;

        guide = 0;
        DpadPOV = 0;
        PovIsButton = false;
    }
};

// Map internal IDs to XInput constants
static const u16 buttonIDs[10] =
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
};

static const u16 povIDs[4] =
{
    XINPUT_GAMEPAD_DPAD_UP,
    XINPUT_GAMEPAD_DPAD_DOWN,
    XINPUT_GAMEPAD_DPAD_LEFT,
    XINPUT_GAMEPAD_DPAD_RIGHT
};

void InitConfig(char* ininame);
void ReadConfig(bool reset = false);
void ReadPadConfig(DWORD dwUserIndex, SWIP* ini);

extern std::vector<DInputDevice> g_Devices;
extern std::vector<Mapping> g_Mappings;

extern bool g_bNative;
extern bool g_bInitBeep;
extern bool g_bDisable;
extern bool g_bContinue;
