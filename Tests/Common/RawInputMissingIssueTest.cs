// @under-test: App.v4/Issues/RawInputMissingIssue.cs, Engine/Input/Devices/RawInputDevice.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using x360ce.App.DInput;
using x360ce.App.Issues;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>A controller DirectInput reads that Raw Input must list and does not is named, so its games can be told why they cannot see it.</summary>
	[TestClass]
	public class RawInputMissingIssueTest
	{
		const string PadPath = @"\\?\hid#vid_054c&pid_0ce6&mi_03#b&18ed46b&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}";
		const string PadHid = @"HID\VID_054C&PID_0CE6&MI_03\B&18ED46B&0&0000";

		static UserDevice Pad()
		{
			return new UserDevice { InstanceGuid = Guid.NewGuid(), HidDevicePath = PadPath, HidDeviceId = PadHid, IsOnline = true, IsEnabled = true, CapType = (int)SharpDX.DirectInput.DeviceType.Gamepad };
		}

		static UserDevice RawTwin(UserDevice pad, bool online)
		{
			return new UserDevice { InstanceGuid = Guid.NewGuid(), InputSourceType = (int)InputSourceType.RawInput, HidDevicePath = pad.HidDevicePath, IsOnline = online, IsEnabled = true };
		}

		static readonly VirtualDriverInstaller.HidHideState Idle = new VirtualDriverInstaller.HidHideState { Installed = true, Answered = true };

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A game controller DirectInput reads, with no Raw Input twin online, is named; one Raw Input lists is not")]
		public void A_controller_missing_from_Raw_Input_is_named()
		{
			var pad = Pad();
			CollectionAssert.AreEqual(new[] { pad }, RawInputMissingIssue.Missing(new[] { pad }, Idle, p => true), "A controller Raw Input never listed is not named.");
			CollectionAssert.AreEqual(new[] { pad }, RawInputMissingIssue.Missing(new[] { pad, RawTwin(pad, false) }, Idle, p => true), "A controller Raw Input dropped is not named.");
			Assert.AreEqual(0, RawInputMissingIssue.Missing(new[] { pad, RawTwin(pad, true) }, Idle, p => true).Length, "A controller Raw Input lists is named.");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Not named: a device Raw Input does not list as a game controller, one HID Hide hides now, an offline one, a keyboard")]
		public void Only_a_controller_Raw_Input_must_list_is_named()
		{
			var pad = Pad();
			Assert.AreEqual(0, RawInputMissingIssue.Missing(new[] { pad }, Idle, p => false).Length, "A device Raw Input does not list as a game controller is named.");
			Assert.AreEqual(0, RawInputMissingIssue.Missing(new[] { pad }, Idle, p => null).Length, "A device whose description cannot be read is named.");
			var hiding = new VirtualDriverInstaller.HidHideState { Installed = true, Answered = true, CloakOn = true, HiddenDevices = new[] { PadHid } };
			Assert.AreEqual(0, RawInputMissingIssue.Missing(new[] { pad }, hiding, p => true).Length, "A controller HID Hide hides now is named here as well as by the HID Hide issue.");
			pad.IsOnline = false;
			Assert.AreEqual(0, RawInputMissingIssue.Missing(new[] { pad }, Idle, p => true).Length, "An offline controller is named.");
			var keyboard = Pad();
			keyboard.CapType = (int)SharpDX.DirectInput.DeviceType.Keyboard;
			Assert.AreEqual(0, RawInputMissingIssue.Missing(new[] { keyboard }, Idle, p => true).Length, "A keyboard is named.");
		}

		[TestMethod, TestCategory("devices")]
		[Description("The rule the Raw Input list goes by: a joystick, gamepad or multi-axis collection on the Generic Desktop page")]
		public void Raw_Input_lists_game_controller_collections()
		{
			Assert.IsTrue(RawInputDevice.IsGameController(0x054C, 0x0CE6, 0x01, 0x05), "A gamepad is not a game controller.");
			Assert.IsTrue(RawInputDevice.IsGameController(0x046D, 0xC29B, 0x01, 0x04), "A joystick collection is not a game controller.");
			Assert.IsFalse(RawInputDevice.IsGameController(0x046D, 0xC52B, 0x01, 0x02), "A mouse collection is a game controller.");
			Assert.IsFalse(RawInputDevice.IsGameController(0x046D, 0xC52B, 0xFF00, 0x01), "A vendor collection is a game controller.");
		}
	}
}
