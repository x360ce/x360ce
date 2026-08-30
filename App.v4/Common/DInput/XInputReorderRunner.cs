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
	/// XInput hands out the lowest free place when a device arrives and cannot be asked for a
	/// particular one. So the order is made by controlling arrivals: take away everything holding a
	/// place, then bring things back one at a time, waiting for each to land before the next goes in.
	/// Waiting is not politeness - two arriving together cannot be told apart, and the order would be
	/// whatever Windows happened to do.
	///
	/// Working out what to do is <see cref="XInputReorderPlan"/>, which touches nothing. This is the
	/// half that touches real hardware, kept apart from it so the plan can be shown to somebody, and
	/// refused, before a controller is switched off rather than after.
	/// </remarks>
	public class XInputReorderRunner
	{
		/// <summary>How long to wait for a place to be given up.</summary>
		static readonly TimeSpan OffLimit = TimeSpan.FromSeconds(15);

		/// <summary>How long to wait for a place to be taken.</summary>
		/// <remarks>
		/// Longer than giving one up, because Windows builds the device again on the way back in.
		/// </remarks>
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

		/// <summary>Waits until one more XInput place is taken than was when this was called.</summary>
		/// <remarks>
		/// Used while bringing controllers back one at a time. Their order of arrival is the order of the
		/// places, so the next must not be switched on until the last has landed.
		/// </remarks>
		public static void WaitForOneMorePlace()
		{
			var before = Occupied();
			WaitForChange(before, OnLimit);
		}

		/// <summary>Carries the plan out, one step at a time, stopping at the first that fails.</summary>
		/// <remarks>
		/// The update loop is held still throughout. It makes and takes away controllers of its own to
		/// match the game settings, and left running it would undo each step as it was taken.
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
			helper.Suspended = true;
			try
			{
				var number = 0;
				foreach (var step in plan.Steps)
				{
					number++;
					Say(string.Format("Step {0} of {1}: {2}...", number, plan.Steps.Count, step));
					if (!RunStep(helper, step))
						return false;
				}
				Say(string.Format("Places at the end   : {0}", Show(Occupied())));
				return true;
			}
			finally
			{
				// Forgotten rather than assumed, so the next pass makes whatever the game asks for now
				// instead of trusting a picture taken before any of this happened. This also lets the
				// loop go again.
				helper.ResumeAfterDeviceRemoval();
				XInputPlaces.Invalidate();
			}
		}

		bool RunStep(DInputHelper helper, XInputReorderPlan.Step step)
		{
			var before = Occupied();
			switch (step.Kind)
			{
				case XInputReorderPlan.StepKind.RemoveVirtual:
					return TakeVirtualAway(helper, step, before);
				case XInputReorderPlan.StepKind.DisableReal:
					return SwitchRealOff(step, before);
				case XInputReorderPlan.StepKind.CreateVirtual:
					return MakeVirtual(helper, step);
				default:
					return SwitchRealOn(step, before);
			}
		}

		bool TakeVirtualAway(DInputHelper helper, XInputReorderPlan.Step step, bool[] before)
		{
			// The tab it belongs to, for the same reason: which place it is in changes, and which tab made
			// it does not.
			var pad = step.Pad;
			if (pad < 1 || pad > 4)
			{
				Say(string.Format("{0} - not one of ours to take away", step));
				return true;
			}
			var error = helper.DisableFeeding((uint)pad);
			if (error != VirtualError.None)
			{
				Failure = string.Format("{0} failed: {1}", step, Describe(error, pad));
				return false;
			}
			WaitForChange(before, OffLimit);
			Say(string.Format("{0} - done", step));
			return true;
		}

		bool MakeVirtual(DInputHelper helper, XInputReorderPlan.Step step)
		{
			// The tab this controller belongs to, which is what carries its mappings. Not the number of the
			// place it is going into: taking that as the tab handed every tab another tab's controller, so
			// the order came out right and every tab pointed at the wrong one.
			//
			// The order is achieved by when it is made, not by which pad is made. Windows gives the place
			// out on arrival, so arriving first is what puts a controller first.
			var pad = step.Pad;
			if (pad < 1 || pad > 4)
			{
				Failure = string.Format("{0} failed: it is not a controller this program made.", step);
				return false;
			}
			var error = helper.EnableFeeding((uint)pad);
			if (error != VirtualError.None)
			{
				Failure = string.Format("{0} failed: {1}", step, Describe(error, pad));
				return false;
			}
			Say(string.Format("{0} - done", step));
			return true;
		}

		bool SwitchRealOff(XInputReorderPlan.Step step, bool[] before)
		{
			// Written down before it is touched, not after. A step that fails half way through leaves a
			// controller switched off, and the note is the only thing that knows to put it back.
			RememberSwitchedOff(step.HardwareId);
			bool ok;
			try { ok = Switch(AdminCommand.DisableDevices, step.HardwareId); }
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				Failure = string.Format("{0} failed: {1}", step, ex.Message);
				return false;
			}
			if (!ok)
			{
				ForgetSwitchedOff(step.HardwareId);
				Failure = string.Format("{0} failed. Switching a controller off needs Administrator.", step);
				return false;
			}
			var after = WaitForChange(before, OffLimit);
			// It may have held no place at all, which is not a failure. Said rather than passed over.
			Log.Add(after.SequenceEqual(before)
				? string.Format("{0} - done, though no place was given up", step)
				: string.Format("{0} - done, places now {1}", step, Show(after)));
			return true;
		}

		bool SwitchRealOn(XInputReorderPlan.Step step, bool[] before)
		{
			var stillOff = "It is still switched off - switch it on in Device Manager, or start this "
				+ "program again and it will be put back.";
			bool ok;
			try { ok = Switch(AdminCommand.EnableDevices, step.HardwareId); }
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				Failure = string.Format("{0} failed: {1}. {2}", step, ex.Message, stillOff);
				return false;
			}
			if (!ok)
			{
				Failure = string.Format("{0} failed. {1}", step, stillOff);
				return false;
			}
			ForgetSwitchedOff(step.HardwareId);
			var after = WaitForChange(before, OnLimit);
			var gained = Enumerable.Range(0, 4).Where(i => !before[i] && after[i]).ToArray();
			// Not a failure, and not hidden either. The order asked for was not the order given.
			Log.Add(gained.Length == 1 && gained[0] != step.ExpectedPlace
				? string.Format("{0} - went to XInput {1} instead", step, gained[0] + 1)
				: string.Format("{0} - done", step));
			return true;
		}

		/// <summary>Switches devices on or off, as Administrator when this program is not.</summary>
		/// <remarks>
		/// Windows will not let an ordinary program switch a device off. Rather than asking somebody to
		/// start the whole program again as Administrator - which loses whatever they were doing, and
		/// leaves it running afterwards with more power than it needs for anything else - a copy is run
		/// for this one job and closes again. Running as Administrator already, the same code runs here
		/// and no copy is made at all.
		/// </remarks>
		static bool Switch(AdminCommand command, params string[] deviceIds)
		{
			var ids = string.Join(",", deviceIds);
			// True when it was done here, because this program is already Administrator.
			if (Program.RunElevated(command, ids))
				{
					XInputPlaces.Invalidate();
					var here = Global.DHelper;
					if (here != null)
						here.UpdateDevicesEnabled = true;
					return true;
				}
			// Whatever happened, the machine is not what it was. Nothing else re-reads it: a device switched
			// off stays in the lists, holding a place it gave up, until something asks again.
			XInputPlaces.Invalidate();
			var helper = Global.DHelper;
			if (helper != null)
				helper.UpdateDevicesEnabled = true;
			return Program.LastAdminResult == Program.AdminResult.Done;
		}

		/// <summary>Where the controller this step acts on is now, or -1 when it holds no place.</summary>
		static int PlaceOfEntry(XInputReorderPlan.Step step)
		{
			XInputPlaces.Invalidate();
			return XInputPlaces.PlaceFor(step.HardwareId);
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
			}
			return text.ToString();
		}
	}
}
