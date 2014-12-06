#pragma once

class Controller;

class Config
{
    friend class ControllerManager;
public:
    Config() {}
    ~Config() {}

    static const DWORD INVALIDBUTTONINDEX = (DWORD)-1;

    enum MappingType { NONE, DIGITAL, AXIS, SLIDER, HAXIS, HSLIDER, CBUT };

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

            a2ddeadzone = 0;
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

            pov[DPAD_UP] = 36000;
            pov[DPAD_DOWN] = 18000;
            pov[DPAD_LEFT] = 27000;
            pov[DPAD_RIGHT] = 9000;

            guide = -1;
            DpadPOV = 0;
            PovIsButton = false;
        }
    };

    static const u16 buttonIDs[10];
    static const u16 povIDs[4];

    static const char* const buttonNames[];
    static const char* const povNames[];
    static const char* const axisNames[];
    static const char* const axisDZNames[];
    static const char* const axisADZNames[];
    static const char* const axisLNames[];
    static const char* const axisBNames[];
    static const char* const triggerNames[];
    static const char* const triggerDZNames[];
    static const char* const triggerBNames[];

    void ReadConfig();

private:
    bool ReadPadConfig(Controller* pController, const std::string& section, SWIP* pSWIP);
    void ReadPadMapping(Controller* pController, const std::string& section, SWIP* pSWIP);

    void ParsePrefix(const std::string& input, MappingType* pMappingType, s8* pValue);

    bool m_initBeep;
    bool m_globalDisable;
};




