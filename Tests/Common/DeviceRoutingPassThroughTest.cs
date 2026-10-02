// @under-test: App.v4/Common/DInput/DeviceRouting.cs, App.v4/Common/DInput/XInputPlaces.cs
// @area: engine   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.IO;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;
using static x360ce.Tests.DeviceRoutingFixtures;

namespace x360ce.Tests
{
	/// <summary>Where a tab's force feedback is passed through to, by name or by the device's own place.</summary>
	/// <remarks>
	/// A row whose settings ask to pass force through sends it to a named XInput place, or to the place
	/// the device itself holds when no place is named. The routing works this out from the settings and
	/// the devices given to <c>DeviceRouting.Build</c>, and a row after one that already answered is
	/// still asked, so a later tab can still pass its own force on.
	/// </remarks>
	[TestClass]
	public class DeviceRoutingPassThroughTest
	{
		/// <summary>A place table holding one id, or none.</summary>
		static Dictionary<string, int> Places(string id = null, int place = XInputPlaces.Unknown)
		{
			var places = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			if (id != null)
				places[id] = place;
			return places;
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A tab passes its force to the place its settings name, and no row after that one is asked")]
		public void Force_is_passed_to_the_place_named()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			var named = PassThrough(3);
			var later = PassThrough(1);

			var routing = DeviceRouting.Build(Game,
				new[] { Row(device.InstanceGuid, MapTo.Controller2, named), Row(Guid.NewGuid(), MapTo.Controller2, later) },
				new[] { named, later }, new[] { device });

			Assert.IsTrue(routing.PassesForceThrough, "A tab passing its force on is not seen, so the engine skips it.");
			Assert.AreEqual(1, routing.PadPassThrough[1].Length, "A row after one that names a place is still asked.");
			PadSetting ps;
			Assert.AreEqual(2, routing.PassThroughPlace(1, Places(device.HidDeviceId, 0), out ps),
				"The force is not passed to the place the settings name.");
			Assert.AreSame(named, ps, "The strengths of other settings than those which named the place apply.");
			Assert.AreEqual(XInputPlaces.Unknown, routing.PassThroughPlace(0, Places(), out ps), "A tab that passes nothing on sends its force somewhere.");
			Assert.IsNull(ps, "A tab that passes nothing on has settings for it.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("With no place named, a tab passes its force to the place its device holds, by either of the device's ids")]
		public void Force_is_passed_to_the_place_the_device_holds()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1", DevDeviceId = "USB\\VID_045E&PID_028E\\1" };
			var own = PassThrough(0);

			var routing = DeviceRouting.Build(Game, new[] { Row(device.InstanceGuid, MapTo.Controller1, own) }, new[] { own }, new[] { device });

			Assert.IsTrue(routing.PassesForceThrough, "A tab passing its force on is not seen, so the engine skips it.");
			PadSetting ps;
			Assert.AreEqual(3, routing.PassThroughPlace(0, Places(device.HidDeviceId, 3), out ps), "The place the device's HID face holds is not used.");
			Assert.AreSame(own, ps, "The strengths of other settings than those which asked for the place apply.");
			Assert.AreEqual(1, routing.PassThroughPlace(0, Places(device.DevDeviceId, 1), out ps), "The place the device holds under its own id is not used.");
			Assert.AreEqual(XInputPlaces.Unknown, routing.PassThroughPlace(0, Places(), out ps), "A device whose place is not known is sent force.");
			Assert.IsNull(ps, "A tab that passes nothing on has settings for it.");
			// Read when the places are worked out, not when the routing is built: a device found again under a new id is
			// looked up by that id.
			device.HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\2";
			Assert.AreEqual(2, routing.PassThroughPlace(0, Places(device.HidDeviceId, 2), out ps),
				"The device is looked up by the id it had when the routing was built.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A device whose place is not known leaves the answer to the tab's next row")]
		public void A_device_whose_place_is_not_known_leaves_it_to_the_next_row()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			var own = PassThrough(0);
			var named = PassThrough(4);

			var routing = DeviceRouting.Build(Game,
				new[] { Row(device.InstanceGuid, MapTo.Controller3, own), Row(Guid.NewGuid(), MapTo.Controller3, named) },
				new[] { own, named }, new[] { device });

			PadSetting ps;
			Assert.AreEqual(3, routing.PassThroughPlace(2, Places(), out ps), "A device whose place is not known stops the next row from answering.");
			Assert.AreSame(named, ps);
			Assert.AreEqual(1, routing.PassThroughPlace(2, Places(device.HidDeviceId, 1), out ps), "The first row that can answer does not.");
			Assert.AreSame(own, ps);
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Nothing is passed on from settings with Pass through off or not stored, a device not listed, another game's row, or no game")]
		public void Nothing_is_passed_on_without_a_row_of_the_game_that_asks()
		{
			var device = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			var off = new PadSetting { ForcePassThrough = "0", ForcePassThroughIndex = "2", PadSettingChecksum = Guid.NewGuid() };
			var named = PassThrough(2);
			var own = PassThrough(0);
			var elsewhere = Row(device.InstanceGuid, MapTo.Controller2, named);
			elsewhere.FileName = "other.exe";
			var rows = new[]
			{
				Row(device.InstanceGuid, MapTo.Controller1, off),
				elsewhere,
				Row(device.InstanceGuid, MapTo.Controller3, PassThrough(1)),
				Row(Guid.NewGuid(), MapTo.Controller4, own),
			};

			var routing = DeviceRouting.Build(Game, rows, new[] { off, named, own }, new[] { device });

			Assert.IsFalse(routing.PassesForceThrough, "The engine works out places for a game none of whose tabs passes force on.");
			Assert.AreEqual(0, routing.PadPassThrough[0].Length, "Settings with Pass through off pass force on.");
			Assert.AreEqual(0, routing.PadPassThrough[1].Length, "Another game's row passes this game's force on.");
			Assert.AreEqual(0, routing.PadPassThrough[2].Length, "Settings that are not stored pass force on.");
			Assert.AreEqual(0, routing.PadPassThrough[3].Length, "A row whose device is not in the list passes force on.");
			Assert.IsFalse(DeviceRouting.Build(null, new[] { Row(device.InstanceGuid, MapTo.Controller1, named) }, new[] { named }, new[] { device }).PassesForceThrough,
				"With no game, force is passed on.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("A place named in Pass through is skipped while a device the person unticked holds it; a device mapped nowhere keeps receiving")]
		public void A_named_place_held_by_an_unticked_device_is_skipped()
		{
			// Every row of the game that maps it is unticked.
			var unticked = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\1" };
			// Unticked on the Devices page, with no row of the game.
			var disabled = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\2", IsEnabled = false };
			// Known, and mapped nowhere in the game.
			var stranger = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\3" };
			// Unticked on one tab and ticked on another.
			var partly = new UserDevice { InstanceGuid = Guid.NewGuid(), HidDeviceId = "HID\\VID_045E&PID_028E&IG_00\\4" };
			var named = PassThrough(2);
			var off = Row(unticked.InstanceGuid, MapTo.Controller2);
			off.IsEnabled = false;
			var partlyOff = Row(partly.InstanceGuid, MapTo.Controller3);
			partlyOff.IsEnabled = false;
			var rows = new[] { Row(Guid.NewGuid(), MapTo.Controller1, named), off, partlyOff, Row(partly.InstanceGuid, MapTo.Controller4) };

			var routing = DeviceRouting.Build(Game, rows, new[] { named }, new[] { unticked, disabled, stranger, partly });

			PadSetting ps;
			Assert.AreEqual(XInputPlaces.Unknown, routing.PassThroughPlace(0, Places(unticked.HidDeviceId, 1), out ps),
				"The tab's force is passed on to a controller whose rows are all unticked.");
			Assert.IsNull(ps, "A place passed over still names the settings that asked for it.");
			Assert.AreEqual(XInputPlaces.Unknown, routing.PassThroughPlace(0, Places(disabled.HidDeviceId, 1), out ps),
				"The tab's force is passed on to a controller unticked on the Devices page.");
			Assert.AreEqual(1, routing.PassThroughPlace(0, Places(stranger.HidDeviceId, 1), out ps),
				"A controller mapped nowhere in the game no longer receives the force its place was named for.");
			Assert.AreEqual(1, routing.PassThroughPlace(0, Places(partly.HidDeviceId, 1), out ps),
				"A controller still ticked on one tab no longer receives the force its place was named for.");
			Assert.AreEqual(1, routing.PassThroughPlace(0, Places(), out ps), "A place nobody known holds no longer receives the force.");
		}

		[TestMethod, TestCategory("force-feedback"), TestCategory("critical")]
		[Description("Every ticked row of the game on a tab can pass its force on, whatever the tab's switch; an unticked row passes nothing on")]
		public void Pass_through_takes_every_ticked_row_of_the_game_on_the_tab()
		{
			var game = new UserGame { FileName = Game.FileName, FileProductName = Game.FileProductName, EnableMask = (int)MapToMask.Controller2 };
			var ps = PassThrough(4);
			var tabOff = Row(Guid.NewGuid(), MapTo.Controller1, ps);
			var rowOff = Row(Guid.NewGuid(), MapTo.Controller2, ps);
			rowOff.IsEnabled = false;

			var routing = DeviceRouting.Build(game, new[] { tabOff, rowOff }, new[] { ps });

			PadSetting answered;
			Assert.AreEqual(3, routing.PassThroughPlace(0, Places(), out answered),
				"A tab switched off no longer passes its force on, so the stop its unplugging sends never reaches the controller.");
			Assert.AreEqual(XInputPlaces.Unknown, routing.PassThroughPlace(1, Places(), out answered),
				"An unticked row passes its tab's force on.");
			Assert.AreEqual(0, routing.PadPassThrough[1].Length, "An unticked row is asked where its tab's force is passed on to.");
		}
	}
}
