// @under-test: App.v4/Issues/HidHideIssue.cs
// @area: devices   @layer: unit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using x360ce.App.DInput;
using x360ce.App.Issues;
using x360ce.Engine.Data;

namespace x360ce.Tests
{
	/// <summary>
	/// A game that sees the pad twice needs HID Hide, not the obsolete HID Guardian. The Issues tab names
	/// the step of HID Hide that is missing, and stays quiet where nothing is wrong.
	/// </summary>
	[TestClass]
	public class HidHideIssueTest
	{
		const string Exe = @"C:\Games\x360ce\x360ce.exe";
		const string PadPath = @"HID\VID_046D&PID_C219\A&1AFDDD43&0&0000";

		static UserDevice Pad(bool online, string path = PadPath)
		{
			var ud = new UserDevice();
			ud.ProductName = "RumblePad 2";
			ud.HidDeviceId = path;
			ud.DevDeviceId = path;
			ud.IsOnline = online;
			return ud;
		}

		static VirtualDriverInstaller.HidHideState Installed(bool cloakOn, params string[] hidden)
		{
			return new VirtualDriverInstaller.HidHideState
			{
				Installed = true,
				Answered = true,
				CloakOn = cloakOn,
				HiddenDevices = hidden,
				Applications = new[] { Exe },
			};
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Without HID Hide, a mapped controller that is online brings the download")]
		public void Without_HID_Hide_the_fix_is_the_download()
		{
			int fixType;
			var text = HidHideIssue.Explain(new VirtualDriverInstaller.HidHideState(), new[] { Pad(true) }, Exe, out fixType);
			Assert.AreEqual(HidHideIssue.FixDownload, fixType);
			StringAssert.Contains(text, "RumblePad 2");
			StringAssert.Contains(text, "Install HID Hide");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Without HID Hide and without an online mapped controller nothing is said")]
		public void Nothing_is_said_without_an_online_controller()
		{
			int fixType;
			Assert.IsNull(HidHideIssue.Explain(new VirtualDriverInstaller.HidHideState(), new[] { Pad(false) }, Exe, out fixType));
			Assert.AreEqual(0, fixType);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Hiding switched off is named, and the fix opens HID Hide")]
		public void Hiding_off_opens_HID_Hide()
		{
			int fixType;
			var text = HidHideIssue.Explain(Installed(false, PadPath), new[] { Pad(true) }, Exe, out fixType);
			Assert.AreEqual(HidHideIssue.FixConfigure, fixType);
			StringAssert.Contains(text, "hiding is off");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A mapped controller HID Hide does not hide is named")]
		public void A_controller_not_hidden_is_named()
		{
			int fixType;
			var text = HidHideIssue.Explain(Installed(true), new[] { Pad(true) }, Exe, out fixType);
			Assert.AreEqual(HidHideIssue.FixConfigure, fixType);
			StringAssert.Contains(text, "does not hide RumblePad 2");
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A hidden controller and an allowed program is no issue, whatever the case of the path")]
		public void Hidden_and_allowed_is_no_issue()
		{
			int fixType;
			Assert.IsNull(HidHideIssue.Explain(Installed(true, PadPath.ToLowerInvariant()), new[] { Pad(true) }, Exe, out fixType));
			Assert.AreEqual(0, fixType);
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("A controller hidden from this program as well is named, although it looks unplugged here")]
		public void Hidden_from_this_program_is_named_while_offline()
		{
			var state = Installed(true, PadPath);
			state.Applications = new string[0];
			int fixType;
			var text = HidHideIssue.Explain(state, new[] { Pad(false) }, Exe, out fixType);
			Assert.AreEqual(HidHideIssue.FixConfigure, fixType);
			StringAssert.Contains(text, "Add x360ce.exe to the application list");
			// With the inverse list, being listed is what refuses it.
			state.Inverse = true;
			state.Applications = new[] { Exe };
			text = HidHideIssue.Explain(state, new[] { Pad(false) }, Exe, out fixType);
			StringAssert.Contains(text, "Remove x360ce.exe from the application list");
		}

		[TestMethod, TestCategory("devices")]
		[Description("Keyboards, mice and devices without a HID path are never asked to be hidden")]
		public void Keyboards_and_non_HID_devices_are_left_alone()
		{
			var keyboard = Pad(true, @"HID\VID_046D&PID_C31C&MI_00\7&1A2B3C4D&0&0000");
			keyboard.CapType = (int)SharpDX.DirectInput.DeviceType.Keyboard;
			var notHid = Pad(true, "");
			int fixType;
			Assert.IsNull(HidHideIssue.Explain(new VirtualDriverInstaller.HidHideState(), new[] { keyboard, notHid }, Exe, out fixType));
			Assert.IsNull(HidHideIssue.Explain(Installed(true), new[] { keyboard, notHid }, Exe, out fixType));
		}

		[TestMethod, TestCategory("devices")]
		[Description("HID Hide installed but not answering says nothing rather than guessing")]
		public void An_unanswered_reading_says_nothing()
		{
			var state = new VirtualDriverInstaller.HidHideState { Installed = true };
			int fixType;
			Assert.IsNull(HidHideIssue.Explain(state, new[] { Pad(true) }, Exe, out fixType));
		}

		[TestMethod, TestCategory("devices"), TestCategory("critical")]
		[Description("Reading HID Hide starts a process, so the engine never calls it")]
		public void The_engine_never_reads_HID_Hide()
		{
			var folder = Path.Combine(Ui.RepoRoot.FullName, "App.v4", "Common", "DInput");
			foreach (var file in Directory.GetFiles(folder, "DInputHelper*.cs"))
				Assert.IsFalse(File.ReadAllText(file).Contains("GetHidHideState("),
					Path.GetFileName(file) + " reads HID Hide. That starts a process and belongs to the issue check.");
		}
	}
}
