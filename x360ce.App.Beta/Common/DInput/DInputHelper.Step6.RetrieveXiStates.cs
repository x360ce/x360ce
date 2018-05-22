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

		void RetrieveXiStates(UserGame game)
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
					// If settings changed then...
					if (SettingsChanged)
					{
						var IsLibrary = game != null && game.IsLibrary;
						if (IsLibrary || !Controller.IsLoaded)
						{
							ReloadLibrary(game);
						}
					}
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

		public void ReloadLibrary(UserGame game)
		{
			lock (Controller.XInputLock)
			{
				var e = new DInputEventArgs();
				// Always load Microsoft XInput DLL by default.
				var useMicrosoft = game.EmulationType != (int)EmulationType.Library;
				Program.ReloadCount++;
				SettingsChanged = false;
				var dllInfo = EngineHelper.GetDefaultDll(useMicrosoft);
				if (dllInfo != null && dllInfo.Exists)
				{
					var vi = FileVersionInfo.GetVersionInfo(dllInfo.FullName);
					e.XInputVersionInfo = vi;
					e.XInputFileInfo = dllInfo;
					// If fast reload of settings is supported then...
					if (Controller.IsLoaded && Controller.IsResetSupported)
					{
						IAsyncResult result;
						Action action = () =>
						{
							Controller.Reset();
						};
						result = action.BeginInvoke(null, null);
						var timeout = !result.AsyncWaitHandle.WaitOne(1000);
						var caption = string.Format("Failed to Reset() controller. '{0}'", dllInfo.FullName);
						e.Error = new Exception(caption);
					}
					// Slow: Reload whole x360ce.dll.
					Exception error;
					Controller.ReLoadLibrary(dllInfo.FullName, out error);
					if (!Controller.IsLoaded)
					{
						var caption = string.Format("Failed to load '{0}'", dllInfo.FullName);
						e.Error = new Exception(caption);
					}
				}
				var ev = XInputReloaded;
				if (ev != null)
					ev(this, e);
			}
		}


	}
}
