// @under-test: App.v4/Common/DInput/DInputHelper.Step4.CombineXiStates.cs, App.v4/Common/DInput/DeviceRouting.cs
// @area: mapping   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.XInput;
using System;
using System.Linq;
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

		static readonly UserGame Game = new UserGame { FileName = "routing.exe", FileProductName = "Routing" };

		/// <summary>The rows each controller reads, as the engine is handed them for a pass.</summary>
		static DeviceRouting Routing(params UserSetting[] settings)
		{
			return DeviceRouting.Build(Game, settings, new PadSetting[0]);
		}

		/// <summary>A row of the current game, switched on, holding the state it was last converted to.</summary>
		static UserSetting Mapped(MapTo controller, Gamepad state)
		{
			var setting = new UserSetting
			{
				MapTo = (int)controller,
				FileName = Game.FileName,
				IsEnabled = true,
				InstanceGuid = Guid.NewGuid(),
			};
			setting.XiState = state;
			return setting;
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A device drives the controller it is mapped to and no other")]
		public void A_device_drives_the_controller_it_is_mapped_to_and_no_other()
		{
			var helper = new DInputHelper();
			helper.CombineXiStates(Routing(Mapped(MapTo.Controller3, new Gamepad
			{
				Buttons = GamepadButtonFlags.A,
				LeftTrigger = 200,
				LeftThumbX = 12345,
			})));

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
			helper.CombineXiStates(Routing(Enumerable.Range(0, 4)
				.Select(i => Mapped((MapTo)(i + 1), new Gamepad { Buttons = buttons[i] }))
				.ToArray()));
			for (var i = 0; i < 4; i++)
				Assert.AreEqual(buttons[i], helper.CombinedXiStates[i].Gamepad.Buttons,
					"Controller " + (i + 1) + " is carrying the wrong device's controls. Two " +
					"players in the same game would be driving each other.");
		}

		[TestMethod, TestCategory("mapping")]
		[Description("Two devices on one controller are combined, not one ignored")]
		public void Two_devices_on_one_controller_are_combined()
		{
			// Sharing one controller between two devices is a supported arrangement, so the second must
			// add to the first rather than replace it.
			var helper = new DInputHelper();
			helper.CombineXiStates(Routing(
				Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.A, LeftTrigger = 10 }),
				Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.B, LeftTrigger = 90 })));
			var first = helper.CombinedXiStates[0].Gamepad;
			Assert.AreEqual(GamepadButtonFlags.A | GamepadButtonFlags.B, first.Buttons,
				"One of the two devices sharing controller one was dropped.");
			Assert.AreEqual(90, first.LeftTrigger,
				"The trigger pressed hardest is the one that counts.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("A row switched off or of another game reaches no controller, so a button it held is let go")]
		public void A_row_the_engine_does_not_convert_reaches_no_controller()
		{
			// Both keep the state they had when they were last converted: here, button A and a full trigger.
			var held = new Gamepad { Buttons = GamepadButtonFlags.A, LeftTrigger = 255 };
			var switchedOff = Mapped(MapTo.Controller1, held);
			switchedOff.IsEnabled = false;
			var otherGame = Mapped(MapTo.Controller1, held);
			otherGame.FileName = "other.exe";
			var live = Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.B });
			var helper = new DInputHelper();

			helper.CombineXiStates(Routing(switchedOff, otherGame, live));
			var first = helper.CombinedXiStates[0].Gamepad;
			Assert.AreEqual(GamepadButtonFlags.B, first.Buttons, "A row the engine no longer converts still holds its button in the game.");
			Assert.AreEqual(0, first.LeftTrigger, "A row the engine no longer converts still holds its trigger in the game.");

			helper.CombineXiStates(Routing(switchedOff, otherGame));
			Assert.IsFalse(helper.CombinedXiConencted[0],
				"A controller whose only rows are switched off or of another game reads as connected.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("critical")]
		[Description("Two sticks on one controller pushed apart cancel, and pushed the same way the further one counts")]
		public void Two_sticks_on_one_controller_are_combined()
		{
			var helper = new DInputHelper();
			helper.CombineXiStates(Routing(
				Mapped(MapTo.Controller2, new Gamepad { LeftThumbX = 20000, LeftThumbY = -3000, RightThumbX = 500, RightThumbY = -100 }),
				Mapped(MapTo.Controller2, new Gamepad { LeftThumbX = -5000, LeftThumbY = -9000, RightThumbX = 700, RightThumbY = 0 })));
			var second = helper.CombinedXiStates[1].Gamepad;
			Assert.AreEqual(15000, second.LeftThumbX, "Two sticks pushed apart do not cancel each other.");
			Assert.AreEqual(-9000, second.LeftThumbY, "Two sticks pushed the same way do not give the further one.");
			Assert.AreEqual(700, second.RightThumbX, "Two sticks pushed the same way do not give the further one.");
			Assert.AreEqual(-100, second.RightThumbY, "A stick at rest takes the other one's push away.");
		}

		[TestMethod, TestCategory("mapping"), TestCategory("performance")]
		[Description("Combining the controllers hands nothing to the collector")]
		public void Combining_hands_nothing_to_the_collector()
		{
			var routing = Routing(
				Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.A, LeftThumbX = 100, LeftTrigger = 10 }),
				Mapped(MapTo.Controller1, new Gamepad { Buttons = GamepadButtonFlags.B, LeftThumbX = -50, RightTrigger = 20 }),
				Mapped(MapTo.Controller3, new Gamepad { Buttons = GamepadButtonFlags.X, RightThumbY = 300 }));
			var helper = new DInputHelper();
			helper.CombineXiStates(routing);
			const int calls = 20000;
			var allocated = Allocations.FewestBytes(5, () =>
			{
				for (var i = 0; i < calls; i++)
					helper.CombineXiStates(routing);
			});
			Assert.IsTrue(allocated < calls,
				"Combining " + calls + " times handed the collector " + allocated + " bytes; it runs on every pass.");
		}

	}
}
