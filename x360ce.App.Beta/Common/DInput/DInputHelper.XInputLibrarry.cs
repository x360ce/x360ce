using SharpDX.XInput;
using System;
using System.Diagnostics;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{
		EmulationType CurrentEmulation;

		public void CheckAndUnloadXInputLibrary(UserGame game, bool getXInputStates)
		{
			lock (Controller.XInputLock)
			{
				var (unloadLoad, emType) = UnloadLoad(game, getXInputStates);
				if (!Controller.IsLoaded || !unloadLoad || emType != CurrentEmulation)
					return;
				Controller.FreeLibrary();
				XInputReloaded?.Invoke(this, new DInputEventArgs());
			}
		}

		private (bool, EmulationType) UnloadLoad(UserGame game, bool getXInputStates)
		{ 
			var emType = (EmulationType)(game?.EmulationType ?? (int)EmulationType.None);
			var unloadLoad =
				// No emulation or
				emType == EmulationType.None ||
				// If no actual XInput states are required for Virtual emulation.
				emType == EmulationType.Virtual && !getXInputStates ||
				// New device was detected so exclusive lock is necessary to retrieve force feedback information.
				// Don't load until device list was not refreshed.
				// DevicesAreUpdating ||
				// This will also release exclusive lock if another game/application must use it.
				// No actual XInput states are required for Library emulation when minimized.
				emType == EmulationType.Library && !Global._MainWindow.FormEventsEnabled;
			return (unloadLoad, emType);
		}

		public void CheckAndLoadXInputLibrary(UserGame game, bool getXInputStates)
		{
			lock (Controller.XInputLock)
			{
				var (unloadLoad, emType) = UnloadLoad(game, getXInputStates);

				if (Controller.IsLoaded || !unloadLoad)
					return;

				//MainForm.Current.Save();
				var e = new DInputEventArgs();
				CurrentEmulation = emType;
				Program.ReloadCount++;
				// Always load Microsoft XInput DLL by default.
				var dllInfo = EngineHelper.GetDefaultDll(emType == EmulationType.Virtual);
				if (dllInfo != null && dllInfo.Exists)
				{
					e.XInputVersionInfo = FileVersionInfo.GetVersionInfo(dllInfo.FullName);
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
						// var timeout = !result.AsyncWaitHandle.WaitOne(1000);
						var caption = string.Format("Failed to Reset() controller. '{0}'", dllInfo.FullName);
						e.Error = new Exception(caption);
					}
					// Slow: Reload whole x360ce.dll.
					else
					{
						Exception error;
						Controller.ReLoadLibrary(dllInfo.FullName, out error);
						if (!Controller.IsLoaded)
						{
							var caption = string.Format("Failed to load '{0}'", dllInfo.FullName);
							e.Error = new Exception(caption);
						}
					}
				}
				// If x360ce DLL loaded and settings changed then...
				var IsLibrary = game != null && game.IsLibrary;
				if (Controller.IsLoaded && IsLibrary && SettingsChanged)
				{
					// Reset configuration.
					Controller.Reset();
					SettingsChanged = false;
				}
				XInputReloaded?.Invoke(this, e);
			}
		}
	}
}
