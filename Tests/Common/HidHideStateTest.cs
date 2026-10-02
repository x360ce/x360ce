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
