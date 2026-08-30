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
			// Whether the states were actually read, rather than whether somebody asked for them.
			// The setting alone was taken as the answer, so with the library not loaded nothing was
			// read, every place reported empty, and each working controller was accused of being
			// broken on evidence nobody had gathered.
			XiStatesRead = wanted;
			// Allow if not testing or testing with option enabled.
			Exception error = null;
			lock (Controller.XInputLock)
			{
				for (uint i = 0; i < 4; i++)
				{
					var gamePad = LiveXiControllers[i];
					// Between reads the last one still stands. Falling through here would write an
					// empty state and "not connected" over it on every pass that does not read, which
					// is most of them - the controller picture and the formula preview both draw from
					// these, and would spend their time showing nothing.
					if (wanted && !due)
						continue;
					State state = new State();
					var success = false;
					var timeout = false;
					if (wanted)
					{
						IAsyncResult result;
						Action action = () =>
						{
							// This can hit CPU hard and used for display only.
							// Do not use when application is minimized. 
							success = gamePad.GetState(out state);
						};
						result = action.BeginInvoke(null, null);
						timeout = !result.AsyncWaitHandle.WaitOne(1000);
					}
					if (timeout)
					{
						error = new Exception("gamePad.GetState(out state) timed out.");
					}
					LiveXiConnected[i] = success && !timeout;
					LiveXiStates[i] = state;
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
