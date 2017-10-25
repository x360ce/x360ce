using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public Controller[] XiControllers;
		public State[] LiveXInputStates;
		public bool[] XiControllerConnected;

		void RetrieveXiStates()
		{
			for (uint i = 0; i < 4; i++)
			{

				lock (XInput.XInputLock)
				{
					var gamePad = XiControllers[i];
					if (XInput.IsLoaded && gamePad.IsConnected)
					{
						LiveXInputStates[i] = gamePad.GetState();
						XiControllerConnected[i] = true;
					}
					else
					{
						LiveXInputStates[i] = new State();
						XiControllerConnected[i] = false;
					}
				}
			}
			var ev = StatesRetrieved;
			if (ev != null)
				ev(this, new EventArgs());
		}

	}
}
