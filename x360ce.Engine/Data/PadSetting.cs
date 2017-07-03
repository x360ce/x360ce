using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class PadSetting
	{
		public PadSetting()
		{
			// Set default values;
			AxisToDPadDeadZone = "256";
			ForceOverall = "100";
			LeftMotorStrength = "100";
			RightMotorStrength = "100";
		}

		public Guid CleanAndGetCheckSum()
		{
			// Make sure to update checksums in database if you are changing this method.
			var list = new List<string>();
			AddValue(ref list, x => x.AxisToDPadDeadZone, "256");
			AddValue(ref list, x => x.AxisToDPadEnabled);
			AddValue(ref list, x => x.AxisToDPadOffset);
			AddValue(ref list, x => x.ButtonA);
			AddValue(ref list, x => x.ButtonB);
			AddValue(ref list, x => x.ButtonGuide);
			AddValue(ref list, x => x.ButtonBack);
			AddValue(ref list, x => x.ButtonStart);
			AddValue(ref list, x => x.ButtonX);
			AddValue(ref list, x => x.ButtonY);
			AddValue(ref list, x => x.DPad);
			AddValue(ref list, x => x.DPadDown);
			AddValue(ref list, x => x.DPadLeft);
			AddValue(ref list, x => x.DPadRight);
			AddValue(ref list, x => x.DPadUp);
			AddValue(ref list, x => x.ForceEnable);
			AddValue(ref list, x => x.ForceOverall, "100");
			AddValue(ref list, x => x.ForceSwapMotor);
			AddValue(ref list, x => x.ForceType);
			AddValue(ref list, x => x.GamePadType);
			AddValue(ref list, x => x.LeftMotorPeriod);
			AddValue(ref list, x => x.LeftShoulder);
			AddValue(ref list, x => x.LeftThumbAntiDeadZoneX);
			AddValue(ref list, x => x.LeftThumbAntiDeadZoneY);
			AddValue(ref list, x => x.LeftThumbAxisX);
			AddValue(ref list, x => x.LeftThumbAxisY);
			AddValue(ref list, x => x.LeftThumbButton);
			AddValue(ref list, x => x.LeftThumbDeadZoneX);
			AddValue(ref list, x => x.LeftThumbDeadZoneY);
			AddValue(ref list, x => x.LeftThumbDown);
			AddValue(ref list, x => x.LeftThumbLeft);
			AddValue(ref list, x => x.LeftThumbRight);
			AddValue(ref list, x => x.LeftThumbUp);
			AddValue(ref list, x => x.LeftTrigger);
			AddValue(ref list, x => x.LeftTriggerDeadZone);
			AddValue(ref list, x => x.PassThrough);
			AddValue(ref list, x => x.RightMotorPeriod);
			AddValue(ref list, x => x.RightShoulder);
			AddValue(ref list, x => x.RightThumbAntiDeadZoneX);
			AddValue(ref list, x => x.RightThumbAntiDeadZoneY);
			AddValue(ref list, x => x.RightThumbAxisX);
			AddValue(ref list, x => x.RightThumbAxisY);
			AddValue(ref list, x => x.RightThumbButton);
			AddValue(ref list, x => x.RightThumbDeadZoneX);
			AddValue(ref list, x => x.RightThumbDeadZoneY);
			AddValue(ref list, x => x.RightThumbDown);
			AddValue(ref list, x => x.RightThumbLeft);
			AddValue(ref list, x => x.RightThumbRight);
			AddValue(ref list, x => x.RightThumbUp);
			AddValue(ref list, x => x.RightTrigger);
			AddValue(ref list, x => x.RightTriggerDeadZone);
			// New
			AddValue(ref list, x => x.LeftThumbLinearX);
			AddValue(ref list, x => x.LeftThumbLinearY);
			AddValue(ref list, x => x.RightThumbLinearX);
			AddValue(ref list, x => x.RightThumbLinearY);
			AddValue(ref list, x => x.LeftMotorStrength, "100");
			AddValue(ref list, x => x.RightMotorStrength, "100");
			AddValue(ref list, x => x.LeftMotorDirection);
			AddValue(ref list, x => x.RightMotorDirection);
			// Axis to Button dead-zones.
			AddValue(ref list, x => x.ButtonADeadZone);
			AddValue(ref list, x => x.ButtonBDeadZone);
			AddValue(ref list, x => x.ButtonBackDeadZone);
			AddValue(ref list, x => x.ButtonStartDeadZone);
			AddValue(ref list, x => x.ButtonXDeadZone);
			AddValue(ref list, x => x.ButtonYDeadZone);
			AddValue(ref list, x => x.LeftThumbButtonDeadZone);
			AddValue(ref list, x => x.RightThumbButtonDeadZone);
			AddValue(ref list, x => x.LeftShoulderDeadZone);
			AddValue(ref list, x => x.RightShoulderDeadZone);
			AddValue(ref list, x => x.DPadDownDeadZone);
			AddValue(ref list, x => x.DPadLeftDeadZone);
			AddValue(ref list, x => x.DPadRightDeadZone);
			AddValue(ref list, x => x.DPadUpDeadZone);
			// If all values are empty or default then...
			if (list.Count == 0)
				return Guid.Empty;
			// Prepare list for checksum.
			var s = string.Join("\r\n", list.ToArray());
			var bytes = System.Text.Encoding.ASCII.GetBytes(s);
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return new Guid(md5.ComputeHash(bytes));
		}

		void AddValue(ref List<string> list, Expression<Func<PadSetting, object>> setting, string defaultValue = "0")
		{
			var p = (PropertyInfo)((MemberExpression)setting.Body).Member;
			var value = (string)p.GetValue(this, null);
			if (notDefault(value, defaultValue))
			{
				list.Add(string.Format("{0}={1}", p.Name, value));
			}
			// If value is default but not empty then reset value.
			else if (value != "")
			{

				p.SetValue(this, "", null);
			}
		}

		#region Do not serialize default values

		bool notDefault<T>(T value, T defaultValue = default(T))
		{
			if (value is string && Equals(value, ""))
				return false;
			if (Equals(value, default(T)))
				return false;
			if (Equals(value, defaultValue))
				return false;
			return true;
		}

		public bool ShouldSerializePadSettingChecksum() { return notDefault(PadSettingChecksum); }
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