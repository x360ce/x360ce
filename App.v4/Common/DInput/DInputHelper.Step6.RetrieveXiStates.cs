using SharpDX.XInput;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using x360ce.App.Controls;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public Controller[] LiveXiControllers;
		public bool[] LiveXiConnected;
		public State[] LiveXiStates;

		// This value will be modified to true when settings on the form changes and 
		// XInput library needs to be reload.
		public bool SettingsChanged = false;

		/// <summary>How long the display reads rest after XInput fails to answer, in milliseconds.</summary>
		/// <remarks>
		/// XInput stops answering while Windows takes controllers away and builds them again, which
		/// is what removing leftover controllers does. The read used to report that as a fault, and
		/// the window answered the fault by switching the XInput view off - for good, and with the
		/// button still showing it on. A pause is all the situation needs: the reads try again by
		/// themselves once the controllers are back.
		/// </remarks>
		public const int XiReadPauseMs = 5000;

		/// <summary>When the display reads may try again, after XInput failed to answer.</summary>
		int _xiReadPausedUntil;

		/// <summary>Whether the display reads are resting after XInput failed to answer.</summary>
		public bool XiReadsPaused
		{
			get { return IsWaiting(_xiReadPausedUntil, Environment.TickCount, XiReadPauseMs); }
		}

		/// <summary>True while a wait of at most <paramref name="limit"/> milliseconds that ends at <paramref name="until"/> is still running at <paramref name="now"/>.</summary>
		/// <remarks>
		/// Both are <see cref="Environment.TickCount"/> values, which wrap, so only their difference is
		/// compared. A wait runs while its end lies ahead by no more than its own length. An end never
		/// set (0) or one left from long ago therefore reads as passed at any uptime, except that a 0 reads
		/// as running for the last <paramref name="limit"/> milliseconds before the count comes round to 0
		/// every 49.7 days, which holds it for no longer than that. Judged by the sign of the difference
		/// alone, a 0 lies ahead for the 24.9 days of every 49.7 that the count is negative.
		/// </remarks>
		public static bool IsWaiting(int until, int now, int limit)
		{
			var wait = unchecked(until - now);
			return wait > 0 && wait <= limit;
		}

		/// <summary>True while <paramref name="now"/> lies less than <paramref name="window"/> milliseconds after <paramref name="last"/>.</summary>
		/// <remarks>
		/// Both are <see cref="Environment.TickCount"/> values, which wrap, so only their difference is
		/// compared, as in <see cref="IsWaiting"/>. The difference turns negative once more than 2^31 ms
		/// (about 24.9 days) lie between them, so a moment that old reads as long past rather than recent.
		/// It allocates nothing and takes no lock, so the engine can ask it on every pass.
		/// </remarks>
		/// <param name="last">When it happened.</param>
		/// <param name="now">The time now.</param>
		/// <param name="window">How long it counts as recent, in milliseconds.</param>
		public static bool IsRecent(int last, int now, int window)
		{
			var elapsed = unchecked(now - last);
			return elapsed >= 0 && elapsed < window;
		}

		/// <summary>How long XInput has to answer before it is taken as not answering, in milliseconds.</summary>
		/// <remarks>
		/// A healthy read of the four places takes microseconds, and loading the library a few milliseconds. Past a
		/// second XInput is not answering, which it does while Windows takes controllers away and builds them again.
		/// The display read rests the view after it, and letting go of the controllers gives up after it.
		/// </remarks>
		public const int XiAnswerMs = 1000;

		#region Locks the input thread tries only when free

		/// <summary>Takes a lock within a limit, and says so through <paramref name="busy"/> from before the wait until <see cref="ExitPublished"/>.</summary>
		/// <remarks>
		/// On .NET Framework a failed try of a lock another thread holds spins before it gives up: about 0.8 ms on a machine
		/// with 16 logical processors. The input thread tries its locks once a pass, and a read of XInput that does not come
		/// back holds one for as long as XInput does not answer. So every holder says it holds the lock, or waits for it, and
		/// the input thread reads that first through <see cref="TryEnterWhenFree"/>.
		/// </remarks>
		/// <param name="gate">The lock.</param>
		/// <param name="busy">Above 0 while a thread holds <paramref name="gate"/> or waits for it.</param>
		/// <param name="limit">How long to wait for the lock.</param>
		internal static bool EnterPublished(object gate, ref int busy, TimeSpan limit)
		{
			Interlocked.Increment(ref busy);
			if (Monitor.TryEnter(gate, limit))
				return true;
			Interlocked.Decrement(ref busy);
			return false;
		}

		/// <summary>Lets go of a lock taken by <see cref="EnterPublished"/> or <see cref="TryEnterWhenFree"/>.</summary>
		internal static void ExitPublished(object gate, ref int busy)
		{
			Monitor.Exit(gate);
			Interlocked.Decrement(ref busy);
		}

		/// <summary>Takes a lock for the input thread without waiting, and without trying while another thread holds it or waits for it.</summary>
		/// <remarks>
		/// One field read while the lock is held, and nothing made, so the pass passes it by at its own rate. A holder that
		/// takes the lock between the read and the try costs one spin, and the next pass reads it held.
		/// </remarks>
		internal static bool TryEnterWhenFree(object gate, ref int busy)
		{
			return Volatile.Read(ref busy) == 0 && EnterPublished(gate, ref busy, TimeSpan.Zero);
		}

		/// <summary>Above 0 while a thread holds <see cref="Controller.XInputLock"/> or waits for it.</summary>
		static int _xinputLockBusy;

		/// <summary>Takes <see cref="Controller.XInputLock"/> within a limit, saying so to the input thread.</summary>
		static bool EnterXInputLock(TimeSpan limit)
		{
			return EnterPublished(Controller.XInputLock, ref _xinputLockBusy, limit);
		}

		/// <summary>Lets go of <see cref="Controller.XInputLock"/> taken by <see cref="EnterXInputLock"/> or <see cref="TryEnterXInputLockWhenFree"/>.</summary>
		static void ExitXInputLock()
		{
			ExitPublished(Controller.XInputLock, ref _xinputLockBusy);
		}

		/// <summary>Takes <see cref="Controller.XInputLock"/> for the input thread, only while no other thread holds it or waits for it.</summary>
		static bool TryEnterXInputLockWhenFree()
		{
			return TryEnterWhenFree(Controller.XInputLock, ref _xinputLockBusy);
		}

		#endregion

		void RetrieveXiStates(UserGame game, bool getXInputStates)
		{
			// A read the reader has answered is taken at once: four states copied into the arrays kept for them.
			if (Volatile.Read(ref _displayRead) == DisplayReadAnswered)
			{
				for (var i = 0; i < 4; i++)
				{
					LiveXiConnected[i] = _displayConnected[i];
					LiveXiStates[i] = _displayStates[i];
				}
				Volatile.Write(ref _displayRead, DisplayReadIdle);
			}
			// These states are shown on screen and nowhere else, and a screen cannot show more than sixty readings a
			// second. Paced here and not by the caller, because the caller uses the same answer to decide whether the
			// XInput library stays loaded, and pacing that made it load and unload all day.
			var due = DueForDisplayRead();
			var wanted = Controller.IsLoaded && getXInputStates;
			if (wanted && XiReadsPaused)
			{
				// Resting: what was read last still stands, and nothing is asked of XInput.
				NotePadPlaces();
				return;
			}
			// Whether the states were actually read, rather than whether somebody asked for them.
			// The setting alone was taken as the answer, so with the library not loaded nothing was
			// read, every place reported empty, and each working controller was accused of being
			// broken on evidence nobody had gathered.
			XiStatesRead = wanted;
			Exception error = null;
			if (!wanted)
			{
				// Nothing is read, so every place reads as empty and not connected.
				for (var i = 0; i < 4; i++)
				{
					LiveXiConnected[i] = false;
					LiveXiStates[i] = new State();
				}
			}
			else if (due)
			{
				var read = Volatile.Read(ref _displayRead);
				// Between reads the last one still stands.
				if (read == DisplayReadIdle)
					AskDisplayRead();
				// Not answered in a second: XInput has stopped answering, which it does while Windows takes controllers
				// away and builds them again. The view rests, and every place reads as empty meanwhile.
				else if (read == DisplayReadAsked && !IsRecent(_displayReadAskedAt, Environment.TickCount, XiAnswerMs))
				{
					_xiReadPausedUntil = unchecked(Environment.TickCount + XiReadPauseMs);
					for (var i = 0; i < 4; i++)
					{
						LiveXiConnected[i] = false;
						LiveXiStates[i] = new State();
					}
					error = new Exception("XInput did not answer for a second; the XInput view rests for "
						+ (XiReadPauseMs / 1000) + " seconds and then reads again.");
				}
			}
			NotePadPlaces();
			// Raised only with a failure, the one thing the window acts on.
			if (error != null)
			{
				var ev = StatesRetrieved;
				if (ev != null)
					ev(this, new DInputEventArgs(error));
			}
		}

		/// <summary>The display read's state: nothing asked, a read asked of the reader, or a read answered and not yet taken.</summary>
		const int DisplayReadIdle = 0;
		const int DisplayReadAsked = 1;
		const int DisplayReadAnswered = 2;
		int _displayRead;

		/// <summary>When the read under way was asked for, as <see cref="Environment.TickCount"/>.</summary>
		int _displayReadAskedAt;

		/// <summary>What the reader read last: reserved here, written by the reader, copied by the input thread once answered.</summary>
		readonly State[] _displayStates = new State[4];
		readonly bool[] _displayConnected = new bool[4];

		/// <summary>Wakes the reader for one read.</summary>
		readonly AutoResetEvent _displayReadWake = new AutoResetEvent(false);

		/// <summary>The thread the display reads run on, made on the first read, or null before it.</summary>
		Thread _displayReader;

		/// <summary>Set when the helper is let go of, so the reader ends at its next wake.</summary>
		volatile bool _displayReaderStop;

		/// <summary>The kind of exception the last failed display read threw, so the same failure is written once.</summary>
		Type _displayReadFault;

		/// <summary>Asks the reader for one read of the four places, making the reader on the first read.</summary>
		void AskDisplayRead()
		{
			_displayReadAskedAt = Environment.TickCount;
			Volatile.Write(ref _displayRead, DisplayReadAsked);
			if (_displayReader == null)
			{
				// One thread for the life of the helper, waiting between reads. A read then waits for nothing and makes
				// nothing: no task, no delegate, no arrays and no wait handle.
				_displayReader = new Thread(ReadDisplayStatesLoop);
				_displayReader.IsBackground = true;
				_displayReader.Name = "XInputDisplayRead";
				_displayReader.Start();
			}
			_displayReadWake.Set();
		}

		void ReadDisplayStatesLoop()
		{
			while (true)
			{
				_displayReadWake.WaitOne();
				if (_displayReaderStop)
					return;
				ReadDisplayStates();
				Volatile.Write(ref _displayRead, DisplayReadAnswered);
			}
		}

		/// <summary>Reads the four places into the reserved answer.</summary>
		/// <remarks>
		/// Under <see cref="Controller.XInputLock"/>, which the library is loaded and let go of under, so it is never
		/// let go of in the middle of a read. XInput can stop answering. The input thread then rests the view, and
		/// since it never waits for this lock, and reads that it is held rather than trying it, it goes on reading the
		/// controllers.
		/// </remarks>
		void ReadDisplayStates()
		{
			try
			{
				EnterXInputLock(Timeout.InfiniteTimeSpan);
				try
				{
					var loaded = Controller.IsLoaded;
					for (var p = 0; p < 4; p++)
					{
						if (loaded)
						{
							_displayConnected[p] = LiveXiControllers[p].GetState(out _displayStates[p]);
						}
						else
						{
							_displayConnected[p] = false;
							_displayStates[p] = new State();
						}
					}
				}
				finally
				{
					ExitXInputLock();
				}
				_displayReadFault = null;
			}
			catch (Exception ex)
			{
				for (var p = 0; p < 4; p++)
					_displayConnected[p] = false;
				// Written when it is news: the view asks sixty times a second.
				if (_displayReadFault != ex.GetType())
				{
					_displayReadFault = ex.GetType();
					JocysCom.ClassLibrary.Runtime.LogHelper.Current.WriteException(ex);
				}
			}
		}

		/// <summary>
		/// Which XInput place holds the controller made for each pad, or -1 where there is none.
		/// </summary>
		/// <remarks>
		/// Index by pad, one to four.
		/// </remarks>
		public int[] XiPlaceForPad = new int[] { -1, -1, -1, -1 };

		/// <summary>Whether the last pass actually read the states back from XInput.</summary>
		public bool XiStatesRead;

		/// <summary>Forgets the place of any controller of ours that has gone away.</summary>
		/// <remarks>
		/// Only forgets. Where a controller went is written down at the one moment it can be known -
		/// as it arrives, by watching which place filled - and nothing can work it out afterwards.
		///
		/// It was worked out afterwards, twice over. First by counting the places against the
		/// controllers we believed we had made, which gave up whenever a real controller held a place
		/// of its own. Then by assuming pad one holds place one, which is what the program asks for
		/// and not what Windows gives: measured with a real controller in the first place and the
		/// second free, a controller made for the second was given the third.
		/// </remarks>
		void NotePadPlaces()
		{
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			for (var i = 0; i < XiPlaceForPad.Length; i++)
				if (client == null || !client.IsControllerConnected((uint)(i + 1)))
					XiPlaceForPad[i] = -1;
		}

	}
}
