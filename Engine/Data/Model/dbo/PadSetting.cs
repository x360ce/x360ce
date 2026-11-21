using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace x360ce.Engine.Data
{

	/// <summary>x360ce Pad Settings</summary>
	[GeneratedCode("MainModel", "2023.1.19")]
	[Table("x360ce_PadSettings")]
	[DataContract(IsReference = true)]
	public partial class PadSetting: JocysCom.ClassLibrary.ComponentModel.NotifyPropertyChanged
	{

		/// <summary>Pad Setting Checksum (unique primary key).</summary>
		[Key]
		[DataMember]
		public Guid PadSettingChecksum {
			get => _PadSettingChecksum;
			set => SetProperty(ref _PadSettingChecksum, value);
		}
		private Guid _PadSettingChecksum;

		/// <summary>Axis ToD Pad Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string AxisToDPadDeadZone {
			get => _AxisToDPadDeadZone;
			set => SetProperty(ref _AxisToDPadDeadZone, value);
		}
		private string _AxisToDPadDeadZone;

		/// <summary>Axis ToD Pad Enabled</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string AxisToDPadEnabled {
			get => _AxisToDPadEnabled;
			set => SetProperty(ref _AxisToDPadEnabled, value);
		}
		private string _AxisToDPadEnabled;

		/// <summary>Axis ToD Pad Offset</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string AxisToDPadOffset {
			get => _AxisToDPadOffset;
			set => SetProperty(ref _AxisToDPadOffset, value);
		}
		private string _AxisToDPadOffset;

		/// <summary>ButtonA</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonA {
			get => _ButtonA;
			set => SetProperty(ref _ButtonA, value);
		}
		private string _ButtonA;

		/// <summary>ButtonB</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonB {
			get => _ButtonB;
			set => SetProperty(ref _ButtonB, value);
		}
		private string _ButtonB;

		/// <summary>Button Big</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonBig {
			get => _ButtonBig;
			set => SetProperty(ref _ButtonBig, value);
		}
		private string _ButtonBig;

		/// <summary>Button Back</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonBack {
			get => _ButtonBack;
			set => SetProperty(ref _ButtonBack, value);
		}
		private string _ButtonBack;

		/// <summary>Button Guide</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonGuide {
			get => _ButtonGuide;
			set => SetProperty(ref _ButtonGuide, value);
		}
		private string _ButtonGuide;

		/// <summary>Button Start</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonStart {
			get => _ButtonStart;
			set => SetProperty(ref _ButtonStart, value);
		}
		private string _ButtonStart;

		/// <summary>ButtonX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonX {
			get => _ButtonX;
			set => SetProperty(ref _ButtonX, value);
		}
		private string _ButtonX;

		/// <summary>ButtonY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string ButtonY {
			get => _ButtonY;
			set => SetProperty(ref _ButtonY, value);
		}
		private string _ButtonY;

		/// <summary>D Pad</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string DPad {
			get => _DPad;
			set => SetProperty(ref _DPad, value);
		}
		private string _DPad;

		/// <summary>D Pad Down</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string DPadDown {
			get => _DPadDown;
			set => SetProperty(ref _DPadDown, value);
		}
		private string _DPadDown;

		/// <summary>D Pad Left</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string DPadLeft {
			get => _DPadLeft;
			set => SetProperty(ref _DPadLeft, value);
		}
		private string _DPadLeft;

		/// <summary>D Pad Right</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string DPadRight {
			get => _DPadRight;
			set => SetProperty(ref _DPadRight, value);
		}
		private string _DPadRight;

		/// <summary>D Pad Up</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string DPadUp {
			get => _DPadUp;
			set => SetProperty(ref _DPadUp, value);
		}
		private string _DPadUp;

		/// <summary>Force Enable</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceEnable {
			get => _ForceEnable;
			set => SetProperty(ref _ForceEnable, value);
		}
		private string _ForceEnable;

		/// <summary>Force Overall</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceOverall {
			get => _ForceOverall;
			set => SetProperty(ref _ForceOverall, value);
		}
		private string _ForceOverall;

		/// <summary>Force Pass Through</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(1)]
		public string ForcePassThrough {
			get => _ForcePassThrough;
			set => SetProperty(ref _ForcePassThrough, value);
		}
		private string _ForcePassThrough;

		/// <summary>Force Pass Through Index</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(1)]
		public string ForcePassThroughIndex {
			get => _ForcePassThroughIndex;
			set => SetProperty(ref _ForcePassThroughIndex, value);
		}
		private string _ForcePassThroughIndex;

		/// <summary>Force Spring Enable</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringEnable {
			get => _ForceSpringEnable;
			set => SetProperty(ref _ForceSpringEnable, value);
		}
		private string _ForceSpringEnable;

		/// <summary>Force Spring Strength</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringStrength {
			get => _ForceSpringStrength;
			set => SetProperty(ref _ForceSpringStrength, value);
		}
		private string _ForceSpringStrength;

		/// <summary>Force Spring Centre</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringCentre {
			get => _ForceSpringCentre;
			set => SetProperty(ref _ForceSpringCentre, value);
		}
		private string _ForceSpringCentre;

		/// <summary>Force Spring Peak</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringPeak {
			get => _ForceSpringPeak;
			set => SetProperty(ref _ForceSpringPeak, value);
		}
		private string _ForceSpringPeak;

		/// <summary>Force Spring Lightening</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringLightening {
			get => _ForceSpringLightening;
			set => SetProperty(ref _ForceSpringLightening, value);
		}
		private string _ForceSpringLightening;

		/// <summary>Force Spring Damping</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringDamping {
			get => _ForceSpringDamping;
			set => SetProperty(ref _ForceSpringDamping, value);
		}
		private string _ForceSpringDamping;

		/// <summary>Force Spring Speed</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSpringSpeed {
			get => _ForceSpringSpeed;
			set => SetProperty(ref _ForceSpringSpeed, value);
		}
		private string _ForceSpringSpeed;

		/// <summary>Wheel Range</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string WheelRange {
			get => _WheelRange;
			set => SetProperty(ref _WheelRange, value);
		}
		private string _WheelRange;

		/// <summary>Force Swap Motor</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceSwapMotor {
			get => _ForceSwapMotor;
			set => SetProperty(ref _ForceSwapMotor, value);
		}
		private string _ForceSwapMotor;

		/// <summary>Force Type</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ForceType {
			get => _ForceType;
			set => SetProperty(ref _ForceType, value);
		}
		private string _ForceType;

		/// <summary>Game Pad Type</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string GamePadType {
			get => _GamePadType;
			set => SetProperty(ref _GamePadType, value);
		}
		private string _GamePadType;

		/// <summary>Left Motor Period</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftMotorPeriod {
			get => _LeftMotorPeriod;
			set => SetProperty(ref _LeftMotorPeriod, value);
		}
		private string _LeftMotorPeriod;

		/// <summary>Left Motor Strength</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftMotorStrength {
			get => _LeftMotorStrength;
			set => SetProperty(ref _LeftMotorStrength, value);
		}
		private string _LeftMotorStrength;

		/// <summary>Left Motor Direction</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftMotorDirection {
			get => _LeftMotorDirection;
			set => SetProperty(ref _LeftMotorDirection, value);
		}
		private string _LeftMotorDirection;

		/// <summary>Left Shoulder</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftShoulder {
			get => _LeftShoulder;
			set => SetProperty(ref _LeftShoulder, value);
		}
		private string _LeftShoulder;

		/// <summary>Left Thumb AxisX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbAxisX {
			get => _LeftThumbAxisX;
			set => SetProperty(ref _LeftThumbAxisX, value);
		}
		private string _LeftThumbAxisX;

		/// <summary>Left Thumb Dead ZoneX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbDeadZoneX {
			get => _LeftThumbDeadZoneX;
			set => SetProperty(ref _LeftThumbDeadZoneX, value);
		}
		private string _LeftThumbDeadZoneX;

		/// <summary>Left Thumb Anti Dead ZoneX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbAntiDeadZoneX {
			get => _LeftThumbAntiDeadZoneX;
			set => SetProperty(ref _LeftThumbAntiDeadZoneX, value);
		}
		private string _LeftThumbAntiDeadZoneX;

		/// <summary>Left Thumb LinearX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbLinearX {
			get => _LeftThumbLinearX;
			set => SetProperty(ref _LeftThumbLinearX, value);
		}
		private string _LeftThumbLinearX;

		/// <summary>Left Thumb AxisY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbAxisY {
			get => _LeftThumbAxisY;
			set => SetProperty(ref _LeftThumbAxisY, value);
		}
		private string _LeftThumbAxisY;

		/// <summary>Left Thumb Dead ZoneY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbDeadZoneY {
			get => _LeftThumbDeadZoneY;
			set => SetProperty(ref _LeftThumbDeadZoneY, value);
		}
		private string _LeftThumbDeadZoneY;

		/// <summary>Left Thumb Anti Dead ZoneY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbAntiDeadZoneY {
			get => _LeftThumbAntiDeadZoneY;
			set => SetProperty(ref _LeftThumbAntiDeadZoneY, value);
		}
		private string _LeftThumbAntiDeadZoneY;

		/// <summary>Left Thumb LinearY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbLinearY {
			get => _LeftThumbLinearY;
			set => SetProperty(ref _LeftThumbLinearY, value);
		}
		private string _LeftThumbLinearY;

		/// <summary>Left Thumb Button</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbButton {
			get => _LeftThumbButton;
			set => SetProperty(ref _LeftThumbButton, value);
		}
		private string _LeftThumbButton;

		/// <summary>Left Thumb Down</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbDown {
			get => _LeftThumbDown;
			set => SetProperty(ref _LeftThumbDown, value);
		}
		private string _LeftThumbDown;

		/// <summary>Left Thumb Left</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbLeft {
			get => _LeftThumbLeft;
			set => SetProperty(ref _LeftThumbLeft, value);
		}
		private string _LeftThumbLeft;

		/// <summary>Left Thumb Right</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbRight {
			get => _LeftThumbRight;
			set => SetProperty(ref _LeftThumbRight, value);
		}
		private string _LeftThumbRight;

		/// <summary>Left Thumb Up</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftThumbUp {
			get => _LeftThumbUp;
			set => SetProperty(ref _LeftThumbUp, value);
		}
		private string _LeftThumbUp;

		/// <summary>Left Trigger</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string LeftTrigger {
			get => _LeftTrigger;
			set => SetProperty(ref _LeftTrigger, value);
		}
		private string _LeftTrigger;

		/// <summary>Left Trigger Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftTriggerDeadZone {
			get => _LeftTriggerDeadZone;
			set => SetProperty(ref _LeftTriggerDeadZone, value);
		}
		private string _LeftTriggerDeadZone;

		/// <summary>Left Trigger Anti Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftTriggerAntiDeadZone {
			get => _LeftTriggerAntiDeadZone;
			set => SetProperty(ref _LeftTriggerAntiDeadZone, value);
		}
		private string _LeftTriggerAntiDeadZone;

		/// <summary>Left Trigger Linear</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftTriggerLinear {
			get => _LeftTriggerLinear;
			set => SetProperty(ref _LeftTriggerLinear, value);
		}
		private string _LeftTriggerLinear;

		/// <summary>Pass Through</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string PassThrough {
			get => _PassThrough;
			set => SetProperty(ref _PassThrough, value);
		}
		private string _PassThrough;

		/// <summary>Right Motor Period</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightMotorPeriod {
			get => _RightMotorPeriod;
			set => SetProperty(ref _RightMotorPeriod, value);
		}
		private string _RightMotorPeriod;

		/// <summary>Right Motor Strength</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightMotorStrength {
			get => _RightMotorStrength;
			set => SetProperty(ref _RightMotorStrength, value);
		}
		private string _RightMotorStrength;

		/// <summary>Right Motor Direction</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightMotorDirection {
			get => _RightMotorDirection;
			set => SetProperty(ref _RightMotorDirection, value);
		}
		private string _RightMotorDirection;

		/// <summary>Right Shoulder</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightShoulder {
			get => _RightShoulder;
			set => SetProperty(ref _RightShoulder, value);
		}
		private string _RightShoulder;

		/// <summary>Right Thumb AxisX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbAxisX {
			get => _RightThumbAxisX;
			set => SetProperty(ref _RightThumbAxisX, value);
		}
		private string _RightThumbAxisX;

		/// <summary>Right Thumb Dead ZoneX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbDeadZoneX {
			get => _RightThumbDeadZoneX;
			set => SetProperty(ref _RightThumbDeadZoneX, value);
		}
		private string _RightThumbDeadZoneX;

		/// <summary>Right Thumb Anti Dead ZoneX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbAntiDeadZoneX {
			get => _RightThumbAntiDeadZoneX;
			set => SetProperty(ref _RightThumbAntiDeadZoneX, value);
		}
		private string _RightThumbAntiDeadZoneX;

		/// <summary>Right Thumb LinearX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbLinearX {
			get => _RightThumbLinearX;
			set => SetProperty(ref _RightThumbLinearX, value);
		}
		private string _RightThumbLinearX;

		/// <summary>Right Thumb AxisY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbAxisY {
			get => _RightThumbAxisY;
			set => SetProperty(ref _RightThumbAxisY, value);
		}
		private string _RightThumbAxisY;

		/// <summary>Right Thumb Dead ZoneY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbDeadZoneY {
			get => _RightThumbDeadZoneY;
			set => SetProperty(ref _RightThumbDeadZoneY, value);
		}
		private string _RightThumbDeadZoneY;

		/// <summary>Right Thumb Anti Dead ZoneY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbAntiDeadZoneY {
			get => _RightThumbAntiDeadZoneY;
			set => SetProperty(ref _RightThumbAntiDeadZoneY, value);
		}
		private string _RightThumbAntiDeadZoneY;

		/// <summary>Right Thumb LinearY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbLinearY {
			get => _RightThumbLinearY;
			set => SetProperty(ref _RightThumbLinearY, value);
		}
		private string _RightThumbLinearY;

		/// <summary>Right Thumb Button</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbButton {
			get => _RightThumbButton;
			set => SetProperty(ref _RightThumbButton, value);
		}
		private string _RightThumbButton;

		/// <summary>Right Thumb Down</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbDown {
			get => _RightThumbDown;
			set => SetProperty(ref _RightThumbDown, value);
		}
		private string _RightThumbDown;

		/// <summary>Right Thumb Left</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbLeft {
			get => _RightThumbLeft;
			set => SetProperty(ref _RightThumbLeft, value);
		}
		private string _RightThumbLeft;

		/// <summary>Right Thumb Right</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbRight {
			get => _RightThumbRight;
			set => SetProperty(ref _RightThumbRight, value);
		}
		private string _RightThumbRight;

		/// <summary>Right Thumb Up</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightThumbUp {
			get => _RightThumbUp;
			set => SetProperty(ref _RightThumbUp, value);
		}
		private string _RightThumbUp;

		/// <summary>Right Trigger</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(128)]
		public string RightTrigger {
			get => _RightTrigger;
			set => SetProperty(ref _RightTrigger, value);
		}
		private string _RightTrigger;

		/// <summary>Right Trigger Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightTriggerDeadZone {
			get => _RightTriggerDeadZone;
			set => SetProperty(ref _RightTriggerDeadZone, value);
		}
		private string _RightTriggerDeadZone;

		/// <summary>Right Trigger Anti Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightTriggerAntiDeadZone {
			get => _RightTriggerAntiDeadZone;
			set => SetProperty(ref _RightTriggerAntiDeadZone, value);
		}
		private string _RightTriggerAntiDeadZone;

		/// <summary>Right Trigger Linear</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightTriggerLinear {
			get => _RightTriggerLinear;
			set => SetProperty(ref _RightTriggerLinear, value);
		}
		private string _RightTriggerLinear;

		/// <summary>ButtonA Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ButtonADeadZone {
			get => _ButtonADeadZone;
			set => SetProperty(ref _ButtonADeadZone, value);
		}
		private string _ButtonADeadZone;

		/// <summary>ButtonB Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ButtonBDeadZone {
			get => _ButtonBDeadZone;
			set => SetProperty(ref _ButtonBDeadZone, value);
		}
		private string _ButtonBDeadZone;

		/// <summary>Button Back Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ButtonBackDeadZone {
			get => _ButtonBackDeadZone;
			set => SetProperty(ref _ButtonBackDeadZone, value);
		}
		private string _ButtonBackDeadZone;

		/// <summary>Button Start Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ButtonStartDeadZone {
			get => _ButtonStartDeadZone;
			set => SetProperty(ref _ButtonStartDeadZone, value);
		}
		private string _ButtonStartDeadZone;

		/// <summary>ButtonX Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ButtonXDeadZone {
			get => _ButtonXDeadZone;
			set => SetProperty(ref _ButtonXDeadZone, value);
		}
		private string _ButtonXDeadZone;

		/// <summary>ButtonY Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string ButtonYDeadZone {
			get => _ButtonYDeadZone;
			set => SetProperty(ref _ButtonYDeadZone, value);
		}
		private string _ButtonYDeadZone;

		/// <summary>Left Thumb Button Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftThumbButtonDeadZone {
			get => _LeftThumbButtonDeadZone;
			set => SetProperty(ref _LeftThumbButtonDeadZone, value);
		}
		private string _LeftThumbButtonDeadZone;

		/// <summary>Right Thumb Button Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightThumbButtonDeadZone {
			get => _RightThumbButtonDeadZone;
			set => SetProperty(ref _RightThumbButtonDeadZone, value);
		}
		private string _RightThumbButtonDeadZone;

		/// <summary>Left Shoulder Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string LeftShoulderDeadZone {
			get => _LeftShoulderDeadZone;
			set => SetProperty(ref _LeftShoulderDeadZone, value);
		}
		private string _LeftShoulderDeadZone;

		/// <summary>Right Shoulder Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string RightShoulderDeadZone {
			get => _RightShoulderDeadZone;
			set => SetProperty(ref _RightShoulderDeadZone, value);
		}
		private string _RightShoulderDeadZone;

		/// <summary>D Pad Down Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string DPadDownDeadZone {
			get => _DPadDownDeadZone;
			set => SetProperty(ref _DPadDownDeadZone, value);
		}
		private string _DPadDownDeadZone;

		/// <summary>D Pad Left Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string DPadLeftDeadZone {
			get => _DPadLeftDeadZone;
			set => SetProperty(ref _DPadLeftDeadZone, value);
		}
		private string _DPadLeftDeadZone;

		/// <summary>D Pad Right Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string DPadRightDeadZone {
			get => _DPadRightDeadZone;
			set => SetProperty(ref _DPadRightDeadZone, value);
		}
		private string _DPadRightDeadZone;

		/// <summary>D Pad Up Dead Zone</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string DPadUpDeadZone {
			get => _DPadUpDeadZone;
			set => SetProperty(ref _DPadUpDeadZone, value);
		}
		private string _DPadUpDeadZone;

		/// <summary>Mouse Left Thumb Style</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbStyle {
			get => _MouseLeftThumbStyle;
			set => SetProperty(ref _MouseLeftThumbStyle, value);
		}
		private string _MouseLeftThumbStyle;

		/// <summary>Mouse Left Thumb SensitivityX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbSensitivityX {
			get => _MouseLeftThumbSensitivityX;
			set => SetProperty(ref _MouseLeftThumbSensitivityX, value);
		}
		private string _MouseLeftThumbSensitivityX;

		/// <summary>Mouse Left Thumb SensitivityY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbSensitivityY {
			get => _MouseLeftThumbSensitivityY;
			set => SetProperty(ref _MouseLeftThumbSensitivityY, value);
		}
		private string _MouseLeftThumbSensitivityY;

		/// <summary>Mouse Left Thumb Smoothing</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbSmoothing {
			get => _MouseLeftThumbSmoothing;
			set => SetProperty(ref _MouseLeftThumbSmoothing, value);
		}
		private string _MouseLeftThumbSmoothing;

		/// <summary>Mouse Left Thumb Return</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbReturn {
			get => _MouseLeftThumbReturn;
			set => SetProperty(ref _MouseLeftThumbReturn, value);
		}
		private string _MouseLeftThumbReturn;

		/// <summary>Mouse Left Thumb Active</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbActive {
			get => _MouseLeftThumbActive;
			set => SetProperty(ref _MouseLeftThumbActive, value);
		}
		private string _MouseLeftThumbActive;

		/// <summary>Mouse Left Thumb Button</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseLeftThumbButton {
			get => _MouseLeftThumbButton;
			set => SetProperty(ref _MouseLeftThumbButton, value);
		}
		private string _MouseLeftThumbButton;

		/// <summary>Mouse Right Thumb Style</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbStyle {
			get => _MouseRightThumbStyle;
			set => SetProperty(ref _MouseRightThumbStyle, value);
		}
		private string _MouseRightThumbStyle;

		/// <summary>Mouse Right Thumb SensitivityX</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbSensitivityX {
			get => _MouseRightThumbSensitivityX;
			set => SetProperty(ref _MouseRightThumbSensitivityX, value);
		}
		private string _MouseRightThumbSensitivityX;

		/// <summary>Mouse Right Thumb SensitivityY</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbSensitivityY {
			get => _MouseRightThumbSensitivityY;
			set => SetProperty(ref _MouseRightThumbSensitivityY, value);
		}
		private string _MouseRightThumbSensitivityY;

		/// <summary>Mouse Right Thumb Smoothing</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbSmoothing {
			get => _MouseRightThumbSmoothing;
			set => SetProperty(ref _MouseRightThumbSmoothing, value);
		}
		private string _MouseRightThumbSmoothing;

		/// <summary>Mouse Right Thumb Return</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbReturn {
			get => _MouseRightThumbReturn;
			set => SetProperty(ref _MouseRightThumbReturn, value);
		}
		private string _MouseRightThumbReturn;

		/// <summary>Mouse Right Thumb Active</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbActive {
			get => _MouseRightThumbActive;
			set => SetProperty(ref _MouseRightThumbActive, value);
		}
		private string _MouseRightThumbActive;

		/// <summary>Mouse Right Thumb Button</summary>
		[Required(AllowEmptyStrings = true)]
		[DataMember]
		[StringLength(16)]
		public string MouseRightThumbButton {
			get => _MouseRightThumbButton;
			set => SetProperty(ref _MouseRightThumbButton, value);
		}
		private string _MouseRightThumbButton;

	}
}
