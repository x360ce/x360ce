using JocysCom.ClassLibrary.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace x360ce.App.DInput
{
	/// <summary>Carries out a plan for putting controllers in a wanted order.</summary>
	/// <remarks>
	/// XInput gives out a place when a device arrives and cannot be asked for a particular one. So the
	/// order is made by controlling arrivals: take away everything holding a place, then bring things
	/// back one at a time, waiting for each to land before the next goes in. Two arriving together
	/// cannot be told apart. Windows does not always give the lowest free place, so where each one
	/// landed is said rather than assumed.
	///
	/// Working out what to do is <see cref="XInputReorderPlan"/>, which touches nothing. This is the
	/// half that touches real hardware, kept apart from it so the plan can be shown to somebody, and
	/// refused, before a controller is switched off rather than after.
	/// </remarks>
	public class XInputReorderRunner
	{
		/// <summary>How long to wait for a real controller switched back on to take a place.</summary>
		/// <remarks>Windows builds the device again on the way back in, which takes seconds.</remarks>
		static readonly TimeSpan OnLimit = TimeSpan.FromSeconds(30);

		/// <summary>What happened, in the order it happened, for showing afterwards.</summary>
		public readonly List<string> Log = new List<string>();

		/// <summary>Why it stopped early, or null when every step was carried out.</summary>
		public string Failure;

		/// <summary>Told what is happening, as each step is reached.</summary>
		/// <remarks>
		/// Each step waits for Windows to build or remove a device, which is seconds rather than an
		/// instant, and there are several. Without this the window sits there saying nothing while
		/// controllers switch off around somebody, which reads as a program that has stopped working.
		/// </remarks>
		public Action<string> Progress;

		void Say(string what)
		{
			Log.Add(what);
			var say = Progress;
			if (say != null)
				say(what);
		}

		/// <summary>Which of the four places report a controller right now.</summary>
		static bool[] Occupied()
		{
			var places = new bool[4];
			for (var i = 0; i < 4; i++)
				places[i] = SystemXInput.IsConnected(i);
			return places;
		}

		static string Show(bool[] places)
		{
			return string.Join(" ", Enumerable.Range(0, 4)
				.Select(i => string.Format("{0}:{1}", i + 1, places[i] ? "taken" : "free")).ToArray());
		}

		/// <summary>Waits until the places differ from what they were, and says what they became.</summary>
		static bool[] WaitForChange(bool[] from, TimeSpan limit)
		{
			var until = DateTime.UtcNow + limit;
			while (DateTime.UtcNow < until)
			{
				var now = Occupied();
				if (!now.SequenceEqual(from))
					return now;
				Thread.Sleep(50);
			}
			return Occupied();
		}

		#region Devices switched off

		/// <summary>Where the intent to switch a device off is written before the device is touched.</summary>
		/// <remarks>
		/// A controller switched off by a program that then stops running is a controller the person
		/// has to find and switch on again themselves, in a window they never opened, with nothing
		/// anywhere saying who did it. The intent is written down first, so the next run can put it
		/// back even if this one never reaches its own tidying up.
		/// </remarks>
		static string PendingFile
		{
			get
			{
				var folder = Path.GetDirectoryName(SettingsManager.IniFileName);
				return Path.Combine(string.IsNullOrEmpty(folder) ? Path.GetTempPath() : folder,
					"x360ce.switched-off.txt");
			}
		}

		public static void RememberSwitchedOff(string deviceId)
		{
			try
			{
				var lines = Pending().ToList();
				if (!lines.Contains(deviceId, StringComparer.OrdinalIgnoreCase))
					lines.Add(deviceId);
				File.WriteAllText(PendingFile, string.Join(Environment.NewLine, lines.ToArray()));
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		public static void ForgetSwitchedOff(string deviceId)
		{
			try
			{
				var lines = Pending()
					.Where(x => !string.Equals(x, deviceId, StringComparison.OrdinalIgnoreCase)).ToArray();
				if (lines.Length == 0)
					File.Delete(PendingFile);
				else
					File.WriteAllText(PendingFile, string.Join(Environment.NewLine, lines));
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		static string[] Pending()
		{
			try
			{
				return File.Exists(PendingFile)
					? File.ReadAllLines(PendingFile).Where(x => x.Trim().Length > 0).ToArray()
					: new string[0];
			}
			catch (IOException) { return new string[0]; }
			catch (UnauthorizedAccessException) { return new string[0]; }
		}

		/// <summary>Switches back on anything an earlier run switched off and never restored.</summary>
		/// <returns>What was put back, for saying so.</returns>
		public static string[] RestoreAnythingLeftOff()
		{
			var restored = new List<string>();
			foreach (var deviceId in Pending())
			{
				try
				{
					if (DeviceDetector.SetDeviceState(deviceId, true))
						restored.Add(deviceId);
				}
				catch (Exception ex) { JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex); }
				ForgetSwitchedOff(deviceId);
			}
			return restored.ToArray();
		}

		#endregion

		/// <summary>Switches real controllers, asking for Administrator at most once in a run.</summary>
		ElevatedDevices _devices;

		/// <summary>How long everything in this program that holds a controller has to let go of it.</summary>
		static readonly TimeSpan StopLimit = TimeSpan.FromSeconds(10);

		/// <summary>How long to wait between the last controller going and the first coming back.</summary>
		/// <remarks>
		/// Windows keeps a place for a controller that has just gone. Made a moment after the real
		/// controller in the first place was switched off, Controller 1's was put elsewhere and taken away
		/// again; made later, it went to the first place. Ten seconds is what others who order
		/// controllers this way have found enough.
		/// </remarks>
		static readonly TimeSpan ForgetWait = TimeSpan.FromSeconds(10);

		/// <summary>Carries the plan out, stopping at the first step that fails.</summary>
		/// <remarks>
		/// Always the same four stages, in the order the plan lists them:
		/// 1. This program stops everything that reads controllers, and takes its own away.
		/// 2. The copy running as Administrator switches the real ones off.
		/// 3. After <see cref="ForgetWait"/>, this program makes its own again, Controller 1's first.
		/// 4. The copy switches the real ones back on, and is told to go.
		/// Only then does this program read controllers again. Nothing reads them in between: XInput and
		/// DirectInput hold open every controller they have answered about, and Windows cannot cleanly
		/// switch off a controller that anything holds.
		/// </remarks>
		public bool Run(XInputReorderPlan plan)
		{
			if (plan == null || plan.Refusal != null || plan.Steps.Count == 0)
			{
				Failure = plan == null ? "There was no plan." : plan.Refusal;
				return false;
			}
			var helper = Global.DHelper;
			if (helper == null)
			{
				Failure = "The device loop is not running, so nothing can be made or taken away.";
				return false;
			}
			// Asked before anything is touched, like everything else here. A plan that has to make a
			// controller and cannot leaves real ones switched off with nothing to put in their place,
			// which is a worse position than the one somebody pressed the button to get out of.
			var makes = plan.Steps.Any(x => x.Kind == XInputReorderPlan.StepKind.CreateVirtual);
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			if (makes && (client == null || client.Targets == null))
			{
				Failure = "This program has no controllers of its own to put anywhere. Turn on virtual "
					+ "emulation for the game being set up, wait for the controllers to appear, and try "
					+ "again.";
				return false;
			}
			Say(string.Format("Places at the start: {0}", Show(Occupied())));
			try
			{
				if (!helper.StopForReorder(StopLimit))
				{
					Failure = "This program could not let go of the controllers in time, so none was "
						+ "switched off. Try again in a moment.";
					return false;
				}
				_devices = new ElevatedDevices { Said = Say };
				var number = 0;
				var waited = false;
				foreach (var step in plan.Steps)
				{
					number++;
					// After a step has failed, only switching real controllers back on is still done. Left
					// off, they would stay off until this program next starts.
					if (Failure != null && step.Kind != XInputReorderPlan.StepKind.EnableReal)
						continue;
					var arrives = step.Kind == XInputReorderPlan.StepKind.CreateVirtual
						|| step.Kind == XInputReorderPlan.StepKind.EnableReal;
					if (arrives && !waited)
					{
						waited = true;
						Say(string.Format("Waiting {0} seconds, so Windows forgets which places were held...", ForgetWait.TotalSeconds));
						Thread.Sleep(ForgetWait);
					}
					Say(string.Format("Step {0} of {1}: {2}...", number, plan.Steps.Count, step));
					RunStep(helper, step);
				}
				Say(string.Format("Places at the end   : {0}", Show(Occupied())));
				return Failure == null;
			}
			finally
			{
				// The copy running as Administrator goes first, then this program picks everything up
				// again. Forgotten rather than assumed, so the next pass makes whatever the game asks for
				// now instead of trusting a picture taken before any of this happened.
				if (_devices != null)
					_devices.Dispose();
				_devices = null;
				helper.ResumeAfterDeviceRemoval();
				XInputPlaces.Invalidate();
			}
		}

		/// <summary>Adds why a step failed, keeping why any earlier one did.</summary>
		bool Fail(string why)
		{
			Failure = Failure == null ? why : Failure + Environment.NewLine + why;
			return false;
		}

		bool RunStep(DInputHelper helper, XInputReorderPlan.Step step)
		{
			switch (step.Kind)
			{
				case XInputReorderPlan.StepKind.RemoveVirtual:
					// Taken away already, with everything else this program was holding.
					Say(string.Format("{0} - done", step));
					return true;
				case XInputReorderPlan.StepKind.DisableReal:
					return SwitchRealOff(step);
				case XInputReorderPlan.StepKind.CreateVirtual:
					return MakeVirtual(helper, step);
				default:
					return SwitchRealOn(step);
			}
		}

		bool MakeVirtual(DInputHelper helper, XInputReorderPlan.Step step)
		{
			// The tab this controller belongs to, which is what carries its mappings. Not the number of the
			// place it is going into: taking that as the tab handed every tab another tab's controller, so
			// the order came out right and every tab pointed at the wrong one.
			var pad = step.Pad;
			if (pad < 1 || pad > 4)
				return Fail(string.Format("{0} failed: it is not a controller this program made.", step));
			// Kept only in its own place, so a controller that is made is where it belongs.
			var error = helper.EnableFeeding((uint)pad);
			// Where it went is said, not only that it went wrong. The tab's own text is about now, and by
			// the time this is read the controller may well have been made again in its own place.
			if (error == VirtualError.PlaceWrong && helper.MisplacedIn[pad - 1] >= 0)
				return Fail(string.Format("{0} failed: Windows put it in XInput {1}, where it would push out "
					+ "another controller, so it was taken away again.", step, helper.MisplacedIn[pad - 1] + 1));
			if (error != VirtualError.None)
				return Fail(string.Format("{0} failed: {1}", step, Describe(error, pad)));
			Say(string.Format("{0} - done", step));
			return true;
		}

		bool SwitchRealOff(XInputReorderPlan.Step step)
		{
			// Written down before it is touched, not after. A step that fails half way through leaves a
			// controller switched off, and the note is the only thing that knows to put it back.
			RememberSwitchedOff(step.HardwareId);
			string error;
			if (!_devices.Switch(false, new[] { step.HardwareId }, out error))
			{
				ForgetSwitchedOff(step.HardwareId);
				return Fail(string.Format("{0} failed: {1}", step, error));
			}
			// Not watched going: asking XInput would open it again while it is being switched off.
			Say(string.Format("{0} - done", step));
			return true;
		}

		bool SwitchRealOn(XInputReorderPlan.Step step)
		{
			var before = Occupied();
			string error;
			if (!_devices.Switch(true, new[] { step.HardwareId }, out error))
				return Fail(string.Format("{0} failed: {1} It is still switched off - switch it on in Device "
					+ "Manager, or start this program again and it will be put back.", step, error));
			ForgetSwitchedOff(step.HardwareId);
			// With all four taken there is no place to wait for.
			var after = before.All(x => x) ? before : WaitForChange(before, OnLimit);
			var gained = Enumerable.Range(0, 4).Where(i => !before[i] && after[i]).ToArray();
			Say(gained.Length == 0 && step.ExpectedPlace < 0
				? string.Format("{0} - done", step)
				: gained.Length == 0
				? string.Format("{0} - switched on, but Windows has given it no place", step)
				: gained[0] != step.ExpectedPlace
					? string.Format("{0} - went to XInput {1} instead", step, gained[0] + 1)
					: string.Format("{0} - done", step));
			return true;
		}

		/// <summary>What the virtual bus said, in words, about the pad it was asked for.</summary>
		static string Describe(VirtualError error, int pad)
		{
			var text = JocysCom.ClassLibrary.Runtime.Attributes.GetDescription(error);
			try { return string.Format(text, pad); }
			catch (FormatException) { return text; }
		}

		/// <summary>What happened, for showing when it is over.</summary>
		public override string ToString()
		{
			var text = new StringBuilder();
			foreach (var line in Log)
				text.AppendLine(line);
			if (Failure != null)
			{
				text.AppendLine();
				text.AppendLine(Failure);
				text.AppendLine();
				text.AppendLine("This is what happened while the order was being made. Since then each controller "
					+ "has been made again as soon as its own place was free: the list above shows where they are now.");
			}
			return text.ToString();
		}
	}
}
