using SharpDX.XInput;
using System;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public Controller[] LiveXiControllers;
		public bool[] LiveXiConnected;
		public State[] LiveXiStates;

		void RetrieveXiStates()
		{
			// Allow if not testing or testing with option enabled.
			Exception error = null;
			lock (Controller.XInputLock)
			{
				// Before states can be retrieved XInput configuration must be checked.
				for (uint i = 0; i < 4; i++)
				{
					var gamePad = LiveXiControllers[i];
					State state = new State();
					var allow = SettingsManager.Options.GetXInputStates;
					var success = false;
					var timeout = false;
					if (Controller.IsLoaded && allow)
					{
						IAsyncResult result;
						Action action = () =>
						{
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
