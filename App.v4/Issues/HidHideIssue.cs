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
	/// named here, and the fix installs HID Hide or opens the program that settles it. Low, because many
	/// games are not affected and nothing here stops the emulation working.
	///
	/// Only Xbox controllers are named for hiding: XInput lists the real one beside the virtual one, so every game
	/// sees both. A device XInput cannot see, such as a wheel or a PlayStation controller, is not named: a game that
	/// reads Xbox controllers is expected to use the virtual one. A game that also reads such a device directly
	/// gets every press twice; Help's HID Hide section covers that.
	///
	/// A hidden controller is invisible to a program HID Hide does not allow, so to this program it looks
	/// unplugged. That case is judged whether or not a controller is online, or mapped at all: waiting for one
	/// would wait for ever, and a copy of the program in a new folder is a new program to HID Hide, whose
	/// controllers are simply missing from its lists.
	/// </remarks>
	public class HidHideIssue : IssueItem
	{
		/// <summary>Fix type that installs HID Hide, as the Options page's HID Hide button does.</summary>
		public const int FixInstall = 1;

		/// <summary>Fix type that opens HID Hide's configuration program.</summary>
		public const int FixConfigure = 2;

		public HidHideIssue() : base()
		{
			Name = "Game Sees Two Controllers";
			FixName = "Open HID Hide";
		}

		/// <summary>Why the last install did not finish; null when it ran to its end or has not run.</summary>
		string LastInstallResult;

		public override void CheckTask()
		{
			// Not in the round of checks at start-up: nothing here stops the program working, and reading
			// HID Hide starts a process. The next round, once the controllers are running, answers it.
			var form = MainForm.Current;
			if (form != null && form.CheckingAtStartup)
				return;
			// HID Hide runs on 64-bit Windows only.
			if (!Environment.Is64BitOperatingSystem)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var devices = GetVirtualMappedDevices();
			int fixType;
			var text = Explain(VirtualDriverInstaller.GetHidHideState(), devices,
				System.Windows.Forms.Application.ExecutablePath, out fixType);
			if (text == null)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var install = fixType == FixInstall;
			var fixName = install ? "Install HID Hide" : "Open HID Hide";
			if (FixName != fixName)
				FixName = fixName;
			// Its setup asks Windows for administrator rights; More... is the release page, to download it by hand.
			FixNeedsAdmin = install;
			var page = install ? VirtualDriverInstaller.HidHideDownloadUrl : null;
			if ((MoreInfo == null ? null : MoreInfo.AbsoluteUri) != page)
				MoreInfo = page == null ? null : new Uri(page);
			if (install && LastInstallResult != null)
				text += Environment.NewLine + LastInstallResult;
			else if (!install)
				LastInstallResult = null;
			SetSeverity(IssueSeverity.Low, fixType, text);
		}

		public override void FixTask()
		{
			// On the Issues tab's worker, so waiting for the download and the setup holds up nothing.
			if (FixType == FixInstall)
			{
				LastInstallResult = new HidHideSetupClient().Install(null);
				return;
			}
			var client = VirtualDriverInstaller.GetHidHideClientPath();
			if (string.IsNullOrEmpty(client))
				ControlsHelper.OpenUrl(VirtualDriverInstaller.HidHideDownloadUrl);
			else
				ControlsHelper.OpenPath(client);
		}

		/// <summary>Mapped devices, online or not, on the current game's tabs that have a virtual controller, ticked in the tab's list and on the Devices page.</summary>
		/// <remarks>An unticked device is not used by the game, so it is never named for hiding.</remarks>
		internal static UserDevice[] GetVirtualMappedDevices()
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
			return SettingsManager.UserDevices.ItemsToArraySynchronized()
				.Where(x => x.IsEnabled && instanceGuids.Contains(x.InstanceGuid))
				.ToArray();
		}

		/// <summary>What to tell the person about hiding the real controllers; null when nothing.</summary>
		/// <param name="state">What HID Hide is set to do.</param>
		/// <param name="devices">Mapped devices on tabs that have a virtual controller, online or not.</param>
		/// <param name="exePath">Full path of this program, as HID Hide lists programs.</param>
		/// <param name="fixType"><see cref="FixInstall"/> or <see cref="FixConfigure"/>; 0 when there is nothing to say.</param>
		public static string Explain(VirtualDriverInstaller.HidHideState state, UserDevice[] devices, string exePath, out int fixType)
		{
			fixType = 0;
			// Only what HID Hide can hide: a keyboard or mouse is never meant to be hidden, and a device
			// without a HID device instance path is not one HID Hide sees.
			var hideable = devices
				.Where(x => !x.IsKeyboard && !x.IsMouse && VirtualDriverInstaller.GetHidHidePaths(x).Length > 0)
				.ToArray();
			var xbox = hideable.Where(x => x.IsOnline && XInputPlaces.IsXInputCapable(x)).ToArray();
			if (!state.Installed)
			{
				if (xbox.Length == 0)
					return null;
				fixType = FixInstall;
				var install = new List<string>();
				AddSeenTwice(install, xbox, "Install HID Hide to hide {0} from games.");
				return string.Join(Environment.NewLine, install.ToArray());
			}
			// Installed but its program did not answer: nothing can be said about its settings.
			if (!state.Answered)
				return null;
			var lines = new List<string>();
			var hidden = hideable.Where(x => IsHidden(state, x)).ToArray();
			var refused = state.CloakOn && !state.IsAllowed(exePath);
			var allow = state.Inverse
				? "Remove " + Path.GetFileName(exePath) + " from the application list in HID Hide."
				: "Add " + Path.GetFileName(exePath) + " to the application list in HID Hide.";
			if (refused && hidden.Length > 0)
				lines.Add(string.Format("HID Hide hides {0} from this program as well, so it cannot read {1}. {2}",
					Names(hidden), hidden.Length == 1 ? "it" : "them", allow));
			else if (refused && state.HiddenDevices.Length > 0)
				lines.Add(string.Format("HID Hide hides {0} from this program, so {1} missing from its lists. {2}",
					state.HiddenDevices.Length == 1 ? "a device" : state.HiddenDevices.Length + " devices",
					state.HiddenDevices.Length == 1 ? "it is" : "they are", allow));
			if (!state.CloakOn)
				AddSeenTwice(lines, xbox, "HID Hide is installed, but hiding is off: turn hiding on in HID Hide to hide {0} from games.");
			var shown = state.CloakOn ? xbox.Where(x => !IsHidden(state, x)).ToArray() : new UserDevice[0];
			AddSeenTwice(lines, shown, "HID Hide does not hide {0} yet: tick {0} under Devices in HID Hide to hide {0} from games.");
			if (lines.Count == 0)
				return null;
			fixType = FixConfigure;
			return string.Join(Environment.NewLine, lines.ToArray());
		}

		/// <summary>Says that every game sees the Xbox controllers twice, then what hides them from games.</summary>
		/// <param name="lines">The lines to add to.</param>
		/// <param name="xbox">The Xbox controllers to name; nothing is added when empty.</param>
		/// <param name="action">What HID Hide is set to and what to do, with {0} for "it" or "them".</param>
		static void AddSeenTwice(List<string> lines, UserDevice[] xbox, string action)
		{
			if (xbox.Length == 0)
				return;
			lines.Add(string.Format(xbox.Length == 1
				? "Every game sees {0} twice: the real controller and the virtual one made from it. "
				: "Every game sees {0} twice: the real controllers and the virtual ones made from them. ",
				Names(xbox)) + string.Format(action, xbox.Length == 1 ? "it" : "them"));
		}

		static bool IsHidden(VirtualDriverInstaller.HidHideState state, UserDevice ud)
		{
			return VirtualDriverInstaller.GetHidHidePaths(ud).Any(state.IsHidden);
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
