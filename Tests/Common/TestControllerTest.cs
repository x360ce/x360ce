// @under-test: Plugins/TestController/TestController.cs, App.v4/Plugins/PluginDevices.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using x360ce.Engine;
using x360ce.Engine.Data;
using x360ce.Plugins;

namespace x360ce.Tests
{
	/// <summary>
	/// The Test Controller, the plugin embedded in x360ce, moves and presses in a fixed pattern, which is what lets mapping
	/// be checked without a physical device in hand. These tests pin that pattern, because a test device that quietly
	/// stopped behaving as described would make every test built on it worthless while still reporting success.
	/// </summary>
	[TestClass]
	public class TestControllerTest
	{

		private static UserDevice NewDevice()
		{
			var device = TestControllerDevice.NewUserDevice();
			Assert.IsNotNull(device, "The test controller could not be created.");
			return device;
		}

		/// <summary>A new instance of the plugin, as the engine makes one for a device, and the state it writes into.</summary>
		private static IPluginDevice NewPlugin(out PluginState state)
		{
			state = new PluginState();
			return TestControllerDevice.Type.NewInstance();
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("The test controller offers the controls the mapping needs to exercise")]
		public void The_test_controller_has_the_controls_it_claims()
		{
			var device = NewDevice();
			Assert.AreEqual(5, device.CapAxeCount, "Axes are needed for both sticks and the triggers.");
			Assert.AreEqual(10, device.CapButtonCount, "Buttons are needed for the face and shoulder controls.");
			Assert.AreEqual(1, device.CapPovCount, "A hat switch is needed for the d-pad.");
			Assert.AreEqual(TestControllerDevice.ProductGuid, device.ProductGuid,
				"It keeps the Test Device's product, so the mappings saved for that keep working.");
			Assert.AreEqual((int)InputSourceType.Plugin, device.InputSourceType, "It is read through its plugin.");
			Assert.IsTrue(device.IsEnabled, "It is useless to a test unless it starts enabled.");
			Assert.IsTrue(device.IsOnline, "It is online while its plugin is loaded.");
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
			var objects = TestControllerDevice.GetDeviceObjects();
			Assert.IsNotNull(objects, "A device with no described controls cannot be mapped.");
			Assert.AreEqual(5 + 1 + 10, objects.Length, "Five axes, a hat switch and ten buttons.");
			Assert.IsNotNull(TestControllerDevice.Type.Description, "It explains itself to a person and to an AI.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Exactly one button is held at any moment, which is what makes the pattern readable")]
		public void Exactly_one_button_is_held_at_a_time()
		{
			// The pattern walks one button at a time. If two were ever held together, a test could not
			// tell which button a mapped output came from.
			PluginState state;
			var plugin = NewPlugin(out state);
			for (var sample = 0; sample < 25; sample++)
			{
				plugin.Read(state);
				var held = state.Buttons.Take(plugin.ButtonCount).Count(x => x);
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
			PluginState state;
			var plugin = NewPlugin(out state);
			for (var sample = 0; sample < 25; sample++)
			{
				plugin.Read(state);
				foreach (var axis in state.Axes)
					Assert.IsTrue(axis >= 0 && axis <= 65535,
						string.Format("An axis reported {0}, outside the range DirectInput allows.", axis));
				foreach (var slider in state.Sliders)
					Assert.IsTrue(slider >= 0 && slider <= 65535,
						string.Format("A slider reported {0}, outside the range DirectInput allows.", slider));
				foreach (var pov in state.Povs.Take(plugin.PovCount))
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
			PluginState first;
			var plugin = NewPlugin(out first);
			plugin.Read(first);
			var now = new PluginState();
			var startedAt = DateTime.UtcNow;
			var moved = false;
			// The pattern completes a sweep in four seconds and then rests for two, so a little over
			// one full cycle is enough to be certain rather than lucky.
			while (!moved && (DateTime.UtcNow - startedAt).TotalSeconds < 7)
			{
				Thread.Sleep(50);
				plugin.Read(now);
				moved = now.Axes[0] != first.Axes[0] || now.Axes[1] != first.Axes[1]
					|| now.Povs[0] != first.Povs[0]
					|| !now.Buttons.Take(plugin.ButtonCount).SequenceEqual(first.Buttons.Take(plugin.ButtonCount));
			}
			Assert.IsTrue(moved, "Nothing on the test controller changed within a full cycle.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("What the test controller reports maps to a usable value for a game")]
		public void What_it_reports_maps_to_a_usable_value()
		{
			// This is the point of the whole device: its output goes through the same conversion a real
			// controller's does, and has to arrive inside the range a game reads.
			PluginState state;
			var plugin = NewPlugin(out state);
			for (var sample = 0; sample < 20; sample++)
			{
				plugin.Read(state);
				var stick = ConvertHelper.GetThumbValue(state.Axes[0], 0f, 0f, 0f, false, false, true);
				Assert.IsTrue(stick >= -32768f && stick <= 32767f,
					string.Format("A stick mapped to {0}, outside what a game can read.", stick));
				var trigger = ConvertHelper.GetThumbValue(state.Axes[1], 0f, 0f, 0f, false, false, false);
				Assert.IsTrue(trigger >= 0f && trigger <= 255f,
					string.Format("A trigger mapped to {0}, outside what a game can read.", trigger));
				Thread.Sleep(1);
			}
		}

	}
}
