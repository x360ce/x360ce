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
			// Stored by the table and the model, and set by nothing: no page offers it and the
			// checksum leaves it out. The column refuses null, and a copy made without it, such as one
			// sent by an older program, would otherwise try to save null there.
			_ButtonBig = "";
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
						maps.Add(new Map(MapCode.ButtonA, ButtonA, GamepadButtonFlags.A, OrDefault(ButtonADeadZone, nameof(ButtonADeadZone))));
						maps.Add(new Map(MapCode.ButtonB, ButtonB, GamepadButtonFlags.B, OrDefault(ButtonBDeadZone, nameof(ButtonBDeadZone))));
						maps.Add(new Map(MapCode.ButtonX, ButtonX, GamepadButtonFlags.X, OrDefault(ButtonXDeadZone, nameof(ButtonXDeadZone))));
						maps.Add(new Map(MapCode.ButtonY, ButtonY, GamepadButtonFlags.Y, OrDefault(ButtonYDeadZone, nameof(ButtonYDeadZone))));
						maps.Add(new Map(MapCode.ButtonBack, ButtonBack, GamepadButtonFlags.Back, OrDefault(ButtonBackDeadZone, nameof(ButtonBackDeadZone))));
						maps.Add(new Map(MapCode.ButtonStart, ButtonStart, GamepadButtonFlags.Start, OrDefault(ButtonStartDeadZone, nameof(ButtonStartDeadZone))));
						maps.Add(new Map(MapCode.DPadUp, DPadUp, GamepadButtonFlags.DPadUp, OrDefault(DPadUpDeadZone, nameof(DPadUpDeadZone))));
						maps.Add(new Map(MapCode.DPadDown, DPadDown, GamepadButtonFlags.DPadDown, OrDefault(DPadDownDeadZone, nameof(DPadDownDeadZone))));
						maps.Add(new Map(MapCode.DPadLeft, DPadLeft, GamepadButtonFlags.DPadLeft, OrDefault(DPadLeftDeadZone, nameof(DPadLeftDeadZone))));
						maps.Add(new Map(MapCode.DPadRight, DPadRight, GamepadButtonFlags.DPadRight, OrDefault(DPadRightDeadZone, nameof(DPadRightDeadZone))));
						maps.Add(new Map(MapCode.LeftShoulder, LeftShoulder, GamepadButtonFlags.LeftShoulder, OrDefault(LeftShoulderDeadZone, nameof(LeftShoulderDeadZone))));
						maps.Add(new Map(MapCode.RightShoulder, RightShoulder, GamepadButtonFlags.RightShoulder, OrDefault(RightShoulderDeadZone, nameof(RightShoulderDeadZone))));
						maps.Add(new Map(MapCode.LeftThumbButton, LeftThumbButton, GamepadButtonFlags.LeftThumb, OrDefault(LeftThumbButtonDeadZone, nameof(LeftThumbButtonDeadZone))));
						maps.Add(new Map(MapCode.RightThumbButton, RightThumbButton, GamepadButtonFlags.RightThumb, OrDefault(RightThumbButtonDeadZone, nameof(RightThumbButtonDeadZone))));
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
			AddValue(ref list, x => x.ForceSpringEnable);
			AddValue(ref list, x => x.ForceSpringStrength);
			AddValue(ref list, x => x.WheelRange);
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
			AddDeadZone(ref list, x => x.ButtonA, x => x.ButtonADeadZone);
			AddDeadZone(ref list, x => x.ButtonB, x => x.ButtonBDeadZone);
			AddDeadZone(ref list, x => x.ButtonBack, x => x.ButtonBackDeadZone);
			AddDeadZone(ref list, x => x.ButtonStart, x => x.ButtonStartDeadZone);
			AddDeadZone(ref list, x => x.ButtonX, x => x.ButtonXDeadZone);
			AddDeadZone(ref list, x => x.ButtonY, x => x.ButtonYDeadZone);
			AddDeadZone(ref list, x => x.LeftThumbButton, x => x.LeftThumbButtonDeadZone);
			AddDeadZone(ref list, x => x.RightThumbButton, x => x.RightThumbButtonDeadZone);
			AddDeadZone(ref list, x => x.LeftShoulder, x => x.LeftShoulderDeadZone);
			AddDeadZone(ref list, x => x.RightShoulder, x => x.RightShoulderDeadZone);
			AddDeadZone(ref list, x => x.DPadDown, x => x.DPadDownDeadZone);
			AddDeadZone(ref list, x => x.DPadLeft, x => x.DPadLeftDeadZone);
			AddDeadZone(ref list, x => x.DPadRight, x => x.DPadRightDeadZone);
			AddDeadZone(ref list, x => x.DPadUp, x => x.DPadUpDeadZone);
			// If all values are empty or default then...
			if (list.Count == 0)
				return Guid.Empty;
			// Sort list to make sure that categorized order above doesn't matter. The order must not
			// depend on the computer's language: Welsh and Albanian read "th" as one letter, Lithuanian
			// and Latvian sort "Y" with "I", Azerbaijani puts "X" after "H" and Hawaiian puts vowels
			// first, and each would give the same settings another checksum there than on the server.
			// Every checksum the server has stored since 2020 is in this order.
			var sorted = list.OrderBy(x => x, StringComparer.InvariantCulture).ToArray();
			// Prepare list for checksum.
			var s = string.Join("\r\n", sorted);
			var bytes = System.Text.Encoding.ASCII.GetBytes(s);
			var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
			return new Guid(md5.ComputeHash(bytes));
		}

		/// <summary>Adds a button's axis dead zone, which counts only while an axis, a slider or a formula drives the button.</summary>
		/// <remarks>
		/// The dead zone is how far an axis must move before it presses the button, so a button driven by
		/// a button never reads it. A value nothing reads, or the default, is stored as nothing: kept, it
		/// made presets that differ in nothing look like two. Nought on a button an axis drives is kept;
		/// it presses the button at the first movement, and people set it.
		///
		/// Driven by an axis means the mapping starts with a, x, s or h, or is a formula starting with =.
		/// The change script that renames stored presets tests the mapping the same way, with
		/// LIKE '[axsh=]%', which ignores case as the database compares text, so the program and the
		/// database cannot disagree about which to count.
		/// </remarks>
		/// <param name="list">The lines the checksum is made from.</param>
		/// <param name="button">The button the dead zone belongs to.</param>
		/// <param name="deadZone">The dead zone.</param>
		void AddDeadZone(ref List<string> list, Expression<Func<PadSetting, object>> button, Expression<Func<PadSetting, object>> deadZone)
		{
			var mapping = (string)((PropertyInfo)((MemberExpression)button.Body).Member).GetValue(this, null) ?? "";
			if (mapping.Length > 0 && "axshAXSH=".IndexOf(mapping[0]) >= 0)
			{
				AddValue(ref list, deadZone, SettingName.DefaultButtonDeadZone);
				return;
			}
			var p = (PropertyInfo)((MemberExpression)deadZone.Body).Member;
			if ((string)p.GetValue(this, null) != "")
				p.SetValue(this, "", null);
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

		/// <summary>A setting's text, or its default when the preset leaves the setting out.</summary>
		/// <remarks>
		/// A preset leaves out a setting that is at its default, so whatever reads a preset puts the
		/// same default back. The defaults are the ones on <see cref="SettingName"/>, which the
		/// controller page shows; reading a missing number as nought instead ran the engine on other
		/// settings than the page showed.
		/// </remarks>
		/// <param name="value">The setting's text in this preset.</param>
		/// <param name="settingName">The setting's name, the same here and on <see cref="SettingName"/>.</param>
		public static string OrDefault(string value, string settingName)
		{
			if (!string.IsNullOrEmpty(value))
				return value;
			return JocysCom.ClassLibrary.Runtime.Attributes.GetDefaultValue<SettingName, string>(settingName) ?? "";
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
		public int GetLeftMotorStrength() { return GetValue(OrDefault(LeftMotorStrength, nameof(LeftMotorStrength)), 0); }
		public int GetRightMotorStrength() { return GetValue(OrDefault(RightMotorStrength, nameof(RightMotorStrength)), 0); }
		public int GetForceOverall() { return GetValue(OrDefault(ForceOverall, nameof(ForceOverall)), 0); }
		/// <summary>Strength of the centering spring. It does nothing until the spring is turned on.</summary>
		public int GetForceSpringStrength() { return GetValue(OrDefault(ForceSpringStrength, nameof(ForceSpringStrength)), 0); }
		/// <summary>Steering range in degrees sent to a Logitech wheel, where nought - the default - leaves the wheel as it is.</summary>
		public int GetWheelRange() { return GetValue(WheelRange, 0); }
		/// <summary>Period of the left, low-frequency motor at full drive, in milliseconds.</summary>
		public int GetLeftMotorPeriod() { return GetValue(OrDefault(LeftMotorPeriod, nameof(LeftMotorPeriod)), 0); }
		/// <summary>Period of the right, high-frequency motor at full drive, in milliseconds.</summary>
		public int GetRightMotorPeriod() { return GetValue(OrDefault(RightMotorPeriod, nameof(RightMotorPeriod)), 0); }

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
		public bool ShouldSerializeButtonBig() { return !isDefault(ButtonBig); }
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
		public bool ShouldSerializeForceSpringEnable() { return !isDefault(ForceSpringEnable); }
		public bool ShouldSerializeForceSpringStrength() { return !isDefault(ForceSpringStrength); }
		public bool ShouldSerializeWheelRange() { return !isDefault(WheelRange); }
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
		public bool ShouldSerializeButtonADeadZone() { return !isDefault(ButtonADeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeButtonBDeadZone() { return !isDefault(ButtonBDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeButtonBackDeadZone() { return !isDefault(ButtonBackDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeButtonStartDeadZone() { return !isDefault(ButtonStartDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeButtonXDeadZone() { return !isDefault(ButtonXDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeButtonYDeadZone() { return !isDefault(ButtonYDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeLeftThumbButtonDeadZone() { return !isDefault(LeftThumbButtonDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeRightThumbButtonDeadZone() { return !isDefault(RightThumbButtonDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeLeftShoulderDeadZone() { return !isDefault(LeftShoulderDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeRightShoulderDeadZone() { return !isDefault(RightShoulderDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeDPadDownDeadZone() { return !isDefault(DPadDownDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeDPadLeftDeadZone() { return !isDefault(DPadLeftDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeDPadRightDeadZone() { return !isDefault(DPadRightDeadZone, SettingName.DefaultButtonDeadZone); }
		public bool ShouldSerializeDPadUpDeadZone() { return !isDefault(DPadUpDeadZone, SettingName.DefaultButtonDeadZone); }

		#endregion

	}
}
