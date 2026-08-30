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
		[Description("A place is claimed from having watched it filled, not from having made the controller")]
		public void A_place_is_claimed_from_watching_not_from_ownership()
		{
			// Being virtual says nothing about where a controller is. Only having watched it arrive
			// does, and that is what must be asked. Asking whether it is virtual instead hands a place
			// we never observed to a controller we never made.
			var source = Read(Path.Combine("App.v4", "Common", "DInput", "XInputPlaces.cs"));
			// The one method, not everything after it. Reading what is a place from what made a device is
			// wrong here and right in the reading that gathers the kinds, so the two must not be confused.
			var resolve = source.Substring(source.IndexOf("public static Dictionary<string, int> Resolve(DeviceInfo[] all"));
			resolve = resolve.Substring(0, resolve.IndexOf("static Dictionary<string, int> _cache"));
			Assert.IsFalse(resolve.Contains("IsVirtualPad"),
				"Where a controller is, is being decided by who made it. A virtual controller this " +
				"program did not make then claims a place nobody watched it take, and the real " +
				"controllers can no longer be named either.");
			StringAssert.Contains(resolve, "RecordedPlace",
				"Nothing asks what was actually watched, which is the only thing that yields a place.");
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
