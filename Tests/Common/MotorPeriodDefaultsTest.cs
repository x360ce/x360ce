// @under-test: Engine/Maps/SettingName.cs, App.v4/Common/SettingsManager.cs
// @area: force-feedback   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The left motor pulses slower than the right, as the motors it stands for do.
	/// </summary>
	/// <remarks>
	/// XInput's left motor is the low-frequency one and the right the high-frequency one. The
	/// default periods said the opposite for years, and the form wrote them into every mapping it
	/// saved, so the defaults change and the mappings that carry the old pair get the new one.
	/// </remarks>
	[TestClass]
	public class MotorPeriodDefaultsTest
	{
		static string Default(string setting)
		{
			var property = typeof(SettingName).GetProperty(setting);
			return (string)((DefaultValueAttribute)property.GetCustomAttributes(typeof(DefaultValueAttribute), false).Single()).Value;
		}

		[TestMethod, TestCategory("force-feedback")]
		public void The_left_motor_default_period_is_the_longer()
		{
			var left = int.Parse(Default("LeftMotorPeriod"));
			var right = int.Parse(Default("RightMotorPeriod"));
			Assert.IsTrue(left > right, string.Format("Left {0} ms, right {1} ms: the low-frequency motor pulses faster than the high-frequency one.", left, right));
		}

		static PadSetting Pad(string left, string right)
		{
			var ps = new PadSetting { LeftMotorPeriod = left, RightMotorPeriod = right, ForceEnable = "1" };
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		[TestMethod, TestCategory("force-feedback")]
		public void A_mapping_carrying_the_old_default_pair_gets_the_new_one_and_stays_linked()
		{
			var ps = Pad("60", "120");
			var was = ps.PadSettingChecksum;
			var mapping = new UserSetting { PadSettingChecksum = was, MapTo = 1 };
			var updated = SettingsManager.UpdateOldDefaultMotorPeriods(new[] { ps }, new[] { mapping });
			Assert.AreEqual(1, updated);
			Assert.AreEqual("120", ps.LeftMotorPeriod);
			Assert.AreEqual("60", ps.RightMotorPeriod);
			Assert.AreNotEqual(was, ps.PadSettingChecksum, "The checksum did not follow the values.");
			Assert.AreEqual(ps.PadSettingChecksum, mapping.PadSettingChecksum, "The mapping lost its settings.");
		}

		[TestMethod, TestCategory("force-feedback")]
		public void Custom_periods_and_server_presets_are_left_as_they_are()
		{
			var custom = Pad("80", "120");
			var customWas = custom.PadSettingChecksum;
			var preset = Pad("60", "120");
			var presetWas = preset.PadSettingChecksum;
			var mapping = new UserSetting { PadSettingChecksum = customWas, MapTo = 1 };
			var updated = SettingsManager.UpdateOldDefaultMotorPeriods(new[] { custom, preset }, new[] { mapping });
			Assert.AreEqual(0, updated);
			Assert.AreEqual("80", custom.LeftMotorPeriod);
			Assert.AreEqual(customWas, custom.PadSettingChecksum, "A chosen value was changed.");
			Assert.AreEqual("60", preset.LeftMotorPeriod);
			Assert.AreEqual(presetWas, preset.PadSettingChecksum, "A preset no mapping uses was changed, and the server would no longer know it.");
		}
	}
}
