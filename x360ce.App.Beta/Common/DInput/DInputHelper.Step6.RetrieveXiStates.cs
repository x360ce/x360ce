using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
			var o = SettingsManager.Options;
			var allow = !o.TestEnabled || o.TestGetXInputStates;
			lock (Controller.XInputLock)
			{
				// Before states can be retrieved xinput configuration must be checked.
				for (uint i = 0; i < 4; i++)
				{
					var gamePad = LiveXiControllers[i];
					State state;
					if (Controller.IsLoaded && allow && gamePad.GetState(out state))
					{
						LiveXiStates[i] = state;
						LiveXiConnected[i] = true;
					}
					else
					{
						LiveXiStates[i] = new State();
						LiveXiConnected[i] = false;
					}
				}
			}
			var ev = StatesRetrieved;
			if (ev != null)
				ev(this, new EventArgs());
		}

	}
}
