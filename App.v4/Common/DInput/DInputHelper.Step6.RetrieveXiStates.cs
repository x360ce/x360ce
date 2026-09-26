using SharpDX.XInput;
using System;
using System.Diagnostics;
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
			get { return IsPaused(_xiReadPausedUntil, Environment.TickCount); }
		}

		/// <summary>True while the pause that ends at <paramref name="until"/> is still running at <paramref name="now"/>.</summary>
		public static bool IsPaused(int until, int now)
		{
			return unchecked(now - until) < 0;
		}

		/// <summary>Runs a native call on a worker and answers whether it returned in time.</summary>
		/// <remarks>
		/// XInput can stop answering, and this thread must not stop with it. The call used to go
		/// through a delegate's BeginInvoke with nothing ever calling EndInvoke, which leaves every
		/// call's wait handle open: four handles a read, up to sixty reads a second, until a long
		/// session ran Windows out of handles and the program closed with "Insufficient system
		/// resources". A task's wait keeps nothing behind. One that runs past its time is left to
		/// finish on the worker; the next read is what tells whether XInput is answering again.
		/// </remarks>
		public static bool RanWithin(Action action, int milliseconds)
		{
			return System.Threading.Tasks.Task.Run(action).Wait(milliseconds);
		}

		void RetrieveXiStates(UserGame game, bool getXInputStates)
		{
			// These states are shown on screen and nowhere else, and a screen cannot show more
			// than sixty readings a second, while asking for one costs a delegate, a hand-off to
			// another thread and a wait handle, four times over. Read on every pass it took seven
			// tenths of every second away from reading the controllers themselves. Paced here and
			// not by the caller, because the caller uses the same answer to decide whether the
			// XInput library stays loaded, and pacing that made it load and unload all day.
			var due = DueForDisplayRead();
			var wanted = Controller.IsLoaded && getXInputStates;
			if (wanted && XiReadsPaused)
			{
				// Resting: what was read last still stands, and nothing is asked of XInput.
				NotePadPlaces();
				var resting = StatesRetrieved;
				if (resting != null)
					resting(this, new DInputEventArgs());
				return;
			}
			// Whether the states were actually read, rather than whether somebody asked for them.
			// The setting alone was taken as the answer, so with the library not loaded nothing was
			// read, every place reported empty, and each working controller was accused of being
			// broken on evidence nobody had gathered.
			XiStatesRead = wanted;
			// Allow if not testing or testing with option enabled.
			Exception error = null;
			lock (Controller.XInputLock)
			{
				// Between reads the last one still stands. Falling through here would write an
				// empty state and "not connected" over it on every pass that does not read, which
				// is most of them - the controller picture and the formula preview both draw from
				// these, and would spend their time showing nothing.
				if (!wanted || due)
				{
					State[] read = null;
					bool[] answered = null;
					if (wanted)
					{
						// This can hit CPU hard and used for display only.
						// Do not use when application is minimized.
						// All four places are read in one hand-over. Each hand-over waits for a worker
						// to pick it up, and four in a row keep this thread past the time the next pass
						// is due. The arrays are made for each read, so a read that never returns writes
						// only into arrays nothing looks at any more.
						var states = new State[4];
						var connected = new bool[4];
						var controllers = LiveXiControllers;
						if (RanWithin(() =>
						{
							for (var p = 0; p < 4; p++)
								connected[p] = controllers[p].GetState(out states[p]);
						}, 1000))
						{
							read = states;
							answered = connected;
						}
						else
						{
							_xiReadPausedUntil = unchecked(Environment.TickCount + XiReadPauseMs);
							error = new Exception("XInput did not answer for a second; the XInput view rests for "
								+ (XiReadPauseMs / 1000) + " seconds and then reads again.");
						}
					}
					for (var i = 0; i < 4; i++)
					{
						LiveXiConnected[i] = answered != null && answered[i];
						LiveXiStates[i] = read == null ? new State() : read[i];
					}
				}
			}
			NotePadPlaces();
			var ev = StatesRetrieved;
			if (ev != null)
				ev(this, new DInputEventArgs(error));
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
