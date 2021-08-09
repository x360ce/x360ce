using SharpDX.XInput;
using System;
using System.Diagnostics;
using x360ce.Engine;
using x360ce.Engine.Data;

namespace x360ce.App.DInput
{
	public partial class DInputHelper
	{

		public bool UpdateDevicesPending;

		public void CheckAndUnloadXInputLibrary(UserGame game, bool getXInputStates)
		{
			lock (Controller.XInputLock)
			{
				// UpdateDevicesEnabled property could be set to true from separate multiple threads.
				// UpdateDevicesPending property is needed to make sure that parameter stays same
				// during RefreshAll(...) action.
				if (UpdateDevicesEnabled)
				{
					UpdateDevicesEnabled = false;
					UpdateDevicesPending = true;
				}
				var e = new DInputEventArgs();
				// Always load Microsoft XInput DLL by default.
				var emType = game == null ? EmulationType.None : (EmulationType)game.EmulationType;
				// Unload if...
				var unload =
					// No emulation or
					emType == EmulationType.None ||
					// Emulation changed or
					emType != CurrentEmulation ||
					 // New device was detected so exclusive lock is necessary to retrieve force feedback information.
					 UpdateDevicesPending ||
					// No actual XInput states are required for Virtual emulation.
					(emType == EmulationType.Virtual && !getXInputStates) ||
					// No actual XInput states are required for Library emulation when minimized.
					// This will also release exclusive lock if another game/application must use it.
					(emType == EmulationType.Library && !Global._MainWindow.FormEventsEnabled);
				if (!unload)
					return;
				if (!Controller.IsLoaded)
					return;
				Controller.FreeLibrary();
				var ev = XInputReloaded;
				if (ev != null)
					ev(this, e);
			}
		}

		EmulationType CurrentEmulation;

		public void CheckAndLoadXInputLibrary(UserGame game, bool getXInputStates)
		{
			lock (Controller.XInputLock)
			{
				// Don't load if loaded.
				if (Controller.IsLoaded)
					return;
				// Don't load until device list was not refreshed.
				if (UpdateDevicesPending)
					return;
				var emType = (EmulationType)(game?.EmulationType ?? (int)EmulationType.None);
				// Don't load if not needed.
				if (emType == EmulationType.None)
					return;
				// If no actual XInput states are required for Virtual emulation.
				if (emType == EmulationType.Virtual && !getXInputStates)
					return;
				// No actual XInput states are required for Library emulation when minimized.
				// This will also release exclusive lock if another game/application must use it.
				if (emType == EmulationType.Library && !Global._MainWindow.FormEventsEnabled)
					return;
				//MainForm.Current.Save();
				var e = new DInputEventArgs();
				// Always load Microsoft XInput DLL by default.
				CurrentEmulation = emType;
				var useMicrosoft = emType == EmulationType.Virtual;
				Program.ReloadCount++;
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
				// If x360ce DLL loaded and settings changed then...
				var IsLibrary = game != null && game.IsLibrary;
				if (Controller.IsLoaded && IsLibrary && SettingsChanged)
				{
					// Reset configuration.
					Controller.Reset();
					SettingsChanged = false;
				}

				var ev = XInputReloaded;
				if (ev != null)
					ev(this, e);
			}
		}


	}
}
