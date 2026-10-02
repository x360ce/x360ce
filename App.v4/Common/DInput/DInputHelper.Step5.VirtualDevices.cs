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
					// Every controller went with the client, let go of by name above or not, and each game's force goes too.
					for (uint i = 1; i <= 4; i++)
						DropGameForce(i);
					// Nothing is asked of the bus now, so nothing it did before is held against it.
					ForgetBusHealth();
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
					client.Targets[i] = NewTarget(client);
			}
			for (uint i = 1; i <= 4; i++)
			{
				var virtualEnabled = WantsVirtual(game, i);
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
						var result = PlugOutcome(i, plugging);
						VirtualErrors[i - 1] = result;
						// A plug held back never reached the bus, so it is not counted for or against it.
						if (!_heldBack[i - 1])
							PlugFailures[i - 1] = NextPlugFailures(PlugFailures[i - 1], result);
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
						if (IsWaiting(_NextPlugAttempt[i - 1], Environment.TickCount, PlugRetryMs))
							continue;
						// Plugging in waits up to five seconds for Windows to give the controller a place
						// and reads the device tree twice: three to four seconds a controller, measured.
						// On this thread that stopped every controller being polled for as long, and a
						// wheel under a spring ran to its stop. The wait is a worker's now; the answer is
						// taken in on a later pass. One at a time: plugging in connects the lower places
						// first, so two at once each undo the other's work and neither arrives.
						if (PlugUnderWay(false))
							continue;
						BeginPlug(i);
						continue;
					}
					// If the virtual target stopped accepting reports then unplug it, so the
					// next update can plug it in again instead of failing on every frame.
					if (!FeedDevice(i))
					{
						var now = Environment.TickCount;
						var drops = NextFeedDrops(FeedDrops[i - 1], LastFeedDropTick[i - 1], now);
						// The time before the count, so a reader that reads the count first never pairs a new
						// count with an old time.
						LastFeedDropTick[i - 1] = now;
						FeedDrops[i - 1] = drops;
						FeedingState[i - 1] = false;
						// Still held by the bus when letting go fails. Asked again at once, it would be found
						// attached and fed straight back, so it waits for the plug gate like a refused plug.
						// Let go of, its game goes with it, so nothing that game asked for plays on while the
						// controller is made again.
						if (!client.UnPlug(i))
							_NextPlugAttempt[i - 1] = unchecked(now + PlugRetryMs);
						else
							DropGameForce(i);
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
						PlugOutcome(i, plugging);
					}
					// If feeding status unknown or enabled then...
					if (!feedingState.HasValue || feedingState.Value || client.IsControllerConnected(i))
					{
						// A controller the bus would not let go of is asked again after the plug wait, not on
						// every pass with an exception and a fault report each time.
						if (IsWaiting(_NextPlugAttempt[i - 1], Environment.TickCount, PlugRetryMs))
							continue;
						var result = DisableFeeding(i);
						VirtualErrors[i - 1] = result;
						if (result != VirtualError.None)
						{
							_NextPlugAttempt[i - 1] = unchecked(Environment.TickCount + PlugRetryMs);
							continue;
						}
						FeedingState[i - 1] = false;
					}
				}
			}
		}

		/// <summary>What was last sent to each XInput place, so the same thing is not sent again.</summary>
		/// <remarks>
		/// Sending vibration is a call into the driver, and this runs up to a thousand times a second.
		/// Motors hold whatever they were last told, so repeating it changes nothing and costs everything.
		///
		/// The large motor's byte above the small motor's: -1 while nothing was sent, 0 once stopped, and above 0 while
		/// any force is running.
		/// </remarks>
		readonly int[] _lastPassedForce = new int[] { -1, -1, -1, -1 };

		/// <summary>The force each controller's game asked for last, 0 to 255 for each motor, by pad index.</summary>
		/// <remarks>
		/// A game says what it wants when that changes, not on every pass. A device whose force comes from
		/// several tabs is driven by what each asked for last, so one tab speaking does not silence another.
		/// Read and written only on the engine thread, and cleared by <see cref="DropGameForce"/> when a pad's controller is
		/// taken away, on the engine thread or while it is suspended.
		/// </remarks>
		readonly byte[] _lastLargeMotor = new byte[4];
		readonly byte[] _lastSmallMotor = new byte[4];

		/// <summary>A bit for each pad whose controller was taken away since the last pass, so the devices it drove are told.</summary>
		int _forcesDropped;

		/// <summary>The routing the devices were last told their force by. Read and written only on the engine thread.</summary>
		/// <remarks>
		/// A new routing can take a tab from a device's force sources: the tab switched off, or its Force
		/// feedback enabled switch. Nothing that tab's game says reaches the device after that, so the device
		/// is told once, at the force of the tabs it has now, or it would go on playing the tab taken away.
		/// </remarks>
		DeviceRouting _forceRouting;

		/// <summary>Where each pad passes its force this pass, the row whose strengths apply, and the force for each place. Reused, not made each pass.</summary>
		/// <remarks>The places and the rows are worked out by <see cref="ResolvePassPlaces"/>, only when they can have changed.</remarks>
		readonly int[] _passPlaces = new int[4];
		readonly PassThroughSource[] _passSources = new PassThroughSource[4];
		readonly byte[] _passLarge = new byte[4];
		readonly byte[] _passSmall = new byte[4];

		/// <summary>The routing, and the version of the XInput place table, the places each pad passes its force to were worked out from. Read and written only on the engine thread.</summary>
		DeviceRouting _passRouting;
		int _passVersion;

		/// <summary>Works out where each pad passes its force, when the routing or the XInput place table changed since the last time.</summary>
		/// <remarks>
		/// Neither changes on most passes, and then this is two compares. The routing is new when the settings,
		/// the game or the devices listed change, and the table when the places are read again after a device
		/// came or went. A new routing can carry new strengths for a pad already sending, worked out with it,
		/// so the pad is counted as arrived once, the way a new routing already re-merges the force it drives.
		///
		/// A place no pad passes to any more, its row unticked, its Pass through switched off or its controller
		/// gone elsewhere, is sent a stop once, through <see cref="_passRetry"/>. Nothing else would reach it, and
		/// its motors would go on at the last force sent.
		/// </remarks>
		/// <param name="routing">The routing of this pass.</param>
		/// <param name="version">The table's <see cref="XInputPlaces.Version"/>, read before the table.</param>
		/// <param name="places">The table, as <see cref="XInputPlaces.Current"/> holds it.</param>
		/// <returns>A bit for each pad with a place, when the places were worked out again; 0 on a pass where neither changed.</returns>
		public int ResolvePassPlaces(DeviceRouting routing, int version, IReadOnlyDictionary<string, int> places)
		{
			if (routing == _passRouting && version == _passVersion)
				return 0;
			_passRouting = routing;
			_passVersion = version;
			var arrived = 0;
			var passedTo = 0;
			for (var pad = 0; pad < _passPlaces.Length; pad++)
			{
				var place = routing.PassThroughPlace(pad, places, out _passSources[pad]);
				_passPlaces[pad] = place;
				if (place >= 0 && place < 4)
				{
					arrived |= 1 << pad;
					passedTo |= 1 << place;
				}
			}
			for (var place = 0; place < _passLarge.Length; place++)
			{
				if ((passedTo & (1 << place)) != 0)
					continue;
				_passLarge[place] = 0;
				_passSmall[place] = 0;
				if (_lastPassedForce[place] > 0)
					_passRetry |= 1 << place;
				else
					_passRetry &= ~(1 << place);
			}
			return arrived;
		}

		/// <summary>Keeps the newest force each pad's game asked for.</summary>
		/// <param name="feedbacks">What each pad's game asked for this pass, or null for nothing new; by pad index.</param>
		/// <param name="lastLarge">The large motor each pad's game asked for last; updated.</param>
		/// <param name="lastSmall">The small motor each pad's game asked for last; updated.</param>
		/// <returns>A bit for each pad whose game asked for something this pass.</returns>
		public static int KeepForces(Xbox360FeedbackReceivedEventArgs[] feedbacks, byte[] lastLarge, byte[] lastSmall)
		{
			var arrived = 0;
			for (var pad = 0; pad < feedbacks.Length && pad < lastLarge.Length; pad++)
			{
				var force = feedbacks[pad];
				if (force == null)
					continue;
				lastLarge[pad] = force.LargeMotor;
				lastSmall[pad] = force.SmallMotor;
				arrived |= 1 << pad;
			}
			return arrived;
		}

		/// <summary>The force for a device whose force comes from these pads: the strongest of each motor, each pad at what its game asked for last.</summary>
		/// <param name="pads">The pad indexes the force comes from.</param>
		/// <param name="arrived">A bit for each pad with something new this pass.</param>
		/// <param name="lastLarge">The large motor each pad's game asked for last.</param>
		/// <param name="lastSmall">The small motor each pad's game asked for last.</param>
		/// <param name="large">The large motor to play.</param>
		/// <param name="small">The small motor to play.</param>
		/// <returns>True when one of the pads has something new this pass, so the device is told.</returns>
		public static bool MergeForces(int[] pads, int arrived, byte[] lastLarge, byte[] lastSmall, out byte large, out byte small)
		{
			var changed = false;
			large = 0;
			small = 0;
			for (var i = 0; i < pads.Length; i++)
			{
				var pad = pads[i];
				if ((arrived & (1 << pad)) != 0)
					changed = true;
				if (lastLarge[pad] > large)
					large = lastLarge[pad];
				if (lastSmall[pad] > small)
					small = lastSmall[pad];
			}
			return changed;
		}

		/// <summary>The force to pass on to each XInput place: the strongest of each motor over the pads passing to it, each pad at what its game asked for last and at its own strengths.</summary>
		/// <param name="places">The place each pad passes its force to, 0 to 3, or -1 for none; by pad index.</param>
		/// <param name="sources">The row each pad passes its force by, whose strengths apply to it; by pad index.</param>
		/// <param name="arrived">A bit for each pad with something new this pass.</param>
		/// <param name="lastLarge">The large motor each pad's game asked for last.</param>
		/// <param name="lastSmall">The small motor each pad's game asked for last.</param>
		/// <param name="large">The large motor for each place; written for the places returned, and kept for the others.</param>
		/// <param name="small">The small motor for each place; written for the places returned, and kept for the others.</param>
		/// <returns>A bit for each place with something new to send. Two pads passing to one place are one force there.</returns>
		public static int MergePassedForces(int[] places, PassThroughSource[] sources, int arrived, byte[] lastLarge, byte[] lastSmall, byte[] large, byte[] small)
		{
			var changed = 0;
			for (var pad = 0; pad < places.Length; pad++)
				if (places[pad] >= 0 && places[pad] < 4 && (arrived & (1 << pad)) != 0)
					changed |= 1 << places[pad];
			// Nothing new for any place: what was sent last still stands, and the motors are still doing it.
			if (changed == 0)
				return 0;
			// Only the places worked out again: another keeps its force, which may still be waiting to be sent.
			for (var place = 0; place < large.Length; place++)
			{
				if ((changed & (1 << place)) == 0)
					continue;
				large[place] = 0;
				small[place] = 0;
			}
			for (var pad = 0; pad < places.Length; pad++)
			{
				var place = places[pad];
				if (place < 0 || place > 3 || (changed & (1 << place)) == 0)
					continue;
				// The strengths apply here as much as anywhere. Nothing further along applies them: a
				// controller's own motors are driven by the driver, which knows nothing of this program's settings. Worked
				// out with the routing, so applying them reads two numbers, takes no lock and makes nothing.
				var l = PadSetting.ApplyForceScale(lastLarge[pad], sources[pad].LargeScale);
				var s = PadSetting.ApplyForceScale(lastSmall[pad], sources[pad].SmallScale);
				if (l > large[place])
					large[place] = l;
				if (s > small[place])
					small[place] = s;
			}
			return changed;
		}

		/// <summary>Sends the force a game asked for on to a real controller, where that is wanted.</summary>
		/// <remarks>
		/// An emulated controller has no motors. A game rumbling one reaches nothing, and on an Xbox
		/// controller there is no other way in: its DirectInput face declares no force feedback at all,
		/// so the force feedback this program drives itself cannot reach it either.
		///
		/// Tabs passing their force to one place are merged there, the strongest of each motor.
		/// </remarks>
		/// <param name="routing">The routing of this pass.</param>
		/// <param name="arrived">A bit for each pad with something new this pass.</param>
		public void PassForcesThrough(DeviceRouting routing, int arrived)
		{
			// No tab passes its force on, none did when the places were last worked out, and nothing is waiting to
			// be sent: nothing to work out, merge or send. A routing that stops passing is worked out once, so the
			// places it passed to are sent a stop.
			if (!routing.PassesForceThrough && (_passRouting == null || !_passRouting.PassesForceThrough) && _passRetry == 0)
				return;
			// A pad whose place was just worked out again counts as arrived once, so a rebuild that changes its
			// stored strengths is sent at once, whether or not its game asks for anything new.
			arrived |= ResolvePassPlaces(routing, XInputPlaces.Version, XInputPlaces.Current);
			// What XInput was too busy to take last pass is sent again as it was worked out, with nothing worked out again.
			var places = MergePassedForces(_passPlaces, _passSources, arrived, _lastLargeMotor, _lastSmallMotor, _passLarge, _passSmall) | _passRetry;
			_passRetry = 0;
			for (var place = 0; place < 4; place++)
				if ((places & (1 << place)) != 0 && !PassForceTo(place, _passLarge[place], _passSmall[place], TimeSpan.Zero))
					_passRetry |= 1 << place;
		}

		/// <summary>A bit for each place whose force is worked out and not yet sent, so it is sent on the next pass: one XInput was too busy to take on the last pass, or the stop for a place no pad passes to any more. Read and written only on the engine thread, and cleared by <see cref="ResumeAfterDeviceRemoval"/> while it is suspended.</summary>
		int _passRetry;

		/// <summary>Sends one force to one place, and only when it differs from the last one sent.</summary>
		/// <param name="limit">How long to wait for Windows' XInput to be free. The input thread passes zero and never waits.</param>
		/// <returns>False when XInput was not free in time and nothing was sent.</returns>
		bool PassForceTo(int place, byte largeMotor, byte smallMotor, TimeSpan limit)
		{
			if (place < 0 || place > 3)
				return true;
			// Motors take the full range, and a byte of it is the top half of each. Multiplying by 257
			// spreads it back so that full means full rather than very nearly half.
			var left = (ushort)(largeMotor * 257);
			var right = (ushort)(smallMotor * 257);
			// Kept as the two bytes, 0 to 65535: never negative, so never the -1 of nothing sent, and above 0 for any force.
			var both = (largeMotor << 8) | smallMotor;
			if (_lastPassedForce[place] == both)
				return true;
			// Recorded only once sent. The input thread sends a refused force again on a later pass.
			if (SystemXInput.SetVibration(place, left, right, limit) == null)
				return false;
			_lastPassedForce[place] = both;
			return true;
		}

		/// <summary>Stops the motors of anything this program was driving through pass-through.</summary>
		/// <remarks>
		/// A controller left buzzing is one the person has to unplug. Nothing else stops it: the force
		/// was sent to the device itself, not to an emulated one that can simply be taken away.
		///
		/// Called by the workers that let go of every controller. Each waits for XInput no longer than it has to answer,
		/// and stops at the first place it could not reach, since the others would wait for the same read.
		/// </remarks>
		public void StopPassedForces()
		{
			var limit = TimeSpan.FromMilliseconds(XiAnswerMs);
			for (var place = 0; place < 4; place++)
				if (_lastPassedForce[place] > 0 && !PassForceTo(place, 0, 0, limit))
					break;
		}

		/// <summary>What each pad's game asked for since the last pass, by pad index, or null for nothing new. Reserved here and filled on every pass.</summary>
		readonly Xbox360FeedbackReceivedEventArgs[] _feedbacks = new Xbox360FeedbackReceivedEventArgs[4];

		/// <summary>Takes what each pad's game asked for since the last pass, and clears it so it is taken once.</summary>
		/// <remarks>
		/// The bus's notification thread and the Test sliders on the interface thread each write one reference. Each
		/// slot is taken and cleared in one atomic step, so nothing written between the two is lost, and no lock is
		/// shared with either writer. <see cref="DropGameForce"/> empties a pad's slot in the same step whenever its
		/// controller is taken away, so nothing asked for before is taken after.
		/// </remarks>
		/// <returns>The engine's own array, filled again on the next call. Read and written only on the engine thread.</returns>
		public Xbox360FeedbackReceivedEventArgs[] CopyAndClearFeedbacks()
		{
			var client = ViGEmClient.Current;
			for (var i = 0; i < _feedbacks.Length; i++)
				_feedbacks[i] = client == null ? null : System.Threading.Interlocked.Exchange(ref client.Feedbacks[i], null);
			return _feedbacks;
		}

		/// <summary>Drops what a pad's game asked for, once its controller is taken away, so none of it plays on.</summary>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <remarks>
		/// The rumble not yet taken is emptied in the one step the engine takes it with, so no lock is shared with its
		/// writers; left, the next pass would take it as new. The last force stops counting towards any device's force or any
		/// place it is passed on to, and those are told on the next pass. Every way a controller goes calls this: let go of,
		/// let go of after a report the bus refused, taken away from another tab's place, left to the bus when a new one takes
		/// its slot, gone with the client when the game leaves virtual emulation, and all of them before they are picked back
		/// up after a Repair, Remove Leftover Pads or Auto-Order. Runs on the input thread, or while it is suspended. Field
		/// writes only: nothing is made or waited for.
		/// </remarks>
		void DropGameForce(uint userIndex)
		{
			var client = ViGEmClient.Current;
			if (client != null)
				System.Threading.Interlocked.Exchange(ref client.Feedbacks[userIndex - 1], null);
			_lastLargeMotor[userIndex - 1] = 0;
			_lastSmallMotor[userIndex - 1] = 0;
			_forcesDropped |= 1 << (int)(userIndex - 1);
		}

		/// <summary>Asks for a force on one pad, as the Test sliders do: it arrives where a game's rumble arrives.</summary>
		public void SetVibration(MapTo userIndex, byte largeMotor, byte smallMotor, byte ledNumber)
		{
			// Read once: the engine can let the client go at any moment.
			var client = ViGEmClient.Current;
			if (client == null)
				return;
			var e = new Xbox360FeedbackReceivedEventArgs(largeMotor, smallMotor, ledNumber);
			System.Threading.Volatile.Write(ref client.Feedbacks[(int)userIndex - 1], e);
		}

		private void Controller_FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
		{
			// Read once: the engine can let the client go at any moment.
			var client = ViGEmClient.Current;
			var targets = client == null ? null : client.Targets;
			if (targets == null)
				return;
			var controller = (Xbox360Controller)sender;
			for (int i = 0; i < 4; i++)
			{
				if (targets[i] == controller)
				{
					// Add force feedback value for processing.
					System.Threading.Volatile.Write(ref client.Feedbacks[i], e);
					break;
				}
			}
		}

		/// <summary>A new controller for one pad, not yet on the bus, whose vibration arrives where a game's rumble is taken.</summary>
		Xbox360Controller NewTarget(ViGEmClient client)
		{
			var controller = new Xbox360Controller(client);
			controller.FeedbackReceived += Controller_FeedbackReceived;
			return controller;
		}

		bool?[] FeedingState = new bool?[4];

		/// <summary>The plug of each controller under way on a worker, or null.</summary>
		readonly System.Threading.Tasks.Task<VirtualError>[] _plugging = new System.Threading.Tasks.Task<VirtualError>[4];

		/// <summary>Starts plugging in the controller for one pad on a worker; a later pass takes in the answer.</summary>
		/// <remarks>
		/// A controller put somewhere else last time, or given no place at all, is made again only once a controller has come
		/// or gone, or it would be made and taken away every few seconds for nothing: each time a device change, and the whole
		/// device list read again. The places it was taken away with go to the worker whatever the attempts since came to,
		/// and the worker looks at the places as it does for a taken one, so the input thread never asks XInput.
		/// </remarks>
		public System.Threading.Tasks.Task<VirtualError> BeginPlug(uint userIndex)
		{
			var misplacedWith = _misplacedWith[userIndex - 1];
			// Set by the worker only when it holds the plug back, so a plug that ends any other way, or faults, is counted.
			_heldBack[userIndex - 1] = false;
			// Set by the worker only when it takes its controller away again.
			_takenAgain[userIndex - 1] = false;
			var plugging = System.Threading.Tasks.Task.Run(() => EnableFeeding(userIndex, misplacedWith));
			_plugging[userIndex - 1] = plugging;
			return plugging;
		}

		/// <summary>Whether a plug is under way on a worker; with <paramref name="unfinishedOnly"/>, only one that has not finished counts.</summary>
		/// <remarks>Asked on every pass while a controller is wanted and a plug runs, so it reads the reserved array and makes nothing.</remarks>
		bool PlugUnderWay(bool unfinishedOnly)
		{
			for (var i = 0; i < _plugging.Length; i++)
			{
				var plugging = _plugging[i];
				if (plugging != null && (!unfinishedOnly || !plugging.IsCompleted))
					return true;
			}
			return false;
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
			if (PlugUnderWay(true))
				return false;
			for (var i = 0; i < _plugging.Length; i++)
			{
				if (_plugging[i] == null)
					continue;
				PlugOutcome((uint)i + 1, _plugging[i]);
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

		/// <summary>The kind of exception, and the bus's answer, each controller's last faulted plug threw.</summary>
		/// <remarks>
		/// Reserved here, so nothing is made per attempt. Read and written through
		/// <see cref="ViGEmClient.NoteFault"/> in <see cref="PlugOutcome"/>, on the input thread, and by
		/// <see cref="Dispose(bool)"/> when the program closes, after the input thread is stopped. Cleared by
		/// <see cref="ForgetBusHealth"/>, on the same threads as <see cref="BusErrors"/>.
		/// </remarks>
		readonly Type[] PlugOutcomeFaults = new Type[4];
		readonly VIGEM_ERROR[] PlugOutcomeFaultCodes = new VIGEM_ERROR[4];

		/// <summary>What a finished plug came to, with a fault written to the log when it is news.</summary>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <param name="plugging">The finished plug.</param>
		/// <remarks>
		/// Every place a finished plug is let go of reads it through here. A faulted task nobody reads
		/// is raised again by the finalizer as an unobserved exception, and that reached the person as
		/// a crash report about a controller that was simply switched off.
		///
		/// A fault is written once per change for each controller, by the rule plugging in and letting go
		/// use. The plug gate tries again every few seconds, and the same fault written each time would bury
		/// the log. A plug that did not fault clears the record, so a fault after it is written again.
		///
		/// A plug that took its controller away, off the bus again from another tab's place or left to the bus when a
		/// new one took its slot, takes what its game asked for with it. That is dropped here, on the input thread or
		/// while it is suspended, and not by the worker.
		/// </remarks>
		public VirtualError PlugOutcome(uint userIndex, System.Threading.Tasks.Task<VirtualError> plugging)
		{
			if (_takenAgain[userIndex - 1])
				DropGameForce(userIndex);
			if (!plugging.IsFaulted)
			{
				ViGEmClient.NoteFault(PlugOutcomeFaults, PlugOutcomeFaultCodes, userIndex, null);
				return plugging.Result;
			}
			var fault = plugging.Exception.GetBaseException();
			if (ViGEmClient.NoteFault(PlugOutcomeFaults, PlugOutcomeFaultCodes, userIndex, fault))
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(fault);
			return VirtualError.Other;
		}

		/// <summary>When each controller may next be asked for a place, after a refusal.</summary>
		readonly int[] _NextPlugAttempt = new int[4];
		/// <summary>How long a refused controller waits before asking for a place again.</summary>
		public const int PlugRetryMs = 2000;

		/// <summary>The last failure the bus answered each controller with, from a plug or a refused report.</summary>
		/// <remarks>
		/// VIGEM_ERROR_NONE until one fails. Written by the plug worker and by the input thread, never both at
		/// once for one controller: reports are sent only while no plug of that controller is under way.
		/// Cleared by <see cref="ForgetBusHealth"/>, on the input thread when a game leaves virtual emulation
		/// and on the thread that calls <see cref="ResumeAfterDeviceRemoval"/> before the input thread
		/// resumes. Read without a lock by the Issues tab's <see cref="Issues.VirtualDriverNotWorkingIssue"/>,
		/// which names it.
		/// </remarks>
		public readonly VIGEM_ERROR[] BusErrors =
		{
			VIGEM_ERROR.VIGEM_ERROR_NONE, VIGEM_ERROR.VIGEM_ERROR_NONE,
			VIGEM_ERROR.VIGEM_ERROR_NONE, VIGEM_ERROR.VIGEM_ERROR_NONE,
		};

		/// <summary>The kind of exception, and the bus's answer, each controller's last failed send or refused report written to the log gave.</summary>
		/// <remarks>
		/// Read and written through <see cref="ViGEmClient.NoteFault(Type[], VIGEM_ERROR[], uint, Exception)"/>,
		/// so a failure is written when it differs from the last one for that controller, never on every try.
		/// Reserved here, so nothing is made per pass. Written by the input thread only, and cleared there by a
		/// report the bus takes. Cleared by <see cref="ForgetBusHealth"/>, on the same threads as <see cref="BusErrors"/>.
		/// </remarks>
		readonly Type[] FeedFaults = new Type[4];
		readonly VIGEM_ERROR[] FeedFaultCodes = new VIGEM_ERROR[4];

		/// <summary>Plugs of each controller in a row that failed while its own XInput place was free.</summary>
		/// <remarks>
		/// Written by the input thread when a plug is taken in; see <see cref="NextPlugFailures"/>. Cleared by
		/// <see cref="ForgetBusHealth"/>, on the same threads as <see cref="BusErrors"/>. Read without a lock
		/// by the Issues tab.
		/// </remarks>
		public readonly int[] PlugFailures = new int[4];

		/// <summary>Reports to each controller the bus refused in a row, each within <see cref="FeedDropWindowMs"/> of the one before.</summary>
		/// <remarks>
		/// Written by the input thread after <see cref="LastFeedDropTick"/>, so a reader that reads this first
		/// never pairs a new count with an old time. Cleared by <see cref="ForgetBusHealth"/>, on the same
		/// threads as <see cref="BusErrors"/>. Read without a lock by the Issues tab.
		/// </remarks>
		public readonly int[] FeedDrops = new int[4];

		/// <summary><see cref="Environment.TickCount"/> at each controller's last refused report.</summary>
		/// <remarks>Written by the input thread only, before <see cref="FeedDrops"/>. Read without a lock by the Issues tab, after the count.</remarks>
		public readonly int[] LastFeedDropTick = new int[4];

		/// <summary>How close together refused reports must come to count as one run.</summary>
		/// <remarks>
		/// A refused report unplugs the controller, and a worker plugs it in again in three to four
		/// seconds. A bus that keeps refusing therefore refuses about every five seconds while the pad
		/// is in use. Thirty seconds holds several of those cycles, so a short pause in play does not
		/// break the run. A single drop an hour after the last one, which is what a driver update
		/// does, starts a new run of one.
		/// </remarks>
		public const int FeedDropWindowMs = 30000;

		/// <summary>Plugs failed in a row in a free place, after one more plug has finished.</summary>
		/// <remarks>
		/// Only an attempt the bus took part in counts. A refusal (Other) and a controller the bus
		/// accepted but Windows never built (PlaceNotGiven) are the bus failing. A controller that
		/// arrived, even in the wrong place, is the bus working. A taken place or a missing client never
		/// reached the bus, so they leave the count as it was.
		/// </remarks>
		public static int NextPlugFailures(int failures, VirtualError result)
		{
			switch (result)
			{
				case VirtualError.None:
				case VirtualError.PlaceWrong:
					return 0;
				case VirtualError.Other:
				case VirtualError.PlaceNotGiven:
					return failures + 1;
				default:
					return failures;
			}
		}

		/// <summary>The run of refused reports after one more, given when the one before it came.</summary>
		public static int NextFeedDrops(int drops, int lastDropTick, int now)
		{
			return drops > 0 && IsRecent(lastDropTick, now, FeedDropWindowMs) ? drops + 1 : 1;
		}

		/// <summary>Forgets what the bus did to each controller, so what it does next is judged afresh.</summary>
		/// <remarks>
		/// That includes a controller held back for the places it was put elsewhere with or given none. Every hold the
		/// Issues tab reads then comes from an attempt made since, and Repair, which the Issues tab offers for one Windows
		/// never built, tries it again instead of leaving it held until a controller comes or goes.
		/// </remarks>
		void ForgetBusHealth()
		{
			for (var i = 0; i < 4; i++)
			{
				BusErrors[i] = VIGEM_ERROR.VIGEM_ERROR_NONE;
				FeedFaults[i] = null;
				PlugOutcomeFaults[i] = null;
				PlugFailures[i] = 0;
				FeedDrops[i] = 0;
				_misplacedWith[i] = -1;
			}
			// The client outlives a Repair, and so would its record of each failed plug and unplug.
			var client = ViGEmClient.Current;
			if (client != null)
				client.ForgetFaults();
		}

		/// <summary>Lets go of every controller, so Windows is able to remove one.</summary>
		/// <remarks>
		/// Windows will not remove a device that anything still holds open, and this program is the
		/// thing holding them: reading the states asks XInput for all four places, over and over. The
		/// removal was therefore refused every time, and each refusal left Windows needing a restart
		/// before it would finish building any new controller - so pressing Remove broke the very
		/// thing it was meant to repair, and said nothing.
		///
		/// The pass under way finishes first, and so does a plug under way. A plug left running would go on after
		/// everything was let go of: its next look at the places loads XInput again, which opens the controllers being
		/// switched off or removed, and its controller can arrive while the driver is repaired, or while Windows is
		/// meant to be forgetting the places before controllers are put in order.
		///
		/// The XInput library is let go of under <see cref="SharpDX.XInput.Controller.XInputLock"/>, which the display
		/// reader holds while XInput answers. A read that does not come back holds it until it does, so the wait has
		/// a limit. Past it nothing is let go of, the input thread goes on, and the person is told XInput is not
		/// answering.
		/// </remarks>
		/// <param name="limit">How long the pass under way, and then a plug under way, have to finish.</param>
		/// <param name="xinputLimit">How long to wait for the XInput library to be free.</param>
		/// <returns>False when the pass, a plug or XInput did not finish in time, and nothing was let go of.</returns>
		public bool ReleaseForDeviceRemoval(TimeSpan limit, TimeSpan xinputLimit)
		{
			Suspended = true;
			// Entered only to know the pass under way has finished, and left at once. The loop asks again inside it and
			// starts nothing more, so nothing needs to be held while the rest is let go of.
			if (!System.Threading.Monitor.TryEnter(PassLock, limit))
			{
				Suspended = false;
				return false;
			}
			System.Threading.Monitor.Exit(PassLock);
			// No new plug starts while the loop is suspended. One still running past the limit is waiting for XInput,
			// since only XInput can keep a plug that long, so the person is told XInput is not answering.
			WaitForPlugging(limit);
			if (PlugUnderWay(true))
			{
				Suspended = false;
				NoteReleaseRefused();
				return false;
			}
			// A controller passed force feedback is buzzing under its own power; taking an emulated one
			// away does not stop it, and nothing else will. Stopped once the pass has finished, so no pass sends it again.
			StopPassedForces();
			if (!EnterXInputLock(xinputLimit))
			{
				Suspended = false;
				NoteReleaseRefused();
				return false;
			}
			try
			{
				if (SharpDX.XInput.Controller.IsLoaded)
					SharpDX.XInput.Controller.FreeLibrary();
			}
			finally
			{
				ExitXInputLock();
			}
			_releaseRefused = false;
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			if (client != null && client.Targets != null)
				for (uint i = 1; i <= 4; i++)
					// Asked for unconditionally: letting go of one that is already gone is the outcome wanted,
					// and is no longer reported as a fault.
					client.UnPlug(i);
				// Nothing of ours holds a place now, so no note of one should outlive this.
				XInputPlaces.Forget();
				XInputPlaces.Invalidate();
			// The ones the bus would not let go of before are asked once more, so Windows can take them as well.
			if (client != null)
				client.UnPlugKept();
			return true;
		}

		/// <summary>What the person is told when the controllers could not be let go of because XInput did not answer.</summary>
		public const string XInputNotAnswering =
			"XInput is not answering, so the controllers could not be let go of. Try again in a moment.";

		/// <summary>Whether the last attempt to let go of the controllers gave up, so the log says so once until one works.</summary>
		bool _releaseRefused;

		/// <summary>Tells the window, and the log once per change, that XInput did not answer in time.</summary>
		/// <remarks>
		/// Said through <see cref="StatesRetrieved"/>, the way a display read that does not answer is said, so it
		/// reaches the window's header from whichever worker asked.
		/// </remarks>
		void NoteReleaseRefused()
		{
			if (!_releaseRefused)
			{
				_releaseRefused = true;
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(XInputNotAnswering,
					System.Diagnostics.EventLogEntryType.Warning);
			}
			var ev = StatesRetrieved;
			if (ev != null)
				ev(this, new DInputEventArgs(new Exception(XInputNotAnswering)));
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
			// The games went with their controllers, as when one is taken away: nothing each asked for counts any more,
			// including a rumble a game or the Test sliders asked for just before they went or while they were away. The
			// first pass then tells every device a game drove, and sends any stop that did not get through, and the next
			// read of the places sends no force that letting go stopped.
			for (uint pad = 1; pad <= 4; pad++)
				DropGameForce(pad);
			_passRetry = 0;
			// A bus just repaired, or controllers just removed, are judged on what happens from now on.
			// Written here while the input thread is still suspended, as the lines above are.
			ForgetBusHealth();
			UpdateDevicesEnabled = true;
			Suspended = false;
		}

		Gamepad[] oldGamepadStates = new Gamepad[4];

		/// <summary>Whether the guide button is held, by pad index. Reserved up front: one flag per pad, not one shared by all four.</summary>
		readonly bool[] IsGuideDown = new bool[4];

		/// <summary>Send the combined state to the virtual controller.</summary>
		/// <returns>False when the bus refused the report or it could not be sent; the caller lets the controller go.</returns>
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
			// A report is sent only when there is something new to say, which also keeps the bus from
			// being asked a thousand times a second a controller for nothing.
			if (!changed)
				return true;
			VIGEM_ERROR answer;
			try
			{
				answer = ViGEmClient.Current.Targets[i - 1].SendReport(ToReport(n));
			}
			catch (Exception ex)
			{
				// A send that cannot be made is a refusal with no answer. It is written once while the same
				// kind repeats, and the controller is let go of like any refusal, so the plug wait paces the
				// next try and the other controllers are still fed on this pass.
				if (ViGEmClient.NoteFault(FeedFaults, FeedFaultCodes, i, ex))
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
			if (!ReportTaken(BusErrors, FeedFaults, FeedFaultCodes, i, answer))
				return false;
			// Only the input thread sends reports, so the guide button each pad tracks needs no lock.
			var isGuidePressed = (n.Buttons & GamepadButtonFlags.Guide) != 0;
			var pressed = GuideChanged(IsGuideDown, i, isGuidePressed);
			if (pressed.HasValue)
			{
				var keys = GuideKeys(SettingsManager.Options.GuideButtonAction);
				if (keys.Length > 0)
				{
					if (pressed.Value)
						JocysCom.ClassLibrary.Processes.KeyboardHelper.SendDown(keys);
					else
						JocysCom.ClassLibrary.Processes.KeyboardHelper.SendUp(keys);
				}
			}
			// Update old state.
			oldGamepadStates[i - 1] = n;
			return true;
		}

		/// <summary>The report the bus takes for an XInput state.</summary>
		/// <remarks>
		/// The bus reads the buttons on the same bits as XInput, the guide button included, so the buttons
		/// are copied whole. A struct, built on the stack: nothing is made for the collector.
		/// </remarks>
		public static XUSB_REPORT ToReport(Gamepad state)
		{
			return new XUSB_REPORT
			{
				wButtons = unchecked((ushort)state.Buttons),
				bLeftTrigger = state.LeftTrigger,
				bRightTrigger = state.RightTrigger,
				sThumbLX = state.LeftThumbX,
				sThumbLY = state.LeftThumbY,
				sThumbRX = state.RightThumbX,
				sThumbRY = state.RightThumbY,
			};
		}

		/// <summary>Takes in what the bus answered a report, and says whether the report was taken.</summary>
		/// <param name="busErrors">The last failure the bus answered each controller with, by pad index. A refusal is written here, and a report taken leaves it.</param>
		/// <param name="faults">The kind of each controller's last failure written to the log, or null; by pad index.</param>
		/// <param name="faultCodes">The answer of each controller's last failure written to the log; by pad index.</param>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <param name="answer">What the bus answered.</param>
		/// <returns>True when the bus took the report.</returns>
		/// <remarks>
		/// Read on every changed report, so it compares numbers and writes one only when it is set. A refusal
		/// goes to the log by <see cref="ViGEmClient.NoteFault(Type[], VIGEM_ERROR[], uint, Type, VIGEM_ERROR)"/>'s
		/// rule: a bus that goes on refusing does so every few seconds while the controller is made again, and a
		/// line each time would bury the log. A report taken clears the log's record, so a refusal after it is
		/// written again, while the Issues tab keeps naming the last one. The caller lets the controller go, and
		/// the plug wait decides when it is made again. A controller that went away is logged as information;
		/// any other refusal is a warning.
		/// </remarks>
		public static bool ReportTaken(VIGEM_ERROR[] busErrors, Type[] faults, VIGEM_ERROR[] faultCodes, uint userIndex, VIGEM_ERROR answer)
		{
			var meaning = BusAnswers.Of(answer);
			if (meaning == BusAnswer.Fine)
			{
				if (faults[userIndex - 1] != null)
					ViGEmClient.NoteFault(faults, faultCodes, userIndex, null);
				return true;
			}
			busErrors[userIndex - 1] = answer;
			if (ViGEmClient.NoteFault(faults, faultCodes, userIndex, typeof(ViGEmException), answer))
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(
					"Virtual controller " + userIndex + ": the bus refused its report: " + BusAnswers.Name(answer) + ".",
					meaning == BusAnswer.Gone
						? System.Diagnostics.EventLogEntryType.Information
						: System.Diagnostics.EventLogEntryType.Warning);
			return false;
		}

		/// <summary>Whether the guide button freshly changed for one pad, updating that pad's held flag to match.</summary>
		/// <param name="isGuideDown">Each pad's held state, by pad index; the pad's own entry is written here.</param>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <param name="isGuidePressed">Whether the guide bit is set in this report.</param>
		/// <returns>True when freshly pressed, false when freshly released, null when the pad's held state did not change.</returns>
		public static bool? GuideChanged(bool[] isGuideDown, uint userIndex, bool isGuidePressed)
		{
			var i = userIndex - 1;
			if (isGuidePressed && !isGuideDown[i])
			{
				isGuideDown[i] = true;
				return true;
			}
			if (!isGuidePressed && isGuideDown[i])
			{
				isGuideDown[i] = false;
				return false;
			}
			return null;
		}

		/// <summary>The keys the guide button presses, read from the option's text once per change of it.</summary>
		/// <remarks>
		/// Asked on every press and release, on the input thread. The same text answers the same array, so a press
		/// makes nothing; a changed text is read once.
		/// </remarks>
		/// <param name="text">The option, such as "{7}{LWin}", or null.</param>
		public static Keys[] GuideKeys(string text)
		{
			if (!ReferenceEquals(text, _guideKeysText))
			{
				_guideKeys = ParseGuideKeys(text);
				_guideKeysText = text;
			}
			return _guideKeys;
		}

		static string _guideKeysText;
		static Keys[] _guideKeys = new Keys[0];

		/// <summary>Reads keys written as numbers or names in braces.</summary>
		static Keys[] ParseGuideKeys(string text)
		{
			var list = new List<Keys>();
			if (string.IsNullOrEmpty(text))
				return list.ToArray();
			var matches = rxKeys.Matches(text);
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
		public void RememberOurPlace(uint userIndex, int place, string hardwareId)
		{
			XiPlaceForPad[userIndex - 1] = place;
			OurHardware[userIndex - 1] = hardwareId;
			XInputPlaces.Remember(hardwareId, place);
			XInputPlaces.Invalidate();
		}

		/// <summary>Which places Windows reports a controller in, or null when XInput did not answer in <see cref="XiAnswerMs"/>.</summary>
		/// <remarks>For reads outside any wait of their own: the place table's, and the last look after a wait for a change.</remarks>
		public static bool[] OccupiedPlaces()
		{
			// Not asked while the loop is suspended: controllers are being put in order, the library has been let go of, and a
			// read would load it again and open every controller.
			var helper = Global.DHelper;
			if (helper != null && helper.Suspended)
				return null;
			return OccupiedPlaces(TimeSpan.FromMilliseconds(XiAnswerMs));
		}

		/// <summary>Which places Windows reports a controller in, or null when XInput did not answer within <paramref name="limit"/>.</summary>
		/// <param name="limit">How long XInput has to answer: what is left of the caller's own wait. Less than zero counts as zero.</param>
		/// <remarks>
		/// Asked through <see cref="SystemXInput.ReadPlaces"/>, so nobody waits past the limit. XInput stops answering while
		/// Windows builds a controller, often for longer than a second, so a worker waiting for one to arrive gives XInput
		/// all the time its wait has left, as a direct call into XInput would take. That it did not answer is written to the
		/// log once, and again only after it has answered. Asked by the plug worker, the reorder runner and the place table's
		/// reads, never by the input thread.
		/// </remarks>
		public static bool[] OccupiedPlaces(TimeSpan limit)
		{
			return OccupiedPlaces(limit, null);
		}

		/// <summary>The same, and fills <paramref name="products"/> with the product number of the controller in each place, 0 where there is none or it is not known.</summary>
		public static bool[] OccupiedPlaces(TimeSpan limit, ushort[] products)
		{
			var places = new bool[4];
			if (SystemXInput.ReadPlaces(places, products, limit > TimeSpan.Zero ? limit : TimeSpan.Zero))
			{
				_placesNotAnswering = false;
				return places;
			}
			if (!_placesNotAnswering)
			{
				_placesNotAnswering = true;
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(PlacesNotAnswering,
					System.Diagnostics.EventLogEntryType.Warning);
			}
			return null;
		}

		/// <summary>What the log says when XInput does not say which places are taken.</summary>
		public const string PlacesNotAnswering =
			"XInput is not answering, so which places are taken is not known. Controllers are made once it answers.";

		/// <summary>Whether XInput last did not say which places are taken, so the log says so once until it answers.</summary>
		static volatile bool _placesNotAnswering;

		/// <summary>How long a plug waits for XInput: for the places before it asks the bus, and for the new controller's place after.</summary>
		/// <remarks>Five seconds; see <see cref="WaitForPlace"/>. The plug runs on a worker, so the input thread never waits for it.</remarks>
		static readonly TimeSpan PlugWindow = TimeSpan.FromSeconds(5);

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
		///
		/// Each read is given what is left of the five seconds. XInput stops answering while Windows builds the controller,
		/// and an answer that comes back between two reads is not used: the next read asks XInput again. Reads given a
		/// second each can therefore miss, one after another, a place that has arrived.
		/// </remarks>
		/// <param name="answered">Whether XInput answered any read in the wait, so a place that never came is told from one nobody could see.</param>
		static int WaitForPlace(bool[] before, out bool answered)
		{
			answered = false;
			var until = DateTime.UtcNow + PlugWindow;
			while (DateTime.UtcNow < until)
			{
				// A read XInput does not answer is waited out like a place not yet given, until the time is up.
				var now = OccupiedPlaces(until - DateTime.UtcNow);
				if (now != null)
				{
					answered = true;
					for (var i = 0; i < 4; i++)
						if (!before[i] && now[i])
							return i;
				}
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

		/// <param name="misplacedWith">The places taken, one bit each, when this controller was last put somewhere else or given no place, or -1. While they are the same, nothing is made.</param>
		public VirtualError EnableFeeding(uint userIndex, int misplacedWith = -1)
		{
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			var client = BusClient(true);
			if (client == null)
				return VirtualError.Missing;
			if (!client.isControllerExists(userIndex))
				return VirtualError.Other;
			var own = (int)userIndex - 1;
			// Put somewhere else or given no place last time: held back until a controller has come or gone. Asked before
			// whether the bus still holds it, since one the bus would not let go of reads as connected while in another tab's
			// place. One the bus would not let go of is not held back: it goes on to be kept below, and the pad says so.
			bool[] before = null;
			if (misplacedWith >= 0)
			{
				// XInput not answering says nothing about the places, so the hold stays for the next attempt.
				before = OccupiedPlaces(PlugWindow);
				if (before == null)
					return VirtualError.NotAnswering;
				if (PlacesMask(before) == misplacedWith && !client.RemovalRefused(userIndex))
				{
					// Said as it was when the controller was taken away, and not counted as a plug the bus failed.
					_heldBack[own] = true;
					return _heldAs[own];
				}
				// A controller has come or gone since: the hold is over, and whatever this attempt comes to says why.
				_misplacedWith[own] = -1;
			}
			if (client.IsControllerConnected(userIndex))
			{
				// On the bus already, so there is nothing to plug in.
				if (!client.RemovalRefused(userIndex))
				{
					_misplacedWith[own] = -1;
					return VirtualError.None;
				}
				// On the bus only because the bus would not let it go. One such controller is kept for each pad; while
				// this pad has one, no other is made for it, and this one stays in its slot, not fed.
				if (client.HasKept(userIndex))
					return VirtualError.RemovalRefused;
				// Found attached, it would be fed, refused and asked to go again for as long as the game runs. A new
				// controller takes its slot and is plugged in below: at most once per pad while the bus holds the one it
				// replaced, on the thread that plugs, never on the input thread.
				client.Replace(userIndex, NewTarget(client));
				// Its game's rumbles, its stop among them, no longer reach the engine, so what that game asked for goes
				// too, dropped when this plug is taken in, whatever the plug comes to.
				_takenAgain[own] = true;
			}
			// Controller N is XInput N and nothing else. Made while its place was taken, it landed in the
			// next tab's place and pushed that one along too, so one real controller in the way broke
			// every tab instead of one. It waits for its own place instead, and asking again is a look at
			// the four places, answered before the device tree is read, not a controller made and taken away.
			// XInput is silent for seconds while Windows builds a controller that has just arrived, so this look is given
			// the plug window, as the wait for the new controller's place is.
			if (before == null)
				before = OccupiedPlaces(PlugWindow);
			// Not known while XInput does not answer. Nothing is made, and the next gated attempt asks again.
			if (before == null)
				return VirtualError.NotAnswering;
			// Most likely held by this pad's own kept controller, which only Repair or a restart takes away.
			if (before[own])
				return client.HasKept(userIndex) ? VirtualError.RemovalRefused : VirtualError.PlaceTaken;
			// Which controllers are on the bus before we ask for one. The one that is there afterwards and
			// was not before is ours, which is the only way of knowing that does not rest on reading a
			// number off a name and hoping it means what it looks like.
			var padsBefore = XInputPlaces.VirtualHardwareNow();
			VIGEM_ERROR plugError;
			if (!client.PlugIn(userIndex, out plugError))
			{
				// What the bus said, so the Issues tab can name it. A refusal without an answer leaves the
				// last one standing rather than wiping it.
				if (plugError != VIGEM_ERROR.VIGEM_ERROR_NONE)
					BusErrors[own] = plugError;
				return VirtualError.Other;
			}
			// Where it went, rather than where it was asked to go. The bus says yes when it accepts a
			// controller, which is not the same as Windows having given it the place we need.
			bool answered;
			var place = WaitForPlace(before, out answered);
			// Kept only in its own place. Windows cannot be asked for one, and anywhere else it holds
			// another tab's place. The places it saw are kept, so it is not tried again until they change.
			if (place != own)
			{
				// Once the bus lets it go, what its game asked for there goes too, dropped when this plug is taken in.
				// Only ever set here, never cleared, so a controller replaced above still has its game's force dropped.
				_takenAgain[own] |= client.UnPlug(userIndex);
				_misplacedWith[own] = PlacesMask(before);
				MisplacedIn[own] = place;
				// XInput said nothing for the whole wait, so where it went is not known and the bus is not to blame: said as
				// XInput not answering. Held back all the same: made again at once, it would be made and taken away for as
				// long as Windows keeps XInput silent over each new controller. Each held attempt says what this one found.
				_heldAs[own] = !answered ? VirtualError.NotAnswering : place < 0 ? VirtualError.PlaceNotGiven : VirtualError.PlaceWrong;
				return _heldAs[own];
			}
			// Written down now, while it is certain. Nothing reports where a controller was put,
			// so the only moment the answer exists is the moment it arrives.
			var appeared = XInputPlaces.VirtualHardwareNow();
			appeared.ExceptWith(padsBefore);
			// Exactly one should have appeared. None means Windows has not finished building it yet, and
			// more than one means somebody else made theirs in the same moment - neither can be claimed.
			RememberOurPlace(userIndex, place, appeared.Count == 1 ? appeared.First() : null);
			_misplacedWith[own] = -1;
			return VirtualError.None;
		}

		/// <summary>Plugs this pad's controller in while controllers are put in order, without asking XInput where it went.</summary>
		/// <remarks>
		/// Where it lands was worked out beforehand and is checked once, when the order is made: asked about in between, XInput
		/// would be loaded again while real controllers are being switched off and on. Nothing is kept about it here.
		/// </remarks>
		public VirtualError PlugForOrder(uint userIndex)
		{
			if (userIndex < 1 || userIndex > 4)
				return VirtualError.Index;
			var client = BusClient(true);
			if (client == null)
				return VirtualError.Missing;
			if (!client.isControllerExists(userIndex))
				return VirtualError.Other;
			// Let go of when the order began, so one still on the bus is one the bus would not let go of.
			if (client.IsControllerConnected(userIndex))
				return VirtualError.RemovalRefused;
			VIGEM_ERROR plugError;
			if (client.PlugIn(userIndex, out plugError))
				return VirtualError.None;
			if (plugError != VIGEM_ERROR.VIGEM_ERROR_NONE)
				BusErrors[userIndex - 1] = plugError;
			return VirtualError.Other;
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
				// Its game is gone with it.
				DropGameForce(userIndex);
			}
			return success
				? VirtualError.None
				: VirtualError.Other;
		}

	}
}
