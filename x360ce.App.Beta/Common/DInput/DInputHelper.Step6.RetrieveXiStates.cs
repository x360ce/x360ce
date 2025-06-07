using SharpDX.XInput;
using System;
using System.Threading;
using System.Threading.Tasks;

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
			Exception error = null;

			if (Controller.IsLoaded && getXInputStates)
			{
				Parallel.For(0, 4, i =>
				{
					var gamePad = LiveXiControllers[i];
					State state = new State();
					var success = false;

					try
					{
						success = gamePad.GetState(out state);
					}
					catch (Exception ex)
					{
						// Capture the first encountered exception
						Interlocked.CompareExchange(ref error, ex, null);
					}

					LiveXiConnected[i] = success;
					LiveXiStates[i] = state;
				});
			}
			else
			{
				for (int i = 0; i < 4; i++)
				{
					LiveXiConnected[i] = false;
					LiveXiStates[i] = new State();
				}
			}

			StatesRetrieved?.Invoke(this, new DInputEventArgs(error));
		}

		//void RetrieveXiStates(bool getXInputStates)
		//{
		//	// Allow if not testing or testing with option enabled.
		//	Exception error = null;
		//	lock (Controller.XInputLock)
		//	{
		//		for (uint i = 0; i < 4; i++)
		//		{
		//			var gamePad = LiveXiControllers[i];
		//			State state = new State();
		//			var success = false;
		//			var timeout = false;
		//			if (Controller.IsLoaded && getXInputStates)
		//			{
		//				Action action = () =>
		//				{
		//					// This can hit CPU hard and used for display only.
		//					// Do not use when application is minimized. 
		//					success = gamePad.GetState(out state);
		//				};
		//				var result = action.BeginInvoke(null, null);
		//				timeout = !result.AsyncWaitHandle.WaitOne(1000);
		//			}
		//			if (timeout)
		//			{
		//				error = new Exception("gamePad.GetState(out state) timed out.");
		//			}
		//			LiveXiConnected[i] = success && !timeout;
		//			LiveXiStates[i] = state;
		//		}
		//	}
		//	StatesRetrieved?.Invoke(this, new DInputEventArgs(error));
		//}

	}
}
