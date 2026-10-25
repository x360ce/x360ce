// @under-test: App.v4/Common/DInput/VirtualDriverInstaller.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using x360ce.App.DInput;

namespace x360ce.Tests
{
	/// <summary>
	/// HID Hide keeps its settings where only an administrator can read them, but its command line program
	/// prints them for anybody. What it prints is read here without running anything, and the one test
	/// that runs it checks that a second look soon after the first does not start it again.
	/// </summary>
	[TestClass]
	public class HidHideStateTest
	{
		/// <summary>What HidHideCLI.exe 1.5.230 printed on 2026-09-27 with nothing hidden.</summary>
		const string Printed15230 =
			"--cloak-off\r\n" +
			"--inv-off\r\n" +
			"--app-reg \"C:\\Program Files\\Nefarius Software Solutions\\HidHide\\x64\\HidHideCLI.exe\"\r\n" +
			"--app-reg \"C:\\Program Files\\Nefarius Software Solutions\\HidHide\\x64\\HidHideClient.exe\"\r\n";

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("What HID Hide 1.5.230 printed reads as: hiding off, nothing hidden, two programs listed")]
		public void What_HID_Hide_printed_is_read()
		{
			var state = VirtualDriverInstaller.ParseHidHideCli(Printed15230);
			Assert.IsTrue(state.Answered);
			Assert.IsFalse(state.CloakOn);
			Assert.IsFalse(state.Inverse);
			Assert.AreEqual(0, state.HiddenDevices.Length);
			CollectionAssert.AreEqual(new[]
			{
				@"C:\Program Files\Nefarius Software Solutions\HidHide\x64\HidHideCLI.exe",
				@"C:\Program Files\Nefarius Software Solutions\HidHide\x64\HidHideClient.exe",
			}, state.Applications);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Hidden devices are read both as the command HID Hide prints and as bare paths")]
		public void Hidden_devices_are_read_in_either_form()
		{
			var state = VirtualDriverInstaller.ParseHidHideCli(
				"--cloak-on\r\n" +
				"--inv-on\r\n" +
				"--dev-hide \"HID\\VID_046D&PID_C29B\\B&3586936B&0&0000\"\r\n" +
				"HID\\VID_046D&PID_C219\\A&1AFDDD43&0&0000\r\n");
			Assert.IsTrue(state.Answered);
			Assert.IsTrue(state.CloakOn);
			Assert.IsTrue(state.Inverse);
			CollectionAssert.AreEqual(new[]
			{
				@"HID\VID_046D&PID_C29B\B&3586936B&0&0000",
				@"HID\VID_046D&PID_C219\A&1AFDDD43&0&0000",
			}, state.HiddenDevices);
		}

		[TestMethod, TestCategory("devices")]
		[Description("No output, or an error message, is never read as settings")]
		public void An_error_is_not_read_as_settings()
		{
			Assert.IsFalse(VirtualDriverInstaller.ParseHidHideCli(null).Answered);
			Assert.IsFalse(VirtualDriverInstaller.ParseHidHideCli("").Answered);
			// Printed by HidHideCLI.exe --dev-gaming on 2026-09-27.
			var state = VirtualDriverInstaller.ParseHidHideCli(
				"Error code 0x048F at src\\HID.cpp(291) `anonymous-namespace'::HidModelInfo: The device is not connected.\r\n");
			Assert.IsFalse(state.Answered);
			Assert.AreEqual(0, state.HiddenDevices.Length);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Device paths and program paths match in any case, and the inverse list turns allowed into refused")]
		public void Matching_ignores_case_and_follows_the_inverse_list()
		{
			var state = new VirtualDriverInstaller.HidHideState
			{
				HiddenDevices = new[] { @"HID\VID_046D&PID_C29B\B&3586936B&0&0000" },
				Applications = new[] { @"C:\Games\x360ce\x360ce.exe" },
			};
			Assert.IsTrue(state.IsHidden(@"hid\vid_046d&pid_c29b\b&3586936b&0&0000"));
			Assert.IsFalse(state.IsHidden(@"HID\VID_046D&PID_C219\A&1AFDDD43&0&0000"));
			Assert.IsFalse(state.IsHidden(null));
			Assert.IsTrue(state.IsAllowed(@"c:\games\X360CE\x360ce.exe"));
			Assert.IsFalse(state.IsAllowed(@"C:\Other\x360ce.exe"));
			state.Inverse = true;
			Assert.IsFalse(state.IsAllowed(@"C:\Games\x360ce\x360ce.exe"));
			Assert.IsTrue(state.IsAllowed(@"C:\Other\x360ce.exe"));
		}

		const string Pad = @"HID\VID_046D&PID_C219\A&1AFDDD43&0&0000";
		const string Wheel = @"HID\VID_046D&PID_C29B\B&3586936B&0&0000";
		const string Exe = @"C:\Games\x360ce\x360ce.exe";

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Hiding a device names it, switches hiding on and lets this program see it; what HID Hide already does is left out")]
		public void Hiding_names_the_device_switches_hiding_on_and_lets_this_program_see_it()
		{
			var state = new VirtualDriverInstaller.HidHideState { Answered = true, HiddenDevices = new[] { Wheel } };
			Assert.AreEqual("--dev-hide \"" + Pad + "\" --app-reg \"" + Exe + "\" --cloak-on",
				VirtualDriverInstaller.GetHidHideChanges(state, new[] { Pad, Wheel }, new string[0], Exe));
			state.CloakOn = true;
			state.Applications = new[] { Exe };
			Assert.AreEqual("--dev-hide \"" + Pad + "\"", VirtualDriverInstaller.GetHidHideChanges(state, new[] { Pad, Wheel }, new string[0], Exe));
			Assert.AreEqual("", VirtualDriverInstaller.GetHidHideChanges(state, new[] { Wheel }, new[] { Pad }, Exe),
				"Nothing is to change, yet a command was written.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Showing a device unhides it only while it is hidden, and touches neither the program list nor hiding")]
		public void Showing_unhides_only_what_is_hidden()
		{
			var state = new VirtualDriverInstaller.HidHideState { Answered = true, CloakOn = true, HiddenDevices = new[] { Wheel } };
			Assert.AreEqual("--dev-unhide \"" + Wheel + "\"", VirtualDriverInstaller.GetHidHideChanges(state, new string[0], new[] { Pad, Wheel }, Exe));
		}

		[TestMethod, TestCategory("devices")]
		[Description("With the inverse list, the program list names those that may not see, so this program is taken off it")]
		public void The_inverse_list_takes_this_program_off()
		{
			var state = new VirtualDriverInstaller.HidHideState { Answered = true, CloakOn = true, Inverse = true, Applications = new[] { Exe } };
			Assert.AreEqual("--dev-hide \"" + Pad + "\" --app-unreg \"" + Exe + "\"", VirtualDriverInstaller.GetHidHideChanges(state, new[] { Pad }, new string[0], Exe));
		}

		[TestMethod, TestCategory("devices")]
		[Description("A device is hidden under its HID device instance paths only")]
		public void A_device_is_hidden_under_its_HID_paths()
		{
			var ud = new x360ce.Engine.Data.UserDevice { HidDeviceId = Pad, DevDeviceId = @"USB\VID_046D&PID_C219\9&26FEC410&0&2", HidParentDeviceId = " " + Pad.ToLowerInvariant() };
			CollectionAssert.AreEqual(new[] { Pad }, VirtualDriverInstaller.GetHidHidePaths(ud));
		}

		[TestMethod, TestCategory("devices")]
		[Description("HID Hide's list of gaming devices reads as each HID device's name and path")]
		public void The_gaming_devices_are_read()
		{
			// The shape HidHideCLI.exe 1.5.230 printed for --dev-gaming on 2026-10-24, one physical device of two HID devices.
			var printed = " [ { \"friendlyName\" : \"Logitech Cordless RumblePad 2\" , \"devices\" : [\r\n"
				+ "{ \"present\" : true , \"product\" : \"Logitech Cordless RumblePad 2\" , \"deviceInstancePath\" : \"HID\\\\VID_046D&PID_C219\\\\a&1afddd43&0&0000\" } ,\r\n"
				+ "{ \"present\" : true , \"product\" : \"\" , \"deviceInstancePath\" : \"HID\\\\VID_046D&PID_C219&COL02\\\\a&1afddd43&0&0001\" } ] } ]";
			CollectionAssert.AreEqual(new[]
			{
				@"Logitech Cordless RumblePad 2: HID\VID_046D&PID_C219\a&1afddd43&0&0000",
				@"Logitech Cordless RumblePad 2: HID\VID_046D&PID_C219&COL02\a&1afddd43&0&0001",
			}, VirtualDriverInstaller.ParseHidHideDevices(printed));
			Assert.AreEqual(0, VirtualDriverInstaller.ParseHidHideDevices("Error code 0x048F").Length, "An error message was read as devices.");
			Assert.AreEqual(0, VirtualDriverInstaller.ParseHidHideDevices(null).Length);
		}

		[TestMethod, TestCategory("devices")]
		[Description("Where HID Hide is installed, the gaming devices its program lists read as names with HID device paths")]
		public void Installed_HID_Hide_lists_its_gaming_devices()
		{
			var lines = VirtualDriverInstaller.GetHidHideGamingDevices();
			foreach (var line in lines)
				Console.WriteLine(line);
			if (!VirtualDriverInstaller.IsHidHideDevicePresent())
				return;
			foreach (var line in lines)
				StringAssert.Contains(line, @"HID\", "A gaming device was read without its HID device path.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("A second look soon after the first reuses the reading instead of starting HID Hide's program again")]
		public void A_second_look_reuses_the_reading()
		{
			var watch = Stopwatch.StartNew();
			var first = VirtualDriverInstaller.GetHidHideState();
			var firstMs = watch.ElapsedMilliseconds;
			var second = VirtualDriverInstaller.GetHidHideState();
			Console.WriteLine("installed {0}, answered {1}, first look {2} ms", first.Installed, first.Answered, firstMs);
			Assert.AreSame(first, second, "The second look started HID Hide's program again.");
			Assert.IsTrue(firstMs < 5000, "Reading HID Hide took " + firstMs + " ms; its program is given three seconds.");
			// Where HID Hide is installed, its program answers without administrator rights.
			if (first.Installed)
				Assert.IsTrue(first.Answered, "HID Hide is installed but its command line program did not answer.");
		}
	}
}
