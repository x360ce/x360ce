using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Nefarius.ViGEm.Client.Targets;

namespace x360ce.App.DInput
{
	/// <summary>Carries out a plan for putting controllers in a wanted order.</summary>
	/// <remarks>
	/// XInput gives out a place when a device arrives and cannot be asked for a particular one. So the
	/// order is made by controlling arrivals: take away everything holding a place, then bring things
	/// back one at a time, leaving Windows time to finish each before the next goes in. Two arriving
	/// together cannot be told apart.
	///
	/// XInput is not asked while real controllers are being switched off, since asking opens every
	/// controller it answers about and Windows cannot cleanly switch off a controller that is open.
	/// It is asked while controllers arrive: measured, a program that asks every second or faster
	/// while they arrive leaves them in the places they were made for, and one that asks once, or
	/// not until they are all there, leaves them in others.
	///
	/// Working out what to do is <see cref="XInputReorderPlan"/>, which touches nothing. This is the
	/// half that touches real hardware, kept apart from it so the plan can be shown to somebody, and
	/// refused, before a controller is switched off rather than after.
	/// </remarks>
	public class XInputReorderRunner
	{
		/// <summary>What happened, in the order it happened, for showing afterwards.</summary>
		public readonly List<string> Log = new List<string>();

		/// <summary>Why it stopped early, or null when every step was carried out.</summary>
		public string Failure;

		/// <summary>Whether anything was touched: set as this program starts letting go of the controllers.</summary>
		/// <remarks>A run refused before it is set changed nothing, so its report says only why.</remarks>
		public bool Started;

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

		static string Show(bool[] places)
		{
			return string.Join(" ", Enumerable.Range(0, 4)
				.Select(i => string.Format("{0}:{1}", i + 1, places[i] ? "taken" : "free")).ToArray());
		}

		#region Devices switched off

		/// <summary>Where the intent to switch a device off is written before the device is touched.</summary>
		/// <remarks>
		/// A controller switched off by a program that then stops running is a controller the person
		/// has to find and switch on again themselves, in a window they never opened, with nothing
		/// anywhere saying who did it. The intent is written down first, so the next run can put it
		/// back even if this one never reaches its own tidying up.
		/// </remarks>
		public static Func<string> PendingFile = () =>
		{
			var folder = Path.GetDirectoryName(SettingsManager.IniFileName);
			return Path.Combine(string.IsNullOrEmpty(folder) ? Path.GetTempPath() : folder,
				"x360ce.switched-off.txt");
		};

		public static void RememberSwitchedOff(string deviceId)
		{
			try
			{
				var lines = Pending().ToList();
				if (!lines.Contains(deviceId, StringComparer.OrdinalIgnoreCase))
					lines.Add(deviceId);
				File.WriteAllText(PendingFile(), string.Join(Environment.NewLine, lines.ToArray()));
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
					File.Delete(PendingFile());
				else
					File.WriteAllText(PendingFile(), string.Join(Environment.NewLine, lines));
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		static string[] Pending()
		{
			try
			{
				return File.Exists(PendingFile())
					? File.ReadAllLines(PendingFile()).Where(x => x.Trim().Length > 0).ToArray()
					: new string[0];
			}
			catch (IOException) { return new string[0]; }
			catch (UnauthorizedAccessException) { return new string[0]; }
		}

		/// <summary>The devices this program switched off for an order and that are still off.</summary>
		/// <remarks>Read from the notes and from what each device says now, so one that was switched back on, or unplugged, is not listed.</remarks>
		public static string[] StillSwitchedOff()
		{
			return Pending().Where(x => DeviceSwitch.IsOff(x) == true).ToArray();
		}

		/// <summary>Switches back on anything an earlier run switched off and never restored.</summary>
		/// <param name="stillOff">What is still off afterwards, and so still noted. Switching needs Administrator, so without it the note stays for a start that has it.</param>
		/// <returns>What was put back: read as off, and switched on.</returns>
		/// <remarks>
		/// Each device is asked, rather than taken on the word of the call that switched it. A device that is on already, or no
		/// longer there, has its note forgotten and is not counted as put back; one that would not come back keeps its note.
		/// </remarks>
		public static string[] RestoreAnythingLeftOff(out string[] stillOff)
		{
			var restored = new List<string>();
			var off = new List<string>();
			foreach (var deviceId in Pending())
			{
				try
				{
					if (DeviceSwitch.IsOff(deviceId) == true)
					{
						try
						{
							DeviceSwitch.SetState(deviceId, true);
							restored.Add(deviceId);
						}
						catch (InvalidOperationException ex)
						{
							JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
							off.Add(deviceId);
							continue;
						}
					}
				}
				catch (Exception ex)
				{
					// Whether it is on is not known, so the note stays and the next start tries again.
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					off.Add(deviceId);
					continue;
				}
				ForgetSwitchedOff(deviceId);
			}
			stillOff = off.ToArray();
			return restored.ToArray();
		}

		#endregion

		/// <summary>Switches real controllers, asking for Administrator at most once in a run.</summary>
		ElevatedDevices _devices;

		/// <summary>How long everything in this program that holds a controller has to let go of it, and XInput has to say which places are taken outside a wait for a change.</summary>
		static readonly TimeSpan StopLimit = TimeSpan.FromSeconds(10);

		/// <summary>How long Windows is left to finish a change before the next is made.</summary>
		/// <remarks>
		/// Windows builds a controller after the bus accepts it, and clears the place of one that went a moment later. Two
		/// changes too close together are not told apart, and the order of the places is then not the order they were made
		/// in. Nothing is asked of XInput while this runs.
		/// </remarks>
		static readonly TimeSpan Settle = TimeSpan.FromSeconds(4);

		/// <summary>The time between two questions to XInput while controllers arrive.</summary>
		static readonly TimeSpan WatchPause = TimeSpan.FromMilliseconds(250);

		/// <summary>The product number of a virtual controller made by this program, which is an Xbox 360 Controller.</summary>
		const ushort VirtualProduct = 0x028E;

		/// <summary>Whether a controller has arrived yet, so XInput is asked from then on.</summary>
		bool _arrivals;

		/// <summary>The hardware each pad's controller was found to be when it arrived, or null when that could not be told; by pad index.</summary>
		readonly string[] _made = new string[4];

		/// <summary>The place the plan expects each pad's controller to take; by pad index.</summary>
		readonly int[] _madeAt = { -1, -1, -1, -1 };

		/// <summary>The temporary controllers that are on the bus, by their number in the plan.</summary>
		readonly Dictionary<int, Xbox360Controller> _decoys = new Dictionary<int, Xbox360Controller>();

		/// <summary>Carries the plan out, stopping at the first step that fails.</summary>
		/// <remarks>
		/// Always the same stages, in the order the plan lists them:
		/// 1. This program stops everything that reads controllers, and takes its own away.
		/// 2. The copy running as Administrator switches the real ones off.
		/// 3. Controllers arrive one at a time: this program's own, temporary ones that hold a place on the way, and the
		///    real ones switched back on by the copy, which is told to go at the end.
		/// 4. XInput is read once, afresh, and compared with what the plan expected.
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
			var makes = plan.Steps.Any(x => x.Kind == XInputReorderPlan.StepKind.CreateVirtual
				|| x.Kind == XInputReorderPlan.StepKind.CreateDecoy);
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			if (makes && (client == null || client.Targets == null))
			{
				Failure = "This program has no controllers of its own to put anywhere. Turn on virtual "
					+ "emulation for the game being set up, wait for the controllers to appear, and try "
					+ "again.";
				return false;
			}
			// Asked before anything is touched. XInput that does not answer now would not answer at the end either, with the
			// order made and nothing to say where it went. Given as long as letting go is, since XInput is silent for seconds
			// while Windows builds a controller that has just arrived.
			var start = DInputHelper.OccupiedPlaces(StopLimit);
			if (start == null)
			{
				Failure = "XInput is not answering, so nothing was changed. Try again in a moment.";
				return false;
			}
			Say(string.Format("Places at the start: {0}", Show(start)));
			try
			{
				Started = true;
				if (!helper.StopForReorder(StopLimit))
				{
					Failure = "This program could not let go of the controllers in time, so none was "
						+ "switched off. Try again in a moment.";
					return false;
				}
				_devices = new ElevatedDevices { Said = Say };
				var number = 0;
				foreach (var step in plan.Steps)
				{
					number++;
					// After a step has failed, only switching real controllers back on is still done. Left
					// off, they would stay off until this program next starts.
					if (Failure != null && step.Kind != XInputReorderPlan.StepKind.EnableReal)
						continue;
					Say(string.Format("Step {0} of {1}: {2}...", number, plan.Steps.Count, step));
					RunStep(helper, step);
				}
				if (Failure == null)
					Verify(helper, plan);
				return Failure == null;
			}
			finally
			{
				// Temporary controllers never outlast the order, whatever happened to it.
				TakeAwayDecoys();
				// The copy running as Administrator goes first, then this program picks everything up
				// again. Forgotten rather than assumed, so the next pass makes whatever the game asks for
				// now instead of trusting a picture taken before any of this happened.
				if (_devices != null)
					_devices.Dispose();
				_devices = null;
				// An order that did not come out is not trusted to have put these where they belong: they are taken away, and
				// the next pass makes each one again in its own place, as it does whenever one is missing.
				if (Failure != null && client != null && client.Targets != null)
				{
					client.UnplugAllControllers();
					XInputPlaces.Forget();
				}
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
				case XInputReorderPlan.StepKind.CreateDecoy:
					return MakeDecoy(step);
				case XInputReorderPlan.StepKind.RemoveDecoy:
					return TakeAwayDecoy(step);
				default:
					return SwitchRealOn(step);
			}
		}

		/// <summary>Leaves Windows time to finish a change, asking XInput meanwhile once controllers are arriving, then says the step is done.</summary>
		bool Done(XInputReorderPlan.Step step)
		{
			WaitSettle(_arrivals);
			Say(string.Format("{0} - done", step));
			return true;
		}

		/// <summary>Waits as long as Windows is given to finish a change.</summary>
		/// <param name="watch">Whether to ask XInput meanwhile: not while a real controller is being switched off.</param>
		static void WaitSettle(bool watch)
		{
			if (watch)
				Watch(Settle);
			else
				Thread.Sleep(Settle);
		}

		/// <summary>Called as a controller is about to arrive: from then on XInput is asked while the controllers arrive.</summary>
		void Arriving()
		{
			if (_arrivals)
				return;
			_arrivals = true;
			Watch(TimeSpan.Zero);
		}

		bool MakeVirtual(DInputHelper helper, XInputReorderPlan.Step step)
		{
			// The tab this controller belongs to, which is what carries its mappings. Not the number of the
			// place it is going into: taking that as the tab handed every tab another tab's controller, so
			// the order came out right and every tab pointed at the wrong one.
			var pad = step.Pad;
			if (pad < 1 || pad > 4)
				return Fail(string.Format("{0} failed: it is not a controller this program made.", step));
			// The controller that appears is the one that was not there before, which is the only way of knowing which it is.
			var before = XInputPlaces.VirtualHardwareNow();
			Arriving();
			var error = helper.PlugForOrder((uint)pad);
			if (error != VirtualError.None)
				return Fail(string.Format("{0} failed: {1}", step, Describe(error, pad)));
			WaitSettle(true);
			var appeared = XInputPlaces.VirtualHardwareNow();
			appeared.ExceptWith(before);
			// More than one, or none yet, cannot be claimed: it holds the place all the same.
			_made[pad - 1] = appeared.Count == 1 ? appeared.First() : null;
			_madeAt[pad - 1] = step.ExpectedPlace;
			Say(string.Format("{0} - done", step));
			return true;
		}

		bool MakeDecoy(XInputReorderPlan.Step step)
		{
			var decoy = new Xbox360Controller(Nefarius.ViGEm.Client.ViGEmClient.Current);
			Arriving();
			try
			{
				decoy.Connect();
			}
			catch (Exception ex)
			{
				decoy.Dispose();
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return Fail(string.Format("{0} failed: the bus would not make it. {1}", step, ex.Message));
			}
			_decoys[step.Decoy] = decoy;
			return Done(step);
		}

		bool TakeAwayDecoy(XInputReorderPlan.Step step)
		{
			Xbox360Controller decoy;
			if (!_decoys.TryGetValue(step.Decoy, out decoy))
				return Fail(string.Format("{0} failed: it was never made.", step));
			_decoys.Remove(step.Decoy);
			try
			{
				decoy.Disconnect();
			}
			catch (Exception ex)
			{
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				decoy.Dispose();
				return Fail(string.Format("{0} failed: the bus would not let go of it. {1}", step, ex.Message));
			}
			decoy.Dispose();
			return Done(step);
		}

		/// <summary>Takes away every temporary controller still on the bus.</summary>
		void TakeAwayDecoys()
		{
			foreach (var decoy in _decoys.Values)
				decoy.Dispose();
			_decoys.Clear();
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
			return Done(step);
		}

		bool SwitchRealOn(XInputReorderPlan.Step step)
		{
			Arriving();
			string error;
			if (!_devices.Switch(true, new[] { step.HardwareId }, out error))
				return Fail(string.Format("{0} failed: {1} It is still switched off - switch it on in Device "
					+ "Manager, or start this program as Administrator and it will be put back.", step, error));
			ForgetSwitchedOff(step.HardwareId);
			return Done(step);
		}

		/// <summary>Reads XInput afresh, as a game started now would, and compares it with what the plan expected.</summary>
		/// <remarks>
		/// The library was let go of when the order began and nothing has loaded it since, so this is its first read: the
		/// places a game started now is given. Which places are taken is all it says. Where each of this program's controllers
		/// is, is noted only when that agrees with the plan; otherwise they are taken away and made again by the next pass.
		/// </remarks>
		void Verify(DInputHelper helper, XInputReorderPlan plan)
		{
			var expected = new bool[4];
			var ours = new bool[4];
			foreach (var step in plan.Steps)
				if ((step.Kind == XInputReorderPlan.StepKind.CreateVirtual || step.Kind == XInputReorderPlan.StepKind.EnableReal)
					&& step.ExpectedPlace >= 0)
				{
					expected[step.ExpectedPlace] = true;
					ours[step.ExpectedPlace] = step.Kind == XInputReorderPlan.StepKind.CreateVirtual;
				}
			var products = new ushort[4];
			var end = DInputHelper.OccupiedPlaces(StopLimit, products);
			if (end == null)
			{
				Say("Places at the end   : XInput is not answering, so where the controllers went is not known.");
				return;
			}
			Say(string.Format("Places at the end   : {0}", Show(end)));
			if (!end.SequenceEqual(expected))
			{
				Fail(string.Format("XInput shows {0}, but the order should have given {1}.", Show(end), Show(expected)));
				return;
			}
			// Which controller is where, when the library says: a place made for one of this program's holds a virtual one.
			for (var place = 0; place < 4; place++)
				if (ours[place] && products[place] != 0 && products[place] != VirtualProduct)
				{
					Fail(string.Format("XInput {0} holds another controller than the one this program made for it, so the order is not the one asked for.", place + 1));
					return;
				}
			for (var pad = 1; pad <= 4; pad++)
				if (_madeAt[pad - 1] >= 0)
					helper.RememberOurPlace((uint)pad, _madeAt[pad - 1], _made[pad - 1]);
		}

		/// <summary>Waits for the time given, asking XInput which places are taken at the start and every <see cref="WatchPause"/>.</summary>
		static void Watch(TimeSpan time)
		{
			var until = DateTime.UtcNow + time;
			do
			{
				DInputHelper.OccupiedPlaces(WatchPause);
				var left = until - DateTime.UtcNow;
				if (left > TimeSpan.Zero)
					Thread.Sleep(left < WatchPause ? left : WatchPause);
			}
			while (DateTime.UtcNow < until);
		}

		/// <summary>What the virtual bus said, in words, about the pad it was asked for.</summary>
		static string Describe(VirtualError error, int pad)
		{
			var text = JocysCom.ClassLibrary.Runtime.Attributes.GetDescription(error);
			try { return string.Format(text, pad, XInputPlaces.HolderWords(null)); }
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
				if (text.Length > 0)
					text.AppendLine();
				text.AppendLine(Failure);
				// Refused before anything was touched, nothing was made again either.
				if (Started)
				{
					text.AppendLine();
					text.AppendLine("This is what happened while the order was being made. Since then each controller "
						+ "has been made again as soon as its own place was free: the list above shows where they are now.");
				}
			}
			return text.ToString();
		}
	}
}
