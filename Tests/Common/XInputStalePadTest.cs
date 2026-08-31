// @under-test: App.v4/Common/DInput/XInputPlaces.cs, App.v4/Common/DInput/XInputReorderPlan.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// A virtual controller this program did not make, and whether anything mistakes it for one it did.
	/// </summary>
	/// <remarks>
	/// There are three kinds of controller in an XInput place, not two: one this program made and
	/// watched arrive, one somebody plugged in, and one that is virtual and not ours - left behind by
	/// a run that did not shut down cleanly, or made by another program on the same bus. The program
	/// already knows the third kind exists; there is a button on the Devices page for removing them.
	///
	/// Everything that only had two kinds put the third in the wrong one. It was named as ours on
	/// screen, which invites the wrong repair. Its place was entered as unknown while the place it
	/// holds was counted as taken, which left one more taken place than could be accounted for, so
	/// real controllers stopped being nameable too - the same failure that made a real controller's
	/// place read blank. And a reorder would have tried to let go of it, which does nothing, because
	/// this program is not holding it.
	/// </remarks>
	[TestClass]
	public class XInputStalePadTest
	{

		static string Read(string relative)
		{
			return File.ReadAllText(Path.Combine(Ui.RepoRoot.FullName, relative));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Where a controller went is remembered against the controller, not a name")]
		public void Where_a_controller_went_is_remembered_against_the_controller()
		{
			// A number at the end of a device name is not a serial number - it is whatever follows the
			// last ampersand, and it belongs to no particular kind of thing. Matching a record by it was
			// wrong twice over: a USB hub above a real controller ends in "&2", so that controller was
			// handed the place of controller two; and the bus numbers its controllers across every
			// program using it while each program numbers its own from one, so with another program
			// holding one, ours were looked up under a name belonging to somebody else.
			//
			// So nothing is read off a name. The controller that appeared between asking for one and it
			// arriving is the one that was made, and it is remembered by itself.
			var source = Read(Path.Combine("App.v4", "Common", "DInput", "XInputPlaces.cs"));
			var lookup = source.Substring(source.IndexOf("static int RecordedPlace"));
			lookup = lookup.Substring(0, lookup.IndexOf("public static string HardwareOf"));
			Assert.IsFalse(lookup.Contains("TrailingNumber"),
				"Where a controller went is still being looked up by a number read off a device name.");
			StringAssert.Contains(lookup, "OursByHardware",
				"The record is not kept against the controller itself.");
			// And the record is only made from what was watched, never from a name.
			var step5 = Read(Path.Combine("App.v4", "Common", "DInput", "DInputHelper.Step5.VirtualDevices.cs"));
			StringAssert.Contains(step5, "VirtualHardwareNow",
				"Nothing watches which controller appeared, so the one that was made is being guessed at.");
			Assert.IsFalse(step5.Contains("target.Serial"),
				"The controller made is still identified by the number the bus gave it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A leftover controller is not described as one this program made")]
		public void A_leftover_controller_is_not_described_as_ours()
		{
			// The list already separates the two elsewhere - Remove Leftover Pads exists precisely
			// because a virtual controller can outlive the run that made it - so saying "made by this
			// program" over one of those points at the wrong repair.
			var source = Read(Path.Combine("App.v4", "Controls", "XInputDevicesUserControl.cs"));
			StringAssert.Contains(source, "IsOneOfOurs",
				"The list calls every virtual controller its own, including ones left behind by an " +
				"earlier run, which it offers no way to fix.");
			StringAssert.Contains(source, "IsOurs",
				"The list does not pass on whether the controller is one this program made.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An order needing a controller we do not hold is refused before anything is touched")]
		public void An_order_needing_a_controller_we_do_not_hold_is_refused()
		{
			// Letting go of a controller this program is not holding does nothing and reports success,
			// so the plan would switch real controllers off to reach an order it could never arrive at.
			var wanted = new[]
			{
				new XInputReorderPlan.Entry { HardwareId = "stray", Name = "Leftover Pad", IsVirtual = true, IsOurs = false, Place = 0 },
				new XInputReorderPlan.Entry { HardwareId = "real", Name = "Xbox Controller", IsVirtual = false, Place = 1 },
			};
			// Asked for in the other order, so there is something to do.
			var plan = XInputReorderPlan.For(new[] { wanted[1], wanted[0] });
			Assert.IsNotNull(plan.Refusal,
				"The plan switches real controllers off to make room for a controller it cannot move.");
			StringAssert.Contains(plan.Refusal, "Leftover Pad");
			Assert.AreEqual(0, plan.Steps.Count, "A refused plan still has steps in it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("An order of controllers this program holds is still allowed")]
		public void An_order_of_controllers_we_hold_is_still_allowed()
		{
			// The refusal above must not swallow the ordinary case, which is the whole feature.
			var plan = XInputReorderPlan.For(new[]
			{
				new XInputReorderPlan.Entry { HardwareId = "real", Name = "Xbox Controller", IsVirtual = false, Place = 1 },
				new XInputReorderPlan.Entry { HardwareId = "ours", Name = "Our Pad", IsVirtual = true, IsOurs = true, Place = 0 },
			});
			Assert.IsNull(plan.Refusal, "An order this program can carry out was refused: " + plan.Refusal);
			Assert.IsTrue(plan.Steps.Count > 0, "Nothing would be done to change the order.");
		}

	}
}
