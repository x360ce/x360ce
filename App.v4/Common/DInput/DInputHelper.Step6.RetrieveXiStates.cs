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
			// Allow if not testing or testing with option enabled.
			Exception error = null;
			lock (Controller.XInputLock)
			{
				for (uint i = 0; i < 4; i++)
				{
					var gamePad = LiveXiControllers[i];
					State state = new State();
					var success = false;
					var timeout = false;
					if (Controller.IsLoaded && getXInputStates)
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
			MatchPadsToPlaces();
			var ev = StatesRetrieved;
			if (ev != null)
				ev(this, new DInputEventArgs(error));
		}

		/// <summary>
		/// Which of the four XInput places holds each of this program's controllers.
		/// </summary>
		/// <remarks>
		/// Index by pad, one to four. Minus one until it is known.
		/// </remarks>
		public int[] XiPlaceForPad = new int[] { -1, -1, -1, -1 };

		/// <summary>
		/// Works out which place Windows gave each controller this program created.
		/// </summary>
		/// <remarks>
		/// This is not a choice the program gets to make. Windows offers four places for a controller
		/// of this kind and decides for itself which one a new controller goes into; there is no way
		/// to ask for a particular one. Measured on a computer with all four places empty, a
		/// controller created by this program was put in the third.
		///
		/// Everything on screen used to assume controller one meant the first place. On that computer
		/// it therefore read an empty place and showed a dead controller, while its own was sitting in
		/// the third. Where somebody had a real Xbox controller plugged in, it read that instead, and
		/// the picture moved when nobody was touching this program's controller at all.
		///
		/// So the place is found rather than assumed. Its own controllers are counted, the occupied
		/// places are counted, and when those agree they are paired in order. That is exact whenever
		/// every occupied place belongs to this program, which is the ordinary case. When something
		/// else is plugged in as well the counts disagree, nothing is claimed, and the old assumption
		/// stands rather than a guess being made.
		/// </remarks>
		void MatchPadsToPlaces()
		{
			var ours = new System.Collections.Generic.List<int>();
			var client = Nefarius.ViGEm.Client.ViGEmClient.Current;
			for (uint i = 1; i <= 4; i++)
				if (client != null && client.IsControllerConnected(i))
					ours.Add((int)i);
			var occupied = new System.Collections.Generic.List<int>();
			for (int place = 0; place < 4; place++)
				if (LiveXiConnected[place])
					occupied.Add(place);
			// Only when the two agree is the pairing certain.
			if (ours.Count == 0 || ours.Count != occupied.Count)
			{
				for (int i = 0; i < XiPlaceForPad.Length; i++)
					XiPlaceForPad[i] = -1;
				return;
			}
			for (int i = 0; i < XiPlaceForPad.Length; i++)
				XiPlaceForPad[i] = -1;
			for (int n = 0; n < ours.Count; n++)
				XiPlaceForPad[ours[n] - 1] = occupied[n];
		}

	}
}
