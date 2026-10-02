using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.Issues
{
	/// <summary>A game that can see a real controller as well as the virtual one made from it.</summary>
	/// <remarks>
	/// A game that reads both gets every press twice, or shows one player as two. HID Hide hides the real
	/// controller from games, but only once it is installed, switched on, told which controllers to hide,
	/// and told that this program may still see them. Each of those is a place people stop, so each is
	/// named here, and the fix opens the page or program that settles it. Low, because many games are not
	/// affected and nothing here stops the emulation working.
	///
	/// A hidden controller is invisible to a program HID Hide does not allow, so to this program it looks
	/// unplugged. That case is judged from mapped controllers whether or not they are online; waiting for
	/// one to be online would wait for ever.
	/// </remarks>
	public class HidHideIssue : IssueItem
	{
		/// <summary>Fix type that opens HID Hide's download page.</summary>
		public const int FixDownload = 1;

		/// <summary>Fix type that opens HID Hide's configuration program.</summary>
		public const int FixConfigure = 2;

		public HidHideIssue() : base()
		{
			Name = "Game Sees Two Controllers";
			FixName = "Open HID Hide";
		}

		public override void CheckTask()
		{
			// Not in the round of checks at start-up: nothing here stops the program working, and reading
			// HID Hide starts a process. The next round, once the controllers are running, answers it.
			var form = MainForm.Current;
			if (form != null && !form.AllowDHelperStart)
				return;
			// HID Hide runs on 64-bit Windows only.
			if (!Environment.Is64BitOperatingSystem)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var devices = GetVirtualMappedDevices();
			if (devices.Length == 0)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			int fixType;
			var text = Explain(VirtualDriverInstaller.GetHidHideState(), devices,
				System.Windows.Forms.Application.ExecutablePath, out fixType);
			if (text == null)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var fixName = fixType == FixDownload ? "Download HID Hide" : "Open HID Hide";
			if (FixName != fixName)
				FixName = fixName;
			SetSeverity(IssueSeverity.Low, fixType, text);
		}

		public override void FixTask()
		{
			var client = FixType == FixConfigure ? VirtualDriverInstaller.GetHidHideClientPath() : null;
			if (string.IsNullOrEmpty(client))
				ControlsHelper.OpenUrl(VirtualDriverInstaller.HidHideDownloadUrl);
			else
				ControlsHelper.OpenPath(client);
		}

		/// <summary>Mapped devices, online or not, on the current game's tabs that have a virtual controller, ticked in the tab's list and on the Devices page.</summary>
		/// <remarks>An unticked device is not used by the game, so it is never named for hiding.</remarks>
		static UserDevice[] GetVirtualMappedDevices()
		{
			var game = SettingsManager.CurrentGame;
			if (game == null)
				return new UserDevice[0];
			var instanceGuids = SettingsManager.GetSettings(game.FileName)
				.Where(x => x.IsEnabled && x.MapTo >= (int)MapTo.Controller1 && x.MapTo <= (int)MapTo.Controller4
					&& DInputHelper.WantsVirtual(game, (uint)x.MapTo))
				.Select(x => x.InstanceGuid)
				.ToArray();
			if (instanceGuids.Length == 0)
				return new UserDevice[0];
			return SettingsManager.UserDevices.ItemsToArraySyncronized()
				.Where(x => x.IsEnabled && instanceGuids.Contains(x.InstanceGuid))
				.ToArray();
		}

		/// <summary>What to tell the person about hiding the real controllers; null when nothing.</summary>
		/// <param name="state">What HID Hide is set to do.</param>
		/// <param name="devices">Mapped devices on tabs that have a virtual controller, online or not.</param>
		/// <param name="exePath">Full path of this program, as HID Hide lists programs.</param>
		/// <param name="fixType"><see cref="FixDownload"/> or <see cref="FixConfigure"/>; 0 when there is nothing to say.</param>
		public static string Explain(VirtualDriverInstaller.HidHideState state, UserDevice[] devices, string exePath, out int fixType)
		{
			fixType = 0;
			// Only what HID Hide can hide: a keyboard or mouse is never meant to be hidden, and a device
			// without a HID device instance path is not one HID Hide sees.
			var hideable = devices
				.Where(x => !x.IsKeyboard && !x.IsMouse && Paths(x).Any(IsHidPath))
				.ToArray();
			var online = hideable.Where(x => x.IsOnline).ToArray();
			if (!state.Installed)
			{
				if (online.Length == 0)
					return null;
				fixType = FixDownload;
				return string.Format(online.Length == 1
					? "A game can see {0} twice, as the real controller and as the virtual one. Install HID Hide to hide the real one."
					: "A game can see {0} twice, as the real controllers and as the virtual ones. Install HID Hide to hide the real ones.",
					Names(online));
			}
			// Installed but its program did not answer: nothing can be said about its settings.
			if (!state.Answered)
				return null;
			var lines = new List<string>();
			var hidden = hideable.Where(x => IsHidden(state, x)).ToArray();
			if (state.CloakOn && hidden.Length > 0 && !state.IsAllowed(exePath))
				lines.Add(string.Format("HID Hide hides {0} from this program as well, so it cannot read {1}. {2}",
					Names(hidden), hidden.Length == 1 ? "it" : "them",
					state.Inverse
						? "Remove " + Path.GetFileName(exePath) + " from the application list in HID Hide."
						: "Add " + Path.GetFileName(exePath) + " to the application list in HID Hide."));
			if (!state.CloakOn && online.Length > 0)
				lines.Add(string.Format("HID Hide is installed but hiding is off, so a game can see {0} twice. Turn hiding on in HID Hide.",
					Names(online)));
			var shown = state.CloakOn ? online.Where(x => !IsHidden(state, x)).ToArray() : new UserDevice[0];
			if (shown.Length > 0)
				lines.Add(string.Format("HID Hide does not hide {0}, so a game can see {1} twice. Tick {1} under Devices in HID Hide.",
					Names(shown), shown.Length == 1 ? "it" : "them"));
			if (lines.Count == 0)
				return null;
			fixType = FixConfigure;
			return string.Join(Environment.NewLine, lines.ToArray());
		}

		/// <summary>The device instance paths HID Hide may list a device under.</summary>
		static string[] Paths(UserDevice ud)
		{
			return new[] { ud.HidDeviceId, ud.DevDeviceId, ud.HidParentDeviceId };
		}

		static bool IsHidPath(string path)
		{
			return !string.IsNullOrEmpty(path) && path.StartsWith(@"HID\", StringComparison.OrdinalIgnoreCase);
		}

		static bool IsHidden(VirtualDriverInstaller.HidHideState state, UserDevice ud)
		{
			return Paths(ud).Any(state.IsHidden);
		}

		static string Names(UserDevice[] devices)
		{
			return string.Join(", ", devices
				.Select(x => string.IsNullOrEmpty(x.ProductName) ? x.InstanceName : x.ProductName)
				.Distinct()
				.ToArray());
		}
	}
}
