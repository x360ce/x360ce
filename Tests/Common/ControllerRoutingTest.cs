// @under-test: App.v4/Common/DInput/DInputHelper.Step4.CombineXiStates.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System.Linq;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// Which controller a mapped device actually drives.
	/// </summary>
	/// <remarks>
	/// Everything else about mapping was tested and this was not, although it is the part a person
	/// notices first. A device is mapped to controller one, two, three or four, and what a game reads
	/// is whatever this puts in that place. Send it to the wrong place, or to every place, and nothing
	/// inside the program looks wrong: the game simply answers a control nobody touched, or ignores
	/// the one being held.
	/// </remarks>
	[TestClass]
	public class ControllerRoutingTest
	{

		/// <summary>The real list, emptied and put back, so this is the same list the program reads.</summary>
		static UserSetting[] Replace(params UserSetting[] settings)
		{
			var existing = SettingsManager.UserSettings.ItemsToArraySyncronized();
			SettingsManager.UserSettings.Items.Clear();
			foreach (var setting in settings)
				SettingsManager.UserSettings.Items.Add(setting);
			return existing;
		}

		static void Restore(UserSetting[] existing)
		{
			SettingsManager.UserSettings.Items.Clear();
			foreach (var setting in existing)
				SettingsManager.UserSettings.Items.Add(setting);
		}

		static UserSetting Mapped(MapTo controller, Gamepad state)
		{
			var setting = new UserSetting { MapTo = (int)controller };
			setting.XiState = state;
			return setting;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A device drives the controller it is mapped to and no other")]
		public void A_device_drives_the_controller_it_is_mapped_to_and_no_other()
		{
			var helper = new DInputHelper();
			var existing = Replace(Mapped(MapTo.Controller3, new Gamepad
			{
				Buttons = GamepadButtonFlags.A,
				LeftTrigger = 200,
				LeftThumbX = 12345,
			}));
			try
			{
				helper.CombineXiStates();

				var third = helper.CombinedXiStates[2].Gamepad;
				Assert.AreEqual(GamepadButtonFlags.A, third.Buttons & GamepadButtonFlags.A,
					"A device mapped to controller three did not reach controller three, so the game " +
					"sees nothing while the person is pressing a button.");
				Assert.AreEqual(200, third.LeftTrigger, "The trigger did not arrive with it.");
				Assert.AreEqual(12345, third.LeftThumbX, "The stick did not arrive with it.");
				Assert.IsTrue(helper.CombinedXiConencted[2],
					"Controller three has a device mapped to it and has to read as connected.");

				foreach (var other in new[] { 0, 1, 3 })
				{
					Assert.AreEqual((GamepadButtonFlags)0, helper.CombinedXiStates[other].Gamepad.Buttons,
						"Controller " + (other + 1) + " answered a device mapped to controller three. " +
						"A game reading it acts on a control nobody touched.");
					Assert.IsFalse(helper.CombinedXiConencted[other],
						"Controller " + (other + 1) + " reads as connected with nothing mapped to it.");
				}
			}
			finally
			{
				Restore(existing);
			}
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Four devices each drive their own controller")]
		public void Four_devices_each_drive_their_own_controller()
		{
			// One device per controller, each holding a different button, so a swap between any two is
			// visible rather than hidden by them all looking alike.
			var buttons = new[]
			{
				GamepadButtonFlags.A, GamepadButtonFlags.B,
				GamepadButtonFlags.X, GamepadButtonFlags.Y,
			};
			var helper = new DInputHelper();
			var existing = Replace(Enumerable.Range(0, 4)
				.Select(i => Mapped((MapTo)(i + 1), new Gamepad { Buttons = buttons[i] }))
				.ToArray());
			try
			{
				helper.CombineXiStates();
				for (var i = 0; i < 4; i++)
					Assert.AreEqual(buttons[i], helper.CombinedXiStates[i].Gamepad.Buttons,
						"Controller " + (i + 1) + " is carrying the wrong device's controls. Two " +
						"players in the same game would be driving each other.");
			}
			finally
			{
				Restore(existing);
			}
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Two devices on one controller are combined, not one ignored")]
		public void Two_devices_on_one_controller_are_combined()
		{
			// Sharing one controller between two devices is a supported arrangement, so the second must
			// add to the first rather than replace it.
			var helper = new DInputHelper();
			var existing = Replace(
				Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.A, LeftTrigger = 10 }),
				Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.B, LeftTrigger = 90 }));
			try
			{
				helper.CombineXiStates();
				var first = helper.CombinedXiStates[0].Gamepad;
				Assert.AreEqual(GamepadButtonFlags.A | GamepadButtonFlags.B, first.Buttons,
					"One of the two devices sharing controller one was dropped.");
				Assert.AreEqual(90, first.LeftTrigger,
					"The trigger pressed hardest is the one that counts.");
			}
			finally
			{
				Restore(existing);
			}
		}

	}
}
