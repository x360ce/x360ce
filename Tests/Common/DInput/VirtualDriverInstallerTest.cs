// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs
// @area: devices   @layer: unit
using JocysCom.ClassLibrary.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// Telling a pad this program created from a controller somebody is holding.
	/// </summary>
	/// <remarks>
	/// The shapes below were measured on a machine that had accumulated fifty two leftover pads. The
	/// program listed every one of them as real hardware, put them back after each restart, and filled
	/// all four XInput places, so a player saw a controller moving on its own.
	///
	/// The cause was that a pad was recognised by walking up its parent chain to the virtual bus. That
	/// works while the bus still holds the pad. It fails for a pad left behind, because the node its
	/// chain points at has already gone, so the walk arrives nowhere and the pad is called real.
	/// </remarks>
	[TestClass]
	public class VirtualDriverInstallerTest
	{

		private const string ViGEmBusId = @"ROOT\SYSTEM\0001";

		private static DeviceInfo Device(string id, string parentId, string hardwareIds)
		{
			return new DeviceInfo { DeviceId = id, ParentDeviceId = parentId, HardwareIds = hardwareIds };
		}

		private static Dictionary<string, DeviceInfo> World(params DeviceInfo[] devices)
		{
			return VirtualDriverInstaller.IndexById(devices);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The bus number is read from the end of a device name, after a backslash or an ampersand")]
		public void The_bus_number_is_read_from_either_shape_of_name()
		{
			// The name the current bus gives its first controller, as pasted in issue 1623 by three people
			// whose program offered to remove the controller it had just created.
			Assert.AreEqual(1u, VirtualDriverInstaller.TrailingNumber(@"USB\VID_045E&PID_028E\01"));
			Assert.AreEqual(12u, VirtualDriverInstaller.TrailingNumber(@"USB\VID_045E&PID_028E\12"));
			// The older shape, with the number after an ampersand.
			Assert.AreEqual(60u, VirtualDriverInstaller.TrailingNumber(@"USB\VID_045E&PID_028E\1&79F5D87&0&60"));
			// A face beneath a controller ends in something that is not the bus number; it climbs to its parent instead.
			Assert.AreEqual(0u, VirtualDriverInstaller.TrailingNumber(@"HID\VID_045E&PID_028E&IG_00\8&2B33A220&0&0000"));
			Assert.AreEqual(0u, VirtualDriverInstaller.TrailingNumber(""));
			Assert.AreEqual(0u, VirtualDriverInstaller.TrailingNumber(null));
			Assert.AreEqual(0u, VirtualDriverInstaller.TrailingNumber(@"USB\VID_045E&PID_028E\"));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Ours is the controller with our number and everything beneath it, never the bus or another number")]
		public void Ours_is_the_controller_with_our_number_and_its_faces()
		{
			// The shape the current bus gives: the bus, a controller named by its number beneath it, and
			// the XInput face Windows adds beneath the controller. A stranger's controller sits beside ours.
			var bus = Device(ViGEmBusId, null, @"Root\ViGEmBus");
			var ours = Device(@"USB\VID_045E&PID_028E\01", ViGEmBusId, @"USB\VID_045E&PID_028E");
			var face = Device(@"HID\VID_045E&PID_028E&IG_00\8&2B33A220&0&0000", ours.DeviceId, @"HID\VID_045E&PID_028E&IG_00");
			var stranger = Device(@"USB\VID_045E&PID_028E\05", ViGEmBusId, @"USB\VID_045E&PID_028E");
			var world = World(bus, ours, face, stranger);
			var held = new List<uint> { 1 };
			Assert.IsTrue(VirtualDriverInstaller.IsOneOfOurs(ours, world, held), "The controller carrying our number is ours.");
			Assert.IsTrue(VirtualDriverInstaller.IsOneOfOurs(face, world, held), "Its face is ours through it.");
			Assert.IsFalse(VirtualDriverInstaller.IsOneOfOurs(stranger, world, held), "A controller with another number is somebody else's, even under the same bus.");
			Assert.IsFalse(VirtualDriverInstaller.IsOneOfOurs(bus, world, held), "The bus is not a controller, whatever its name ends in.");
			Assert.IsFalse(VirtualDriverInstaller.IsOneOfOurs(ours, world, new List<uint>()), "Holding nothing claims nothing.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad still held by the virtual bus is recognised")]
		public void A_pad_the_bus_still_holds_is_ours()
		{
			var bus = Device(ViGEmBusId, null, @"Root\ViGEmBus");
			var stem = Device(@"USB\VID_045E&PID_028E\1&79F5D87&0&60", ViGEmBusId, @"USB\VID_045E&PID_028E");
			var pad = Device(@"USB\VID_045E&PID_028E&IG_0F\2&14BB91BF&0&0F", stem.DeviceId, @"USB\VID_045E&PID_028E&IG_0F");
			Assert.IsTrue(VirtualDriverInstaller.IsVirtualPad(pad, World(bus, stem, pad)));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad left behind, whose parent has gone, is recognised as ours")]
		public void A_pad_left_behind_is_ours()
		{
			// Exactly what was measured: the pad names a parent that is no longer anywhere in the
			// system, because removing the node above it did not take its children with it.
			var pad = Device(@"USB\VID_045E&PID_028E&IG_0F\2&14BB91BF&0&0F",
				@"USB\VID_045E&PID_028E\1&79f5d87&0&06", @"USB\VID_045E&PID_028E&IG_0F");
			Assert.IsTrue(VirtualDriverInstaller.IsVirtualPad(pad, World(pad)),
				"A pad whose parent has gone was called real hardware. That is what put fifty of " +
				"them in the device list and put them back after every restart.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A pad created moments ago, with no hardware list yet, is still recognised")]
		public void A_pad_with_no_hardware_list_yet_is_still_ours()
		{
			// Measured: three pads leaked during testing arrived with an empty hardware list and were
			// let through, while older ones were caught. The marker was in the identifier all along.
			var pad = Device(@"HID\VID_045E&PID_028E&IG_39\3&96A9016&0&0000",
				@"USB\VID_045E&PID_028E&IG_39\2&1B68A5EB&0&39", "");
			Assert.IsTrue(VirtualDriverInstaller.IsVirtualPad(pad, World(pad)),
				"A pad whose hardware list has not been filled in yet was called real hardware.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("The bus is never mistaken for one of the pads it makes")]
		public void The_bus_is_not_one_of_its_own_pads()
		{
			// The list this feeds exists so that everything on it can be deleted. Putting the bus on
			// it removes the virtual driver, and with it the ability to emulate anything at all.
			var bus = Device(ViGEmBusId, null, @"Root\ViGEmBus");
			Assert.IsFalse(VirtualDriverInstaller.IsVirtualPad(bus, World(bus)),
				"The virtual bus was listed as a leftover pad. Deleting that list would uninstall it.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller somebody is holding is never taken for one of ours")]
		public void A_real_controller_is_not_ours()
		{
			// A real device reaches the top of the tree through nodes that all exist. Measured at
			// between five and nine steps for every real device on the machine in question.
			var root = Device(@"HTREE\ROOT\0", null, null);
			var hub = Device(@"USB\ROOT_HUB30\4&2C4A1B1&0", root.DeviceId, @"USB\ROOT_HUB30");
			var pad = Device(@"USB\VID_054C&PID_0CE6\7&186C5E73&1&4", hub.DeviceId, @"USB\VID_054C&PID_0CE6");
			Assert.IsFalse(VirtualDriverInstaller.IsVirtualPad(pad, World(root, hub, pad)));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A broken chain only counts against a device carrying the XInput marker")]
		public void A_broken_chain_alone_does_not_hide_somebody_s_wheel()
		{
			// This is the guarantee that makes the rule safe to apply during a scan. A wheel or a
			// stick with an odd chain stays visible; only the shape this program itself produces can
			// be judged missing. Hiding somebody's own controller would be a worse fault than
			// showing a leftover.
			var wheel = Device(@"USB\VID_046D&PID_C29B\1&2A3B4C5D", @"USB\SOMETHING_GONE\0", @"USB\VID_046D&PID_C29B");
			Assert.IsFalse(VirtualDriverInstaller.IsVirtualPad(wheel, World(wheel)),
				"A device with no XInput marker was hidden because its chain was broken.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A chain that leads back on itself ends the walk instead of the window")]
		public void A_chain_that_loops_does_not_hang()
		{
			// The walk this replaces had nothing to stop it. Two nodes naming each other as parent
			// would have held the interface thread for as long as the program ran.
			var a = Device("A", "B", @"USB\VID_045E&PID_028E&IG_01");
			var b = Device("B", "A", @"USB\VID_045E&PID_028E");
			var world = World(a, b);
			var finished = false;
			var task = System.Threading.Tasks.Task.Run(() =>
			{
				VirtualDriverInstaller.IsVirtualPad(a, world);
				finished = true;
			});
			Assert.IsTrue(task.Wait(2000) && finished, "The walk did not come back. A loop in the tree hangs the program.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Installing over an existing bus updates it rather than adding a second one")]
		public void Installing_over_an_existing_bus_does_not_add_another()
		{
			// Measured: this computer ended up with two buses because install was run twice, and the
			// second run made a second bus rather than touching the first. Nothing looks wrong when
			// it happens, which is why it has to be held here.
			Assert.AreEqual("install", VirtualDriverInstaller.GetInstallCommand(0),
				"With no bus present there is nothing to update, so one has to be made.");
			Assert.AreEqual("update", VirtualDriverInstaller.GetInstallCommand(1),
				"A bus is already there. Installing again would leave two.");
			Assert.AreEqual("update", VirtualDriverInstaller.GetInstallCommand(5),
				"Several are already there. Installing again would make it six.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The XInput marker is read the way Microsoft documents it")]
		public void The_marker_is_read_case_insensitively()
		{
			Assert.IsTrue(VirtualDriverInstaller.CarriesInputGroup(@"USB\VID_045E&PID_028E&IG_0F"));
			Assert.IsTrue(VirtualDriverInstaller.CarriesInputGroup(@"usb\vid_045e&pid_028e&ig_0f"));
			Assert.IsFalse(VirtualDriverInstaller.CarriesInputGroup(@"USB\VID_054C&PID_0CE6"));
			Assert.IsFalse(VirtualDriverInstaller.CarriesInputGroup(null));
			Assert.IsFalse(VirtualDriverInstaller.CarriesInputGroup(""));
		}

	}
}
