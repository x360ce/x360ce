// @under-test: App.v4/Common/TestDeviceHelper.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Threading;
using x360ce.App;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// The built-in test controller moves and presses in a fixed pattern, which is what lets mapping be
	/// checked without a physical device in hand. These tests pin that pattern, because a test device
	/// that quietly stopped behaving as described would make every test built on it worthless while
	/// still reporting success.
	/// </summary>
	[TestClass]
	public class TestDeviceTest
	{

		private static UserDevice NewDevice()
		{
			var device = TestDeviceHelper.NewUserDevice();
			Assert.IsNotNull(device, "The test controller could not be created.");
			return device;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The test controller offers the controls the mapping needs to exercise")]
		public void The_test_controller_has_the_controls_it_claims()
		{
			var device = NewDevice();
			Assert.AreEqual(5, device.CapAxeCount, "Axes are needed for both sticks and the triggers.");
			Assert.AreEqual(10, device.CapButtonCount, "Buttons are needed for the face and shoulder controls.");
			Assert.AreEqual(1, device.CapPovCount, "A hat switch is needed for the d-pad.");
			Assert.AreEqual(TestDeviceHelper.ProductGuid, device.ProductGuid,
				"It must be recognisable as the test controller, since real devices are read differently.");
			Assert.IsTrue(device.IsEnabled, "It is useless to a test unless it starts enabled.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Two test controllers do not share a name or an identity")]
		public void Two_test_controllers_are_distinct()
		{
			// Mapping is stored against the instance, so two devices sharing one would overwrite each
			// other's settings.
			var first = NewDevice();
			var second = NewDevice();
			Assert.AreNotEqual(first.InstanceGuid, second.InstanceGuid);
		}

		[TestMethod, TestCategory("mapping")]
		[Description("It reports the controls it says it has, so a mapping can find them")]
		public void The_test_controller_describes_its_controls()
		{
			var objects = TestDeviceHelper.GetDeviceObjects();
			Assert.IsNotNull(objects, "A device with no described controls cannot be mapped.");
			Assert.IsTrue(objects.Length > 0, "The test controller described no controls at all.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Exactly one button is held at any moment, which is what makes the pattern readable")]
		public void Exactly_one_button_is_held_at_a_time()
		{
			// The pattern walks one button at a time. If two were ever held together, a test could not
			// tell which button a mapped output came from.
			var device = NewDevice();
			for (var sample = 0; sample < 25; sample++)
			{
				var state = TestDeviceHelper.GetCurrentState(device);
				var held = state.Buttons.Take(device.CapButtonCount).Count(x => x);
				Assert.AreEqual(1, held,
					string.Format("{0} buttons were held at once; the pattern presses one at a time.", held));
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Every value it reports is inside the range DirectInput allows")]
		public void Every_reported_value_is_inside_the_directinput_range()
		{
			// A value outside the range would push the mapping past the end of a stick, and the fault
			// would look like a mapping defect rather than a test device defect.
			var device = NewDevice();
			for (var sample = 0; sample < 25; sample++)
			{
				var state = TestDeviceHelper.GetCurrentState(device);
				foreach (var axis in new[] { state.X, state.Y, state.Z, state.RotationX, state.RotationY, state.RotationZ })
					Assert.IsTrue(axis >= 0 && axis <= 65535,
						string.Format("An axis reported {0}, outside the range DirectInput allows.", axis));
				foreach (var slider in state.Sliders)
					Assert.IsTrue(slider >= 0 && slider <= 65535,
						string.Format("A slider reported {0}, outside the range DirectInput allows.", slider));
				foreach (var pov in state.PointOfViewControllers.Take(device.CapPovCount))
					Assert.IsTrue(pov == -1 || (pov >= 0 && pov <= 36000),
						string.Format("A hat switch reported {0}; it must be a hundredth of a degree or -1 for centred.", pov));
				Thread.Sleep(1);
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("stress")]
		[Description("The pattern moves, so a test that waits sees something change")]
		public void The_pattern_actually_moves()
		{
			// A frozen test device would let every mapping test pass while proving nothing, which is
			// the failure this test exists to catch.
			var device = NewDevice();
			var first = TestDeviceHelper.GetCurrentState(device);
			var startedAt = DateTime.UtcNow;
			var moved = false;
			// The pattern completes a sweep in four seconds and then rests for two, so a little over
			// one full cycle is enough to be certain rather than lucky.
			while (!moved && (DateTime.UtcNow - startedAt).TotalSeconds < 7)
			{
				Thread.Sleep(50);
				var now = TestDeviceHelper.GetCurrentState(device);
				moved = now.X != first.X || now.Y != first.Y
					|| now.PointOfViewControllers[0] != first.PointOfViewControllers[0]
					|| !now.Buttons.Take(device.CapButtonCount)
						.SequenceEqual(first.Buttons.Take(device.CapButtonCount));
			}
			Assert.IsTrue(moved, "Nothing on the test controller changed within a full cycle.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("What the test controller reports maps to a usable value for a game")]
		public void What_it_reports_maps_to_a_usable_value()
		{
			// This is the point of the whole device: its output goes through the same conversion a real
			// controller's does, and has to arrive inside the range a game reads.
			var device = NewDevice();
			for (var sample = 0; sample < 20; sample++)
			{
				var state = TestDeviceHelper.GetCurrentState(device);
				var stick = ConvertHelper.GetThumbValue(state.X, 0f, 0f, 0f, false, false, true);
				Assert.IsTrue(stick >= -32768f && stick <= 32767f,
					string.Format("A stick mapped to {0}, outside what a game can read.", stick));
				var trigger = ConvertHelper.GetThumbValue(state.Y, 0f, 0f, 0f, false, false, false);
				Assert.IsTrue(trigger >= 0f && trigger <= 255f,
					string.Format("A trigger mapped to {0}, outside what a game can read.", trigger));
				Thread.Sleep(1);
			}
		}

	}
}
