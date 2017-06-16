using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class PadSetting
	{
		public Guid GetCheckSum()
		{
			// Make sure to update checksums in database if you are changing this method.
			var list = new List<string>();
			AddValue(ref list, "AxisToDPadDeadZone", AxisToDPadDeadZone, null, "", "256");
			AddValue(ref list, "AxisToDPadEnabled", AxisToDPadEnabled, null, "", "0");
			AddValue(ref list, "AxisToDPadOffset", AxisToDPadOffset, null, "", "0");
			AddValue(ref list, "ButtonA", ButtonA, null, "", "0");
			AddValue(ref list, "ButtonB", ButtonB, null, "", "0");
			AddValue(ref list, "ButtonGuide", ButtonGuide, null, "", "0");
			AddValue(ref list, "ButtonBack", ButtonBack, null, "", "0");
			AddValue(ref list, "ButtonStart", ButtonStart, null, "", "0");
			AddValue(ref list, "ButtonX", ButtonX, null, "", "0");
			AddValue(ref list, "ButtonY", ButtonY, null, "", "0");
			AddValue(ref list, "DPad", DPad, null, "", "0");
			AddValue(ref list, "DPadDown", DPadDown, null, "", "0");
			AddValue(ref list, "DPadLeft", DPadLeft, null, "", "0");
			AddValue(ref list, "DPadRight", DPadRight, null, "", "0");
			AddValue(ref list, "DPadUp", DPadUp, null, "", "0");
			AddValue(ref list, "ForceEnable", ForceEnable, null, "", "0");
			AddValue(ref list, "ForceOverall", ForceOverall, null, "", "100");
			AddValue(ref list, "ForceSwapMotor", ForceSwapMotor, null, "", "0");
			AddValue(ref list, "ForceType", ForceType, null, "", "0");
			AddValue(ref list, "GamePadType", GamePadType, null, "", "0");
			AddValue(ref list, "LeftMotorPeriod", LeftMotorPeriod, null, "", "0");
			AddValue(ref list, "LeftShoulder", LeftShoulder, null, "", "0");
			AddValue(ref list, "LeftThumbAntiDeadZoneX", LeftThumbAntiDeadZoneX, null, "", "0");
			AddValue(ref list, "LeftThumbAntiDeadZoneY", LeftThumbAntiDeadZoneY, null, "", "0");
			AddValue(ref list, "LeftThumbAxisX", LeftThumbAxisX, null, "", "0");
			AddValue(ref list, "LeftThumbAxisY", LeftThumbAxisY, null, "", "0");
			AddValue(ref list, "LeftThumbButton", LeftThumbButton, null, "", "0");
			AddValue(ref list, "LeftThumbDeadZoneX", LeftThumbDeadZoneX, null, "", "0");
			AddValue(ref list, "LeftThumbDeadZoneY", LeftThumbDeadZoneY, null, "", "0");
			AddValue(ref list, "LeftThumbDown", LeftThumbDown, null, "", "0");
			AddValue(ref list, "LeftThumbLeft", LeftThumbLeft, null, "", "0");
			AddValue(ref list, "LeftThumbRight", LeftThumbRight, null, "", "0");
			AddValue(ref list, "LeftThumbUp", LeftThumbUp, null, "", "0");
			AddValue(ref list, "LeftTrigger", LeftTrigger, null, "", "0");
			AddValue(ref list, "LeftTriggerDeadZone", LeftTriggerDeadZone, null, "", "0");
			AddValue(ref list, "PassThrough", PassThrough, null, "", "0");
			AddValue(ref list, "RightMotorPeriod", RightMotorPeriod, null, "", "0");
			AddValue(ref list, "RightShoulder", RightShoulder, null, "", "0");
			AddValue(ref list, "RightThumbAntiDeadZoneX", RightThumbAntiDeadZoneX, null, "", "0");
			AddValue(ref list, "RightThumbAntiDeadZoneY", RightThumbAntiDeadZoneY, null, "", "0");
			AddValue(ref list, "RightThumbAxisX", RightThumbAxisX, null, "", "0");
			AddValue(ref list, "RightThumbAxisY", RightThumbAxisY, null, "", "0");
			AddValue(ref list, "RightThumbButton", RightThumbButton, null, "", "0");
			AddValue(ref list, "RightThumbDeadZoneX", RightThumbDeadZoneX, null, "", "0");
			AddValue(ref list, "RightThumbDeadZoneY", RightThumbDeadZoneY, null, "", "0");
			AddValue(ref list, "RightThumbDown", RightThumbDown, null, "", "0");
			AddValue(ref list, "RightThumbLeft", RightThumbLeft, null, "", "0");
			AddValue(ref list, "RightThumbRight", RightThumbRight, null, "", "0");
			AddValue(ref list, "RightThumbUp", RightThumbUp, null, "", "0");
			AddValue(ref list, "RightTrigger", RightTrigger, null, "", "0");
			AddValue(ref list, "RightTriggerDeadZone", RightTriggerDeadZone, null, "", "0");
			// New
			AddValue(ref list, "LeftThumbLinearX", LeftThumbLinearX, null, "", "0");
			AddValue(ref list, "LeftThumbLinearY", LeftThumbLinearY, null, "", "0");
			AddValue(ref list, "RightThumbLinearX", RightThumbLinearX, null, "", "0");
			AddValue(ref list, "RightThumbLinearY", RightThumbLinearY, null, "", "0");
			AddValue(ref list, "LeftMotorStrength", LeftMotorStrength, null, "", "100");
			AddValue(ref list, "RightMotorStrength", RightMotorStrength, null, "", "100");
			AddValue(ref list, "LeftMotorDirection", LeftMotorDirection, null, "", "0");
			AddValue(ref list, "RightMotorDirection", RightMotorDirection, null, "", "0");
			// Axis to Button dead-zones.
			AddValue(ref list, "ButtonADeadZone", ButtonADeadZone, null, "", "0");
			AddValue(ref list, "ButtonBDeadZone", ButtonBDeadZone, null, "", "0");
			AddValue(ref list, "ButtonBackDeadZone", ButtonBackDeadZone, null, "", "0");
			AddValue(ref list, "ButtonStartDeadZone", ButtonStartDeadZone, null, "", "0");
			AddValue(ref list, "ButtonXDeadZone", ButtonXDeadZone, null, "", "0");
			AddValue(ref list, "ButtonYDeadZone", ButtonYDeadZone, null, "", "0");
			AddValue(ref list, "LeftThumbButtonDeadZone", LeftThumbButtonDeadZone, null, "", "0");
			AddValue(ref list, "RightThumbButtonDeadZone", RightThumbButtonDeadZone, null, "", "0");
			AddValue(ref list, "LeftShoulderDeadZone", LeftShoulderDeadZone, null, "", "0");
			AddValue(ref list, "RightShoulderDeadZone", RightShoulderDeadZone, null, "", "0");
			AddValue(ref list, "DPadDownDeadZone", DPadDownDeadZone, null, "", "0");
			AddValue(ref list, "DPadLeftDeadZone", DPadLeftDeadZone, null, "", "0");
			AddValue(ref list, "DPadRightDeadZone", DPadRightDeadZone, null, "", "0");
			AddValue(ref list, "DPadUpDeadZone", DPadUpDeadZone, null, "", "0");
			// If all values are empty or default then...
			if (list.Count == 0)
				return Guid.Empty;
			// Prepare list for checksum.
			var s = string.Join("\r\n", list.ToArray());
			var bytes = System.Text.Encoding.ASCII.GetBytes(s);
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return new Guid(md5.ComputeHash(bytes));
		}

		void AddValue(ref List<string> list, string name, string value, params string[] ignore)
		{
			if (ignore.Contains(value)) return;
			list.Add(string.Format("{0}={1}", name, value));
		}

		#region Do not serialize default values

		bool notDefault(string value, string defaultValue = null)
		{
			return (value != "" && value != "0" && value != null && value != defaultValue);
		}

		//public bool ShouldSerializePadSettingChecksum() { return IsDefault(PadSettingChecksum); }
		public bool ShouldSerializeAxisToDPadDeadZone() { return notDefault(AxisToDPadDeadZone, "256"); }
		public bool ShouldSerializeAxisToDPadEnabled() { return notDefault(AxisToDPadEnabled); }
		public bool ShouldSerializeAxisToDPadOffset() { return notDefault(AxisToDPadOffset); }
		public bool ShouldSerializeButtonA() { return notDefault(ButtonA); }
		public bool ShouldSerializeButtonB() { return notDefault(ButtonB); }
		public bool ShouldSerializeButtonBig() { return notDefault(ButtonBig); }
		public bool ShouldSerializeButtonBack() { return notDefault(ButtonBack); }
		public bool ShouldSerializeButtonGuide() { return notDefault(ButtonGuide); }
		public bool ShouldSerializeButtonStart() { return notDefault(ButtonStart); }
		public bool ShouldSerializeButtonX() { return notDefault(ButtonX); }
		public bool ShouldSerializeButtonY() { return notDefault(ButtonY); }
		public bool ShouldSerializeDPad() { return notDefault(DPad); }
		public bool ShouldSerializeDPadDown() { return notDefault(DPadDown); }
		public bool ShouldSerializeDPadLeft() { return notDefault(DPadLeft); }
		public bool ShouldSerializeDPadRight() { return notDefault(DPadRight); }
		public bool ShouldSerializeDPadUp() { return notDefault(DPadUp); }
		public bool ShouldSerializeForceEnable() { return notDefault(ForceEnable); }
		public bool ShouldSerializeForceOverall() { return notDefault(ForceOverall, "100"); }
		public bool ShouldSerializeForceSwapMotor() { return notDefault(ForceSwapMotor); }
		public bool ShouldSerializeForceType() { return notDefault(ForceType); }
		public bool ShouldSerializeGamePadType() { return notDefault(GamePadType); }
		public bool ShouldSerializeLeftMotorPeriod() { return notDefault(LeftMotorPeriod); }
		public bool ShouldSerializeLeftShoulder() { return notDefault(LeftShoulder); }
		public bool ShouldSerializeLeftThumbAntiDeadZoneX() { return notDefault(LeftThumbAntiDeadZoneX); }
		public bool ShouldSerializeLeftThumbAntiDeadZoneY() { return notDefault(LeftThumbAntiDeadZoneY); }
		public bool ShouldSerializeLeftThumbAxisX() { return notDefault(LeftThumbAxisX); }
		public bool ShouldSerializeLeftThumbAxisY() { return notDefault(LeftThumbAxisY); }
		public bool ShouldSerializeLeftThumbButton() { return notDefault(LeftThumbButton); }
		public bool ShouldSerializeLeftThumbDeadZoneX() { return notDefault(LeftThumbDeadZoneX); }
		public bool ShouldSerializeLeftThumbDeadZoneY() { return notDefault(LeftThumbDeadZoneY); }
		public bool ShouldSerializeLeftThumbDown() { return notDefault(LeftThumbDown); }
		public bool ShouldSerializeLeftThumbLeft() { return notDefault(LeftThumbLeft); }
		public bool ShouldSerializeLeftThumbRight() { return notDefault(LeftThumbRight); }
		public bool ShouldSerializeLeftThumbUp() { return notDefault(LeftThumbUp); }
		public bool ShouldSerializeLeftTrigger() { return notDefault(LeftTrigger); }
		public bool ShouldSerializeLeftTriggerDeadZone() { return notDefault(LeftTriggerDeadZone); }
		public bool ShouldSerializePassThrough() { return notDefault(PassThrough); }
		public bool ShouldSerializeRightMotorPeriod() { return notDefault(RightMotorPeriod); }
		public bool ShouldSerializeRightShoulder() { return notDefault(RightShoulder); }
		public bool ShouldSerializeRightThumbAntiDeadZoneX() { return notDefault(RightThumbAntiDeadZoneX); }
		public bool ShouldSerializeRightThumbAntiDeadZoneY() { return notDefault(RightThumbAntiDeadZoneY); }
		public bool ShouldSerializeRightThumbAxisX() { return notDefault(RightThumbAxisX); }
		public bool ShouldSerializeRightThumbAxisY() { return notDefault(RightThumbAxisY); }
		public bool ShouldSerializeRightThumbButton() { return notDefault(RightThumbButton); }
		public bool ShouldSerializeRightThumbDeadZoneX() { return notDefault(RightThumbDeadZoneX); }
		public bool ShouldSerializeRightThumbDeadZoneY() { return notDefault(RightThumbDeadZoneY); }
		public bool ShouldSerializeRightThumbDown() { return notDefault(RightThumbDown); }
		public bool ShouldSerializeRightThumbLeft() { return notDefault(RightThumbLeft); }
		public bool ShouldSerializeRightThumbRight() { return notDefault(RightThumbRight); }
		public bool ShouldSerializeRightThumbUp() { return notDefault(RightThumbUp); }
		public bool ShouldSerializeRightTrigger() { return notDefault(RightTrigger); }
		public bool ShouldSerializeRightTriggerDeadZone() { return notDefault(RightTriggerDeadZone); }
		public bool ShouldSerializeLeftThumbLinearX() { return notDefault(LeftThumbLinearX); }
		public bool ShouldSerializeLeftThumbLinearY() { return notDefault(LeftThumbLinearY); }
		public bool ShouldSerializeRightThumbLinearX() { return notDefault(RightThumbLinearX); }
		public bool ShouldSerializeRightThumbLinearY() { return notDefault(RightThumbLinearY); }
		public bool ShouldSerializeLeftMotorStrength() { return notDefault(LeftMotorStrength, "100"); }
		public bool ShouldSerializeRightMotorStrength() { return notDefault(RightMotorStrength, "100"); }
		public bool ShouldSerializeLeftMotorDirection() { return notDefault(LeftMotorDirection); }
		public bool ShouldSerializeRightMotorDirection() { return notDefault(RightMotorDirection); }
		public bool ShouldSerializeButtonADeadZone() { return notDefault(ButtonADeadZone); }
		public bool ShouldSerializeButtonBDeadZone() { return notDefault(ButtonBDeadZone); }
		public bool ShouldSerializeButtonBackDeadZone() { return notDefault(ButtonBackDeadZone); }
		public bool ShouldSerializeButtonStartDeadZone() { return notDefault(ButtonStartDeadZone); }
		public bool ShouldSerializeButtonXDeadZone() { return notDefault(ButtonXDeadZone); }
		public bool ShouldSerializeButtonYDeadZone() { return notDefault(ButtonYDeadZone); }
		public bool ShouldSerializeLeftThumbButtonDeadZone() { return notDefault(LeftThumbButtonDeadZone); }
		public bool ShouldSerializeRightThumbButtonDeadZone() { return notDefault(RightThumbButtonDeadZone); }
		public bool ShouldSerializeLeftShoulderDeadZone() { return notDefault(LeftShoulderDeadZone); }
		public bool ShouldSerializeRightShoulderDeadZone() { return notDefault(RightShoulderDeadZone); }
		public bool ShouldSerializeDPadDownDeadZone() { return notDefault(DPadDownDeadZone); }
		public bool ShouldSerializeDPadLeftDeadZone() { return notDefault(DPadLeftDeadZone); }
		public bool ShouldSerializeDPadRightDeadZone() { return notDefault(DPadRightDeadZone); }
		public bool ShouldSerializeDPadUpDeadZone() { return notDefault(DPadUpDeadZone); }

		#endregion

	}
}