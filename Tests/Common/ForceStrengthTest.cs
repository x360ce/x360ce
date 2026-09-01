// @under-test: Engine/Data/PadSetting.cs, App.v4/Common/DInput/DInputHelper.Step5.VirtualDevices.cs
// @area: settings   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Xml.Serialization;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// What a force feedback strength of nothing does, which is meant to be nothing.
	/// </summary>
	/// <remarks>
	/// A strength is a percentage of the force a game asks for, so zero means the motors stay still.
	/// It did the opposite: turning strength down to nothing gave full force, the one setting whose
	/// whole purpose is to be turned down.
	///
	/// The cause is that settings are written as text and an empty one means "not set", which is then
	/// read back as the default. Zero was being counted as empty. That is right for the settings which
	/// say which button does what, where nought is no button, and wrong for a percentage, where nought
	/// is a real answer and the default is a hundred. So a strength of zero was erased on its way past
	/// and read back as full.
	/// </remarks>
	[TestClass]
	public class ForceStrengthTest
	{
		/// <summary>The three strengths, and the dead zone which is the same shape of setting.</summary>
		static readonly string[] PercentagesWithANonZeroDefault =
		{
			"ForceOverall", "LeftMotorStrength", "RightMotorStrength", "AxisToDPadDeadZone",
		};

		static string Get(PadSetting ps, string name)
		{
			return (string)typeof(PadSetting).GetProperty(name).GetValue(ps, null);
		}

		static void Set(PadSetting ps, string name, string value)
		{
			typeof(PadSetting).GetProperty(name).SetValue(ps, value, null);
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A strength of zero is still zero after the settings are tidied")]
		public void A_strength_of_zero_survives_being_tidied()
		{
			// Every set of settings taken off the screen is tidied and named in one step, so this runs
			// the moment the slider is moved rather than only when the program is next started.
			foreach (var name in PercentagesWithANonZeroDefault)
			{
				var ps = new PadSetting();
				Set(ps, name, "0");
				ps.CleanAndGetCheckSum();
				Assert.AreEqual("0", Get(ps, name),
					name + " set to nothing is erased while the settings are tidied, so it reads back " +
					"as its default and the motors run at full force.");
			}
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A strength of zero is read back as zero, not as the default")]
		public void A_strength_of_zero_is_read_back_as_zero()
		{
			var ps = new PadSetting();
			ps.ForceOverall = "0";
			ps.LeftMotorStrength = "0";
			ps.RightMotorStrength = "0";
			ps.CleanAndGetCheckSum();
			Assert.AreEqual(0, ps.GetForceOverall(), "Overall strength of nothing reads back as force.");
			Assert.AreEqual(0, ps.GetLeftMotorStrength(), "Left strength of nothing reads back as force.");
			Assert.AreEqual(0, ps.GetRightMotorStrength(), "Right strength of nothing reads back as force.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A strength of zero is written to the settings file")]
		public void A_strength_of_zero_is_written_out()
		{
			// Left out of the file it comes back as the default the next time the program starts, which
			// is a hundred - so the setting would not survive being closed even once.
			var ps = new PadSetting();
			ps.ForceOverall = "0";
			var writer = new StringWriter();
			new XmlSerializer(typeof(PadSetting)).Serialize(writer, ps);
			StringAssert.Contains(writer.ToString(), "<ForceOverall>0</ForceOverall>",
				"An overall strength of nothing is left out of the saved settings, so it is read back " +
				"as full force the next time the program is started.");
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("A strength of nothing leaves the motors still")]
		public void A_strength_of_nothing_leaves_the_motors_still()
		{
			// The force passed on to a controller's own motors is scaled here and nowhere else. The
			// driver on the other side of it knows nothing of this program's settings.
			var ps = new PadSetting();
			ps.ForceOverall = "0";
			Assert.AreEqual(0, ps.ApplyForceStrength(255, true), "Overall strength of nothing still rumbles.");
			Assert.AreEqual(0, ps.ApplyForceStrength(255, false), "Overall strength of nothing still rumbles.");
			var one = new PadSetting();
			one.LeftMotorStrength = "0";
			Assert.AreEqual(0, one.ApplyForceStrength(255, true), "Left strength of nothing still rumbles.");
			Assert.AreEqual(255, one.ApplyForceStrength(255, false),
				"Turning the left motor down took the right one with it.");
		}

		[TestMethod, TestCategory("settings")]
		[Description("A strength between the ends scales the force, and full strength changes nothing")]
		public void A_strength_between_the_ends_scales_the_force()
		{
			// Untouched settings must pass the force through exactly as it arrived, or every person who
			// never opened the tab would find their rumble changed by an upgrade.
			var untouched = new PadSetting();
			Assert.AreEqual(255, untouched.ApplyForceStrength(255, true));
			Assert.AreEqual(200, untouched.ApplyForceStrength(200, false));
			var half = new PadSetting();
			half.ForceOverall = "50";
			Assert.AreEqual(128, half.ApplyForceStrength(255, true));
			// A percentage of a percentage: half the pad, half that motor, a quarter of the force.
			var quarter = new PadSetting();
			quarter.ForceOverall = "50";
			quarter.LeftMotorStrength = "50";
			Assert.AreEqual(64, quarter.ApplyForceStrength(255, true));
			Assert.AreEqual(128, quarter.ApplyForceStrength(255, false));
		}

		[TestMethod, TestCategory("settings"), TestCategory("critical")]
		[Description("Nought still means no button for the settings that name a button")]
		public void Nought_still_means_unset_where_it_named_no_button()
		{
			// The other half. Those settings have no default of their own, and nought there means the
			// button is not mapped. Storing it would give an untouched set of settings a name, and every
			// preset already saved answers to the name it had without it.
			foreach (var name in new[] { "ButtonA", "ButtonB", "DPadUp", "LeftShoulder" })
			{
				var ps = new PadSetting();
				Set(ps, name, "0");
				ps.CleanAndGetCheckSum();
				Assert.AreEqual("", Get(ps, name),
					name + " set to nought is now kept rather than treated as unmapped, which renames " +
					"every set of settings that has it.");
			}
		}
	}
}
