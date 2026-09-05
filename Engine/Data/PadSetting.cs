using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;

namespace x360ce.Engine.Data
{
	public partial class PadSetting
	{
		public PadSetting()
		{
			PropertyChanged += PadSetting_PropertyChanged;
			MapsChanged = true;
		}

		bool MapsChanged;
		object MapsLock = new object();

		[XmlIgnore]
		public List<Map> Maps
		{
			get
			{
				lock (MapsLock)
				{
					// Rebuilt only when something actually changed. Every property of this object reports
					// its own change, and that is what sets the flag, so the answer here cannot go stale.
					//
					// This used to rebuild every time it was asked, which is once per poll, about a
					// thousand times a second: thirty thousand of these objects a second, each reading
					// its text again, and all of them thrown away. It was the largest single cost the
					// program had while sitting doing nothing.
					if (MapsChanged)
					{
						var maps = new List<Map>();
						// Add buttons.
						maps.Add(new Map(MapCode.ButtonGuide, ButtonGuide, GamepadButtonFlags.Guide, ""));
						maps.Add(new Map(MapCode.ButtonA, ButtonA, GamepadButtonFlags.A, ButtonADeadZone));
						maps.Add(new Map(MapCode.ButtonB, ButtonB, GamepadButtonFlags.B, ButtonBDeadZone));
						maps.Add(new Map(MapCode.ButtonX, ButtonX, GamepadButtonFlags.X, ButtonXDeadZone));
						maps.Add(new Map(MapCode.ButtonY, ButtonY, GamepadButtonFlags.Y, ButtonYDeadZone));
						maps.Add(new Map(MapCode.ButtonBack, ButtonBack, GamepadButtonFlags.Back, ButtonBackDeadZone));
						maps.Add(new Map(MapCode.ButtonStart, ButtonStart, GamepadButtonFlags.Start, ButtonStartDeadZone));
						maps.Add(new Map(MapCode.DPadUp, DPadUp, GamepadButtonFlags.DPadUp, DPadUpDeadZone));
						maps.Add(new Map(MapCode.DPadDown, DPadDown, GamepadButtonFlags.DPadDown, DPadDownDeadZone));
						maps.Add(new Map(MapCode.DPadLeft, DPadLeft, GamepadButtonFlags.DPadLeft, DPadLeftDeadZone));
						maps.Add(new Map(MapCode.DPadRight, DPadRight, GamepadButtonFlags.DPadRight, DPadRightDeadZone));
						maps.Add(new Map(MapCode.LeftShoulder, LeftShoulder, GamepadButtonFlags.LeftShoulder, LeftShoulderDeadZone));
						maps.Add(new Map(MapCode.RightShoulder, RightShoulder, GamepadButtonFlags.RightShoulder, RightShoulderDeadZone));
						maps.Add(new Map(MapCode.LeftThumbButton, LeftThumbButton, GamepadButtonFlags.LeftThumb, LeftThumbButtonDeadZone));
						maps.Add(new Map(MapCode.RightThumbButton, RightThumbButton, GamepadButtonFlags.RightThumb, RightThumbButtonDeadZone));
						// Add triggers.
						maps.Add(new Map(MapCode.LeftTrigger, LeftTrigger, TargetType.LeftTrigger, LeftTriggerDeadZone, LeftTriggerAntiDeadZone, LeftTriggerLinear));
						maps.Add(new Map(MapCode.RightTrigger, RightTrigger, TargetType.RightTrigger, RightTriggerDeadZone, RightTriggerAntiDeadZone, RightTriggerLinear));
						// Add thumbs.
						maps.Add(new Map(MapCode.LeftThumbAxisX, LeftThumbAxisX, TargetType.LeftThumbX, LeftThumbDeadZoneX, LeftThumbAntiDeadZoneX, LeftThumbLinearX));
						maps.Add(new Map(MapCode.LeftThumbAxisY, LeftThumbAxisY, TargetType.LeftThumbY, LeftThumbDeadZoneY, LeftThumbAntiDeadZoneY, LeftThumbLinearY));
						maps.Add(new Map(MapCode.RightThumbAxisX, RightThumbAxisX, TargetType.RightThumbX, RightThumbDeadZoneX, RightThumbAntiDeadZoneX, RightThumbLinearX));
						maps.Add(new Map(MapCode.RightThumbAxisY, RightThumbAxisY, TargetType.RightThumbY, RightThumbDeadZoneY, RightThumbAntiDeadZoneY, RightThumbLinearY));
						// Add thumbs positive max and negative max map.
						maps.Add(new Map(MapCode.LeftThumbUp, LeftThumbUp, TargetType.LeftThumbY, short.MaxValue));
						maps.Add(new Map(MapCode.LeftThumbDown, LeftThumbDown, TargetType.LeftThumbY, short.MinValue));
						maps.Add(new Map(MapCode.LeftThumbLeft, LeftThumbLeft, TargetType.LeftThumbX, short.MinValue));
						maps.Add(new Map(MapCode.LeftThumbRight, LeftThumbRight, TargetType.LeftThumbX, short.MaxValue));
						maps.Add(new Map(MapCode.RightThumbUp, RightThumbUp, TargetType.RightThumbY, short.MaxValue));
						maps.Add(new Map(MapCode.RightThumbDown, RightThumbDown, TargetType.RightThumbY, short.MinValue));
						maps.Add(new Map(MapCode.RightThumbLeft, RightThumbLeft, TargetType.RightThumbX, short.MinValue));
						maps.Add(new Map(MapCode.RightThumbRight, RightThumbRight, TargetType.RightThumbX, short.MaxValue));
						// Assign list.
						_Maps = maps;
						MapsChanged = false;
					}
					return _Maps;
				}
			}
		}
		List<Map> _Maps;

		private void PadSetting_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			lock (MapsLock)
			{
				MapsChanged = true;
			}
		}

		public Guid CleanAndGetCheckSum(List<string> list = null)
		{
			// Make sure to update checksums in database if you are changing this method.
			list = list ?? new List<string>();
			// GamePad.
			AddValue(ref list, x => x.PassThrough);
			AddValue(ref list, x => x.GamePadType);
			// Force Feedback.
			AddValue(ref list, x => x.ForceEnable);
			AddValue(ref list, x => x.ForceType);
			AddValue(ref list, x => x.ForceSwapMotor);
			AddValue(ref list, x => x.ForcePassThrough);
			AddValue(ref list, x => x.ForcePassThroughIndex);
			AddValue(ref list, x => x.ForceOverall, "100");
			AddValue(ref list, x => x.ForceSpringStrength);
			AddValue(ref list, x => x.LeftMotorPeriod);
			AddValue(ref list, x => x.LeftMotorDirection);
			AddValue(ref list, x => x.LeftMotorStrength, "100");
			AddValue(ref list, x => x.RightMotorPeriod);
			AddValue(ref list, x => x.RightMotorDirection);
			AddValue(ref list, x => x.RightMotorStrength, "100");
			// D-PAD
			AddValue(ref list, x => x.AxisToDPadDeadZone, "256");
			AddValue(ref list, x => x.AxisToDPadEnabled);
			AddValue(ref list, x => x.AxisToDPadOffset);
			// Buttons.
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
			AddValue(ref list, x => x.LeftShoulder);
			AddValue(ref list, x => x.LeftThumbButton);
			AddValue(ref list, x => x.RightShoulder);
			AddValue(ref list, x => x.RightThumbButton);
			// Right Trigger.
			AddValue(ref list, x => x.RightTrigger);
			AddValue(ref list, x => x.RightTriggerDeadZone);
			AddValue(ref list, x => x.RightTriggerAntiDeadZone);
			AddValue(ref list, x => x.RightTriggerLinear);
			// Left Thumb Virtual Buttons.
			AddValue(ref list, x => x.LeftThumbUp);
			AddValue(ref list, x => x.LeftThumbRight);
			AddValue(ref list, x => x.LeftThumbDown);
			AddValue(ref list, x => x.LeftThumbLeft);
			// Left Thumb Axis X
			AddValue(ref list, x => x.LeftThumbAxisX);
			AddValue(ref list, x => x.LeftThumbDeadZoneX);
			AddValue(ref list, x => x.LeftThumbAntiDeadZoneX);
			AddValue(ref list, x => x.LeftThumbLinearX);
			// Left Thumb Axis Y
			AddValue(ref list, x => x.LeftThumbAxisY);
			AddValue(ref list, x => x.LeftThumbDeadZoneY);
			AddValue(ref list, x => x.LeftThumbAntiDeadZoneY);
			AddValue(ref list, x => x.LeftThumbLinearY);
			// Left Trigger.
			AddValue(ref list, x => x.LeftTrigger);
			AddValue(ref list, x => x.LeftTriggerDeadZone);
			AddValue(ref list, x => x.LeftTriggerAntiDeadZone);
			AddValue(ref list, x => x.LeftTriggerLinear);
			// Right Thumb Virtual Buttons.
			AddValue(ref list, x => x.RightThumbUp);
			AddValue(ref list, x => x.RightThumbRight);
			AddValue(ref list, x => x.RightThumbDown);
			AddValue(ref list, x => x.RightThumbLeft);
			// Right Thumb Axis X
			AddValue(ref list, x => x.RightThumbAxisX);
			AddValue(ref list, x => x.RightThumbDeadZoneX);
			AddValue(ref list, x => x.RightThumbAntiDeadZoneX);
			AddValue(ref list, x => x.RightThumbLinearX);
			// Right Thumb Axis Y
			AddValue(ref list, x => x.RightThumbAxisY);
			AddValue(ref list, x => x.RightThumbDeadZoneY);
			AddValue(ref list, x => x.RightThumbAntiDeadZoneY);
			AddValue(ref list, x => x.RightThumbLinearY);
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
			// Sort list to make sure that categorized order above doesn't matter.
			var sorted = list.OrderBy(x => x).ToArray();
			// Prepare list for checksum.
			var s = string.Join("\r\n", sorted);
			var bytes = System.Text.Encoding.ASCII.GetBytes(s);
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return new Guid(md5.ComputeHash(bytes));
		}

		void AddValue(ref List<string> list, Expression<Func<PadSetting, object>> setting, string defaultValue = "0")
		{
			var p = (PropertyInfo)((MemberExpression)setting.Body).Member;
			var value = (string)p.GetValue(this, null);
			// If value is not empty or default then...
			if (!isDefault(value, defaultValue))
				list.Add(string.Format("{0}={1}", p.Name, value));
			// If value is default but not empty then reset value.
			else if (value != "")
				p.SetValue(this, "", null);
		}

		#region Do not serialize default values

		public bool isDefault<T>(T value, T defaultValue = default(T))
		{
			// If value is default for the type then...
			if (Equals(value, default(T)))
				return true;
			// If value is default.
			if (Equals(value, defaultValue))
				return true;
			// If value is string and empty then...
			if (value is string && Equals(value, ""))
				return true;
			// Nought counts as untouched only where nothing else is the default. Those settings say
			// which button does what, and no button is nought. A setting given a default of its own is
			// a number - a strength, a dead zone - where nought is an answer, and the lowest one there
			// is. Counting it as untouched read it back as the default instead, so a force feedback
			// strength turned down to nothing came back as a hundred and ran the motors at full force.
			if (value is string && Equals(value, "0") && (Equals(defaultValue, null) || Equals(defaultValue, "0")))
				return true;
			return false;
		}

		public int GetValue(string s, int defaultValue)
		{
			if (string.IsNullOrEmpty(s))
				return defaultValue;
			int value;
			int.TryParse(s, out value);
			return value;
		}

		// Get non standard values.
		public int GetLeftMotorStrength() { return GetValue(LeftMotorStrength, 100); }
		public int GetRightMotorStrength() { return GetValue(RightMotorStrength, 100); }
		public int GetForceOverall() { return GetValue(ForceOverall, 100); }
		/// <summary>Strength of the centering spring, where nought - the default - means no spring.</summary>
		public int GetForceSpringStrength() { return GetValue(ForceSpringStrength, 0); }

		/// <summary>The force to send a motor, after the strengths this pad is set to.</summary>
		/// <remarks>
		/// A percentage of a percentage: the overall strength scales the pad, the motor strength scales
		/// one motor within it. Nought at either point leaves that motor still, which is what turning a
		/// strength down to nothing asks for.
		///
		/// This is for force sent straight to a controller's own motors, where nothing else applies the
		/// strengths. Force this program drives through DirectInput is scaled by the device itself, as
		/// effect gain, and must not be scaled twice.
		/// </remarks>
		/// <param name="motor">The force a game asked of one motor.</param>
		/// <param name="leftMotor">True for the large motor, false for the small one.</param>
		public byte ApplyForceStrength(byte motor, bool leftMotor)
		{
			var overall = LimitPercent(GetForceOverall());
			var strength = LimitPercent(leftMotor ? GetLeftMotorStrength() : GetRightMotorStrength());
			return (byte)Math.Round(motor * overall * strength / 10000d, MidpointRounding.AwayFromZero);
		}

		/// <summary>A percentage, kept inside nought and a hundred whatever was typed into it.</summary>
		static int LimitPercent(int value)
		{
			return value < 0 ? 0 : value > 100 ? 100 : value;
		}

		public bool ShouldSerializePadSettingChecksum() { return !isDefault(PadSettingChecksum); }
		public bool ShouldSerializeAxisToDPadDeadZone() { return !isDefault(AxisToDPadDeadZone, "256"); }
		public bool ShouldSerializeAxisToDPadEnabled() { return !isDefault(AxisToDPadEnabled); }
		public bool ShouldSerializeAxisToDPadOffset() { return !isDefault(AxisToDPadOffset); }
		public bool ShouldSerializeButtonA() { return !isDefault(ButtonA); }
		public bool ShouldSerializeButtonB() { return !isDefault(ButtonB); }
		public bool ShouldSerializeButtonBack() { return !isDefault(ButtonBack); }
		public bool ShouldSerializeButtonGuide() { return !isDefault(ButtonGuide); }
		public bool ShouldSerializeButtonStart() { return !isDefault(ButtonStart); }
		public bool ShouldSerializeButtonX() { return !isDefault(ButtonX); }
		public bool ShouldSerializeButtonY() { return !isDefault(ButtonY); }
		public bool ShouldSerializeDPad() { return !isDefault(DPad); }
		public bool ShouldSerializeDPadDown() { return !isDefault(DPadDown); }
		public bool ShouldSerializeDPadLeft() { return !isDefault(DPadLeft); }
		public bool ShouldSerializeDPadRight() { return !isDefault(DPadRight); }
		public bool ShouldSerializeDPadUp() { return !isDefault(DPadUp); }
		public bool ShouldSerializeForceEnable() { return !isDefault(ForceEnable); }
		public bool ShouldSerializeForceOverall() { return !isDefault(ForceOverall, "100"); }
		public bool ShouldSerializeForcePassThrough() { return !isDefault(ForcePassThrough); }
		public bool ShouldSerializeForcePassThroughIndex() { return !isDefault(ForcePassThroughIndex); }
		public bool ShouldSerializeForceSpringStrength() { return !isDefault(ForceSpringStrength); }
		public bool ShouldSerializeForceSwapMotor() { return !isDefault(ForceSwapMotor); }
		public bool ShouldSerializeForceType() { return !isDefault(ForceType); }
		public bool ShouldSerializeGamePadType() { return !isDefault(GamePadType); }
		public bool ShouldSerializeLeftMotorPeriod() { return !isDefault(LeftMotorPeriod); }
		public bool ShouldSerializeLeftShoulder() { return !isDefault(LeftShoulder); }
		public bool ShouldSerializeLeftThumbAntiDeadZoneX() { return !isDefault(LeftThumbAntiDeadZoneX); }
		public bool ShouldSerializeLeftThumbAntiDeadZoneY() { return !isDefault(LeftThumbAntiDeadZoneY); }
		public bool ShouldSerializeLeftThumbAxisX() { return !isDefault(LeftThumbAxisX); }
		public bool ShouldSerializeLeftThumbAxisY() { return !isDefault(LeftThumbAxisY); }
		public bool ShouldSerializeLeftThumbButton() { return !isDefault(LeftThumbButton); }
		public bool ShouldSerializeLeftThumbDeadZoneX() { return !isDefault(LeftThumbDeadZoneX); }
		public bool ShouldSerializeLeftThumbDeadZoneY() { return !isDefault(LeftThumbDeadZoneY); }
		public bool ShouldSerializeLeftThumbDown() { return !isDefault(LeftThumbDown); }
		public bool ShouldSerializeLeftThumbLeft() { return !isDefault(LeftThumbLeft); }
		public bool ShouldSerializeLeftThumbRight() { return !isDefault(LeftThumbRight); }
		public bool ShouldSerializeLeftThumbUp() { return !isDefault(LeftThumbUp); }
		public bool ShouldSerializeLeftTrigger() { return !isDefault(LeftTrigger); }
		public bool ShouldSerializeLeftTriggerDeadZone() { return !isDefault(LeftTriggerDeadZone); }
		public bool ShouldSerializeLeftTriggerAntiDeadZone() { return !isDefault(LeftTriggerAntiDeadZone); }
		public bool ShouldSerializeLeftTriggerLinear() { return !isDefault(LeftTriggerLinear); }
		public bool ShouldSerializePassThrough() { return !isDefault(PassThrough); }
		public bool ShouldSerializeRightMotorPeriod() { return !isDefault(RightMotorPeriod); }
		public bool ShouldSerializeRightShoulder() { return !isDefault(RightShoulder); }
		public bool ShouldSerializeRightThumbAntiDeadZoneX() { return !isDefault(RightThumbAntiDeadZoneX); }
		public bool ShouldSerializeRightThumbAntiDeadZoneY() { return !isDefault(RightThumbAntiDeadZoneY); }
		public bool ShouldSerializeRightThumbAxisX() { return !isDefault(RightThumbAxisX); }
		public bool ShouldSerializeRightThumbAxisY() { return !isDefault(RightThumbAxisY); }
		public bool ShouldSerializeRightThumbButton() { return !isDefault(RightThumbButton); }
		public bool ShouldSerializeRightThumbDeadZoneX() { return !isDefault(RightThumbDeadZoneX); }
		public bool ShouldSerializeRightThumbDeadZoneY() { return !isDefault(RightThumbDeadZoneY); }
		public bool ShouldSerializeRightThumbDown() { return !isDefault(RightThumbDown); }
		public bool ShouldSerializeRightThumbLeft() { return !isDefault(RightThumbLeft); }
		public bool ShouldSerializeRightThumbRight() { return !isDefault(RightThumbRight); }
		public bool ShouldSerializeRightThumbUp() { return !isDefault(RightThumbUp); }
		public bool ShouldSerializeRightTrigger() { return !isDefault(RightTrigger); }
		public bool ShouldSerializeRightTriggerDeadZone() { return !isDefault(RightTriggerDeadZone); }
		public bool ShouldSerializeRightTriggerAntiDeadZone() { return !isDefault(RightTriggerAntiDeadZone); }
		public bool ShouldSerializeRightTriggerLinear() { return !isDefault(RightTriggerLinear); }
		public bool ShouldSerializeLeftThumbLinearX() { return !isDefault(LeftThumbLinearX); }
		public bool ShouldSerializeLeftThumbLinearY() { return !isDefault(LeftThumbLinearY); }
		public bool ShouldSerializeRightThumbLinearX() { return !isDefault(RightThumbLinearX); }
		public bool ShouldSerializeRightThumbLinearY() { return !isDefault(RightThumbLinearY); }
		public bool ShouldSerializeLeftMotorStrength() { return !isDefault(LeftMotorStrength, "100"); }
		public bool ShouldSerializeRightMotorStrength() { return !isDefault(RightMotorStrength, "100"); }
		public bool ShouldSerializeLeftMotorDirection() { return !isDefault(LeftMotorDirection); }
		public bool ShouldSerializeRightMotorDirection() { return !isDefault(RightMotorDirection); }
		public bool ShouldSerializeButtonADeadZone() { return !isDefault(ButtonADeadZone); }
		public bool ShouldSerializeButtonBDeadZone() { return !isDefault(ButtonBDeadZone); }
		public bool ShouldSerializeButtonBackDeadZone() { return !isDefault(ButtonBackDeadZone); }
		public bool ShouldSerializeButtonStartDeadZone() { return !isDefault(ButtonStartDeadZone); }
		public bool ShouldSerializeButtonXDeadZone() { return !isDefault(ButtonXDeadZone); }
		public bool ShouldSerializeButtonYDeadZone() { return !isDefault(ButtonYDeadZone); }
		public bool ShouldSerializeLeftThumbButtonDeadZone() { return !isDefault(LeftThumbButtonDeadZone); }
		public bool ShouldSerializeRightThumbButtonDeadZone() { return !isDefault(RightThumbButtonDeadZone); }
		public bool ShouldSerializeLeftShoulderDeadZone() { return !isDefault(LeftShoulderDeadZone); }
		public bool ShouldSerializeRightShoulderDeadZone() { return !isDefault(RightShoulderDeadZone); }
		public bool ShouldSerializeDPadDownDeadZone() { return !isDefault(DPadDownDeadZone); }
		public bool ShouldSerializeDPadLeftDeadZone() { return !isDefault(DPadLeftDeadZone); }
		public bool ShouldSerializeDPadRightDeadZone() { return !isDefault(DPadRightDeadZone); }
		public bool ShouldSerializeDPadUpDeadZone() { return !isDefault(DPadUpDeadZone); }

		#endregion

	}
}
