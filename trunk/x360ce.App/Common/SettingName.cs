using System;
using System.Collections.Generic;
using System.Text;

namespace x360ce.App
{
    /// <summary>
    /// Map between .NET and INI file. This makes refactoring easier.
    /// </summary>
    /// <remarks>
    /// Adopted names: Internal Code Name - "Public Name"
    /// Big = "Guide"
    /// Thumb = "Stick"
    /// Shoulder = "Bumper"
    /// </remarks>
    public struct SettingName
    {
        // [Options] section.
        public const string UseInitBeep = "UseInitBeep";
        public const string DebugMode = "DebugMode";
        public const string Log = "Log";
        // [FakeAPI] section.
        public const string FakeWinTrust = "FakeWinTrust";
        public const string FakeWmi = "FakeWMI";
        public const string FakeDi = "FakeDI";
        public const string FakeVid = "FakeVID";
        public const string FakePid = "FakePID";
        // [PAD] section.
        public const string ProductName = "ProductName";
        public const string InstanceGuid = "Instance";
        public const string Vid = "VID";
        public const string Pid = "PID";
        public const string GamePadType = "ControllerType";
        public const string NativeMode = "Native";
        // Main Mapping
        public const string LeftThumbButton = "Left Thumb";
        public const string LeftThumbAxisX = "Left Analog X";
        public const string LeftThumbAxisY = "Left Analog Y";
        public const string LeftThumbRight = "Left Analog X+ Button";
        public const string LeftThumbLeft = "Left Analog X- Button";
        public const string LeftThumbUp = "Left Analog Y+ Button";
        public const string LeftThumbDown = "Left Analog Y- Button";
        public const string RightThumbButton = "Right Thumb";
        public const string RightThumbAxisX = "Right Analog X";
        public const string RightThumbAxisY = "Right Analog Y";
        public const string RightThumbRight = "Right Analog X+ Button";
        public const string RightThumbLeft = "Right Analog X- Button";
        public const string RightThumbUp = "Right Analog Y+ Button";
        public const string RightThumbDown = "Right Analog Y- Button";
        public const string DPad = "D-pad POV";
        public const string DPadUp = "D-pad Up";
        public const string DPadDown = "D-pad Down";
        public const string DPadLeft = "D-pad Left";
        public const string DPadRight = "D-pad Right";
        public const string ButtonBack = "Back";
        public const string ButtonStart = "Start";
        public const string ButtonA = "A";
        public const string ButtonB = "B";
        public const string ButtonX = "X";
        public const string ButtonY = "Y";
        public const string LeftShoulder = "Left Shoulder";
        public const string RightShoulder = "Right Shoulder";
        public const string LeftTrigger = "Left Trigger";
        public const string LeftTriggerDeadZone = "TriggerDeadzone";
        public const string RightTrigger = "Right Trigger";
        public const string RightTriggerDeadZon = "Right Trigger Deadzone";
        // Force Feedback.
        public const string ForceEnable = "UseForceFeedback";
        public const string ForceSwapMotor = "SwapMotor";
        public const string ForceLeftMotorInvert = "LeftMotorDirection";
        public const string ForceRightMotorInvert = "RightMotorDirection";
        public const string ForceOverall = "ForcePercent";
        // Axis To D-Pad
        public const string AxisToDPadEnabled = "AxisToDPad";
        public const string AxisToDPadDeadZone = "AxisToDPadDeadZone";
        public const string AxisToDPadOffset = "AxisToDPadOffset";
    }
}
