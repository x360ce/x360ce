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

		[TestMethod, TestCategory("force-feedback")]
		public void The_defaults_are_the_default_multiplier_of_the_measured_motors()
		{
			// The attributes need constants, so the numbers are spelt out twice; this keeps them one.
			Assert.AreEqual(MotorModel.PeriodAtFullMs(MotorModel.DefaultMultiplier, true).ToString(), Default("LeftMotorPeriod"));
			Assert.AreEqual(MotorModel.PeriodAtFullMs(MotorModel.DefaultMultiplier, false).ToString(), Default("RightMotorPeriod"));
			Assert.AreEqual(MotorModel.DefaultMultiplier, MotorModel.MultiplierOf(int.Parse(Default("LeftMotorPeriod")), int.Parse(Default("RightMotorPeriod"))));
		}

		[TestMethod, TestCategory("force-feedback")]
		public void Every_preset_lands_on_a_slider_step_within_range()
		{
			// The sliders step by 4 ms from 0 to 400.
			foreach (var k in MotorModel.Multipliers)
				foreach (var left in new[] { true, false })
				{
					var ms = MotorModel.PeriodAtFullMs(k, left);
					Assert.AreEqual(0, ms % 4, string.Format("{0}x {1}: {2} ms is not a slider step.", k, left ? "left" : "right", ms));
					Assert.IsTrue(ms <= 400, string.Format("{0}x {1}: {2} ms is past the slider.", k, left ? "left" : "right", ms));
				}
			Assert.IsNull(MotorModel.MultiplierOf(100, 100), "A pair no preset makes was taken for one.");
		}

		[TestMethod, TestCategory("force-feedback")]
		public void The_help_document_states_the_model_numbers()
		{
			// docs/Help.ForceFeedback.md is what the page's info button shows. Its tables are written
			// by hand, so this reads them back against the numbers the program runs on.
			var path = System.IO.Path.Combine(Ui.RepoRoot.FullName, "docs", "Help.ForceFeedback.md");
			// Table rows, with the pipes taken off so a row reads as its first cell onwards.
			var lines = System.IO.File.ReadAllLines(path).Select(l => l.Trim().Trim('|').Trim()).ToArray();
			foreach (var k in MotorModel.Multipliers)
			{
				var row = lines.FirstOrDefault(l => l.StartsWith(k + "x "));
				Assert.IsNotNull(row, "The presets table has no row for " + k + "x.");
				var numbers = System.Text.RegularExpressions.Regex.Matches(row, @"(\d+) ms").Cast<System.Text.RegularExpressions.Match>()
					.Select(m => int.Parse(m.Groups[1].Value)).ToArray();
				CollectionAssert.AreEqual(new[] { MotorModel.PeriodAtFullMs(k, true), MotorModel.PeriodAtFullMs(k, false) }, numbers,
					k + "x row says " + row.Trim());
			}
			var left = lines.First(l => l.StartsWith("Left ") && l.Contains("%"));
			var right = lines.First(l => l.StartsWith("Right ") && l.Contains("%"));
			StringAssert.Contains(left, MotorModel.LeftPeriodAtFullMs + " ms");
			StringAssert.Contains(right, MotorModel.RightPeriodAtFullMs + " ms");
			StringAssert.Contains(left, (int)(MotorModel.LeftStartsAt * 100) + " %");
			StringAssert.Contains(right, (int)(MotorModel.RightStartsAt * 100) + " %");
		}

		[TestMethod, TestCategory("force-feedback")]
		public void The_period_stretches_as_the_drive_falls_and_is_the_setting_at_full_drive()
		{
			// Full drive plays the setting as it is.
			Assert.AreEqual(160, MotorModel.PeriodMs(160, true, 1.0));
			Assert.AreEqual(64, MotorModel.PeriodMs(64, false, 1.0));
			// Half drive: the left motor measured 18.7 of 25.2 Hz, the right 54 of 62 Hz.
			Assert.AreEqual((int)Math.Round(160 * 25.2 / 18.7), MotorModel.PeriodMs(160, true, 0.5));
			Assert.AreEqual((int)Math.Round(64 * 62.1 / 50.1), MotorModel.PeriodMs(64, false, 0.5));
			// The bottom of the line is the spin-start speed, never slower; the top is clamped.
			Assert.AreEqual((int)Math.Round(160 * 25.2 / 12.2), MotorModel.PeriodMs(160, true, 0.0));
			Assert.AreEqual(MotorModel.PeriodMs(160, true, 0.0), MotorModel.PeriodMs(160, true, -1.0));
			Assert.AreEqual(160, MotorModel.PeriodMs(160, true, 2.0));
			// No period stays no period (a constant effect).
			Assert.AreEqual(0, MotorModel.PeriodMs(0, true, 0.5));
		}

		[TestMethod, TestCategory("force-feedback")]
		public void The_period_slider_says_what_100_is_and_keeps_milliseconds()
		{
			// The slider runs 0 to 100 and the setting 0 to 400 ms. Described only by the setting, the
			// slider offered 160 as a value it would refuse.
			using (var slider = new System.Windows.Forms.TrackBar { Maximum = 100 })
			{
				var item = SettingsManager.AddMap("PAD1", () => SettingName.LeftMotorPeriod, slider);
				try
				{
					StringAssert.Contains(slider.AccessibleDescription, "100 is 400");
					SettingsManager.Current.LoadSetting(slider, SettingName.LeftMotorPeriod, "160");
					Assert.AreEqual(40, slider.Value);
					Assert.AreEqual("160", SettingsManager.Current.GetSettingValue(slider));
				}
				finally
				{
					SettingsManager.Current.SettingsMap.Remove(item);
				}
			}
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
			Assert.AreEqual(SettingName.DefaultLeftMotorPeriod, ps.LeftMotorPeriod);
			Assert.AreEqual(SettingName.DefaultRightMotorPeriod, ps.RightMotorPeriod);
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
