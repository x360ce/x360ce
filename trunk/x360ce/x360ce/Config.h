#pragma once

class Controller;

static const DWORD INVALIDBUTTONINDEX = (DWORD)-1;

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
        positiveButtonID = negativeButtonID = 0;
        hasDigital = false;

        a2ddeadzone = false;
        a2doffset = 0;
        axisdeadzone = 0;
        antideadzone = 0;
        axislinear = 0;
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
    s8 Button[10];
    s8 guide;
    s8 DpadPOV; // Index of POV switch to use for the D-pad
    bool PovIsButton;

    Mapping()
    {
        for (u32 i = 0; i < _countof(Button); ++i) Button[i] = -1;

        pov[GAMEPAD_DPAD_UP] = 36000;
        pov[GAMEPAD_DPAD_DOWN] = 18000;
        pov[GAMEPAD_DPAD_LEFT] = 27000;
        pov[GAMEPAD_DPAD_RIGHT] = 9000;

        guide = -1;
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
void ReadConfig();
void ReadPadConfig(Controller* pController, const std::string& section, SWIP *ini);
void ReadPadMapping(Controller* pController, const std::string& section, SWIP *ini);

extern bool g_bNative;
extern bool g_bInitBeep;
extern bool g_bDisable;
extern bool g_bContinue;
