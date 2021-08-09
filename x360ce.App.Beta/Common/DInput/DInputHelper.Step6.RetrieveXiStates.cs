using SharpDX.XInput;
using System;

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

		void RetrieveXiStates(bool getXInputStates)
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
			var ev = StatesRetrieved;
			if (ev != null)
				ev(this, new DInputEventArgs(error));
		}

	}
}
