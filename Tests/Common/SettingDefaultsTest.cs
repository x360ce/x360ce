// @under-test: Engine/Data/PadSetting.cs
// @area: presets   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A setting a preset leaves out is at its default, and the engine runs it at that default.
	/// </summary>
	/// <remarks>
	/// A preset leaves a setting out when it is at its default, so a reader of the preset puts the
	/// default back. The defaults are the ones on <see cref="SettingName"/>, which the controller page
	/// shows. The engine read a missing number as nought instead: a dead zone of 0 while the page showed
	/// 8192, no centering spring while it showed 30, no motor period while it showed 160 and 64.
	/// </remarks>
	[TestClass]
	public class SettingDefaultsTest
	{
		static int Default(string setting)
		{
			var property = typeof(SettingName).GetProperty(setting);
			var attribute = (System.ComponentModel.DefaultValueAttribute)property
				.GetCustomAttributes(typeof(System.ComponentModel.DefaultValueAttribute), false).Single();
			return int.Parse((string)attribute.Value);
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("A button dead zone a preset leaves out is the default the page shows")]
		public void A_button_dead_zone_left_out_is_the_default()
		{
			var maps = new PadSetting { ButtonA = "a3", ButtonB = "3" }.Maps;
			Assert.AreEqual(Default("ButtonADeadZone"), maps.Single(x => x.ButtonFlag == SharpDX.XInput.GamepadButtonFlags.A).DeadZone);
			Assert.AreEqual(Default("ButtonBDeadZone"), maps.Single(x => x.ButtonFlag == SharpDX.XInput.GamepadButtonFlags.B).DeadZone);
		}

		[TestMethod, TestCategory("presets")]
		[Description("A button dead zone a preset gives is the one used, nought included")]
		public void A_button_dead_zone_given_is_the_one_used()
		{
			var maps = new PadSetting { ButtonA = "a3", ButtonADeadZone = "0", ButtonB = "a4", ButtonBDeadZone = "100" }.Maps;
			Assert.AreEqual(0, maps.Single(x => x.ButtonFlag == SharpDX.XInput.GamepadButtonFlags.A).DeadZone);
			Assert.AreEqual(100, maps.Single(x => x.ButtonFlag == SharpDX.XInput.GamepadButtonFlags.B).DeadZone);
		}

		/// <summary>What a preset keeps of a button's dead zone once cleaned for storing, and its checksum.</summary>
		static PadSetting Cleaned(string button, string deadZone)
		{
			var ps = new PadSetting { ButtonA = button, ButtonADeadZone = deadZone };
			ps.PadSettingChecksum = ps.CleanAndGetCheckSum();
			return ps;
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("A dead zone at its default is stored as nothing, and names the same preset as none")]
		public void A_dead_zone_at_its_default_is_stored_as_nothing()
		{
			var atDefault = Cleaned("a3", Default("ButtonADeadZone").ToString());
			Assert.AreEqual("", atDefault.ButtonADeadZone, "The default was stored.");
			Assert.AreEqual(Cleaned("a3", "").PadSettingChecksum, atDefault.PadSettingChecksum, "The default and nothing are two presets.");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("A dead zone of nought on a button an axis drives is kept: it presses the button at once")]
		public void A_dead_zone_of_nought_on_an_axis_is_kept()
		{
			var zero = Cleaned("a3", "0");
			Assert.AreEqual("0", zero.ButtonADeadZone, "A dead zone set to nought was wiped, and comes back as the default.");
			Assert.AreNotEqual(Cleaned("a3", "").PadSettingChecksum, zero.PadSettingChecksum, "Nought and the default are one preset.");
			Assert.AreEqual("0", Cleaned("s2", "0").ButtonADeadZone, "Slider");
			Assert.AreEqual("0", Cleaned("x-1", "0").ButtonADeadZone, "Half axis");
			Assert.AreEqual("0", Cleaned("=a1*2", "0").ButtonADeadZone, "Formula");
		}

		[TestMethod, TestCategory("presets"), TestCategory("critical")]
		[Description("A dead zone on a button nothing but a button drives is stored as nothing, whatever it holds")]
		public void A_dead_zone_no_axis_reads_is_stored_as_nothing()
		{
			foreach (var button in new[] { "3", "-3", "b3", "p1", "d2", "" })
				foreach (var deadZone in new[] { "0", "5000" })
				{
					var ps = Cleaned(button, deadZone);
					Assert.AreEqual("", ps.ButtonADeadZone, "Button '" + button + "' kept dead zone " + deadZone + ", which nothing reads.");
					Assert.AreEqual(Cleaned(button, "").PadSettingChecksum, ps.PadSettingChecksum, "Button '" + button + "': an unread dead zone made another preset.");
				}
		}

		[TestMethod, TestCategory("presets"), TestCategory("force-feedback")]
		[Description("A spring strength and motor periods a preset leaves out are the defaults the page shows")]
		public void Force_settings_left_out_are_the_defaults()
		{
			var ps = new PadSetting();
			Assert.AreEqual(Default("ForceSpringStrength"), ps.GetForceSpringStrength(), "Spring strength");
			Assert.AreEqual(Default("LeftMotorPeriod"), ps.GetLeftMotorPeriod(), "Left motor period");
			Assert.AreEqual(Default("RightMotorPeriod"), ps.GetRightMotorPeriod(), "Right motor period");
			Assert.AreEqual(Default("ForceOverall"), ps.GetForceOverall(), "Overall strength");
			Assert.AreEqual(Default("LeftMotorStrength"), ps.GetLeftMotorStrength(), "Left motor strength");
			Assert.AreEqual(Default("RightMotorStrength"), ps.GetRightMotorStrength(), "Right motor strength");
		}

		[TestMethod, TestCategory("presets"), TestCategory("force-feedback")]
		[Description("Force settings a preset gives are the ones used, nought included")]
		public void Force_settings_given_are_the_ones_used()
		{
			var ps = new PadSetting { ForceSpringStrength = "0", LeftMotorPeriod = "0", RightMotorPeriod = "120" };
			Assert.AreEqual(0, ps.GetForceSpringStrength());
			Assert.AreEqual(0, ps.GetLeftMotorPeriod());
			Assert.AreEqual(120, ps.GetRightMotorPeriod());
		}
	}
}
