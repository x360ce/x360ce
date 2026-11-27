using JocysCom.ClassLibrary.Controls.IssuesControl;
using System;
using System.Collections.Generic;
using System.Linq;
using x360ce.App.DInput;
using x360ce.Engine;
using x360ce.Engine.Data;
using JocysCom.ClassLibrary.Controls.UiTree;

namespace x360ce.App.Issues
{
	/// <summary>A controller Windows has, and DirectInput reads, that Raw Input does not list, so games reading Raw Input cannot see it.</summary>
	/// <remarks>
	/// Windows opens a controller for Raw Input when a program first asks to read it. Hidden by HID Hide at that moment,
	/// whatever programs HID Hide allows, the controller is left out of Raw Input, and stays out when HID Hide shows it
	/// again, until it starts again: plugged in again, or restarted. This program hides its controllers only once its own
	/// Raw Input reader has them open, which keeps them; a controller restarted or plugged in while hidden, or hidden by
	/// another program first, is still left out. Plugging it in again needs nothing; the fix restarts it instead, which
	/// needs Administrator: one copy of this program runs as Administrator for it, and the button shows the shield.
	/// Restarting stores nothing, so a restart that stops half way leaves no controller switched off.
	///
	/// Only a controller Raw Input must list is judged: its own HID description names a game controller collection, as
	/// the Raw Input list goes by. One HID Hide hides now is left to <see cref="HidHideIssue"/>. A controller missing on
	/// one round only is not named: DirectInput and Raw Input see a controller arrive a moment apart.
	///
	/// HidHide-RawInput-Workaround: removed when a released HID Hide lets Raw Input's own open through
	/// (nefarius/HidHide pull request 226); `.ai/developer.instructions.md` says how to check.
	/// </remarks>
	public class RawInputMissingIssue : IssueItem
	{
		public RawInputMissingIssue() : base()
		{
			Name = UiLanguage.T("RawInputMissingIssue.GamesCannotSeeController", "Games Cannot See Controller");
			FixName = UiLanguage.T("RawInputMissingIssue.RestartController", "Restart Controller");
			FixNeedsAdmin = true;
		}

		/// <summary>The controllers found missing on the round before, by instance.</summary>
		HashSet<Guid> _missingBefore = new HashSet<Guid>();

		/// <summary>The device instance paths the fix restarts.</summary>
		string[] _restart = new string[0];

		/// <summary>Why the last restart did not finish; null when it did or has not run.</summary>
		string _lastFixResult;

		public override void CheckTask()
		{
			// Not in the round of checks at start-up: the device list is not taken in yet, and Raw Input's half of it with it.
			var form = MainForm.Current;
			var engine = Global.DHelper;
			if (form != null && form.CheckingAtStartup || engine == null || engine.RefreshDevicesCount == 0 || engine.RawInputError != null)
			{
				SetSeverity(IssueSeverity.None);
				return;
			}
			var devices = SettingsManager.UserDevices.ItemsToArraySynchronized();
			var hidHide = VirtualDriverInstaller.GetHidHideState();
			var missing = Missing(devices, hidHide, RawInputDevice.IsGameControllerAt);
			Record(engine.Recording, devices, hidHide, missing);
			// Named only when missing on two rounds in a row.
			var named = missing.Where(x => _missingBefore.Contains(x.InstanceGuid)).ToArray();
			_missingBefore = new HashSet<Guid>(missing.Select(x => x.InstanceGuid));
			if (named.Length == 0)
			{
				_lastFixResult = null;
				SetSeverity(IssueSeverity.None);
				return;
			}
			_restart = named.SelectMany(VirtualDriverInstaller.GetHidHidePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
			var names = string.Join(", ", named.Select(x => string.IsNullOrEmpty(x.ProductName) ? x.InstanceName : x.ProductName).Distinct().ToArray());
			var text = string.Format(named.Length == 1
				? UiLanguage.T("RawInputMissingIssue.WindowsHasButAGameThat", "Windows has {0}, but a game that reads controllers through Raw Input cannot see it until it starts again, as after HID Hide stops hiding it. Restart it here, or unplug it and plug it in again.")
				: UiLanguage.T("RawInputMissingIssue.WindowsHasButAGameThat2", "Windows has {0}, but a game that reads controllers through Raw Input cannot see them until they start again, as after HID Hide stops hiding them. Restart them here, or unplug them and plug them in again."),
				names);
			if (_lastFixResult != null)
				text += Environment.NewLine + _lastFixResult;
			SetSeverity(IssueSeverity.Moderate, 1, text);
		}

		/// <summary>What the check read, as last written to a bug report recording, and that recording.</summary>
		string _recorded;
		Recording.ReportRecorder _recordedTo;

		/// <summary>Writes what the check read into a running bug report recording, when it changed. Device GUIDs become their labels there.</summary>
		void Record(Recording.ReportRecorder recording, UserDevice[] devices, VirtualDriverInstaller.HidHideState hidHide, UserDevice[] missing)
		{
			if (recording == null)
				return;
			var hideBox = devices.Where(x => x.IsDirectInput && x.IsOnline && x.IsHidden).Select(x => x.InstanceGuid.ToString()).ToArray();
			var text = string.Format("raw input check: HID Hide {0}; Hide box ticked: {1}; missing: {2}",
				!hidHide.Installed ? "not installed" : !hidHide.Answered ? "did not answer" : hidHide.CloakOn ? "hides " + hidHide.HiddenDevices.Length : "hides nothing",
				hideBox.Length == 0 ? "none" : string.Join(" ", hideBox),
				missing.Length == 0 ? "none" : string.Join(" ", missing.Select(x => x.InstanceGuid.ToString()).ToArray()));
			if (text == _recorded && recording == _recordedTo)
				return;
			_recorded = text;
			_recordedTo = recording;
			recording.Note(text);
		}

		/// <summary>The controllers online through DirectInput that Raw Input must list and does not, leaving out those hidden on purpose.</summary>
		/// <remarks>
		/// Hidden on purpose: its Hide box is ticked, or HID Hide hides it now. While HID Hide is installed and does not answer,
		/// which it hides is not known, so none is judged.
		/// </remarks>
		/// <param name="devices">The devices listed.</param>
		/// <param name="hidHide">What HID Hide is set to do.</param>
		/// <param name="isGameController">Reads a HID interface's own description: whether Raw Input lists it as a game controller, or null when it cannot be read.</param>
		internal static UserDevice[] Missing(UserDevice[] devices, VirtualDriverInstaller.HidHideState hidHide, Func<string, bool?> isGameController)
		{
			if (hidHide.Installed && !hidHide.Answered)
				return new UserDevice[0];
			return devices
				.Where(x => x.IsDirectInput && x.IsOnline && !x.IsHidden && !x.IsKeyboard && !x.IsMouse && !string.IsNullOrEmpty(x.HidDevicePath))
				.Where(x =>
				{
					var twin = x.RawInputTwin(devices);
					return twin == null || !twin.IsOnline;
				})
				.Where(x => !(hidHide.CloakOn && VirtualDriverInstaller.GetHidHidePaths(x).Any(hidHide.IsHidden)))
				.Where(x => isGameController(x.HidDevicePath) == true)
				.ToArray();
		}

		public override void FixTask()
		{
			// On the Issues tab's worker, so waiting for Windows to ask for Administrator, and for the controllers, holds up nothing.
			var ids = _restart;
			if (ids.Length == 0)
				return;
			string error;
			bool done;
			using (var devices = new ElevatedDevices())
			{
				done = devices.Switch(false, ids, out error) && devices.Switch(true, ids, out error);
				_lastFixResult = done ? null : "The restart did not finish: " + error;
			}
			// Restarted controllers come back as new arrivals, which the device list reads as for any controller plugged in.
			// A failed restart keeps them named, so the next round shows why it failed.
			if (done)
				_missingBefore.Clear();
		}
	}
}
