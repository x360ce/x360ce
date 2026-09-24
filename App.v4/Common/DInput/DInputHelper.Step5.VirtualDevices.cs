using System;
using JocysCom.ClassLibrary.Controls;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using SharpDX.XInput;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		/// <summary>True while the virtual bus client is in use by the current game.</summary>
		bool virtualModeActive;

		/// <summary>
		/// Enable or disable virtual controllers depending on game settings.
		/// </summary>
		/// <param name="game"></param>
		void UpdateVirtualDevices(UserGame game)
		{

			// Allow if not testing or testing with option enabled.
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestSetXInputStates;
			if (!allow)
				return;
			// The master switch comes first: off means no emulated controller whatever the game asks
			// for, so a wheel can be swapped for a real pad without leaving the game.
			var isVirtual = o.XInputEnabled
				&& game != null && (game.EmulationType & (int)EmulationType.Virtual) != 0;
			// If game does not use virtual emulation then...
			if (!isVirtual)
			{
				// Dispose once when leaving virtual mode. This method runs on every update,
				// so disposing unconditionally made the next call allocate and connect a new
				// native client, repeating the whole cycle at the polling frequency.
				if (virtualModeActive)
				{
					// A plug still under way is left to finish first; a later pass lets go of what it made.
					if (!SettlePlugging())
						return;
					// Each controller is let go of by name first, which is the path that also forgets
					// the place it held and the hardware that was ours. Disposing the client alone takes
					// the controllers away and leaves those notes behind.
					for (uint i = 1; i <= 4; i++)
						DisableFeeding(i);
					ViGEmClient.DisposeCurrent();
				}
				virtualModeActive = false;
				return;
			}
			// If virtual driver is missing then return.
			if (!ViGEmClient.isVBusExists(true))
				return;
			virtualModeActive = true;
			var client = ViGEmClient.Current;
			if (client.Targets == null)
			{
				client.Targets = new Xbox360Controller[4];
				for (int i = 0; i < 4; i++)
				{
					var controller = new Xbox360Controller(client);
					client.Targets[i] = controller;
					controller.FeedbackReceived += Controller_FeedbackReceived;
				}
			}
			for (uint i = 1; i <= 4; i++)
			{
				var mapTo = (MapTo)i;
				var flag = AppHelper.GetMapFlag(mapTo);
				var value = (MapToMask)(game?.EnableMask ?? (int)MapToMask.None);
				// Tested by bit rather than with HasFlag, which boxes both values, and this runs on every pass.
				var virtualEnabled = (value & flag) != 0;
				var feedingState = FeedingState[i - 1];
				var plugging = _plugging[i - 1];
				if (virtualEnabled)
				{
					if (plugging != null)
					{
						// Being plugged in on a worker. Nothing is fed until that has finished.
						if (!plugging.IsCompleted)
							continue;
						_plugging[i - 1] = null;
						var result = PlugOutcome(plugging);
						VirtualErrors[i - 1] = result;
						if (result != VirtualError.None)
						{
							_NextPlugAttempt[i - 1] = unchecked(Environment.TickCount + PlugRetryMs);
							// Kept and moved on from. Giving up on the whole pass here meant that one
							// controller which could not be made stopped the other three from being tried,
							// and said nothing about any of it.
							continue;
						}
						FeedingState[i - 1] = true;
					}
					// If feeding status unknown or not enabled then...
					else if (!feedingState.HasValue || !feedingState.Value || !client.IsControllerConnected(i))
					{
						// A refusal is not asked about again for a while. Asking costs a probe of all four
						// places and a read of the device tree, and asked on every pass it held the whole
						// thread to a few passes a second for as long as the places stayed full - and a
						// wheel fed at that rate swings from side to side.
						if (unchecked(Environment.TickCount - _NextPlugAttempt[i - 1]) < 0)
							continue;
						// Plugging in waits up to five seconds for Windows to give the controller a place
						// and reads the device tree twice: three to four seconds a controller, measured.
						// On this thread that stopped every controller being polled for as long, and a
						// wheel under a spring ran to its stop. The wait is a worker's now; the answer is
						// taken in on a later pass. One at a time: plugging in connects the lower places
						// first, so two at once each undo the other's work and neither arrives.
						if (_plugging.Any(x => x != null))
							continue;
						BeginPlug(i);
						continue;
					}
					// If the virtual target stopped accepting reports then unplug it, so the
					// next update can plug it in again instead of failing on every frame.
					if (!FeedDevice(i))
					{
						FeedingState[i - 1] = false;
						client.UnPlug(i);
					}
				}
				else
				{
					// A plug still under way is left to finish first, so that what it made is let go of too.
					if (plugging != null)
					{
						if (!plugging.IsCompleted)
							continue;
						_plugging[i - 1] = null;
						PlugOutcome(plugging);
					}
					// If feeding status unknown or enabled then...
					if (!feedingState.HasValue || feedingState.Value || client.IsControllerConnected(i))
					{
						var result = DisableFeeding(i);
						VirtualErrors[i - 1] = result;
						if (result != VirtualError.None)
							continue;
						FeedingState[i - 1] = false;
					}
				}
			}
		}

		/// <summary>What was last sent to each XInput place, so the same thing is not sent again.</summary>
		/// <remarks>
		/// Sending vibration is a call into the driver, and this runs up to a thousand times a second.
		/// Motors hold whatever they were last told, so repeating it changes nothing and costs everything.
		/// </remarks>
		readonly int[] _lastPassedForce = new int[] { -1, -1, -1, -1 };

		/// <summary>Sends the force a game asked for on to a real controller, where that is wanted.</summary>
		/// <remarks>
		/// An emulated controller has no motors. A game rumbling one reaches nothing, and on an Xbox
		/// controller there is no other way in: its DirectInput face declares no force feedback at all,
		/// so the force feedback this program drives itself cannot reach it either.
		/// </remarks>
		void PassForcesThrough(Xbox360FeedbackReceivedEventArgs[] feedbacks)
		{
			for (var pad = 1; pad <= 4; pad++)
			{
				PadSetting ps;
				var place = AppHelper.GetForcePassThroughPlace((MapTo)pad, out ps);
				if (place < 0)
					continue;
				var force = feedbacks[pad - 1];
				// Nothing new to say. The last thing sent still stands, and the motors are still doing it.
				if (force == null)
					continue;
				// The strengths apply here as much as anywhere. Nothing further along applies them: a
				// controller's own motors are driven by the driver, which knows nothing of this program's
				// settings, so a strength turned down had no effect on this route at all.
				PassForceTo(place,
					ps.ApplyForceStrength(force.LargeMotor, true),
					ps.ApplyForceStrength(force.SmallMotor, false));
			}
		}

		/// <summary>Sends one force to one place, and only when it differs from the last.</summary>
		void PassForceTo(int place, byte largeMotor, byte smallMotor)
		{
			if (place < 0 || place > 3)
				return;
			// Motors take the full range, and a byte of it is the top half of each. Multiplying by 257
			// spreads it back so that full means full rather than very nearly half.
			var left = (ushort)(largeMotor * 257);
			var right = (ushort)(smallMotor * 257);
			var both = (left << 16) | right;
			if (_lastPassedForce[place] == both)
				return;
			_lastPassedForce[place] = both;
			SystemXInput.SetVibration(place, left, right);
		}

		/// <summary>Stops the motors of anything this program was driving through pass-through.</summary>
		/// <remarks>
		/// A controller left buzzing is one the person has to unplug. Nothing else stops it: the force
		/// was sent to the device itself, not to an emulated one that can simply be taken away.
		/// </remarks>
		public void StopPassedForces()
		{
			for (var place = 0; place < 4; place++)
				if (_lastPassedForce[place] > 0)
					PassForceTo(place, 0, 0);
		}

		object FeedbackLock = new object();

		Xbox360FeedbackReceivedEventArgs[] CopyAndClearFeedbacks()
		{
			lock (FeedbackLock)
			{
				var client = ViGEmClient.Current;
				if (client == null)
					return new Xbox360FeedbackReceivedEventArgs[4];
				var list = client.Feedbacks.ToArray();
				for (int i = 0; i < 4; i++)
				{
					client.Feedbacks[i] = null;
				}
				return list;
			}
		}

		public void SetVibration(MapTo userIndex, byte largeMotor, byte smallMotor, byte ledNumber)
		{
			var client = ViGEmClient.Current;
			if (client == null)
				return;
			var e = new Xbox360FeedbackReceivedEventArgs(largeMotor, smallMotor, ledNumber);
			ViGEmClient.Current.Feedbacks[(int)userIndex - 1] = e;
		}

		private void Controller_FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
		{
			lock (FeedbackLock)
			{
				var controller = (Xbox360Controller)sender;
				for (int i = 0; i < 4; i++)
				{
					if (ViGEmClient.Current.Targets[i] == controller)
					{
						// Add force feedback value for processing.
						ViGEmClient.Current.Feedbacks[i] = e;
						break;
					}
				}
			}
		}

		bool?[] FeedingState = new bool?[4];

		/// <summary>The plug of each controller under way on a worker, or null.</summary>
		readonly System.Threading.Tasks.Task<VirtualError>[] _plugging = new System.Threading.Tasks.Task<VirtualError>[4];

		/// <summary>Starts plugging in the controller for one pad on a worker; a later pass takes in the answer.</summary>
		public System.Threading.Tasks.Task<VirtualError> BeginPlug(uint userIndex)
		{
			var plugging = System.Threading.Tasks.Task.Run(() => EnableFeeding(userIndex));
			_plugging[userIndex - 1] = plugging;
			return plugging;
		}

		/// <summary>Takes in every finished plug and forgets it. False, with nothing taken in, while one is still under way.</summary>
		/// <remarks>
		/// The bus is never let go of while a plug is under way. The plug would win that race: its
		/// controller connects after every other one has been taken away, and the bus keeps it after
		/// the program has ended - a controller nobody owns, holding one of the four places until
		/// Windows restarts.
		/// </remarks>
		bool SettlePlugging()
		{
			if (_plugging.Any(x => x != null && !x.IsCompleted))
				return false;
			for (var i = 0; i < _plugging.Length; i++)
			{
				if (_plugging[i] == null)
					continue;
				PlugOutcome(_plugging[i]);
				_plugging[i] = null;
			}
			return true;
		}

		/// <summary>Waits for every plug under way, up to the time given, then takes in what they did.</summary>
		/// <remarks>A plug waits up to five seconds for Windows to give its controller a place.</remarks>
		void WaitForPlugging(TimeSpan timeout)
		{
			var until = DateTime.UtcNow + timeout;
			foreach (var plugging in _plugging)
			{
				if (plugging == null)
					continue;
				var left = until - DateTime.UtcNow;
				((IAsyncResult)plugging).AsyncWaitHandle.WaitOne(left > TimeSpan.Zero ? left : TimeSpan.Zero);
			}
			SettlePlugging();
		}

		/// <summary>What a finished plug came to, with a fault written to the log.</summary>
		/// <remarks>
		/// Every place a finished plug is let go of reads it through here. A faulted task nobody reads
		/// is raised again by the finalizer as an unobserved exception, and that reached the person as
		/// a crash report about a controller that was simply switched off.
		/// </remarks>
		static VirtualError PlugOutcome(System.Threading.Tasks.Task<VirtualError> plugging)
		{
			if (!plugging.IsFaulted)
				return plugging.Result;
			JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(plugging.Exception.GetBaseException());
			return VirtualError.Other;
		}

		/// <summary>When each controller may next be asked for a place, after a refusal.</summary>
		readonly int[] _NextPlugAttempt = new int[4];
		/// <summary>How long a refused controller waits before asking for a place again.</summary>
		public const int PlugRetryMs = 2000;

		/// <summary>Lets go of every controller, so Windows is able to remove one.</summary>
		/// <remarks>
		/// Windows will not remove a device that anything still holds open, and this program is the
		/// thing holding them: reading the states asks XInput for all four places, over and over. The
		/// removal was therefore refused every time, and each refusal left Windows needing a restart
		/// before it would finish building any new controller - so pressing Remove broke the very
		/// thing it was meant to repair, and said nothing.
		/// </remarks>
		public void ReleaseForDeviceRemoval()
		{
			// A controller passed force feedback is buzzing under its own power; taking an emulated one
			// away does not stop it, and nothing else will.
			StopPassedForces();
			Suspended = true;
			lock (SharpDX.XInput.Controller.XInputLock)
				if (SharpDX.XInput.Controller.IsLoaded)
					SharpDX.XInput.Controller.FreeLibrary();
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			if (client != null && client.Targets != null)
				for (uint i = 1; i <= 4; i++)
					// Asked for unconditionally: letting go of one that is already gone is the outcome wanted,
					// and is no longer reported as a fault.
					client.UnPlug(i);
				// Nothing of ours holds a place now, so no note of one should outlive this.
				XInputPlaces.Forget();
				XInputPlaces.Invalidate();
		}

		/// <summary>Picks the controllers back up after a removal.</summary>
		public void ResumeAfterDeviceRemoval()
		{
			// Forgotten rather than assumed, so the next pass plugs in whatever is wanted now.
			for (var i = 0; i < FeedingState.Length; i++)
			{
				FeedingState[i] = null;
				_NextPlugAttempt[i] = Environment.TickCount;
			}
			UpdateDevicesEnabled = true;
			Suspended = false;
		}

		Gamepad[] oldGamepadStates = new Gamepad[4];

		bool IsGuideDown;
		object guideLock = new object();

		/// <summary>Send the combined state to the virtual controller.</summary>
		/// <returns>False when the report could not be delivered to the virtual bus.</returns>
		public bool FeedDevice(uint i)
		{
			// Get old and new game pad values.
			var n = CombinedXiStates[i - 1].Gamepad;
			// Compare with old state.
			var o = oldGamepadStates[i - 1];
			var changed =
				n.Buttons != o.Buttons ||
				n.LeftThumbX != o.LeftThumbX ||
				n.LeftThumbY != o.LeftThumbY ||
				n.LeftTrigger != o.LeftTrigger ||
				n.RightThumbX != o.RightThumbX ||
				n.RightThumbY != o.RightThumbY ||
				n.RightTrigger != o.RightTrigger;
			// The report is built only when there is something to send. Built on every pass, it would cost a
			// new report and thirty boxed flags a controller a millisecond, nearly all of them thrown away.
			if (!changed)
				return true;
			var report = new Xbox360Report();
			report.SetButtonState(Xbox360Buttons.A, n.Buttons.HasFlag(GamepadButtonFlags.A));
			report.SetButtonState(Xbox360Buttons.B, n.Buttons.HasFlag(GamepadButtonFlags.B));
			report.SetButtonState(Xbox360Buttons.X, n.Buttons.HasFlag(GamepadButtonFlags.X));
			report.SetButtonState(Xbox360Buttons.Y, n.Buttons.HasFlag(GamepadButtonFlags.Y));
			report.SetButtonState(Xbox360Buttons.Start, n.Buttons.HasFlag(GamepadButtonFlags.Start));
			report.SetButtonState(Xbox360Buttons.Back, n.Buttons.HasFlag(GamepadButtonFlags.Back));
			report.SetButtonState(Xbox360Buttons.LeftThumb, n.Buttons.HasFlag(GamepadButtonFlags.LeftThumb));
			report.SetButtonState(Xbox360Buttons.RightThumb, n.Buttons.HasFlag(GamepadButtonFlags.RightThumb));
			report.SetButtonState(Xbox360Buttons.LeftShoulder, n.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder));
			report.SetButtonState(Xbox360Buttons.RightShoulder, n.Buttons.HasFlag(GamepadButtonFlags.RightShoulder));
			report.SetButtonState(Xbox360Buttons.Up, n.Buttons.HasFlag(GamepadButtonFlags.DPadUp));
			report.SetButtonState(Xbox360Buttons.Right, n.Buttons.HasFlag(GamepadButtonFlags.DPadRight));
			report.SetButtonState(Xbox360Buttons.Down, n.Buttons.HasFlag(GamepadButtonFlags.DPadDown));
			report.SetButtonState(Xbox360Buttons.Left, n.Buttons.HasFlag(GamepadButtonFlags.DPadLeft));
			report.SetButtonState(Xbox360Buttons.Guide, n.Buttons.HasFlag(GamepadButtonFlags.Guide));
			report.SetAxis(Xbox360Axes.LeftTrigger, n.LeftTrigger);
			report.SetAxis(Xbox360Axes.RightTrigger, n.RightTrigger);
			report.SetAxis(Xbox360Axes.LeftThumbX, n.LeftThumbX);
			report.SetAxis(Xbox360Axes.LeftThumbY, n.LeftThumbY);
			report.SetAxis(Xbox360Axes.RightThumbX, n.RightThumbX);
			report.SetAxis(Xbox360Axes.RightThumbY, n.RightThumbY);
			// Update controller.
			try
			{
				ViGEmClient.Current.Targets[i - 1].SendReport(report);
			}
			catch (Nefarius.ViGEm.Client.ViGEmException ex)
				when (ex.Code == Nefarius.ViGEm.Client.VIGEM_ERROR.VIGEM_ERROR_INVALID_TARGET
					|| ex.Code == Nefarius.ViGEm.Client.VIGEM_ERROR.VIGEM_ERROR_TARGET_NOT_PLUGGED_IN)
			{
				// The controller went away underneath us, which happens when the bus drops one - a driver
				// update, most often. It is put back on the next pass and nobody sees anything. Saying so
				// is worth a line in the log and not a fault report to somebody who cannot act on it.
				return false;
			}
			catch (System.Exception ex)
			{
				// The virtual bus can drop a target while a game is running, for example
				// when the driver is updated. Report the failure instead of letting it
				// escape into the update loop and stop the controller thread.
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
			lock (guideLock)
			{
				var isGuidePressed = n.Buttons.HasFlag(GamepadButtonFlags.Guide);
				if (isGuidePressed && !IsGuideDown)
				{
					var keys = GetGuideKeys();
					if (keys.Count() > 0)
						JocysCom.ClassLibrary.Processes.KeyboardHelper.SendDown(keys);
					IsGuideDown = true;
				}
				if (!isGuidePressed && IsGuideDown)
				{
					var keys = GetGuideKeys();
					if (keys.Count() > 0)
						JocysCom.ClassLibrary.Processes.KeyboardHelper.SendUp(keys);
					IsGuideDown = false;
				}
			}
			// Update old state.
			oldGamepadStates[i - 1] = n;
			return true;
		}

		private static Keys[] GetGuideKeys()
		{
			var list = new List<Keys>();
			var keys = SettingsManager.Options.GuideButtonAction;
			var matches = rxKeys.Matches(keys);
			foreach (Match m in matches)
			{
				var s = m.Groups["key"].Value;
				byte keyCode;
				// Try parse as byte/number first.
				if (byte.TryParse(s, out keyCode))
				{
					list.Add((Keys)keyCode);
					continue;
				}
				// Try parse as "Keys" enum (ignore case).
				Keys keyValue;
				if (System.Enum.TryParse(s, true, out keyValue))
				{
					list.Add(keyValue);
					continue;
				}
			}
			return list.ToArray();
		}

		private static Regex rxKeys = new Regex("{(?<key>[0-9a-zA-Z]+)}");

		public static VirtualError CheckInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (ViGEmClient.isVBusExists(false))
				return VirtualError.None;
			Program.RunElevated(AdminCommand.InstallViGEmBus);
			return VirtualError.None;
		}

		public static VirtualError CheckUnInstallVirtualDriver()
		{
			// If driver is installed already then return.
			if (!ViGEmClient.isVBusExists(false))
				return VirtualError.None;
			Program.RunElevated(AdminCommand.UninstallViGEmBus);
			return VirtualError.None;
		}
		/// <summary>The controller each pad owns, so it can be forgotten by name when the pad goes.</summary>
		readonly string[] OurHardware = new string[4];

		/// <summary>Notes which controller was made and where it was put.</summary>
		/// <remarks>
		/// The controller itself, not a number read off the end of a device name. Names carry numbers
		/// that belong to nothing in particular - a USB hub above a real controller ends in one, and the
		/// bus numbers its controllers across every program using it while each program numbers its own
		/// from one. Both were mistaken for ours.
		/// </remarks>
		void RememberOurPlace(uint userIndex, int place, string hardwareId)
		{
			XiPlaceForPad[userIndex - 1] = place;
			OurHardware[userIndex - 1] = hardwareId;
			XInputPlaces.Remember(hardwareId, place);
			XInputPlaces.Invalidate();
		}

		static bool[] OccupiedPlaces()
		{
			var places = new bool[4];
			for (var i = 0; i < 4; i++)
				places[i] = SystemXInput.IsConnected(i);
			return places;
		}

		/// <summary>Waits for a place to fill, and says which one did.</summary>
		/// <remarks>
		/// Windows builds the controller after the bus accepts it, so the place does not appear at
		/// once. Two seconds was the longest seen on a machine with a real controller already
		/// holding a place; five is given, and -1 answers a place that never arrives.
		///
		/// Which one filled, rather than whether the wanted one did. Windows does not hand out the
		/// place that was asked for, nor even the lowest free one: measured on a machine with a real
		/// controller in the first place and the second one free, a controller made for the second
		/// was given the third. Asking only whether it went where it was wanted threw away the one
		/// thing nothing else can supply - where it actually went.
		/// </remarks>
		static int WaitForPlace(bool[] before)
		{
			var until = DateTime.UtcNow.AddSeconds(5);
			while (DateTime.UtcNow < until)
			{
				var now = OccupiedPlaces();
				for (var i = 0; i < 4; i++)
					if (!before[i] && now[i])
						return i;
				System.Threading.Thread.Sleep(50);
			}
			return -1;
		}

		/// <summary>The bus client to work with for the rest of one call, or null when there is none.</summary>
		/// <remarks>
		/// Plugging in runs on a worker for seconds, and the client is let go of on the input thread
		/// whenever the game leaves virtual mode or the program closes. Read once, the reference is
		/// ours for the call whatever happens to the shared one; read at each step, it was null by
		/// the time the controller was connected, and the worker died on it.
		/// </remarks>
		static ViGEmClient BusClient(bool createIfMissing)
		{
			if (!ViGEmClient.isVBusExists(createIfMissing))
				return null;
			var client = ViGEmClient.Current;
			return client == null || client.Disposing || client.IsDisposed ? null : client;
		}

		public VirtualError EnableFeeding(uint userIndex)
		{
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			var client = BusClient(true);
			if (client == null)
				return VirtualError.Missing;
			if (!client.isControllerExists(userIndex))
				return VirtualError.Other;
			if (client.IsControllerConnected(userIndex))
				return VirtualError.None;
			// Windows cannot be asked for a particular place, and gives neither the one asked for nor
			// reliably the lowest free one. That was assumed, and a controller landing anywhere else was
			// taken away again - so a real controller holding the first place stopped a second tab from
			// getting a controller at all, with a free place sitting there. A tab with nothing behind it
			// reaches no game whatsoever, which is worse than one whose controller sits somewhere
			// unexpected and is shown doing so.
			//
			// So it is made wherever Windows puts it, and where that was is written down. The device
			// lists and the tab light all read that, so an unexpected place is visible rather than
			// silently wrong.
			var before = OccupiedPlaces();
			// Nothing can be given a place when there is none, and asking anyway costs the five seconds
			// spent waiting for one to appear. Answered before the device tree is read, which is the
			// expensive part and would be wasted on a refusal.
			if (before.All(x => x))
				return VirtualError.PlaceNotGiven;
			// Which controllers are on the bus before we ask for one. The one that is there afterwards and
			// was not before is ours, which is the only way of knowing that does not rest on reading a
			// number off a name and hoping it means what it looks like.
			var padsBefore = XInputPlaces.VirtualHardwareNow();
			if (!client.PlugIn(userIndex))
				return VirtualError.Other;
			// Where it went, rather than where it was asked to go. The bus says yes when it accepts a
			// controller, which is not the same as Windows having given it the place we need.
			var place = WaitForPlace(before);
			if (place < 0)
			{
				client.UnPlug(userIndex);
				return VirtualError.PlaceNotGiven;
			}
			// Written down now, while it is certain. Nothing reports where a controller was put,
			// so the only moment the answer exists is the moment it arrives.
			var appeared = XInputPlaces.VirtualHardwareNow();
			appeared.ExceptWith(padsBefore);
			// Exactly one should have appeared. None means Windows has not finished building it yet, and
			// more than one means somebody else made theirs in the same moment - neither can be claimed.
			RememberOurPlace(userIndex, place, appeared.Count == 1 ? appeared.First() : null);
			return VirtualError.None;
		}

		public VirtualError DisableFeeding(uint userIndex)
		{
			bool success;
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			var client = BusClient(false);
			if (client == null)
				return VirtualError.Missing;
			if (!client.isControllerExists(userIndex))
				return VirtualError.None;
			if (!client.IsControllerConnected(userIndex))
				return VirtualError.None;
			success = client.UnPlug(userIndex);
			if (success)
			{
				// The place it held is nobody's now. Left behind, it would go on being counted against
				// this program, and whatever takes the place next would be named as ours.
				XiPlaceForPad[userIndex - 1] = -1;
				// And forget where it was. The note was kept for the life of the program, so a controller taken
				// away went on claiming its place; the next one given that place claimed it as well, and two
				// controllers holding one place is not a thing that can be true. Everything after it followed:
				// the place a real controller held could no longer be worked out, because one more place was
				// spoken for than there were controllers to hold them.
				XInputPlaces.Forget(OurHardware[userIndex - 1]);
				OurHardware[userIndex - 1] = null;
				XInputPlaces.Invalidate();
			}
			return success
				? VirtualError.None
				: VirtualError.Other;
		}

	}
}
