using JocysCom.ClassLibrary.Controls.IssuesControl;
using JocysCom.ClassLibrary.IO;
using Nefarius.ViGEm.Client.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using x360ce.App;
using x360ce.App.DInput;
using x360ce.App.Issues;
using System.Linq;

namespace Nefarius.ViGEm.Client
{

	[SuppressUnmanagedCodeSecurity]
	partial class ViGEmClient
	{

		public ViGEmClient(out VIGEM_ERROR error)
		{
			try
			{
				NativeHandle = NativeMethods.vigem_alloc();
				error = NativeMethods.vigem_connect(NativeHandle);
			}
			catch (DllNotFoundException ex)
			{
				// System.DllNotFoundException:
				// Unable to load DLL 'vigemclient.dll':
				// The specified module could not be found.
				// (Exception from HRESULT: 0x8007007E)
				if (ex.HResult == unchecked((int)0x8007007E))
				{
					// Probably the "Microsoft Visual C++ v14 Redistributable" (Visual Studio 2017-2026) is missing.
					// Latest supported downloads:
					// https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist
					// 32-bit: https://aka.ms/vc14/vc_redist.x86.exe
					// 64-bit: https://aka.ms/vc14/vc_redist.x64.exe
				}
				throw;
			}
			catch (Exception)
			{
				throw;
			}
		}


		/// <summary>Bus numbers this program has put on the bus, connected now or not.</summary>
		/// <remarks>
		/// Asking the bus what is connected right now answers a different question. A controller is
		/// ours from the moment we create it and stays ours while Windows is still taking it away after
		/// we have let go. In the gap between those two moments the bus says no and Windows says yes,
		/// and reading that gap as somebody else's leftover made the program offer to remove the very
		/// controller it had just made.
		/// </remarks>
		static readonly HashSet<uint> UsedSerialSet = new HashSet<uint>();

		/// <summary>Copy of the bus numbers this program has used.</summary>
		public static uint[] UsedSerials
		{
			get { lock (UsedSerialSet) return UsedSerialSet.ToArray(); }
		}

		/// <summary>Records a controller as ours, while its number can still be read.</summary>
		/// <remarks>
		/// The number comes from the handle the bus gives out, so it is readable only while connected.
		/// Asked for after the controller has gone it answers nothing, which is why it is taken here
		/// rather than when the question is later asked.
		/// </remarks>
		static void RememberSerial(ViGEmTarget target)
		{
			if (target == null)
				return;
			var serial = target.Serial;
			if (serial == 0)
				return;
			lock (UsedSerialSet)
				UsedSerialSet.Add(serial);
		}

		public Xbox360Controller[] Targets;
		public Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[] Feedbacks = new Targets.Xbox360.Xbox360FeedbackReceivedEventArgs[4];
		/// <summary>How many places for controllers of this kind Windows offers.</summary>
		public const int PlaceCount = 4;

		public bool isControllerExists(uint userIndex)
		{
			// Not properly implemented yet.
			var t = Targets;
			return (t != null && (userIndex - 1) < t.Length && t[userIndex - 1] != null);
		}

		public bool UnPlug(uint i)
		{
			// Not properly implemented yet.
			// Disposing itself lets every controller go through here, so only a freed handle says no.
			var t = Targets;
			if (IsDisposed || t == null || !IsValidIndex(i) || i > t.Length)
				return false;
			try
			{
				t[i - 1].Disconnect();
			}
			catch (ViGEmException ex) when (BusAnswers.Of(ex.Code) == BusAnswer.Gone)
			{
				// Being told it is already gone, unplugged or taken with the bus, is the thing that was
				// wanted, not a failure. The bus drops a controller by itself when the program that made it
				// ends or the driver changes, so asking whether one is still there before letting go answers
				// about a moment that has passed by the time the answer arrives. Guarding the call was the
				// old approach and it still lost that race.
				//
				// Reported as a fault, this filled the support mailbox with wishes already granted:
				// fifteen of twenty-nine reports over two days said nothing else.
			}
			catch (Exception ex)
			{
				// Written when it is news. A controller the bus will not let go of is asked again at every
				// retry, and the same answer written each time would bury the log.
				if (NoteFault(_unplugFaultTypes, _unplugFaultCodes, i, ex))
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				// The bus is asked what happened rather than told; a failed disconnect leaves the
				// controller in whatever state it is really in, and the next pass reads that.
				return false;
			}
			NoteFault(_unplugFaultTypes, _unplugFaultCodes, i, null);
			// Off the bus, so it no longer goes without vibration. The logged refusal stays: the same answer
			// on the next plug is not news.
			RumbleErrors[i - 1] = VIGEM_ERROR.VIGEM_ERROR_NONE;
			return true;
		}

		/// <summary>Whether the bus refused to let go of this pad's controller the last time one was asked to go, and nothing has cleared it since.</summary>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <remarks>
		/// Read from the unplug record <see cref="NoteFault(Type[], VIGEM_ERROR[], uint, Exception)"/> keeps: set by a
		/// refused unplug; cleared by one that works, by a plug while the pad keeps no controller (<see cref="HasKept"/>),
		/// and by <see cref="ForgetFaults"/>, as after a Repair. A controller the bus still holds reaches the plug worker
		/// only after it was asked to go, so when the plug worker reads this it is about the controller in the slot.
		/// </remarks>
		public bool RemovalRefused(uint userIndex)
		{
			return IsValidIndex(userIndex) && _unplugFaultTypes[userIndex - 1] != null;
		}

		/// <summary>The controller the bus would not let go of, taken out of each pad's slot; null for none. One per pad.</summary>
		/// <remarks>
		/// Kept, not freed, while the bus holds it: the bus library can still use it and the vibration handler it was given,
		/// and a freed controller would be used after it was gone. One per pad: while a pad has one, no other is made for
		/// it, so a bus that lets nothing go holds at most one more controller for each pad. Written by the thread that
		/// plugs in, and cleared by <see cref="UnPlugKept"/> once the bus lets it go. Letting go of the client closes its
		/// connection to the bus, which takes away everything the bus still holds for it.
		/// </remarks>
		readonly Xbox360Controller[] _keptTargets = new Xbox360Controller[PlaceCount];

		/// <summary>Whether this pad already has a controller the bus would not let go of.</summary>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		public bool HasKept(uint userIndex)
		{
			return IsValidIndex(userIndex) && System.Threading.Volatile.Read(ref _keptTargets[userIndex - 1]) != null;
		}

		/// <summary>Puts a new controller in this pad's slot in place of one the bus would not let go of, which is kept.</summary>
		/// <param name="userIndex">The pad, 1 to 4, which has none kept yet: see <see cref="HasKept"/>.</param>
		/// <param name="fresh">The new controller, not yet on the bus.</param>
		public void Replace(uint userIndex, Xbox360Controller fresh)
		{
			var t = Targets;
			System.Threading.Volatile.Write(ref _keptTargets[userIndex - 1], t[userIndex - 1]);
			t[userIndex - 1] = fresh;
		}

		/// <summary>The kind of exception, and the bus's answer, each controller's last failed unplug gave.</summary>
		/// <remarks>Reserved with the client, so nothing is made per attempt. Written only through <see cref="NoteFault"/>, and read there and by <see cref="RemovalRefused"/>.</remarks>
		readonly Type[] _unplugFaultTypes = new Type[PlaceCount];
		readonly VIGEM_ERROR[] _unplugFaultCodes = new VIGEM_ERROR[PlaceCount];

		/// <summary>The kind of exception, and the bus's answer, each controller's last failed plug gave.</summary>
		/// <remarks>Reserved with the client, so nothing is made per attempt. Read and written only through <see cref="NoteFault"/>.</remarks>
		readonly Type[] _plugFaultTypes = new Type[PlaceCount];
		readonly VIGEM_ERROR[] _plugFaultCodes = new VIGEM_ERROR[PlaceCount];

		/// <summary>What the bus answered each controller's registration for vibration, for the Issues tab; by pad index.</summary>
		/// <remarks>
		/// VIGEM_ERROR_NONE while the controller takes vibration or is not on the bus. Set by
		/// <see cref="PlugIn"/> through <see cref="NoteRumble"/>, and cleared by a plug that fails, by
		/// <see cref="UnPlug"/> and by <see cref="ForgetFaults"/>. Written by the plug worker, and by the input
		/// thread or a Repair's worker when they let a controller go or forget what the bus did; never two at
		/// once for one controller, except a Repair that begins while a plug is under way. Read without a lock
		/// by <see cref="VirtualDriverNotWorkingIssue"/> on the Issues panel's worker, one whole number per
		/// controller.
		/// </remarks>
		public readonly VIGEM_ERROR[] RumbleErrors =
		{
			VIGEM_ERROR.VIGEM_ERROR_NONE, VIGEM_ERROR.VIGEM_ERROR_NONE,
			VIGEM_ERROR.VIGEM_ERROR_NONE, VIGEM_ERROR.VIGEM_ERROR_NONE,
		};
		/// <summary>The kind of exception, and the bus's answer, each controller's last refused vibration written to the log gave.</summary>
		/// <remarks>Reserved with the client, so nothing is made per attempt. Read and written only through <see cref="NoteFault"/>.</remarks>
		readonly Type[] _rumbleFaultTypes = new Type[PlaceCount];
		readonly VIGEM_ERROR[] _rumbleFaultCodes = new VIGEM_ERROR[PlaceCount];

		/// <summary>Records how plugging in or letting go of a controller went, and says whether a failure is news.</summary>
		/// <param name="types">The kind of exception each controller's last failure in this direction threw, or null; by pad index.</param>
		/// <param name="codes">The bus's answer to each controller's last failure in this direction; by pad index.</param>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <param name="fault">What was thrown, or null when it worked.</param>
		/// <returns>True when the fault differs, in kind or in answer, from the last one recorded for the pad.</returns>
		/// <remarks>
		/// A failure is written once per change. The plug gate asks again every few seconds, and the same
		/// answer written each time would bury the log. Success clears the record, so a failure after it is
		/// written again. Plugging in and letting go each keep their own record. Two array reads and a
		/// compare: nothing is made.
		/// </remarks>
		public static bool NoteFault(Type[] types, VIGEM_ERROR[] codes, uint userIndex, Exception fault)
		{
			if (fault == null)
			{
				types[userIndex - 1] = null;
				return false;
			}
			var vex = fault as ViGEmException;
			return NoteFault(types, codes, userIndex, fault.GetType(), vex == null ? VIGEM_ERROR.VIGEM_ERROR_NONE : vex.Code);
		}

		/// <summary>Records a failure by its kind and the bus's answer, and says whether it is news, by the rule of <see cref="NoteFault(Type[], VIGEM_ERROR[], uint, Exception)"/>.</summary>
		/// <remarks>For an answer the bus returned rather than threw, so nothing is made to record it.</remarks>
		public static bool NoteFault(Type[] types, VIGEM_ERROR[] codes, uint userIndex, Type type, VIGEM_ERROR code)
		{
			var i = userIndex - 1;
			if (types[i] == type && codes[i] == code)
				return false;
			types[i] = type;
			codes[i] = code;
			return true;
		}

		/// <summary>Keeps what the bus answered a controller's registration for vibration, and writes a refusal to the log when it is news.</summary>
		/// <param name="current">What each controller's registration answered, for the Issues tab; by pad index. Set to the answer.</param>
		/// <param name="types">The kind of each controller's last refusal written to the log, or null; by pad index.</param>
		/// <param name="codes">The answer of each controller's last refusal written to the log; by pad index.</param>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <param name="answer">What the bus answered the registration.</param>
		/// <remarks>
		/// A controller whose vibration is refused is kept: buttons, sticks and triggers reach the game, and
		/// only the game's vibration does not reach the controller.
		///
		/// The two records last differently. The Issues tab's value follows the controller, and letting it go
		/// clears only that. The log's record follows the answer, by <see cref="NoteFault"/>'s rule: the program
		/// makes a controller again by itself every few seconds when Windows does not build it or the bus
		/// refuses its reports, and the same refusal each time is written once. Vibration that works, or a
		/// Repair, clears it. Runs once per plug, on the plug worker, never on the engine path.
		/// </remarks>
		public static void NoteRumble(VIGEM_ERROR[] current, Type[] types, VIGEM_ERROR[] codes, uint userIndex, VIGEM_ERROR answer)
		{
			current[userIndex - 1] = answer;
			var refusal = BusAnswers.Of(answer) == BusAnswer.Fine ? null : new ViGEmException(answer);
			if (NoteFault(types, codes, userIndex, refusal))
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(
					"Virtual controller " + userIndex + " works, but the bus refused its vibration: " +
					BusAnswers.Name(answer) + ".",
					System.Diagnostics.EventLogEntryType.Warning);
		}

		/// <summary>Forgets each controller's last plug and unplug failure and its refused vibration, so the next one is written.</summary>
		/// <remarks>Called when what the bus did is forgotten, as after a Repair, which leaves this client in place.</remarks>
		public void ForgetFaults()
		{
			for (uint pad = 1; pad <= PlaceCount; pad++)
			{
				NoteFault(_plugFaultTypes, _plugFaultCodes, pad, null);
				NoteFault(_unplugFaultTypes, _unplugFaultCodes, pad, null);
				NoteFault(_rumbleFaultTypes, _rumbleFaultCodes, pad, null);
				RumbleErrors[pad - 1] = VIGEM_ERROR.VIGEM_ERROR_NONE;
			}
		}

		/// <summary>Connects the controller for this pad, and only that one.</summary>
		/// <param name="userIndex">The pad, 1 to 4.</param>
		/// <param name="error">What the bus answered when it refused: its code, or VIGEM_ERROR_NONE when it
		/// took the controller, was never asked, or failed without giving a code.</param>
		/// <remarks>
		/// Windows gives out the XInput place, and cannot be asked for one. The places below used to be
		/// filled with brief controllers first, so this one would land above them. Those took places that
		/// belong to other tabs: with a real controller in the first place, the brief one took the second,
		/// this one landed in the third, and the second was left empty. Some were also left behind,
		/// holding a place until the program ended. Where this one lands is checked by the caller, which
		/// keeps it only in its own place.
		/// </remarks>
		public bool PlugIn(uint userIndex, out VIGEM_ERROR error)
		{
			error = VIGEM_ERROR.VIGEM_ERROR_NONE;
			// A client being let go of has no bus behind it any more. Its native handle is freed, and a
			// worker that took this client seconds ago, before the game left virtual mode, must be told
			// no rather than sent into a handle that no longer exists.
			var t = Targets;
			if (Disposing || IsDisposed || t == null || !IsValidIndex(userIndex) || userIndex > t.Length)
				return false;
			try
			{
				// Throws only when the bus refuses the controller itself, and then nothing is on the bus. A
				// controller that is on the bus comes back from here and is remembered as ours at once.
				t[userIndex - 1].Connect();
				RememberSerial(t[userIndex - 1]);
				// A new controller: a failure to plug it in or let go of it later is news, whatever the last one said. Not
				// while the bus holds one of this pad's it would not let go of: that refusal was written, and the same
				// answer about the one made in its place is not news.
				NoteFault(_plugFaultTypes, _plugFaultCodes, userIndex, null);
				if (!HasKept(userIndex))
					NoteFault(_unplugFaultTypes, _unplugFaultCodes, userIndex, null);
				// Kept and working when its vibration is refused, and the Issues tab says so.
				NoteRumble(RumbleErrors, _rumbleFaultTypes, _rumbleFaultCodes, userIndex, t[userIndex - 1].RumbleError);
				return true;
			}
			catch (Exception ex)
			{
				// A refused add leaves nothing on the bus, so this pad has no controller that goes without vibration.
				RumbleErrors[userIndex - 1] = VIGEM_ERROR.VIGEM_ERROR_NONE;
				// Handed back, so the Issues tab can name what the bus said rather than only that it said no.
				var vex = ex as ViGEmException;
				if (vex != null)
					error = vex.Code;
				// Written when it is news. A bus that refuses is asked again at every retry of the plug gate.
				if (NoteFault(_plugFaultTypes, _plugFaultCodes, userIndex, ex))
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				return false;
			}
		}

		public void UnplugAllControllers()
		{
			for (uint i = 1; i <= 4; i++)
			{
				// Asked for unconditionally. Whether one is connected is answered about a moment that has
				// passed by the time it is acted on, and letting go of one that is already gone is no longer
				// treated as a fault.
				UnPlug(i);
			}
			// And each one the bus would not let go of before, so none is left on the bus.
			UnPlugKept();
		}

		/// <summary>Asks the bus once more to let go of each pad's kept controller, and forgets each one it lets go of.</summary>
		/// <remarks>
		/// Called after the four slots are let go of: by Repair, Remove and Auto-Order before Windows is asked to change
		/// anything, and when the client is let go of. One try each, so a bus that still refuses is asked once per attempt,
		/// and its refusal is written once per change by the unplug record. One the bus lets go of, or has already, is
		/// dropped, and its native part is freed when it is collected.
		/// </remarks>
		public void UnPlugKept()
		{
			if (IsDisposed)
				return;
			for (uint i = 1; i <= PlaceCount; i++)
			{
				var kept = System.Threading.Volatile.Read(ref _keptTargets[i - 1]);
				if (kept == null)
					continue;
				try
				{
					kept.Disconnect();
				}
				catch (ViGEmException ex) when (BusAnswers.Of(ex.Code) == BusAnswer.Gone)
				{
					// Gone already, which is what was asked for.
				}
				catch (Exception ex)
				{
					if (NoteFault(_unplugFaultTypes, _unplugFaultCodes, i, ex))
						JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
					continue;
				}
				// Only the one asked about: a plug worker can keep another here meanwhile.
				System.Threading.Interlocked.CompareExchange(ref _keptTargets[i - 1], null, kept);
			}
		}

		/// <summary>Whether the bus is holding the controller in this place.</summary>
		/// <remarks>
		/// This used to answer from a flag the program set when it plugged one in. A flag cannot know
		/// that the controller was taken away afterwards, so it went on saying yes: the tab showed a
		/// green light for a controller that no longer existed, and the update loop skipped making a
		/// new one because it read the same flag and believed there already was one. Two faults, one
		/// cause, and the light was the part that lied.
		/// </remarks>
		public bool IsControllerConnected(uint i)
		{
			if (!IsValidIndex(i))
				return false;
			var t = Targets;
			if (t == null || i - 1 >= t.Length)
				return false;
			var target = t[i - 1];
			return target != null && target.IsAttached;
		}

		/// <summary>Controller positions are 1-4. Index outside the range must not throw.</summary>
		bool IsValidIndex(uint i)
			=> i >= 1 && i <= PlaceCount;

		#region Static Members

		public static volatile ViGEmClient Current;
		public static object ClientLock = new object();

		/// <summary>How long a refused connection is left before the bus is asked again.</summary>
		/// <remarks>
		/// The input thread asks on every pass, and connecting enumerates the bus and opens it. A bus
		/// that refuses is asked again only after this, not a thousand times a second.
		/// </remarks>
		public const int ConnectRetryMs = 5000;

		/// <summary>What the bus answered the last time a connection to it was attempted.</summary>
		/// <remarks>
		/// VIGEM_ERROR_NONE before the first attempt and after one that worked. Written under
		/// <see cref="ClientLock"/> by whichever thread connects. Read without any lock by
		/// <see cref="x360ce.App.Issues.VirtualDriverNotWorkingIssue"/> in the Issues tab, which must
		/// neither make a client nor wait behind the input thread to learn whether the bus works. Read it
		/// before <see cref="LastConnectTick"/>.
		/// </remarks>
		public static VIGEM_ERROR LastConnectError { get { return _LastConnectError; } }
		static volatile VIGEM_ERROR _LastConnectError = VIGEM_ERROR.VIGEM_ERROR_NONE;

		/// <summary><see cref="Environment.TickCount"/> when a connection was last attempted and <see cref="LastConnectError"/> answered.</summary>
		/// <remarks>
		/// Written before the answer itself, so a reader that reads the answer first and this second
		/// never pairs a new answer with an older time.
		/// </remarks>
		public static int LastConnectTick { get { return _LastConnectTick; } }
		static volatile int _LastConnectTick;

		/// <summary>Keeps what connecting answered, and writes it to the log when it differs from the last answer.</summary>
		/// <remarks>
		/// Once per change: a refusing bus is asked every few seconds for as long as a game uses virtual
		/// emulation, and a line each time would bury everything else in the log.
		/// </remarks>
		static void RecordConnect(VIGEM_ERROR error)
		{
			var previous = _LastConnectError;
			_LastConnectTick = Environment.TickCount;
			_LastConnectError = error;
			if (error == previous)
				return;
			if (error == VIGEM_ERROR.VIGEM_ERROR_NONE)
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(
					"Virtual bus connected again after " + BusAnswers.Name(previous) + ".",
					System.Diagnostics.EventLogEntryType.Information);
			else
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(
					"Virtual bus refused the connection: " + BusAnswers.Name(error) + ".",
					System.Diagnostics.EventLogEntryType.Warning);
		}

		public static void DisposeCurrent()
		{
			lock (ClientLock)
			{
				// If virtual client is initialized then...
				var client = Current;
				if (client == null)
					return;
				// Taken away first, so a reader without this lock that reads it from here on finds none, not a
				// client being let go of. The input thread calls this itself when a game leaves virtual mode,
				// and closing calls it after that thread has stopped, so no reader holds it while it is let go of.
				Current = null;
				try
				{
					client.Dispose();
				}
				catch (Exception ex)
				{
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				}
			}
		}

		/// <summary>Whether the Visual C++ runtime is installed: <see cref="RuntimeUnknown"/>, <see cref="RuntimePresent"/> or <see cref="RuntimeMissing"/>.</summary>
		/// <remarks>Written under <see cref="ClientLock"/>. Read without it by <see cref="isVBusExists"/>, one whole number.</remarks>
		static volatile int _Runtime;
		const int RuntimeUnknown = 0;
		const int RuntimePresent = 1;
		const int RuntimeMissing = 2;

		/// <summary>Forgets whether the Visual C++ runtime is installed, so the next bus check looks again.</summary>
		/// <remarks>
		/// The answer is kept because <see cref="isVBusExists"/> runs on every engine cycle, and it changes
		/// only when the runtime is installed. The runtime issue calls this after its installer succeeds, from
		/// the fix's worker thread. The lock keeps it from landing inside a check under way, which would write
		/// its older answer over this one. It is held for one assignment, and the engine reads the answer
		/// without it.
		/// </remarks>
		public static void ForgetRuntimeInstalled()
		{
			lock (ClientLock)
				_Runtime = RuntimeUnknown;
		}

		/// <summary>
		/// Check ViGEm client. Create if not exists.
		/// </summary>
		/// <remarks>
		/// Without a client, the lock is taken for the runtime check while its answer is unknown: once, and
		/// again after <see cref="ForgetRuntimeInstalled"/>. A runtime known to be missing is answered without
		/// the lock. Otherwise it is taken for a connect attempt or a library load, at most once per
		/// <see cref="ConnectRetryMs"/> while either keeps failing; also while a client is let go of.
		/// </remarks>
		/// <returns></returns>
		public static bool isVBusExists(bool createIfMissing = false)
		{
			// The input thread asks on every pass. A client in place is answered from field reads, without the lock the
			// interface takes when it installs or removes the driver. With one in place, nothing below can answer
			// otherwise: it is kept only after a connect that worked, which needed the runtime.
			var current = Current;
			if (current != null && !current.Disposing && !current.IsDisposed)
				return true;
			// A refusal is not asked about again for a while. Both fields are volatile, and the answer is
			// written before its time, so this is never paired with an older time. Read before the lock, so a
			// pass with no client takes no lock while the gate the Install and Uninstall buttons also take
			// through this lock is closed.
			if (_LastConnectError != VIGEM_ERROR.VIGEM_ERROR_NONE
				&& DInputHelper.IsRecent(_LastConnectTick, Environment.TickCount, ConnectRetryMs))
				return false;
			// A library that just failed to load is not due again either. Both fields are volatile, so this
			// is read before the lock the same way, and a pass with no client takes no lock while either
			// gate is closed.
			if (!LibraryLoadDue())
				return false;
			// Nor is a runtime known to be missing, until its fix forgets the answer. Read before the lock the same
			// way, so a pass on a machine without the runtime takes no lock.
			if (_Runtime == RuntimeMissing)
				return false;
			lock (ClientLock)
			{
				// If Visual Studio C++ 2015 Redistributable installation unknown then...
				if (_Runtime == RuntimeUnknown)
				{
					var issue = Environment.Is64BitProcess
						? (IssueItem)new CppX64RuntimeInstallIssue()
						: (IssueItem)new CppX86RuntimeInstallIssue();
					issue.Check();
					_Runtime = issue.Severity == IssueSeverity.None ? RuntimePresent : RuntimeMissing;
				}
				if (_Runtime == RuntimeMissing)
					return false;
				// Checked again: another thread may have just recorded a refusal while this one waited for the lock.
				if (_LastConnectError != VIGEM_ERROR.VIGEM_ERROR_NONE
					&& DInputHelper.IsRecent(_LastConnectTick, Environment.TickCount, ConnectRetryMs))
					return false;
				// If client exists and it was not disposed then...
				if (Current != null && !Current.Disposing && !Current.IsDisposed)
					return true;
				VIGEM_ERROR error;
				// A library that just failed to load is not extracted, hashed and asked for again on every
				// pass; the same gate a refused connect uses paces it instead.
				if (!IsLoaded && LibraryLoadDue())
				{
					LoadLibrary();
					RecordLoad(!IsLoaded);
				}
				// Without the native library there is nothing to allocate against, and the call
				// below would throw DllNotFoundException on the input thread and take the whole
				// program down. A missing library is reported the same way a missing C++ runtime
				// is: no bus, which the Issues tab already explains and offers to fix.
				if (!IsLoaded)
					return false;
				var client = new ViGEmClient(out error);
				RecordConnect(error);
				if (error == VIGEM_ERROR.VIGEM_ERROR_NONE)
				{
					Current = client;
				}
				else
				{
					client.Dispose();
					FreeLibrary();
				}
				return error == VIGEM_ERROR.VIGEM_ERROR_NONE;
			}
		}

		static Exception LastLoadException;

		/// <summary><see cref="Environment.TickCount"/> when the library last failed to load.</summary>
		static volatile int _LastLoadFailTick;

		/// <summary>Whether the library's last load attempt failed. Cleared by a load that leaves it loaded.</summary>
		static volatile bool _LastLoadFailed;

		/// <summary>Whether the library is due to be tried again, from when it last failed to load.</summary>
		static bool LibraryLoadDue()
		{
			return !_LastLoadFailed || !DInputHelper.IsRecent(_LastLoadFailTick, Environment.TickCount, ConnectRetryMs);
		}

		/// <summary>Keeps whether the library's last load attempt failed, and writes it to the log when that changes.</summary>
		/// <remarks>
		/// Once per change, matching <see cref="RecordConnect"/>: a library that keeps failing to load is
		/// tried again every <see cref="ConnectRetryMs"/> while a game uses virtual emulation, and a line
		/// each time would bury everything else in the log.
		/// </remarks>
		static void RecordLoad(bool failed)
		{
			var previous = _LastLoadFailed;
			_LastLoadFailTick = Environment.TickCount;
			_LastLoadFailed = failed;
			if (failed == previous)
				return;
			if (failed)
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(LastLoadException);
			else
				JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteLog(
					"The virtual bus library loaded after failing to load before.",
					System.Diagnostics.EventLogEntryType.Information);
		}

		public static string LibraryName { get { return _LibraryName; } }
		static string _LibraryName;

		static IntPtr libHandle;
		public static bool IsLoaded { get { return libHandle != IntPtr.Zero; } }

		static void LoadLibrary()
		{
			try
			{
				// Extract ViGEm library from Embedded resource.
				var name = "ViGEmClient.dll";
				var chName = x360ce.Engine.EngineHelper.GetResourceChecksumFile(name);
				var fileName = System.IO.Path.Combine(x360ce.Engine.EngineHelper.AppDataPath, "Temp", chName);
				var fi = new FileInfo(fileName);
				if (!fi.Exists)
				{
					if (!fi.Directory.Exists)
						fi.Directory.Create();
					var sr = Program.GetResourceStream(name);
					if (sr == null)
						return;
					FileStream sw = null;
					sw = new FileStream(fileName, FileMode.Create, FileAccess.Write);
					var buffer = new byte[1024];
					while (true)
					{
						var count = sr.Read(buffer, 0, buffer.Length);
						if (count == 0)
							break;
						sw.Write(buffer, 0, count);
					}
					sr.Close();
					sw.Close();
				}
				_LibraryName = fileName;
				// Load library into memory.
				Exception loadException;
				libHandle = JocysCom.ClassLibrary.Win32.NativeMethods.LoadLibrary(_LibraryName, out loadException);
				if (libHandle == IntPtr.Zero)
					LastLoadException = loadException;
			}
			catch (Exception ex)
			{
				// Written by RecordLoad, once per change: a library that keeps failing is tried every few seconds.
				LastLoadException = ex;
			}
		}

		public static void FreeLibrary()
		{
			if (!IsLoaded)
				return;
			Exception error;
			JocysCom.ClassLibrary.Win32.NativeMethods.FreeLibrary(libHandle, out error);
			libHandle = IntPtr.Zero;
		}

		#endregion

	}
}
